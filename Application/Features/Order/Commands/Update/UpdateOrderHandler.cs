using MediatR;

public class UpdateOrderHandler(
    IOrderRepository orderRepository,
    IAddressService addressService,
    IZoneService zoneService,
    IWeightService weightService,
    IPriceService priceService)
    : IRequestHandler<UpdateOrderCommand>
{
    public async Task Handle(
        UpdateOrderCommand request,
        CancellationToken ct)
    {
        var transaction = await orderRepository.BeginTransactionAsync();

        try
        {
            var order = await orderRepository.FindAsync(request.OrderId, ct);
            if (order is null)
                throw new NotFoundException("Order not found");

            if (order.Status != OrderStatus.PENDING)
                throw new InvalidOrderTransitionException("");

            var dto = request.Dto;

            order.FromAddressId = dto.FromAddressId;
            order.ToAddressId   = dto.ToAddressId;
            order.ServiceId = dto.ServiceId;

            var removedItems = order.Items
                .Where(x => dto.Items.All(i => i.Id != x.Id))
                .ToList();

            foreach (var item in removedItems)
            {
                order.Items.Remove(item);
            }

            foreach (var item in dto.Items)
            {
                var exitItem = order.Items.FirstOrDefault(i => i.Id == item.Id);

                if (exitItem != null)
                {
                    exitItem.Name     = item.Name;
                    exitItem.Quantity = item.Quantity;
                    exitItem.Weight   = item.Weight;
                }
                else
                {
                    order.Items.Add(new Item
                    {
                        Id       = Guid.NewGuid(),
                        Name     = item.Name,
                        Quantity = item.Quantity,
                        Weight   = item.Weight,
                    });
                }
            }

            var zone   = zoneService.GetZone(order.Sender, order.Receiver);
            var weight = weightService.Calculate(order.Items);
            var price  = priceService.Calculate(zone, weight);

            order.Cost = price.Cost;
            order.Fee = price.Fee;
            order.Total = price.Total;

            await orderRepository.SaveChangesAsync(ct);

            await transaction.CommitAsync(ct);
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }
}