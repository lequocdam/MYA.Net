public class OrderService(
    IOrderRepository          orderRepository,
    IOrderHistoryRepository   orderHistoryRepository,
    ITrackingRepository       trackingRepository,
    IAddressRepository        addressRepository,
    IServiceRepository        serviceRepository,
    IProvinceRepository       provinceRepository,
    IWarehouseService         warehouseService,
    IPricingService           pricingService,
    IWeightService            weightService,
    IOrderPermissionSpecification orderPermissionSpec,
    IOrderFilterSpecification     orderFilterSpec,
    IEventBus             eventBus,
    ILogger<OrderService> logger
) : IOrderService
{
    public async Task<OrderPageDto> GetAllAsync(
        OrderFilterDto filter,
        Guid           userId,
        string         role,
        Guid?          warehouseId,
        CancellationToken ct)
    {
        var query = orderRepository.Query();

        query = orderPermissionSpec.Apply(query, userId, role, warehouseId);
        query = orderFilterSpec.Apply(query, filter);

        var total = await query.CountAsync(ct);
        var skip  = (filter.Page - 1) * filter.PageSize;

        var orders = await query
            .OrderByDescending(x => x.Date)
            .Skip(skip)
            .Take(filter.PageSize)
            .Select(x => new OrderDto(
                x.Id,
                x.Code,
                x.Date,
                x.Total,
                x.Status))
            .ToListAsync(ct);

        return new OrderPageDto(
            filter.Page,
            filter.PageSize,
            total,
            orders);
    }

    // ─────────────────────────────────────────────
    // GET DETAIL
    // ─────────────────────────────────────────────
    public async Task<OrderDetailDto> GetDetailAsync(
        Guid orderId,
        Guid userId,
        string role,
        Guid? warehouseId,
        CancellationToken ct)
    {
        var order = await orderRepository.Query()
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == orderId, ct)
            ?? throw new NotFoundException("Order", orderId);

        var allowed = role switch
        {
            "Admin"  => true,
            "Staff"  => order.WarehouseId == warehouseId,
            _        => order.UserId == userId
        };

        if (!allowed)
            throw new ForbiddenException("Không có quyền xem đơn hàng này");

        return new OrderDetailDto(
            order.Id,
            order.Code,
            order.Date,
            new AddressDto(
                order.FromAddressSnapshot.Name,
                order.FromAddressSnapshot.Phone,
                order.FromAddressSnapshot.Province,
                order.FromAddressSnapshot.District,
                order.FromAddressSnapshot.Ward,
                order.FromAddressSnapshot.Street),
            new AddressDto(
                order.ToAddressSnapshot.Name,
                order.ToAddressSnapshot.Phone,
                order.ToAddressSnapshot.Province,
                order.ToAddressSnapshot.District,
                order.ToAddressSnapshot.Ward,
                order.ToAddressSnapshot.Street),
            order.Cost,
            order.Fee,
            order.CodAmount,
            order.Total,
            order.Status,
            order.Items.Select(i => new ItemDto(
                i.Image,
                i.Name,
                i.Quantity,
                i.Weight,
                i.Length,
                i.Width,
                i.Height)).ToList());
    }

    public async Task<OrderDto> CreateAsync(
        CreateOrderDto dto,
        Guid userId,
        CancellationToken ct)
    {
        var addresses = await addressRepository.Query()
            .Where(a => a.Id == dto.FromAddressId || a.Id == dto.ToAddressId)
            .Select(a => new Address(
                a.Id,
                a.WardId,
                a.CityId,
                a.Latitude,
                a.Longitude))
            .ToListAsync(ct);

        var fromAddress = addresses.FirstOrDefaultAsync(a => a.Id == dto.FromAddressId)
            ?? throw new NotFoundException("From address not found");

        var toAddress = addresses.FirstOrDefaultAsync(a => a.Id == dto.ToAddressId)
            ?? throw new NotFoundException("To address not found");

        var fromAddressSnapshot = 

        var warehouse = await warehouseService.GetByAddressAsync(fromAddress, ct);
        var zone = await zoneService.GetAsync(fromAddress, toAddress);
        var weight = weightEngine.Calculate(dto.Items);
        var price = await pricingService.GetAsync(
            dto.ServiceId, 
            zoneId,
            weight, 
            dto.Cod, ct);

        var items = 

        return await CreateCoreAsync(
            fromAddress,
            toAddress,
            dto.ServiceId,
            warehouseId,
            price,
            userId,
            dto.Items)
    }

    // ─────────────────────────────────────────────
    // CREATE LIST
    // ─────────────────────────────────────────────
    public async Task<OrderListDto> CreateListAsync(
        IFormFile file,
        Guid userId,
        CancellationToken ct)
    {
        using var stream   = file.OpenReadStream();
        using var workbook = new XLWorkbook(stream);

        var sheet = workbook.Worksheet("Orders")
            ?? throw new BadRequestException("Orders sheet not found");

        var rows = sheet
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

        if (!rows.Any())
            throw new BadRequestException("File không có dữ liệu");

        // ── Gom lookup ra ngoài loop ──────────────
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

        foreach (var group in groups)
        {
            var first = group.First();
            try
            {
                if (!services.TryGetValue(first.ServiceName, out var service))
                    throw new BadRequestException($"Dịch vụ '{first.ServiceName}' không tồn tại");

                if (!provinces.TryGetValue(first.FromProvince, out var fromProvince))
                    throw new BadRequestException($"Tỉnh '{first.FromProvince}' không tồn tại");

                if (!provinces.TryGetValue(first.ToProvince, out var toProvince))
                    throw new BadRequestException($"Tỉnh '{first.ToProvince}' không tồn tại");

                var fromAddress = new AddressRaw(
                    first.FromName, first.FromPhone,
                    fromProvince.Code, first.FromDistrict,
                    first.FromWard, first.FromStreet);

                var toAddress = new AddressRaw(
                    first.ToName, first.ToPhone,
                    toProvince.Code, first.ToDistrict,
                    first.ToWard, first.ToStreet);

                var warehouse = await warehouseService.GetByAddressAsync(fromAddress, ct);
                var items     = group.Select(i => new Item
                {
                    Id       = Guid.NewGuid(),
                    Image    = i.ItemImage,
                    Name     = i.ItemName,
                    Quantity = i.ItemQuantity,
                    Weight   = i.ItemWeight,
                    Length   = i.ItemLength,
                    Width    = i.ItemWidth,
                    Height   = i.ItemHeight,
                }).ToList();

                var weight = weightService.Calculate(items);
                var price  = await pricingService.CalculateAsync(
                    service.Id, fromAddress, toAddress, weight, null, ct);
                var code   = await GenerateUniqueCodeAsync(ct);

                var order = await CreateOrderCoreAsync(
                    code,
                    userId,
                    service.Id,
                    warehouse.Id,
                    AddressSnapshot.From(fromAddress),
                    AddressSnapshot.From(toAddress),
                    price,
                    items,
                    ct);

                results.Add(order);
            }
            catch (Exception ex)
            {
                errors.Add(new BatchErrorDto(
                    group.Select(r => r.RowNumber).ToList(),
                    ex.Message));

                logger.LogWarning(
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
    public async Task UpdateAsync(
        UpdateOrderDto dto,
        Guid orderId,
        Guid userId,
        CancellationToken ct)
    {
        var addresses = await addressRepository.Query()
            .Where(a => a.Id == dto.FromAddressId || a.Id == dto.ToAddressId)
            .ToListAsync(ct);

        var fromAddress = addresses.FirstOrDefault(a => a.Id == dto.FromAddressId)
            ?? throw new NotFoundException("From address", dto.FromAddressId);

        var toAddress = addresses.FirstOrDefault(a => a.Id == dto.ToAddressId)
            ?? throw new NotFoundException("To address", dto.ToAddressId);

        var weight = weightService.Calculate(dto.Items);
        var price  = await pricingService.CalculateAsync(
            dto.ServiceId, fromAddress, toAddress, weight, dto.Cod, ct);

        await using var transaction = await orderRepository.BeginTransactionAsync(ct);
        try
        {
            var order = await orderRepository.Query()
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == orderId, ct)
                ?? throw new NotFoundException("Order", orderId);

            if (order.UserId != userId)
                throw new ForbiddenException("Không có quyền cập nhật đơn hàng này");

            if (order.Status != OrderStatus.Pending)
                throw new InvalidOrderTransitionException(
                    "Chỉ cập nhật được đơn ở trạng thái Pending", order.Status);

            order.Update(
                AddressSnapshot.From(fromAddress),
                AddressSnapshot.From(toAddress),
                dto.ServiceId,
                price.Cost,
                price.Fee,
                price.Total);

            order.UpdateItems(dto.Items.Select(i => new ItemUpdate(
                i.Id, i.Name, i.Quantity,
                i.Weight, i.Length, i.Width, i.Height)).ToList());

            await orderHistoryRepository.AddAsync(new OrderHistory
            {
                Id      = Guid.NewGuid(),
                OrderId = orderId,
                UserId  = userId,
                Note    = "Đã cập nhật đơn hàng",
                Date    = DateTime.UtcNow,
            }, ct);

            await trackingRepository.AddAsync(new Tracking
            {
                Id      = Guid.NewGuid(),
                OrderId = orderId,
                Message = "Đã cập nhật đơn hàng",
                Date    = DateTime.UtcNow,
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

        await eventBus.Publish(new OrderUpdatedEvent(orderId, userId));
    }

    // ─────────────────────────────────────────────
    // ESTIMATE
    // ─────────────────────────────────────────────
    public async Task<EstimateResultDto> EstimateAsync(
        EstimateDto dto,
        CancellationToken ct)
    {
        var addresses = await addressRepository.Query()
            .Where(a => a.Id == dto.FromAddressId || a.Id == dto.ToAddressId)
            .ToListAsync(ct);

        var fromAddress = addresses.FirstOrDefault(a => a.Id == dto.FromAddressId)
            ?? throw new NotFoundException("From address", dto.FromAddressId);

        var toAddress = addresses.FirstOrDefault(a => a.Id == dto.ToAddressId)
            ?? throw new NotFoundException("To address", dto.ToAddressId);

        var weight = weightService.Calculate(dto.Items);
        var price  = await pricingService.CalculateAsync(
            dto.ServiceId, fromAddress, toAddress, weight, dto.Cod, ct);

        var days = price.Zone switch
        {
            Zone.Local       => 1,
            Zone.SameRegion  => 2,
            Zone.CrossRegion => 4,
            Zone.Remote      => 7,
            _                => 5
        };

        return new EstimateResultDto(
            price.Zone,
            weight,
            price.Cost,
            price.Fee,
            price.CodFee,
            price.Total,
            days);
    }

    public async Task<BulkResultDto> TransitionAsync(
        BulkTransitionDto dto,
        Guid userId,
        CancellationToken ct)
    {
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
    // CANCEL (User)
    // ─────────────────────────────────────────────
    public async Task<BulkResultDto> CancelAsync(
        CancelDto dto,
        Guid userId,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.Reason))
            throw new BadRequestException("Lý do hủy không được để trống");

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

        var result = await TransitionCoreAsync(
            authorizedIds,
            OrderTrigger.Cancel,
            userId,
            $"Hủy đơn. Lý do: {dto.Reason}",
            ct);

        var allFailed = result.Failed
            .Concat(unauthorizedIds.Select(id =>
                new BulkErrorDto(id, "Không có quyền hủy đơn hàng này")))
            .ToList();

        return new BulkResultDto(result.Succeeded, allFailed);
    }

    private async Task<OrderDto> CreateCoreAsync(
        Address fromAddress,
        Address toAddress,
        Guid serviceId,
        Guid warehouseId,
        Price price,
        Guid userId,
        List<Item> items,
        CancellationToken ct)
    {
        await using var transaction = await orderRepository.BeginTransactionAsync(ct);
        try
        {
            var order = new AddressSnapshot
            {
                Id = Guid.NewGuid(),
                Code = GenerateCode(),
                serviceId,
                warehouseId,
                price.Cost,
                price.Fee,
                price.Total,
                userId,
                 Items = items
                .Select(i => new Item(
                    i.Image,
                    i.Name,
                    i.Quantity,
                    i.Weight,
                    i.Length,
                    i.Width,
                    i.Height))
                .ToList()
            }

            await orderRepository.AddAsync(order, ct);

            await addressSnapshotRepository.AddAsync(new AddressSnapshot
            {
                Id = Guid.NewGuid(),
                Name = fromAddress.Name,
                Phone = fromAddress.Phone,
                Street = fromAddress.Street,
                Ward = fromAddress.Ward,
                City = fromAddress.City,
                OrderId = order.Id,
            }, ct);

            await addressSnapshotRepository.AddAsync(new AddressSnapshot
            {
                Id = Guid.NewGuid(),
                Name = toAddress.Name,
                Phone = toAddress.Phone,
                Street = toAddress.Street,
                Ward = toAddress.Ward,
                City = toAddress.City,
                OrderId = order.Id,
            }, ct);

            await historyRepository.AddAsync(new History
            {
                Id = Guid.NewGuid(),
                Date = DateTime.UtcNow,
                OrderId = order.Id,
                Status = order.Status,
                Note = "",
                UserId = userId,
            }, ct);

            await trackingRepository.AddAsync(new Tracking
            {
                Id = Guid.NewGuid(),
                Date = DateTime.UtcNow,
                OrderId = order.Id,
                Status = order.Status,
                Message = GetTrackingMessage(OrderStatus.Pending),
            }, ct);

            await orderRepository.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            await eventBus.Publish(new OrderCreatedEvent(order.Id));

            return new OrderDto(
                order.Id,
                order.Code,
                order.Date,
                order.Total,
                order.Status);
        }
        catch (Exception e)
        {
            await transaction.RollbackAsync(ct);
            logger.LogError(e,
                "CreateOrderCore failed. UserId={UserId}", userId);
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

            if (!orders.Any())
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
                        $"Không thể chuyển '{order.Status}' với trigger '{trigger}'"));
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

            await orderHistoryRepository.AddRangeAsync(histories, ct);
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

        foreach (var orderId in succeeded)
        {
            try
            {
                await eventBus.Publish(new OrderStatusChangedEvent(orderId));
            }
            catch (Exception e)
            {
                logger.LogWarning(e,
                    "Publish event failed. OrderId={OrderId}", orderId);
            }
        }

        return new BulkResultDto(succeeded, failed);
    }

    private async Task<string> GenerateUniqueCodeAsync(CancellationToken ct)
    {
        for (int i = 0; i < 5; i++)
        {
            var code   = GenerateCode();
            var exists = await orderRepository.Query()
                .AnyAsync(o => o.Code == code, ct);
            if (!exists) return code;
        }
        throw new InvalidOperationException(
            "Không thể tạo mã đơn hàng duy nhất sau 5 lần thử");
    }

    private static string GenerateCode() =>
        $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..4].ToUpper()}";

    private static string GetTrackingMessage(OrderStatus status) => status switch
    {
        OrderStatus.Pending         => "Đơn hàng đang chờ xác nhận",
        OrderStatus.Confirmed       => "Đơn hàng đã được xác nhận",
        OrderStatus.PickingUp       => "Đang lấy hàng từ người gửi",
        OrderStatus.PickedUp        => "Đã lấy hàng thành công",
        OrderStatus.Transiting      => "Hàng đang trên đường trung chuyển",
        OrderStatus.Arrived         => "Hàng đã về kho đích",
        OrderStatus.Delivering      => "Đơn hàng đang trên đường giao đến bạn",
        OrderStatus.Completed       => "Giao hàng thành công",
        OrderStatus.Failed          => "Giao hàng thất bại",
        OrderStatus.ReturnRequested => "Đang xử lý yêu cầu hoàn hàng",
        OrderStatus.Returning       => "Hàng đang được hoàn về người gửi",
        OrderStatus.Returned        => "Hoàn hàng thành công",
        OrderStatus.Cancelled       => "Đơn hàng đã bị hủy",
        _                           => status.ToString()
    };
}