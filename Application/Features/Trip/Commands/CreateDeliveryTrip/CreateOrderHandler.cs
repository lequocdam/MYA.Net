cat > /home/claude/OrderService.Cqrs/Commands/CreateOrder/CreateOrderCommand.cs << 'EOF'
using MediatR;

namespace YourApp.Application.Orders.Commands.CreateOrder;

/// <summary>Tạo 1 order đơn lẻ (tương ứng OrderService.CreateAsync gốc).</summary>
public sealed record CreateOrderCommand(
    CurrentUser CurrentUser,
    Guid ServiceId,
    Guid FromAddressId,
    Guid ToAddressId,
    bool? Cod,
    List<CreateItemReq> Items
) : IRequest<Guid>;
EOF

cat > /home/claude/OrderService.Cqrs/Commands/CreateOrder/CreateOrderCommandHandler.cs << 'EOF'
using MediatR;
using YourApp.Application.Orders.Abstractions;

namespace YourApp.Application.Orders.Commands.CreateOrder;

public sealed class CreateCommandHandler(
    IAddressRepository   addressRepository,
    IWarehouseService    warehouseService,
    IQuoteService        quoteService,
    IOrderWriteService   orderWriteService
) : IRequestHandler<CreateOrderCommand, Guid>
{
    public async Task<Guid> Handle(CreateDeliveryTripCommand req, CancellationToken ct)
    {
        var spec = new OrdersForDeliveryTripSpec(req.OrderIds);
        var orders = await orderRepo.ListAsync(spec, ct);

        TripCreationPolicy.ValidateOrders(orders);

        var trip = new Trip
        {
            Id = tripId,
            Code = await GenerateCodeAsync(ct),
            Type = dto.Type,
            Status = DeliveryTripStatus.Prepared,
            FromId = route.FromId,
            ToId = route.ToId,  
            Date = DateTime.UtcNow
        };

        trip.Stops = BuildStops(
            tripId,
            route);

        foreach (var order in orders)
        {
            trip.Orders.Add(new TripOrder
            {
                TripId = tripId,
                OrderId = order.Id,
                AssignedAt = DateTime.UtcNow
            });
        }

        await tripRepository.AddAsync(trip, ct);

        return tripId;
    }
}

