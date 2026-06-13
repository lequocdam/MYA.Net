public class OrderFilterSpecification
{
    public IQueryable<Order> Apply(
        IQueryable<Order> query,
        OrderFilterDto filter)
    {
        if (!string.IsNullOrWhiteSpace(filter.Code))
        {
            query = query.Where(q =>
                q.Code.Contains(filter.Code));
        }

        if (filter.FromDate.HasValue)
        {
            query = query.Where(q =>
                q.Date >= filter.FromDate.Value);
        }

        if (filter.ToDate.HasValue)
        {
            query = query.Where(q =>
                q.Date <= filter.ToDate.Value);
        }

        if (filter.Status.HasValue)
        {
            query = query.Where(q =>
                q.Status == filter.Status.Value);
        }

        return query;
    }
}