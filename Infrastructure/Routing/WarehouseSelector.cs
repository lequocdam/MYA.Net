public sealed class WarehouseSelector(
    IWarehouseRepository repository,
    WarehouseSelectionPolicy policy) : IWarehouseSelector
{
    public async Task<Warehouse> SelectPickupWarehouseAsync(
        Address pickupAddress,
        Guid serviceId, CancellationToken ct)
    {
        var query = new CandidateQuery(
            pickupAddress.CityId,
            pickupAddress.WardId,
            serviceId);

        var candidates = await repository.GetPickupCandidatesAsync(query, ct);

        return policy.Select(candidates);
    }

    public async Task<Warehouse> SelectDeliveryWarehouseAsync(
        Address address,
        Guid serviceId,
        CancellationToken ct)
    {
        var candidates = await repository.GetDeliveryCandidatesAsync(
            address,
            serviceId,
            ct);

        return policy.Select(candidates);
    }
}