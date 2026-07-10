using MediatR;
using Microsoft.Extensions.Logging;
using YourApp.Application.Orders.Abstractions;
using YourApp.Application.Common.Exceptions;

namespace MYA.Application.Orders.Commands.Create;

public sealed class CreateHandler(
    ICurrentUserService currentUserService,
    IAddressRepository addressRepository,
    IQuoteService quoteService,
    IWriteOrderService writeOrderService,
    ILogger<CreateOrderCommandHandler> logger) : IRequestHandler<CreateCommand, Guid>
{
    public async Task<Guid> Handle(CreateCommand command, CancellationToken ct)
    {
        var currentUser = currentUserService.GetCurrent();

        var fromTask = addressRepository.GetByIdAsync(command.FromAddressId, ct);
        var toTask = addressRepository.GetByIdAsync(command.ToAddressId, ct);

        await Task.WhenAll(fromTask, toTask);

        var fromAddress = await fromTask;
            ?? throw new NotFoundException("From address");
        var fromAddress = await toTask;
            ?? throw new NotFoundException("To address");
            
        AddressPolicy.EnsureActive(fromAddress);
        AddressPolicy.EnsureActive(toAddress);
        AddressPolicy.EnsureDifferent(fromAddress, ToAddress);

        var items = command.Items
            .Select(x => OrderItem.Create(
                x.Name,
                x.WeightKg,
                x.Quantity,
                x.LenghtCm,
                x.WidthCm,
                x.HeightCm))
            .ToList();

        var warehouse = await routingService.AssignWarehouseAsync(new WarehouseSelectionRequest(
            command.FromAddress,
            command.ToAddress),
            ct);

        var price = await quoteService.GetAsync(new QuoteInput(
            command.ServiceId,
            from, to,
            command.Cod,
            command.Items),
            ct);

        var orderContext = new OrderContext(  
            currentUser.Id,
            warehouse.Id,
            command.ServiceId,
            from,
            to,
            price,
            items);

        return await writeService.CreateAsync(orderContext, ct);
    }
}