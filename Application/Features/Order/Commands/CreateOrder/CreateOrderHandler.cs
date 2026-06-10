using MediatR;
using Microsoft.Extensions.Logging;

public class CreateOrderHandler(
    IOrderRepository orderRepository,
    IAddressService addressService,
    IZoneService zoneService,
    IWeightService weightService,
    IPriceService priceService,
    IOrderHistoryService orderHistoryService,
    ITrackingService trackingService,
    IEventBus eventBus,
    ILogger<CreateOrderHandler> logger)
    : IRequestHandler<CreateOrderCommand, OrderDto>
{
    public async Task<OrderDto> Handle(
        CreateOrderCommand request,
        CancellationToken ct)
    {
        var dto = request.Dto;
        var userId = request.UserId;

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
                Status        = OrderStatus.PENDING,
                Date          = DateTime.UtcNow,
                FromAddressId = dto.FromAddressId,
                ToAddressId   = dto.ToAddressId,
                ServiceId     = dto.ServiceId,
                WarehouseId   = dto.WarehouseId,
                Cost          = price.Cost,
                Fee           = price.Fee,
                Total         = price.Total,
                Items         = dto.Items
                    .Select(i => new Item
                    {
                        Id = Guid.NewGuid(),
                        Image = i.Image,
                        Name = i.Name,
                        Type = i.Type,
                        Quantity = i.Quantity,
                        Weight = i.Weight,
                        Length = i.Length,
                        Width = i.Width,
                        Height = i.Height
                    })
                    .ToList();
                UserId        = userId,
            };

            orderRepository.Add(order, ct);
            await orderRepository.SaveChangesAsync(ct);

            await orderHistoryService.CreateAsync(new OrderHistory
            {
                Id = Guid.NewGuid(),
                Note = "Đã tạo đơn hàng",
                Date = DateTime.UtcNow,
                OrderId = order.Id,
                UserId = userId,
            });

            await trackingService.CreateAsync(new Tracking
            {
                Id = Guid.NewGuid(),
                Message = "Đã tạo đơn hàng",
                Date = DateTime.UtcNow,
                OrderId = order.Id,
                UserId = userId,
            });

            await transaction.CommitAsync(ct);

            await eventBus.Publish(
                new OrderCreatedEvent(order.Id),
                ct);

            return new OrderDto
            {
                Id = order.Id,
                Code = order.Code,
                SenderId = order.SenderId,
                ReceiverId = order.ReceiverId,
                Service = order.Service,
                Cost = order.Cost,
                Fee = order.Fee,
                Total = order.Total,
                Status = order.Status,
                Date = order.Date,
                UserId = order.UserId,
                Items = order.Items
            };
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(ct);

            logger.LogError(
                ex,
                "Create order failed. UserId={UserId}",
                userId);

            throw;
        }
    }

    private static string GenerateCode()
    {
        return $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid():N[..4]}";
    }
}
