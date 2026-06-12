using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class OrderService(
    IOrderRepository orderRepository,
    IAddressService addressService,
    IZoneService zoneService,
    IWeightService _weightService,
    IEventBus eventBus,
    ILogger<OrderService> logger,
    IMapper mapper) : IOrderService
{
    public async Task<OrderPage<OrderDto>> GetAllAsync(
        OrderFilterDto filter,
        Guid userId,
        CancellationToken ct)
    {
        var query = orderRepository
            .Query()
            .Where(o => o.UserId == userId);

        if (!string.IsNullOrWhiteSpace(filter.Code))
            query = query.Where(o => o.Code.Contains(filter.Code));

        if (filter.From.HasValue)
            query = query.Where(o => o.Date >= filter.From.Value);

        if (filter.To.HasValue)
            query = query.Where(o => o.Date <= filter.To.Value);

        if (filter.Status.HasValue)
            query = query.Where(o => o.Status == filter.Status.Value);

        var total = await query.CountAsync(ct);

        var skip = (filter.Page - 1) * filter.PageSize;

        var orders = await query
            .OrderByDescending(o => o.Date)
            .Skip(skip)
            .Take(filter.PageSize)
            .Select(o => new OrderDto
            {
                Id = o.Id,
                Code = o.Code,
                Date = o.Date,
                FromId = o.FromId,
                ToId = o.ToId,
                ServiceId = o.ServiceId,
                Total = o.Total,
                Status = o.Status
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
        await using var transaction = await orderRepository.BeginTransactionAsync();

        try
        {
            var zone   = await zoneService.GetAsync(dto.FromAddressId, dto.ToAddressId);
            var weight = await weightService.CalculateAsync(dto.Items);
            var price  = await priceService.CalculateAsync(zone, weight);

            var order = new Order
            {
                Id            = Guid.NewGuid(),
                Code          = GenerateCode(),
                Cost          = price.Cost,
                Fee           = price.Fee,
                Total         = price.Total,
                Status        = OrderStatus.WAITTING,
                Date          = DateTime.UtcNow,
                FromAddressId = dto.FromAddressId,
                ToAddressId   = dto.ToAddressId,
                ServiceId     = dto.ServiceId,
                WarehouseId   = dto.WarehouseId,
                Items         = mapper.Map<List<Item>>(dto.Items),
                UserId        = userId,
            };

            await orderRepository.Add(order, ct);

            await orderHistoryService.CreateAsync(new OrderHistory
            {
                Id      = Guid.NewGuid(),
                Note    = "Đã tạo đơn hàng",
                Date    = DateTime.UtcNow,
                OrderId = order.Id,
                UserId  = userId,
            });

            await trackingService.CreateAsync(new Tracking
            {
                Id      = Guid.NewGuid(),
                Message = "Đã tạo đơn hàng",
                Date    = DateTime.UtcNow,
                OrderId = order.Id,
                UserId  = userId,
            });

            await orderRepository.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            return mapper.Map<OrderDto>(order);
        }
        catch (Exception e)
        {
            await transaction.RollbackAsync(ct);
            logger.LogError(
                e,
                "Create order failed. UserId={UserId}",
                userId);
            throw;
        }
    }

    public async Task<OrderListDto> CreateListAsync(
        IFormFile file,
        Guid userId,
        CancellationToken ct)
    {
        using var stream = file.OpenReadStream();
        using var workbook = new XLWorkbook(stream);

        var sheet = workbook.Worksheet("Orders")
            ?? throw new BadRequestException("Orders sheet not found");

        var rows = sheet
        .RowsUsed()
        .Skip(1)
        .Select(row => new CreateOrdersDto
        {
            FromAddressName    = row.Cell(1).GetString().Trim(),
            FromAddressPhone   = row.Cell(2).GetString().Trim(),
            FromAddressEmail   = row.Cell(3).GetString().Trim(),
            FromAddressAddress = row.Cell(4).GetString().Trim(),

            ToAddressName      = row.Cell(5).GetString().Trim(),
            ToAddressPhone     = row.Cell(6).GetString().Trim(),
            ToAddressEmail     = row.Cell(7).GetString().Trim(),
            ToAddressAddress   = row.Cell(8).GetString().Trim(),

            ServiceName        = row.Cell(9).GetString().Trim(),
            WarehouseName      = row.Cell(10).GetString().Trim(),

            ItemImage          = row.Cell(11).GetString().Trim(),
            ItemName           = row.Cell(12).GetString().Trim(),
            ItemWeight         = row.Cell(13).GetValue<double>(),
            ItemQuantity       = row.Cell(14).GetValue<int>(),
            ItemLength         = row.Cell(15).GetValue<double>(),
            ItemWidth          = row.Cell(16).GetValue<double>(),
            ItemHeight         = row.Cell(17).GetValue<double>(),

            RowNumber          = row.RowNumber()
        })
        .ToList();

        var groups = rows
            .GroupBy(r => new
            {
                r.FromAddressPhone,
                r.ToAddressPhone,
                r.ServiceName,
                r.WarehouseName
            })
            .ToList();

        var results  = new List<OrderDTO>();
        var errors   = new List<BatchErrorDTO>();

        foreach (var group in groups)
        {
            var first = group.First();
            try
            {
                var fromAddress = await addressService.GetByNameAsync(
                    first.FromAddressName,
                    first.FromAddressPhone,
                    first.FromAddressEmail,
                    first.FromAddressAddress,
                    ct);

                var toAddress = await addressService.GetByNameAsync(
                    first.ToAddressName,
                    first.ToAddressPhone,
                    first.ToAddressEmail,
                    first.ToAddressAddress,
                    ct);

                var service = await serviceService.GetByNameAsync(
                    first.ServiceName,
                    ct);

                var warehouse = await warehouseService.GetByNameAsync(
                    first.WarehouseName,
                    ct);

                var dto = new CreateOrderDto
                {
                    FromAddressId = fromAddress.Id,
                    ToAddressId   = toAddress.Id,
                    ServiceId     = service.Id,
                    WarehouseId   = warehouse.Id,
                    Items = group.Select(i => new CreateItemDto
                    {
                        Image    = i.ItemImage,
                        Name     = i.ItemName,
                        Quantity = i.ItemQuantity,
                        Weight   = i.ItemWeight,
                        Length   = i.ItemLength,
                        Width    = i.ItemWidth,
                        Height   = i.ItemHeight
                    }).ToList()
                };

                var order = await CreateAsync(dto, userId, ct);
                results.Add(order);
            }
            catch (Exception ex)
            {
                errors.Add(new BatchErrorDTO
                {
                    Rows    = group.Select(r => r.RowNumber).ToList(),
                    Reason  = ex.Message,
                });
                _logger.LogWarning("CreateFromExcel row error. Rows={Rows} Error={Error}",
                    string.Join(",", group.Select(r => r.RowNumber)), ex.Message);
            }
        }

        return new OrderListDTO
        {
            Created = results,
            Errors  = errors,
        };
    }

    public async Task Update(Guid orderId, UpdatingOrderDTO dto, Guid userId)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var order = await _context.Orders.FindAsync(orderId)
                ?? throw new NotFoundException("Order", orderId);

            if (order.UserId != userId)
                throw new ForbiddenException("Bạn không có quyền sửa đơn hàng này");

            var updatedStatuses = new[]
            {
                OrderStatus.PENDING,
                OrderStatus.CÒNIRMED
            };

            if (!updatedStatuses.Contains(order.Status))
                throw new InvalidOrderTransitionException(
                    order.Status,
                    "Không thể cập nhật đơn khi đang trong quá trình vận chuyển"
                );

            order = new Order
            {
                SenderId   = sender.Id,
                ReceiverId = receiver.Id,
                Category   = dto.Category,
                Cost       = price.Cost,
                Fee        = price.Fee,
                Total      = price.Total,
                Date       = now,
                Items      = dto.Items.Select(i => new Item
                {
                    Image    = i.Image,
                    Name     = i.Name,
                    Type     = i.Type,
                    Quantity = i.Quantity,
                    Weight   = i.Weight,
                    Length   = i.Length,
                    Width    = i.Width,
                    Height   = i.Height,
                }).ToList()
            };

            _context.OrderHistories.Add(new OrderHistory
            {
                OrderId = orderId,
                UserId  = userId,
                Status  = OrderStatus.Cancelled,
                Note    = $"Hủy bởi khách. Lý do: {reason}",
                Date    = now
            });

            _context.Tracking.Add(new Tracking
            {
                OrderId = orderId,
                Status  = OrderStatus.Cancelled,
                Message = "Đơn hàng đã bị hủy",
                Date    = now
            });

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            await _eventBus.Publish(new OrderStatusChangedEvent
            {
                OrderId = orderId,
                Status  = OrderStatus.Cancelled
            });
        }
        catch (Exception ex) when (ex is not NotFoundException
                                && ex is not ForbiddenException
                                && ex is not InvalidOrderTransitionException)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Cancel order failed. OrderId={OrderId} UserId={UserId}",
                orderId, userId);
            throw;
        }
    }

    public async Task<EstimateDTO> Estimate(EstimateDTO dto)
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

    public async Task UpdateStatus(Guid orderId, string trigger, Guid userId)
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
    }

    public async Task BulkUpdateStatusAsync(
        BulkUpdateStatusDto dto,
        Guid userId,
        CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        try
        {
            // 1. Load all orders 1 lần
            var orders = await _context.Orders
                .Where(o => dto.OrderIds.Contains(o.Id))
                .ToListAsync(ct);

            if (orders.Count == 0)
                throw new NotFoundException("Orders not found");

            var now = DateTime.UtcNow;

            var histories = new List<OrderHistory>();
            var trackings = new List<Tracking>();

            foreach (var order in orders)
            {
                var workflow = new OrderWorkflow(order.Status);

                if (!workflow.Can(dto.Trigger))
                    continue; // hoặc throw tùy business

                var newStatus = workflow.Fire(dto.Trigger);

                // update entity
                order.Status = newStatus;

                // history
                histories.Add(new OrderHistory
                {
                    Id = Guid.NewGuid(),
                    OrderId = order.Id,
                    UserId = userId,
                    Status = newStatus,
                    Note = dto.Trigger,
                    Date = now
                });

                // tracking
                trackings.Add(new Tracking
                {
                    Id = Guid.NewGuid(),
                    OrderId = order.Id,
                    Status = newStatus,
                    Message = GetTrackingMessage(newStatus),
                    Date = now
                });
            }

            // 2. Add batch
            _context.OrderHistories.AddRange(histories);
            _context.Tracking.AddRange(trackings);

            // 3. Save 1 lần
            await _context.SaveChangesAsync(ct);

            await transaction.CommitAsync(ct);

            // 4. Event (có thể batch)
            foreach (var order in orders)
            {
                await _eventBus.Publish(new OrderStatusChangedEvent
                {
                    OrderId = order.Id,
                    Status = order.Status
                });
            }
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(ct);
            _logger.LogError(ex, "Bulk update status failed");
            throw;
        }
    }

    // CANCEL
    public async Task Cancel(Guid orderId, string reason, Guid userId)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var order = await _context.Orders.FindAsync(orderId)
                ?? throw new NotFoundException("Order", orderId);

            if (order.UserId != userId)
                throw new ForbiddenException("Bạn không có quyền hủy đơn hàng này");

            // Chỉ hủy được khi đơn chưa lấy hàng
            var cancellableStatuses = new[]
            {
                OrderStatus.Pending,
                OrderStatus.Confirmed
            };

            if (!cancellableStatuses.Contains(order.Status))
                throw new InvalidOrderTransitionException(
                    order.Status,
                    "Không thể hủy đơn khi đang trong quá trình vận chuyển"
                );

            var now          = DateTime.UtcNow;
            order.Status     = OrderStatus.Cancelled;
            order.CancelledAt = now;

            _context.OrderHistories.Add(new OrderHistory
            {
                OrderId = orderId,
                UserId  = userId,
                Status  = OrderStatus.Cancelled,
                Note    = $"Hủy bởi khách. Lý do: {reason}",
                Date    = now
            });

            _context.Tracking.Add(new Tracking
            {
                OrderId = orderId,
                Status  = OrderStatus.Cancelled,
                Message = "Đơn hàng đã bị hủy",
                Date    = now
            });

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            await _eventBus.Publish(new OrderStatusChangedEvent
            {
                OrderId = orderId,
                Status  = OrderStatus.Cancelled
            });
        }
        catch (Exception ex) when (ex is not NotFoundException
                                && ex is not ForbiddenException
                                && ex is not InvalidOrderTransitionException)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Cancel order failed. OrderId={OrderId} UserId={UserId}",
                orderId, userId);
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