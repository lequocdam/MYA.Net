using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace YourApp.Services;

/// <summary>
/// Service xử lý nghiệp vụ Order: tạo, cập nhật, hủy, chuyển trạng thái, tra cứu.
/// Lưu ý: các interface/DTO dưới đây giả định theo những gì xuất hiện trong code gốc.
/// Cần đối chiếu lại với namespace/contract thực tế của bạn trước khi build.
/// </summary>
public sealed class OrderService(
    IOrderRepository             orderRepository,
    IOrderHistoryRepository      orderHistoryRepository,
    ITrackingRepository          trackingRepository,
    IAddressRepository           addressRepository,
    IAddressSnapshotRepository   addressSnapshotRepository,
    IServiceRepository           serviceRepository,
    IProvinceRepository          provinceRepository,
    IWarehouseService            warehouseService,
    IPricingService              pricingService,
    IWeightService                weightService,
    IQuoteService                 quoteService,
    IMapper                       mapper,
    IOrderPermissionSpecification orderPermissionSpec,
    IFilterOrderSpec              filterOrderSpec,
    IEventBus                     eventBus,
    ILogger<OrderService>         logger
) : IOrderService
{
    private const int MaxCodeGenerationAttempts = 5;

    // ─────────────────────────────────────────────
    // QUERY
    // ─────────────────────────────────────────────

    public async Task<OrderRes> GetOrdersAsync(
        OrderFilterReq req,
        CurrentUser currentUser,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(req);
        ArgumentNullException.ThrowIfNull(currentUser);

        var spec = new OrderFilterSpecification(req, currentUser);

        var count = await orderRepository.CountAsync(spec, ct);
        if (count == 0)
        {
            return new OrderRes(req.Page, req.PageSize, 0, new List<OrderDto>());
        }

        var orders = await orderRepository.ToListAsync(spec, ct);
        var result = mapper.Map<List<OrderDto>>(orders);

        return new OrderPageDto(req.Page, req.PageSize, total, result);
    }

    public async Task<OrderDetailDto> GetByIdAsync(
        Guid orderId,
        CurrentUser currentUser,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(currentUser);

        var spec = new OrderDetailSpecification(orderId);

        var order = await orderRepository.FirstOrDefaultAsync(spec, ct)
            ?? throw new NotFoundException("Order", orderId);

        orderPermissionSpec.Validate(order, currentUser);

        return mapper.Map<OrderDetailDto>(order);
    }

    // ─────────────────────────────────────────────
    // CREATE (single)
    // ─────────────────────────────────────────────

    public async Task<Guid> CreateAsync(
        CurrentUser currentUser,
        CreateReq req,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(currentUser);
        ValidateCreateRequest(req);

        var fromAddress = await addressRepository.FindByIdAsync(req.FromAddressId, ct)
            ?? throw new NotFoundException("From address", req.FromAddressId);

        var toAddress = await addressRepository.FindByIdAsync(req.ToAddressId, ct)
            ?? throw new NotFoundException("To address", req.ToAddressId);

        var items = BuildItems(req.Items);

        var warehouse = await warehouseService.GetByAddressAsync(fromAddress, ct)
            ?? throw new BadRequestException("Không tìm thấy kho phù hợp với địa chỉ gửi hàng");

        var quote = await quoteService.GetAsync(
            req.ServiceId, fromAddress, toAddress, req.Cod, items, ct);

        var createContext = new CreateContext(
            currentUser.UserId,
            warehouse.Id,
            req.ServiceId,
            fromAddress,
            toAddress,
            quote,
            items);

        return await CreateCoreAsync(createContext, ct);
    }

    // ─────────────────────────────────────────────
    // CREATE (batch từ Excel)
    // ─────────────────────────────────────────────

    public async Task<OrderListDto> CreateListAsync(
        IFormFile         file,
        Guid              userId,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(file);

        if (file.Length == 0)
            throw new BadRequestException("File rỗng");

        const long maxFileSizeBytes = 10 * 1024 * 1024; // 10MB
        if (file.Length > maxFileSizeBytes)
            throw new BadRequestException("File vượt quá dung lượng cho phép (10MB)");

        List<ExcelRowDto> rows;
        await using (var stream = file.OpenReadStream())
        using (var workbook = new XLWorkbook(stream))
        {
            var sheet = workbook.Worksheet("Orders")
                ?? throw new BadRequestException("Sheet 'Orders' không tồn tại trong file");

            rows = sheet
                .RowsUsed()
                .Skip(1)
                .Select(row => new ExcelRowDto(
                    FromName     : row.Cell(1).GetString().Trim(),
                    FromPhone    : row.Cell(2).GetString().Trim(),
                    FromProvince : row.Cell(3).GetString().Trim(),
                    FromDistrict : row.Cell(4).GetString().Trim(),
                    FromWard     : row.Cell(5).GetString().Trim(),
                    FromStreet   : row.Cell(6).GetString().Trim(),
                    ToName       : row.Cell(7).GetString().Trim(),
                    ToPhone      : row.Cell(8).GetString().Trim(),
                    ToProvince   : row.Cell(9).GetString().Trim(),
                    ToDistrict   : row.Cell(10).GetString().Trim(),
                    ToWard       : row.Cell(11).GetString().Trim(),
                    ToStreet     : row.Cell(12).GetString().Trim(),
                    ServiceName  : row.Cell(13).GetString().Trim(),
                    ItemImage    : row.Cell(14).GetString().Trim(),
                    ItemName     : row.Cell(15).GetString().Trim(),
                    ItemWeight   : row.Cell(16).GetValue<double>(),
                    ItemQuantity : row.Cell(17).GetValue<int>(),
                    ItemLength   : row.Cell(18).GetValue<double>(),
                    ItemWidth    : row.Cell(19).GetValue<double>(),
                    ItemHeight   : row.Cell(20).GetValue<double>(),
                    RowNumber    : row.RowNumber()))
                .ToList();
        }

        if (rows.Count == 0)
            throw new BadRequestException("File không có dữ liệu");

        const int maxRows = 1000;
        if (rows.Count > maxRows)
            throw new BadRequestException($"File vượt quá giới hạn {maxRows} dòng");

        // Batch lookup để tránh N+1 query
        var serviceNames = rows.Select(r => r.ServiceName).Distinct().ToList();
        var services = await serviceRepository.Query()
            .Where(s => serviceNames.Contains(s.Name))
            .ToDictionaryAsync(s => s.Name, ct);

        var provinceNames = rows
            .SelectMany(r => new[] { r.FromProvince, r.ToProvince })
            .Distinct()
            .ToList();
        var provinces = await provinceRepository.Query()
            .Where(p => provinceNames.Contains(p.Name))
            .ToDictionaryAsync(p => p.Name, ct);

        var groups = rows
            .GroupBy(r => new { r.FromPhone, r.ToPhone, r.ServiceName })
            .ToList();

        var results = new List<OrderDto>();
        var errors  = new List<BatchErrorDto>();

        // Lưu ý: mỗi group là 1 transaction riêng (qua CreateCoreAsync) => batch này
        // KHÔNG atomic toàn bộ. Đây là chủ đích: 1 dòng lỗi không làm hỏng cả file,
        // người dùng sửa lại dòng lỗi rồi import lại riêng dòng đó.
        foreach (var group in groups)
        {
            var first = group.First();
            try
            {
                if (!services.TryGetValue(first.ServiceName, out var service))
                    throw new BadRequestException($"Dịch vụ '{first.ServiceName}' không tồn tại");

                if (!provinces.TryGetValue(first.FromProvince, out var fromProvince))
                    throw new BadRequestException($"Tỉnh gửi '{first.FromProvince}' không tồn tại");

                if (!provinces.TryGetValue(first.ToProvince, out var toProvince))
                    throw new BadRequestException($"Tỉnh nhận '{first.ToProvince}' không tồn tại");

                var fromAddress = new AddressRaw(
                    first.FromName, first.FromPhone,
                    fromProvince.Code, first.FromDistrict,
                    first.FromWard, first.FromStreet);

                var toAddress = new AddressRaw(
                    first.ToName, first.ToPhone,
                    toProvince.Code, first.ToDistrict,
                    first.ToWard, first.ToStreet);

                var warehouse = await warehouseService.GetByAddressAsync(fromAddress, ct)
                    ?? throw new BadRequestException("Không tìm thấy kho phù hợp với địa chỉ gửi hàng");

                var items = group.Select(i => Item.Create(
                    i.ItemName, i.ItemQuantity, i.ItemWeight,
                    i.ItemLength, i.ItemWidth, i.ItemHeight)).ToList();

                var weight = weightService.Calculate(items);
                var price  = await pricingService.CalculateAsync(
                    service.Id, fromAddress, toAddress, weight, null, ct);

                var quote = new Quote(weight, price.Cost, price.Fee, price.Total);

                var createContext = new CreateContext(
                    userId, warehouse.Id, service.Id, fromAddress, toAddress, quote, items);

                var orderId = await CreateCoreAsync(createContext, ct);

                var created = await orderRepository.FirstOrDefaultAsync(
                    new OrderDetailSpecification(orderId), ct);

                if (created is not null)
                    results.Add(mapper.Map<OrderDto>(created));
            }
            catch (Exception ex)
            {
                errors.Add(new BatchErrorDto(
                    group.Select(r => r.RowNumber).ToList(),
                    ex.Message));

                logger.LogWarning(ex,
                    "CreateListAsync row error. Rows={Rows} Error={Error}",
                    string.Join(",", group.Select(r => r.RowNumber)),
                    ex.Message);
            }
        }

        return new OrderListDto(results, errors);
    }

    // ─────────────────────────────────────────────
    // UPDATE
    // ─────────────────────────────────────────────

    public async Task<OrderDto> UpdateAsync(
        Guid orderId,
        UpdateOrderDto dto,
        Guid userId,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(dto);

        if (dto.Items is null || dto.Items.Count == 0)
            throw new BadRequestException("Đơn hàng phải có ít nhất 1 sản phẩm");

        Order order;

        await using var transaction = await orderRepository.BeginTransactionAsync(ct);
        try
        {
            order = await orderRepository.Query()
                .FirstOrDefaultAsync(o => o.Id == orderId, ct)
                ?? throw new NotFoundException("Order", orderId);

            if (order.UserId != userId)
                throw new ForbiddenException("Bạn không có quyền cập nhật đơn hàng này");

            if (order.Status != OrderStatus.Pending)
                throw new InvalidOrderTransitionException(
                    "Chỉ có thể cập nhật đơn hàng đang ở trạng thái chờ xử lý", order.Status);

            var addresses = await addressRepository.Query()
                .Where(a => a.Id == dto.FromAddressId || a.Id == dto.ToAddressId)
                .ToListAsync(ct);

            var fromAddress = addresses.FirstOrDefault(a => a.Id == dto.FromAddressId)
                ?? throw new NotFoundException("From address", dto.FromAddressId);

            var toAddress = addresses.FirstOrDefault(a => a.Id == dto.ToAddressId)
                ?? throw new NotFoundException("To address", dto.ToAddressId);

            var weight = weightService.Calculate(dto.Items);
            var price = await pricingService.CalculateAsync(
                dto.ServiceId, fromAddress, toAddress, weight, dto.Cod, ct);

            order.Update(
                AddressSnapshot.From(fromAddress),
                AddressSnapshot.From(toAddress),
                dto.ServiceId,
                price.Cost,
                price.Fee,
                price.Total);

            order.UpdateItems(dto.Items.Select(i => new ItemUpdate(
                i.Id, i.Name, i.Quantity, i.Weight, i.Length, i.Width, i.Height)).ToList());

            var now = DateTime.UtcNow;

            await orderHistoryRepository.AddAsync(new OrderHistory
            {
                Id      = Guid.NewGuid(),
                OrderId = orderId,
                UserId  = userId,
                Note    = "Đã cập nhật đơn hàng",
                Date    = now,
            }, ct);

            await trackingRepository.AddAsync(new Tracking
            {
                Id      = Guid.NewGuid(),
                OrderId = orderId,
                Message = "Đã cập nhật đơn hàng",
                Date    = now,
            }, ct);

            await orderRepository.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }
        catch (Exception e)
        {
            await transaction.RollbackAsync(ct);
            logger.LogError(e,
                "Update order failed. OrderId={OrderId} UserId={UserId}",
                orderId, userId);
            throw;
        }

        // Publish ngoài transaction — tránh phát event khi rollback.
        // Lỗi publish không nên làm fail request (đã update DB thành công).
        await TryPublishAsync(new OrderUpdatedEvent(orderId, userId), ct);

        return mapper.Map<OrderDto>(order);
    }

    // ─────────────────────────────────────────────
    // ESTIMATE
    // ─────────────────────────────────────────────

    public async Task<EstimateRes> EstimateAsync(
        EstimateReq req,
        CancellationToken ct)
    {
        ValidateCreateRequest(req);

        var fromAddress = await addressRepository.FindByIdAsync(req.FromAddressId, ct)
            ?? throw new NotFoundException("From address", req.FromAddressId);

        var toAddress = await addressRepository.FindByIdAsync(req.ToAddressId, ct)
            ?? throw new NotFoundException("To address", req.ToAddressId);

        var items = BuildItems(req.Items);

        var quote = await quoteService.GetAsync(
            req.ServiceId, fromAddress, toAddress, req.Cod, items, ct);

        return new EstimateRes(
            req.ServiceId,
            quote.ZoneId,
            quote.Weight,
            quote.Cost,
            quote.Fee,
            quote.Cod,
            quote.Total);
    }

    // ─────────────────────────────────────────────
    // TRANSITION (staff)
    // ─────────────────────────────────────────────

    public async Task<BulkResultDto> TransitionAsync(
        BulkTransitionDto dto,
        Guid userId,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(dto);

        if (dto.OrderIds is null || dto.OrderIds.Count == 0)
            throw new BadRequestException("Danh sách đơn hàng không được để trống");

        if (!OrderTrigger.AllowedForStaff.Contains(dto.Trigger))
            throw new BadRequestException($"Trigger '{dto.Trigger}' không hợp lệ");

        return await TransitionCoreAsync(
            dto.OrderIds,
            dto.Trigger,
            userId,
            dto.Trigger,
            ct);
    }

    // ─────────────────────────────────────────────
    // CANCEL (user)
    // ─────────────────────────────────────────────

    public async Task<BulkResultDto> CancelAsync(
        CancelDto dto,
        Guid userId,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(dto);

        if (string.IsNullOrWhiteSpace(dto.Reason))
            throw new BadRequestException("Lý do hủy không được để trống");

        if (dto.OrderIds is null || dto.OrderIds.Count == 0)
            throw new BadRequestException("Danh sách đơn hàng không được để trống");

        var orders = await orderRepository.Query()
            .Where(o => dto.OrderIds.Contains(o.Id))
            .Select(o => new { o.Id, o.UserId })
            .ToListAsync(ct);

        var unauthorizedIds = orders
            .Where(o => o.UserId != userId)
            .Select(o => o.Id)
            .ToList();

        var authorizedIds = orders
            .Where(o => o.UserId == userId)
            .Select(o => o.Id)
            .ToList();

        // ID không tồn tại trong DB (không thuộc orders) cũng cần báo lỗi, không được im lặng bỏ qua
        var notFoundIds = dto.OrderIds
            .Except(orders.Select(o => o.Id))
            .ToList();

        var result = authorizedIds.Count > 0
            ? await TransitionCoreAsync(
                authorizedIds,
                OrderTrigger.Cancel,
                userId,
                $"Hủy đơn. Lý do: {dto.Reason}",
                ct)
            : new BulkResultDto(new List<Guid>(), new List<BulkErrorDto>());

        var allFailed = result.Failed
            .Concat(unauthorizedIds.Select(id =>
                new BulkErrorDto(id, "Không có quyền hủy đơn hàng này")))
            .Concat(notFoundIds.Select(id =>
                new BulkErrorDto(id, "Đơn hàng không tồn tại")))
            .ToList();

        return new BulkResultDto(result.Succeeded, allFailed);
    }

    // ─────────────────────────────────────────────
    // PRIVATE HELPERS
    // ─────────────────────────────────────────────

    private static void ValidateCreateRequest(CreateReq req)
    {
        ArgumentNullException.ThrowIfNull(req);

        if (req.Items is null || req.Items.Count == 0)
            throw new BadRequestException("Đơn hàng phải có ít nhất 1 sản phẩm");

        if (req.FromAddressId == req.ToAddressId)
            throw new BadRequestException("Địa chỉ gửi và nhận không được trùng nhau");
    }

    private static void ValidateCreateRequest(EstimateReq req)
    {
        ArgumentNullException.ThrowIfNull(req);

        if (req.Items is null || req.Items.Count == 0)
            throw new BadRequestException("Đơn hàng phải có ít nhất 1 sản phẩm");
    }

    private static List<Item> BuildItems(IEnumerable<CreateItemReq> items) =>
        items.Select(x => Item.Create(
                x.Name, x.Quantity, x.Weight, x.Length, x.Width, x.Height))
            .ToList();

    private static AddressDto MapAddress(AddressSnapshot s) => new(
        s.Name, s.Phone, s.Province, s.District, s.Ward, s.Street);

    private async Task<Guid> CreateCoreAsync(
        CreateContext createContext,
        CancellationToken ct)
    {
        await using var transaction = await orderRepository.BeginTransactionAsync(ct);
        try
        {
            var order = Order.Create(
                createContext.UserId,
                createContext.WarehouseId,
                createContext.ServiceId,
                createContext.Quote,
                createContext.Items);

            await orderRepository.AddAsync(order, ct);

            var fromAddressSnap = AddressSnapshot.Create(order.Id, createContext.FromAddress);
            var toAddressSnap = AddressSnapshot.Create(order.Id, createContext.ToAddress);

            await addressSnapshotRepository.AddAsync(fromAddressSnap, ct);
            await addressSnapshotRepository.AddAsync(toAddressSnap, ct);

            var orderHistory = OrderHistory.Create(order.Id, order.UserId, order.Status);
            await orderHistoryRepository.AddAsync(orderHistory, ct);

            var tracking = Tracking.Create(order.Id, order.Status);
            await trackingRepository.AddAsync(tracking, ct);

            await orderRepository.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            await TryPublishAsync(new OrderCreatedEvent(order.Id), ct);

            return order.Id;
        }
        catch (Exception e)
        {
            await transaction.RollbackAsync(ct);
            logger.LogError(e,
                "CreateCore failed. UserId={UserId}", createContext.UserId);
            throw;
        }
    }

    private async Task<BulkResultDto> TransitionCoreAsync(
        List<Guid> orderIds,
        string     trigger,
        Guid       userId,
        string     note,
        CancellationToken ct)
    {
        var succeeded = new List<Guid>();
        var failed    = new List<BulkErrorDto>();

        await using var transaction = await orderRepository.BeginTransactionAsync(ct);
        try
        {
            var orders = await orderRepository.Query()
                .Where(o => orderIds.Contains(o.Id))
                .ToListAsync(ct);

            if (orders.Count == 0)
                throw new NotFoundException("Orders not found");

            var now       = DateTime.UtcNow;
            var histories = new List<OrderHistory>();
            var trackings = new List<Tracking>();

            foreach (var order in orders)
            {
                var workflow = new OrderWorkflow(order.Status);

                if (!workflow.Can(trigger))
                {
                    failed.Add(new BulkErrorDto(
                        order.Id,
                        $"Không thể chuyển trạng thái '{order.Status}' bằng trigger '{trigger}'"));
                    continue;
                }

                var newStatus = workflow.Fire(trigger);
                order.Status  = newStatus;

                histories.Add(new OrderHistory
                {
                    Id      = Guid.NewGuid(),
                    OrderId = order.Id,
                    UserId  = userId,
                    Status  = newStatus,
                    Note    = note,
                    Date    = now,
                });

                trackings.Add(new Tracking
                {
                    Id      = Guid.NewGuid(),
                    OrderId = order.Id,
                    Status  = newStatus,
                    Message = GetTrackingMessage(newStatus),
                    Date    = now,
                });

                succeeded.Add(order.Id);
            }

            if (histories.Count > 0)
                await orderHistoryRepository.AddRangeAsync(histories, ct);

            if (trackings.Count > 0)
                await trackingRepository.AddRangeAsync(trackings, ct);

            await orderRepository.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }
        catch (Exception e)
        {
            await transaction.RollbackAsync(ct);
            logger.LogError(e,
                "TransitionCore failed. Trigger={Trigger}", trigger);
            throw;
        }

        // Publish ngoài transaction, lỗi publish không rollback DB và không throw ra ngoài
        foreach (var orderId in succeeded)
        {
            await TryPublishAsync(new OrderStatusChangedEvent(orderId), ct);
        }

        return new BulkResultDto(succeeded, failed);
    }

    /// <summary>
    /// Publish event với cơ chế bảo vệ: lỗi publish chỉ log, không làm fail nghiệp vụ
    /// đã commit thành công. Production thực sự nên thay bằng outbox pattern
    /// (lưu event vào bảng riêng trong cùng transaction, có background worker
    /// đọc và publish + retry) để tránh mất event khi service crash giữa lúc publish.
    /// </summary>
    private async Task TryPublishAsync(object @event, CancellationToken ct)
    {
        try
        {
            await eventBus.Publish(@event);
        }
        catch (Exception e)
        {
            logger.LogWarning(e,
                "Publish event failed. EventType={EventType}", @event.GetType().Name);
        }
    }

    private async Task<string> GenerateUniqueCodeAsync(CancellationToken ct)
    {
        for (var i = 0; i < MaxCodeGenerationAttempts; i++)
        {
            var code   = GenerateCode();
            var exists = await orderRepository.Query()
                .AnyAsync(o => o.Code == code, ct);
            if (!exists) return code;
        }

        throw new InvalidOperationException(
            $"Không thể tạo mã đơn hàng duy nhất sau {MaxCodeGenerationAttempts} lần thử");
    }

    private static string GenerateCode() =>
        $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..4].ToUpperInvariant()}";

    private static string GetTrackingMessage(OrderStatus status) => status switch
    {
        OrderStatus.Pending    => "Đơn hàng đang chờ xử lý",
        OrderStatus.Confirmed  => "Đơn hàng đã được xác nhận",
        OrderStatus.PickedUp   => "Đơn hàng đã được lấy",
        OrderStatus.InTransit  => "Đơn hàng đang vận chuyển",
        OrderStatus.Delivered  => "Đơn hàng đã được giao",
        OrderStatus.Cancelled  => "Đơn hàng đã bị hủy",
        _                      => $"Trạng thái đơn hàng: {status}",
    };
}