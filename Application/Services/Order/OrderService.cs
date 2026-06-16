using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class OrderService(
    IOrderRepository orderRepository,
    IOrderPermissionSpecification orderPermissionSpecification,
    IOrderFilterSpecification orderFilterSpecification,
    // SERVICES
    IZoneService    zoneService,
    IPricingService pricingService,
    // ENGINES
    IWeightService weightService,
    IEventBus eventBus,
    ILogger<OrderService> logger,
    IMapper mapper) : IOrderService
{
    public async Task<OrderPage<OrderDto>> GetAllAsync(
        OrderFilterDto filter,
        Guid userId,
        string role,
        Guid? warehouseId,
        CancellationToken ct)
    {
        var query = orderRepository.Query();

        query = permissionSpec.Apply(
            query,
            userId,
            role,
            warehouseId);

        query = filterSpec.Apply(
            query,
            filter);

        var total = await query.CountAsync(ct);

        var skip = (filter.Page - 1) * filter.PageSize;

        var orders = await query
            .OrderByDescending(x => x.Date)
            .Skip(skip)
            .Take(filter.PageSize)
            .Select(x => new OrderDto
            {
                Id = x.Id,
                Code = x.Code,
                Date = x.Date,
                Total = x.Total,
                Status = x.Status
            })
            .ToListAsync(ct);

        return new OrderPage<OrderDto>
        {
            Page = filter.Page,
            PageSize = filter.PageSize,
            Total = total,
            Items = orders
        };
    }

    public async Task<OrderDetailDto> GetDetailAsync(
        Guid orderId, 
        Guid userId,
        CancellationToken ct)
    {
        return order = await orderRepository.Query()
        .AsNoTracking()
        .Select(o => new OrderDetailDto
        {
            Id = o.Id,
            Code = o.Code,
            Date = o.Date,
            FromAddress = new AddressDto
            {
                Name = o.FromAddress.Name,
                Phone = o.FromAddress.Phone,
                Email = o.FromAddress.Email,
                Address = o.FromAddress.Address,
            },
            ToAddress = new AddressDto
            {
                Name = o.ToAddress.Name,
                Phone = o.ToAddress.Phone,
                Email = o.ToAddress.Email,
                Address = o.ToAddress.Address,
            },
            Service = new ServiceDto
            {
                Name = o.Service.Name,
            },
            Cost = o.Cost,
            Fee = o.Fee,
            Total = o.Total,
            Status = o.Status,
            Items = o.Items
                .Select(i => new ItemDto
                {
                    Image = i.Image,
                    Name = i.Name,
                    Quantity = i.Quantity,
                    Weight = i.Weight,
                    Length = i.Length,
                    Width = i.Width,
                    Height = i.Height,
                })
                .ToList(ct)
        })
        .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId, ct)
            ?? throw new NotFoundException("Order", orderId);
    }

    public async Task<OrderDto> CreateAsync(
        CreateOrderDto dto,
        Guid userId,
        CancellationToken ct)
    {
        var fromTask = addressService.GetByIdAsync(dto.FromAddressId);
        var toTask   = addressService.GetByIdAsync(dto.ToAddressId);

        await Task.WhenAll(fromTask, toTask);

        var fromAddress = fromTask.Result;
        var toAddress   = toTask.Result;

        var fromSnapshot = AddressSnapshot.From(fromAddress);
        var toSnapshot   = AddressSnapshot.From(toAddress);

        var warehouse = await warehouseService.GetNearestAsync(fromAddress, ct);

        var zone   = await zoneService.GetAsync(fromAddress, toAddress, ct);
        var weight = await weightService.Calculate(dto.Items);
        var price  = await pricingService.CalculateAsync(dto.ServiceId, zone, weight, dto.Cod, ct);

        using var transaction = await db.Database.BeginTransactionAsync(ct);
        try
        {
            var order = Order.Create(
                userId,
                dto.ServiceId,
                warehouse.Id,
                fromSnapshot,
                toSnapshot,
                price.Cost,
                price.Fee,
                dto.Items);

            await orderRepository.AddAsync(order, ct);

            await orderHistoryRepository.AddAsync(
                new OrderHistory
                {
                    OrderId = order.Id,
                    Status = OrderStatus.WAITTING,
                    CreatedAt = DateTime.UtcNow
                },
                ct);

            await trackingRepository.AddAsync(
                new Tracking
                {
                    OrderId = order.Id,
                    Status = OrderStatus.WAITTING,
                    CreatedAt = DateTime.UtcNow
                },
                ct);

            await orderRepository.SaveChangesAsync(ct);

            await transaction.CommitAsync(ct);

            await eventBus.Publish(
                new OrderCreatedEvent(order.Id));

            return mapper.Map<OrderDto>(order);
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }

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
            .Select(row => new RowDto
            {
                FromName   = row.Cell(1).GetString().Trim(),
                FromPhone  = row.Cell(2).GetString().Trim(),
                FromCity   = row.Cell(3).GetString().Trim(),
                FromWard   = row.Cell(4).GetString().Trim(),
                FromStreet = row.Cell(5).GetString().Trim(),

                ToName   = row.Cell(6).GetString().Trim(),
                ToPhone  = row.Cell(7).GetString().Trim(),
                ToCity   = row.Cell(8).GetString().Trim(),
                ToWard   = row.Cell(9).GetString().Trim(),
                ToStreet = row.Cell(10).GetString().Trim(),

                ServiceName = row.Cell(11).GetString().Trim(),

                ItemImage    = row.Cell(12).GetString().Trim(),
                ItemName     = row.Cell(13).GetString().Trim(),
                ItemWeight   = row.Cell(14).GetValue<double>(),
                ItemQuantity = row.Cell(15).GetValue<int>(),
                ItemLength   = row.Cell(16).GetValue<double>(),
                ItemWidth    = row.Cell(17).GetValue<double>(),
                ItemHeight   = row.Cell(18).GetValue<double>(),

                RowNumber    = row.RowNumber()
            })
            .ToList();

        if (!rows.Any())
            throw new BadRequestException("File không có dữ liệu");

        var serviceNames = rows
            .Select(r => r.ServiceName)
            .Distinct()
            .ToList();

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
            .GroupBy(r => new
            {
                r.FromPhone,
                r.ToPhone,
                r.ServiceName,
            })
            .ToList();

        var results = new List<OrderDto>();
        var errors  = new List<BatchErrorDto>();

        foreach (var group in groups)
        {
            var first = group.First();
            try
            {
                // Validate service từ dict — không query DB
                if (!services.TryGetValue(first.ServiceName, out var service))
                    throw new BadRequestException($"Dịch vụ '{first.ServiceName}' không tồn tại");

                // Validate province từ dict — không query DB
                if (!provinces.TryGetValue(first.FromProvince, out var fromProvince))
                    throw new BadRequestException($"Tỉnh/thành '{first.FromProvince}' không tồn tại");

                if (!provinces.TryGetValue(first.ToProvince, out var toProvince))
                    throw new BadRequestException($"Tỉnh/thành '{first.ToProvince}' không tồn tại");

                // Build raw address — CreateAsync tự resolve warehouse + zone
                var dto = new CreateOrderDto
                {
                    FromAddress = new AddressRawDto(
                        Name         : first.FromName,
                        Phone        : first.FromPhone,
                        ProvinceCode : fromProvince.Code,
                        District     : first.FromDistrict,
                        Ward         : first.FromWard,
                        Street       : first.FromStreet
                    ),
                    ToAddress = new AddressRawDto(
                        Name         : first.ToName,
                        Phone        : first.ToPhone,
                        ProvinceCode : toProvince.Code,
                        District     : first.ToDistrict,
                        Ward         : first.ToWard,
                        Street       : first.ToStreet
                    ),

                    ServiceId = service.Id,
                    
                    Items = group.Select(i => new CreateItemDto(
                        Image    : i.ItemImage,
                        Name     : i.ItemName,
                        Quantity : i.ItemQuantity,
                        Weight   : i.ItemWeight,
                        Length   : i.ItemLength,
                        Width    : i.ItemWidth,
                        Height   : i.ItemHeight
                    )).ToList()
                };

                var order = await CreateAsync(dto, userId, ct);
                results.Add(order);
            }
            catch (Exception ex)
            {
                errors.Add(new BatchErrorDto(
                    Rows  : group.Select(r => r.RowNumber).ToList(),
                    Reason: ex.Message
                ));

                logger.LogWarning(
                    "CreateListAsync row error. Rows={Rows} Error={Error}",
                    string.Join(",", group.Select(r => r.RowNumber)),
                    ex.Message);
            }
        }

        return new OrderListDto(
            Created: results,
            Errors : errors
        );
    }

    public async Task UpdateAsync( 
        UpdateOrderDto dto, 
        Guid orderId,
        Guid userId, 
        CancellationToken ct)
    {
        using var transaction = await db.Database.BeginTransactionAsync();

        try
        {
            var order = await db.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == orderId, ct)
                ?? throw new NotFoundException("Order not found");

            if (order.UserId != userId)
                throw new ForbiddenException("Không có quyền cập nhật đơn hàng");

            if (order.Status != OrderStatus.WAITTING)
                throw new InvalidOrderTransitionException("Không có quyền cập nhật đơn hàng khi đang chờ");

            var fromAddressTask = await addressRepository.Query()
                .FirstOrDefaultAsync(a => a.Id == dto.FromAddressId, ct)
                ?? throw new NotFoundException("Address not found");

            var toAddressTask = await addressRepository.Query()
                .FirstOrDefaultAsync(a => a.Id == dto.ToAddressId, ct)
                ?? throw new NotFoundException("Address not found");

            await Task.WhenAll(fromAddressTask, toAddressTask);

            var fromAddress = fromTask.Result;
            var toAddress   = toTask.Result;

            var items  = dto.Items.Select(i => new ItemUpdate(
                i.Id, 
                i.Name, 
                i.Quantity, 
                i.Weight, 
                i.Length, 
                i.Width, 
                i.Height))
                .ToList();

            var zone   = await zoneService.GetAsync(fromAddress.ProvinceCode, toAddress.ProvinceCode, ct);
            var weight = weightService.Calculate(items);
            var price  = await pricingService.CalculateAsync(dto.ServiceId, zone, weight, dto.Cod, ct);

            order.Update(
                AddressSnapshot.From(fromAddress),
                AddressSnapshot.From(toAddress),
                dto.ServiceId,
                price.Cost,
                price.Fee,
                price.Total);

            order.UpdateItems(items);

            await ordesrHistoryRepository.AddAsync(new OrderHistory
            {
                Id      = Guid.NewGuid(),
                Note    = "Đã cập nhật đơn hàng",
                Date    = DateTime.UtcNow,
                OrderId = orderId,
                UserId  = userId,
            }, ct);

            await trackingRepository.AddAsync(new Tracking
            {
                Id      = Guid.NewGuid(),
                Message = "Đã cập nhật đơn hàng",
                Date    = DateTime.UtcNow,
                OrderId = orderId,
            }, ct);

            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }
        catch (Exception e) 
        {
            await transaction.RollbackAsync(ct);
            _logger.LogError(ex, "Cancel order failed. OrderId={OrderId} UserId={UserId}",
                orderId, userId);
            throw;
        }
    }

    public async Task<EstimateD> Estimate(EstimateDTO dto)
    {
        var zone = _zoneService.GetZone(dto.sender, dto.receiver);
        var weight = _weightService.Calculate(dto.Items);

        var price = _priceService.Calculate(
            zone,
            weight,
        );

        var deliveryDays = zone switch
        {
            "Internal" => 1,
            "SameRegion" => 2,
            "CrossRegion" => 4,
            _ => 5
        };

        return new EstimateDTO
        {
            Zone = zone,

            Weight = weight,

            Cost = price.Cost,

            Fee = price.Fee,

            Total = price.Total,

            EstimatedDeliveryDays = deliveryDays
        };
    }

    /*public async Task UpdateStatus(Guid orderId, string trigger, Guid userId)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var order = await _context.Orders.FindAsync(orderId)
                ?? throw new NotFoundException("Order", orderId);

            var workflow = new OrderWorkflow(order.Status);

            if (!workflow.Can(trigger))
                throw new InvalidOrderTransitionException(order.Status, trigger);

            var now       = DateTime.UtcNow;
            var newStatus = workflow.Fire(trigger);
            order.Status  = newStatus;

            _context.OrderHistories.Add(new OrderHistory
            {
                OrderId = orderId,
                UserId  = userId,
                Status  = newStatus,
                Note    = trigger,
                Date    = now
            });

            _context.Tracking.Add(new Tracking
            {
                OrderId = orderId,
                Status  = newStatus,
                Message = GetTrackingMessage(newStatus),
                Date    = now
            });

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            await _eventBus.Publish(new OrderStatusChangedEvent
            {
                OrderId = orderId,
                Status  = newStatus
            });
        }
        catch (Exception ex) when (ex is not NotFoundException
                                && ex is not InvalidOrderTransitionException)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Update status failed. OrderId={OrderId} Trigger={Trigger}",
                orderId, trigger);
            throw;
        }
    */}

    public async Task ConfirmAsync(
        ConfirmDto dto,
        Guid userId,
        CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        try
        {
            var orders = await db.Orders
                .Where(o => dto.OrderIds.Contains(o.Id))
                .ToListAsync(ct);

            if (!orders.Any())
                throw new NotFoundException("Orders not found");

            var now = DateTime.UtcNow;

            var histories = new List<OrderHistory>();
            var trackings = new List<Tracking>();

            foreach (var order in orders)
            {
                var workflow = new OrderWorkflow(order.Status);

                if (!workflow.Can(dto.Trigger))
                    throw new InvalidOrderTransitionException(order.Status, dto.Trigger);

                var newStatus = workflow.Fire(dto.Trigger);

                order.Status = newStatus;

                histories.Add(new OrderHistory
                {
                    Id = Guid.NewGuid(),
                    OrderId = order.Id,
                    UserId = userId,
                    Status = newStatus,
                    Note = dto.Trigger,
                    Date = now
                });

                trackings.Add(new Tracking
                {
                    Id = Guid.NewGuid(),
                    OrderId = order.Id,
                    Status = newStatus,
                    Message = GetTrackingMessage(newStatus),
                    Date = now
                });
            }

            db.OrderHistories.AddRange(histories);
            db.Trackings.AddRange(trackings);

            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            foreach (var order in orders)
            {
                await eventBus.Publish(new OrderStatusChangedEvent
                {
                    OrderId = order.Id,
                    Status = order.Status
                });
            }
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }

    public async Task CancelAsync(
    CancelDto dto,
    Guid userId,
    CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        try
        {
            var orders = await db.Orders
                .Where(o => dto.OrderIds.Contains(o.Id))
                .ToListAsync(ct);

            if (!orders.Any())
                throw new NotFoundException("Orders not found");

            var now = DateTime.UtcNow;

            var histories = new List<OrderHistory>();
            var trackings = new List<Tracking>();

            foreach (var order in orders)
            {
                if (order.UserId != userId)
                    throw new ForbiddenException("Bạn không có quyền hủy đơn hàng này");

                var workflow = new OrderWorkflow(order.Status);

                if (!workflow.Can(dto.Trigger))
                    throw new InvalidOrderTransitionException(order.Status, dto.Trigger);

                var newStatus = workflow.Fire(dto.Trigger);

                order.Status = newStatus;

                histories.Add(new OrderHistory
                {
                    Id = Guid.NewGuid(),
                    OrderId = order.Id,
                    UserId = userId,
                    Status = newStatus,
                    Note = $"Hủy đơn. Lý do: {dto.Reason}",
                    Date = now
                });

                trackings.Add(new Tracking
                {
                    Id = Guid.NewGuid(),
                    OrderId = order.Id,
                    Status = newStatus,
                    Message = "Đơn hàng đã bị hủy",
                    Date = now
                });
            }

            db.OrderHistories.AddRange(histories);
            db.Trackings.AddRange(trackings);

            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            foreach (var order in orders)
            {
                await eventBus.Publish(new OrderStatusChangedEvent
                {
                    OrderId = order.Id,
                    Status = order.Status
                });
            }
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }

    // ─────────────────────────────────────────────
    // PRIVATE HELPERS
    // ─────────────────────────────────────────────
    private async Task<string> GenerateUniqueCodeAsync()
    {
        for (int i = 0; i < 5; i++)
        {
            var code   = GenerateCode();
            var exists = await _context.Orders.AnyAsync(o => o.Code == code);
            if (!exists) return code;
        }
        throw new InvalidOperationException("Không thể tạo mã đơn hàng duy nhất sau 5 lần thử");
    }

    private static string GenerateCode() =>
        $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..4].ToUpper()}";

    private static string GetTrackingMessage(OrderStatus status) => status switch
    {
        OrderStatus.Pending        => "Đơn hàng đang chờ xác nhận",
        OrderStatus.Confirmed      => "Đơn hàng đã được xác nhận",
        OrderStatus.PickingUp      => "Đang lấy hàng từ người gửi",
        OrderStatus.InWarehouse    => "Hàng đã về kho trung chuyển",
        OrderStatus.OutForDelivery => "Đơn hàng đang trên đường giao đến bạn",
        OrderStatus.Delivered      => "Giao hàng thành công",
        OrderStatus.ReturnRequested => "Đang xử lý yêu cầu hoàn hàng",
        OrderStatus.Returning      => "Hàng đang được hoàn về người gửi",
        OrderStatus.Returned       => "Hoàn hàng thành công",
        OrderStatus.Cancelled      => "Đơn hàng đã bị hủy",
        _                          => status.ToString()
    };
}