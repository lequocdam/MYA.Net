using MediatR;

public class ChangeServiceHandler(
    IOrderRepository orderRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<ChangeServiceCommand>
{
    public async Task Handle(
        ChangeServiceCommand command,
        CancellationToken ct)
    {
            var orderAggregate = await orderRepository.FindByIdAsync(command.Id, ct);
                ?? throw new NotFoundException("Order not found");

            var fromTask = addressRepository.FindByIdAsync(command.FromId, ct);
            var toTask = addressRepository.FindByIdAsync(command.ToId, ct);

            await Task.WhenAll(fromTask, toTask);

            var from = await fromTask;
            var to = await toTask;

            var order = new Order(  
                command.ServiceId,
                from,
                to,
                warehouse.Id);

            orderAggregate.Update(order);

            await orderRepository.SaveChangesAsync(ct);
    }
}