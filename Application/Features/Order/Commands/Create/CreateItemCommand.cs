using MediatR;

namespace MYA.Application.Orders.Commands.Create;

public sealed record CreateItemCommand(
    string Name,
    int Quantity,
    decimal Weight,
    decimal Length,
    decimal Width,
    decimal Height
);