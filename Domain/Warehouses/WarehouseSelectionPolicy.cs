public sealed class WarehouseSelectionPolicy
{
    public Warehouse Select(IReadOnlyCollection<Warehouse> warehouses)
    {
        var warehouse = warehouses
            .Where(x => x.IsActive)
            .Where(x => x.HasCapacity())
            .OrderByDescending(x => x.Priority)
            .ThenBy(x => x.CurrentLoad)
            .FirstOrDefault();

        return warehouse ?? throw new BusinessException("No warehouse available");
    }
}