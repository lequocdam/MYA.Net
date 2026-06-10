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

            if (order.UserId != request.UserId)
                throw new ForbiddenException(
                    "Bạn không có quyền sửa đơn hàng này");

            var allowedStatuses = new[]
            {
                OrderStatus.Pending,
            };

            if (!allowStatuses.Contains(order.Status))
            {
                throw new InvalidOrderTransitionException(
                    order.Status,
                    "Không thể cập nhật đơn khi đang vận chuyển");
            }

            var dto = request.Dto;

            order.FromAddressId = dto.FromAddressId;
            order.ToAddressId   = dto.ToAddressId;
            order.ServiceId = dto.ServiceId;

            // Xóa item cũ
            order.Items.Clear();

            // Thêm item mới
            foreach (var item in dto.Items)
            {
                order.Items.Add(new Item
                {
                    Id = Guid.NewGuid(),
                    Image = item.Image,
                    Name = item.Name,
                    Type = item.Type,
                    Quantity = item.Quantity,
                    Weight = item.Weight,
                    Length = item.Length,
                    Width = item.Width,
                    Height = item.Height
                });
            }

            var zone = zoneService.GetZone(
                order.Sender,
                order.Receiver);

            var weight =
                weightService.Calculate(order.Items);

            var price =
                priceService.Calculate(
                    zone,
                    weight);

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