using MediatR;
using Microsoft.Extensions.Logging;
using YourApp.Application.Orders.Abstractions;
using YourApp.Application.Common.Exceptions;

namespace MYA.Application.Orders.Commands.Create;

public sealed class CreateHandler(
    ICurrentUserService currentUserService,
    IAddressRepository addressRepository,
    IWarehouseRoutingService warehouseRoutingService,
    IQuoteService quoteService,
    IWriteOrderService writeOrderService,
    ILogger<CreateOrderCommandHandler> logger) : IRequestHandler<CreateCommand, Guid>
{
    public async Task<Guid> Handle(CreateOrderCommand request, CancellationToken ct)
    {
        var currentUser = currentUserService.GetCurrentUser();

        var fromAddressEntity = await addressRepository.FirstOrDefaultAsync(request.FromAddressId, ct)
            ?? throw new NotFoundException("From address", request.FromAddressId);

        var toAddressEntity = await addressRepository.FirstOrDefaultAsync(request.ToAddressId, ct)
            ?? throw new NotFoundException("To address", request.ToAddressId);

        AddressPolicy.Validate(
            currentUser,
            fromAddress, toAddress);

        var warehouse = await warehouseRoutingService.AssignWarehouseAsync(
            fromAddress,
            request.ServiceId,
            ct);

        var quote = await quoteService.GetAsync(
            request.ServiceId,
            fromAddressEntity,
            toAddressEntity,
            request.Cod,
            request.Items,
            ct);

        var items = request.Items
            .Select(x => new Item(
                x.Name,
                x.Quantity,
                x.Weight,
                x.Length,
                x.Width,
                x.Height))
            .ToList();

        var order = new Order(
            UserId = currentUser.userId,
            WarehouseId = warehouse.Id,
            ServiceId = request.ServiceId,
            Quote = quote,
            Items = items);
        
        try
        {
            var orderId = await writeOrderService.CreateAsync(
                order,
                fromAddressEntity,
                toAddressEntity,
                ct);

            return orderId;
        }
        catch (Exception e)
        {
            logger.LogError(e, "CreateOrder failed. UserId={UserId}", currentUser.Id);
            throw;
        }
    }
}