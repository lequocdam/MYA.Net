using MediatR;

namespace MYA.Application.Orders.Commands.Create;

public sealed record CreateCommand(
    Guid ServiceId,
    Guid FromAddressId,
    Guid ToAddressId,
    decimal CodAmount,
    string? Note,
    List<CreateItemCommand> Items) : IRequest<Guid>;

public sealed record CreateItemCommand(
    string Name,
    int Quantity,
    decimal Weight,
    decimal Length,
    decimal Width,
    decimal Height
);