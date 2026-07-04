using MediatR;
using MYA.Application.Orders.Abstractions;
using MYA.Application.Orders.Policies;

namespace MYA.Application.Orders.Commands.CancelOrders;

public sealed class CancelHandler(
    ICurrentUserService currentUserService,
    IOrderRepository orderRepository,
    IOrderPermissionSpecification permissionSpec,
    IOrderTransitionService orderTransitionService) : IRequestHandler<CancelOrdersCommand, BulkResultDto>
{
    public async Task<BulkResultDto> Handle(CancelCommand request, CancellationToken ct)
    {
        var currentUser = currentUserService.GetCurrentUser();

        var owners = await orderRepository.Query()
            .Where(o => request.OrderIds.Contains(o.Id))
            .Select(o => new { o.Id, o.UserId })
            .ToListAsync(ct);

        var orders = await orderRepository.ListAsync(
            new LoadOrdersSpec(orderIds), ct);

        var authorizedOrders = owners
            .Where(o => permissionSpec.IsAuthorized(o.UserId, currentUser))
            .Select(o => o.Id)
            .ToList();

        var authorizedOrders = await orderRepository.ListAsync(
            new IsAuthorized(orderIds), ct);

        var unauthorized = owners
            .Where(o => !permissionSpec.IsAuthorized(o.UserId, currentUser))
            .Select(o => new BulkErrorDto(o.Id, "Không có quyền hủy đơn hàng này"))
            .ToList();

        var unauthorizedOrders = await orderRepository.ListAsync(
            new !IsAuthorized(orders), ct);

        var notFoundOrders = request.OrderIds
            .Except(owners.Select(o => o.Id))
            .Select(id => new BulkErrorDto(id, "Không tìm thấy đơn hàng"))
            .ToList();

        var result = await orderTransitionService.TransitionAsync(
            authorizedIds,
            OrderTrigger.Cancel,
            currentUser.Id,
            note: $"Hủy đơn. Lý do: {request.Reason}",
            ct);

        var allFailed = result.Failed.Concat(unauthorized).Concat(notFound).ToList();
        return new BulkResultDto(result.Succeeded, allFailed);
    }
}