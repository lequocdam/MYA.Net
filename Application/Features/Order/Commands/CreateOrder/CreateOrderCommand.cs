using MediatR;

public record CreateOrderCommand(
    CreateOrderDto Dto,
    Guid UserId
) : IRequest<OrderDto>;