using MYA.Application.Orders.Events;

namespace MYA.Infrastructure.Services;

public sealed class OrderWriteService(
    IOrderRepository orderRepository,
    IAddressSnapshotRepository addressSnapshotRepository,
    IOrderHistoryRepository orderHistoryRepository,
    ITrackingRepository trackingRepository,
    IOutboxRepository outboxRepository) : IOrderWriteService
{
    public async Task<Guid> CreateAsync(
        Order order,
        AddressEntity fromAddressEntity,
        AddressEntity toAddressEntity,
        CancellationToken ct)
    {
        await using var transaction = await orderRepository.BeginTransactionAsync(ct);

        var orderEntity = Order.Create(order);

        await orderRepository.AddAsync(orderEntity, ct);

        await orderRepository.SaveChangesAsync(ct);

        await SaveAddressSnapshotsAsync(fromAddress, toAddress);

        await SaveOrderHistoryAsync(order, ct);

        await SaveTrackingAsync(order, ct);

        await PublishOrderCreatedAsync(order, ct);

        await transaction.CommitAsync(ct);

        return orderEntity.Id;
    }

    private async Task SaveAddressSnapshotsAsync(
        Order order,
        AddressEntity fromAddressEntity,
        AddressEntity toAddressEntity,
        CancellationToken ct)
    {
        var fromAddress = new Address(
            OrderId = order.Id,
            AddressEntity = fromAddressEntity,
        )

        var toAddress = new Address(
            OrderId = order.Id,
            AddressEntity = toAddressEntity
        )
        
        await addressSnapshotRepository.AddAsync(fromAddress, ct);

        await addressSnapshotRepository.AddAsync(toAddress, ct);
    }

    private async Task SaveOrderHistoryAsync(
        Order order,
        CancellationToken ct)
    {
        await orderHistoryRepository.AddAsync(
            OrderHistory.Create(
                order.Id,
                order.UserId,
                order.Status),
            ct);
    }

    private async Task SaveTrackingAsync(
        Order order,
        CancellationToken ct)
    {
        await trackingRepository.AddAsync(
            Tracking.Create(
                order.Id,
                order.Status),
            ct);
    }

    private async Task PublishOrderCreatedAsync(
        Order order,
        CancellationToken ct)
    {
        await outboxRepository.AddAsync(
            OutboxMessage.Create(
                nameof(OrderCreatedEvent),
                new OrderCreatedEvent(order.Id)),
            ct);
    }

    public async Task<BulkResultDto> TransitionAsync(
        IReadOnlyList<Guid> orderIds,
        string trigger, CancellationToken ct)
    {
        var succeededOrders = new List<Guid>();
        var failedOrders = new List<BulkErrorDto>();

        if (orderIds.Count == 0)
            return new BulkResultDto(succeeded, failed);

        await using var transaction = await orderRepository.BeginTransactionAsync(ct);

        var orders = await orderRepository.ListAsync(
            new LoadOrdersSpec(orderIds), ct);

        var foundOrders = orders.ToHashSet();
        failed.AddRange(orderIds
            .Where(id => !foundIds.Contains(id))
            .Select(id => new BulkErrorDto(id, "Không tìm thấy đơn hàng")));

        var now = DateTime.UtcNow;

        foreach (var order in orders)
        {
            try
            {
                order.Transition(trigger);

                await SaveOrderHistorysAsync(order, ct);

                await SaveTrackingAsync(order, ct);

                await PublishOrderCreatedAsync(order, ct);

                succeeded.Add(order.Id);
            }
            catch (InvalidOrderTransitionException e)
            {
                failed.Add(new BulkErrorDto(order.Id, e.Message));
            }
        }

        try
        {
            await orderRepository.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            // Order nào bị conflict (đã bị request khác sửa entre lúc mình
            // xử lý) → tách ra khỏi succeeded, add vào failed, KHÔNG fail
            // toàn bộ batch. Đây là lý do enterprise cần optimistic
            // concurrency thay vì "ai save sau thắng" (lost update).
            var conflictedIds = ex.Entries
                .Select(e => (Guid)e.Property("Id").CurrentValue!)
                .ToHashSet();

            succeeded.RemoveAll(id => conflictedIds.Contains(id));
            failed.AddRange(conflictedIds.Select(id =>
                new BulkErrorDto(id, "Đơn hàng vừa được cập nhật bởi thao tác khác, vui lòng thử lại")));

            foreach (var entry in ex.Entries)
                entry.State = EntityState.Detached;

            await orderRepository.SaveChangesAsync(ct);
        }

        await transaction.CommitAsync(ct);

        // Event chỉ thật sự publish qua background dispatcher đọc Outbox —
        // KHÔNG gọi eventBus.Publish ở đây, tránh lặp lại lỗi "publish sau
        // commit rồi rollback nhầm" đã thấy ở CreateCoreAsync gốc.
        return new BulkResultDto(succeeded, failed);
    }
}