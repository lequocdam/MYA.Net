public sealed class RoutingService(
    IWarehouseSelector warehouseSelector,
    IHubPlanner hubPlanner) : IRoutingService
{
    public async Task<RoutePlan> PlanRouteAsync(RouteRequest request, CancellationToken ct)
    {
        var fromWarehouse = await warehouseSelector.SelectFromWarehouseAsync(request.FromAddress, request.ServiceId, ct);

        var toWarehouse = await warehouseSelector.SelectToWarehouseAsync(request.ToAddress, request.ServiceId, ct);

        var hubs = await hubPlanner.PlanAsync(
            pickup,
            delivery,
            ct);

        return RoutePlan.Create(
            pickup.Id,
            delivery.Id,
            hubs);
    }
}