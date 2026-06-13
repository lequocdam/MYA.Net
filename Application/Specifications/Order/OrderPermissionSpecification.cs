public class OrderPermissionSpecification
{
    public IQueryable<Order> Apply(
        IQueryable<Order> query,
        Guid userId,
        string role,
        Guid? warehouseId)
    {
        if (role == "Admin")
        {
            return query;
        }

        if (role == "Manager")
        {
            return query.Where(q =>
                q.WarehouseId == warehouseId);
        }

        return query.Where(x =>
            q.UserId == userId);
    }
}