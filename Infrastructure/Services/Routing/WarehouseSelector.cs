public sealed class WarehouseSelector(
    IZoneResolver zoneResolver,
    IWarehouseRepository warehouseRepository,
    IWarehouseFilter warehouseFilter,
    IWarehouseRankingPolicy warehouseRankingPolicy)
    : IWarehouseSelector
{
    public async Task<Warehouse> SelectAsync(
        Order order,
        CancellationToken ct)
    {
        var fromAddressEntity = await addressRepository.FirstOrDefaultAsync(order.FromAddressId, ct)
            ?? throw new NotFoundException("From address", request.FromAddressId);

        var zone = await zoneResolver.ResolveAsync(
            order.FromAddress,
            ct);

        var warehouses = await warehouseRepository.FindWarehousesByAsync(zone, order.ServiceId, ct);

        if (warehouses.Count == 0)
            throw new BusinessException("Not found.");

        // 3. Filter
        var available = warehouseFilter.Filter(
            candidates,
            order);

        if (available.Count == 0)
            throw new BusinessException("No available warehouse.");

        // 4. Rank
        var warehouse = warehouseRankingPolicy.Select(
            available,
            order);

        // 5. Return
        return warehouse;
    }
}