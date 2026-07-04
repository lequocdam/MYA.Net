using MediatR;

public record UpdateOrderCommand(
    UpdateOrderDto Dto,
    Guid OrderId,
    Guid UserId) : IRequest;