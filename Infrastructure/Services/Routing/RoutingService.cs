public sealed class RoutingService
{
    public async Task<WarehouseEntity> AssignWarehouseAsync(Order order)
    {
        return await warehouseSelector.SelectAsync(order);
    }

    Task<DeliveryRoute> PlanDeliveryAsync(
        IReadOnlyCollection<Order> orders,
        CancellationToken ct);

    Task<TransitRoute> PlanTransitAsync(
        Guid fromWarehouseId,
        Guid toWarehouseId,
        IReadOnlyCollection<Order> orders,
        CancellationToken ct);
}