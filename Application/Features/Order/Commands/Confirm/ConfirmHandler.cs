using MediatR;
using MYA.Application.Common;
using MYA.Application.Orders.Abstractions;

namespace MYA.Application.Orders.Commands.Confirm;

public sealed class ConfirmHandler(
    ICurrentUserService currentUserService,
    IOrderTransitionService orderTransitionService
) : IRequestHandler<TransitionOrdersCommand, BulkResultDto>
{
    public async Task<BulkResultDto> Handle(TransitionOrdersCommand request, CancellationToken ct)
    {
        var currentUser = currentUserService.GetCurrentUser();

        return await orderTransitionService.TransitionAsync(
            OrderTrigger.Confirm,
            request.OrderIds, ct);
    }
}