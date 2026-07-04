using MediatR;

namespace MYA.Application.Orders.Commands.Cancel;

public sealed record CancelCommand(
    List<Guid> OrderIds,
    string Reason) : IRequest<BulkResultDto>;