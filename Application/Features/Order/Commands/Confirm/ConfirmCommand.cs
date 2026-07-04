using MediatR;

namespace MYA.Application.Orders.Commands.Confirm;

public sealed record ConfirmCommand(
    List<Guid> OrderIds) : IRequest<BulkResultDto>;