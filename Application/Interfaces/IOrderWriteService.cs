namespace MYA.Application.Interfaces;

public interface IOrderWriteService
{
    Task<Order> CreateAsync(
        CurrentUser currentUser,
        Warehouse warehouse,
        Guid serviceId,
        Quote quote,
        List<Item> items,
        Address fromAddress,
        Address toAddress,
        CancellationToken ct);
}

    Task<BulkResultDto> TransitionAsync(
        IReadOnlyList<Guid> orderIds,
        OrderTrigger trigger,
        Guid actorUserId,
        string note,
        CancellationToken ct);