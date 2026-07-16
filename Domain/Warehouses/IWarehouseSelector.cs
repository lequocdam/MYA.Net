public interface IWarehouseSelector
{
    Task<Warehouse> SelectPickupAsync(
        AddressSnapshot address,
        Guid serviceId,
        CancellationToken ct);

    Task<Warehouse> SelectDeliveryAsync(
        AddressSnapshot address,
        Guid serviceId,
        CancellationToken ct);
}