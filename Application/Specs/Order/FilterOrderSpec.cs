public sealed class FilterOrderSpec : IFilterOrderSpec
{
    public IQueryable<Order> Apply(
        IQueryable<Order> query,
        OrderFilterDto filter)
    {
        if (filter.WarehouseId.HasValue)
        {
            query = query.Where(x =>
                x.WarehouseId == filter.WarehouseId);
        }

        if (filter.ServiceId.HasValue)
        {
            query = query.Where(x =>
                x.ServiceId == filter.ServiceId);
        }

        if (!string.IsNullOrWhiteSpace(filter.Code))
        {
            query = query.Where(x =>
                x.Code.Contains(filter.Code));
        }

        if (filter.Status.HasValue)
        {
            query = query.Where(x =>
                x.Status == filter.Status.Value);
        }

        if (filter.FromDate.HasValue)
        {
            query = query.Where(x =>
                x.Date >= filter.FromDate.Value);
        }

        if (filter.ToDate.HasValue)
        {
            query = query.Where(x =>
                x.Date <= filter.ToDate.Value);
        }

        return query;
    }
}