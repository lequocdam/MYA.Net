using MediatR;

public record ChangeServiceCommand(
    Guid Id,
    Guid ServiceId) : IRequest;