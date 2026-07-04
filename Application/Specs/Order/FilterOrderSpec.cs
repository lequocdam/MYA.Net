public sealed class OrderFilterSpecification : IOrderFilterSpecification
{
    public IQueryable<Order> Apply(
        IQueryable<Order> query,
        OrderFilterDto filter)
    {
        ArgumentNullException.ThrowIfNull(filter);

        if (filter.WarehouseId.HasValue)
        {
            query = query.Where(x =>
                x.WarehouseId == filter.WarehouseId.Value);
        }

        if (filter.ServiceId.HasValue)
        {
            query = query.Where(x =>
                x.ServiceId == filter.ServiceId.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.Code))
        {
            var code = filter.Code.Trim();
            query = query.Where(x =>
                EF.Functions.Like(x.Code, $"%{code}%"));
        }

        if (filter.Status.HasValue)
        {
            query = query.Where(x =>
                x.Status == filter.Status.Value);
        }

        if (filter.FromDate.HasValue)
        {
            var from = filter.FromDate.Value.Date;
            query = query.Where(x => x.Date >= from);
        }

        if (filter.ToDate.HasValue)
        {
            // Bao trọn hết ngày ToDate (đến 23:59:59.999)
            var to = filter.ToDate.Value.Date.AddDays(1);
            query = query.Where(x => x.Date < to);
        }

        return query.OrderByDescending(x => x.Date);
    }
}