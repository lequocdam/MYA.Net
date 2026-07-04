using MediatR;

namespace MYA.Application.Orders.Commands.Create;

public sealed record CreateOrderCommand(
    Guid ServiceId,
    Guid FromAddressId,
    Guid ToAddressId,
    bool? Cod,
    List<CreateItemCommand> Items) : IRequest<Guid>;