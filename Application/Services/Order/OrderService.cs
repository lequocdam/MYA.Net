using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class OrderService(
    IOrderRepository orderRepository,
    IAddressService addressService,
    IZoneService zoneService,
    IWeightService _weightService,
    IEventBus eventBus,
    ILogger<OrderService> logger) : IOrderService
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

    public async Task<OrderDto> CreateAsync(CreateOrderDto dto, Guid userId, CancellationToken ct)
    {
        var transaction = await orderRepository.BeginTransactionAsync();

        try
        {
            var sender = addressService.CreateAsync(new Address
            {
                Id      = Guid.NewGuid(),
                Name    = dto.Sender.Name,
                Phone   = dto.Sender.Phone,
                Email   = dto.Sender.Email,
                Address = dto.Sender.Address,
            });

            var receiver = addressService.CreateAsync(new Address
            {
                Id = Guid.NewGuid(),
                Name    = dto.Receiver.Name,
                Phone   = dto.Receiver.Phone,
                Email   = dto.Receiver.Email,
                Address = dto.Receiver.Address,
            });

            var items = dto.Items
                .Select(i => new Items
                {
                    Id = Guid.NewGuid(),
                    Image    = i.Image,
                    Name     = i.Name,
                    Type     = i.Type,
                    Quantity = i.Quantity,
                    Weight   = i.Weight,
                    Length   = i.Length,
                    Width    = i.Width,
                    Height   = i.Height,
                })
                .ToList()

            var zone   = zoneService.GetAsync(sender, receiver);
            var weight = weightService.CalculateAsync(items);
            var price  = priceService.CalculateAsync(zone, weight);

            var order = new Order
            {
                Id         = Guid.NewGuid(),
                Code       = GenerateCode(),
                SenderId   = sender.Id,
                ReceiverId = receiver.Id,
                ServiceId  = dto.ServiceId,
                Cost       = price.Cost,
                Fee        = price.Fee,
                Total      = price.Total,
                Status     = OrderStatus.PENDING,
                Date       = DateTime.UtcNow,
                UserId     = userId,
                Items      = items,
            };

            orderRepository.Add(order);

            orderHistoryService.CreateAsync(new OrderHistory
            {
                Id = Guid.NewGuid(),
                Note    = "Đã tạo đơn hàng",
                Date    = DateTime.UtcNow,
                OrderId = order.Id,
                UserId  = userId,
            });

            trackingService.CreateAsync(new Tracking
            {
                Id = Guid.NewGuid(),
                Message = "Đã tạo đơn hàng",
                Date    = DateTime.UtcNow,
                OrderId = order.Id,
                UserId  = userId,
            });

            await orderRepository.SaveChangesAsync();
            await transaction.CommitAsync();

            return new OrderDto
            {
                Id         = order.Id,
                Code       = order.Code,
                SenderId   = order.SenderId,
                ReceiverId = order.ReceiverId,
                Service    = order.Service,
                Cost       = order.Cost,
                Fee        = order.Fee,
                Total      = order.Total,
                Status     = order.Status,
                Date       = DateTime.UtcNow,
                UserId     = order.UserId,
                Items      = order.Items,
            };
        }
        catch (Exception e)
        {
            await transaction.RollbackAsync();
            logger.LogError("Create order failed. UserId={UserId}", userId);
        }
    }

    // CREATE LIST
    public async Task<BatchResultDTO> CreateFromExcel(IFormFile file, Guid userId)
    {
        using var stream = file.OpenReadStream();
        using var workbook = new XLWorkbook(stream);

        var sheet = workbook.Worksheet("Orders")
            ?? throw new BadRequestException("Sheet 'Orders' không tồn tại trong file Excel");

        // Parse rows → group by order (vì mỗi row là 1 item, nhiều row cùng SenderPhone+ReceiverPhone = 1 đơn)
        var rawRows = sheet.RowsUsed().Skip(1).Select(row => new
        {
            SenderName     = row.Cell(1).GetString().Trim(),
            SenderPhone    = row.Cell(2).GetString().Trim(),
            SenderAddress  = row.Cell(3).GetString().Trim(),
            ReceiverName   = row.Cell(4).GetString().Trim(),
            ReceiverPhone  = row.Cell(5).GetString().Trim(),
            ReceiverAddress= row.Cell(6).GetString().Trim(),
            Category       = row.Cell(7).GetString().Trim(),
            ItemName       = row.Cell(8).GetString().Trim(),
            ItemWeight     = row.Cell(9).GetValue<decimal>(),
            ItemQty        = row.Cell(10).GetValue<int>(),
            ItemLength     = row.Cell(11).GetValue<decimal>(),
            ItemWidth      = row.Cell(12).GetValue<decimal>(),
            ItemHeight     = row.Cell(13).GetValue<decimal>(),
            RowNumber      = row.RowNumber(),
        }).ToList();

        // Group thành từng đơn: key = SenderPhone + ReceiverPhone + Category
        var groups = rawRows
            .GroupBy(r => (r.SenderPhone, r.ReceiverPhone, r.Category))
            .ToList();

        var results  = new List<OrderDTO>();
        var errors   = new List<BatchErrorDTO>();

        foreach (var group in groups)
        {
            var first = group.First();
            try
            {
                var dto = new CreatedOrderDTO
                {
                    Sender = new AddressInputDTO
                    {
                        Name    = first.SenderName,
                        Phone   = first.SenderPhone,
                        Address = first.SenderAddress,
                    },
                    Receiver = new AddressInputDTO
                    {
                        Name    = first.ReceiverName,
                        Phone   = first.ReceiverPhone,
                        Address = first.ReceiverAddress,
                    },
                    Category = first.Category,
                    Items    = group.Select(r => new ItemInputDTO
                    {
                        Name   = r.ItemName,
                        Weight = r.ItemWeight,
                        Quantity = r.ItemQty,
                        Length = r.ItemLength,
                        Width  = r.ItemWidth,
                        Height = r.ItemHeight,
                    }).ToList(),
                };

                var order = await Create(dto, userId);
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

        return new BatchResultDTO
        {
            Created = results,
            Errors  = errors,
        };
    }

    // UPDATE
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

    // UPDATE STATUS
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