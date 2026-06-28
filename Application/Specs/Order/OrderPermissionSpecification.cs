public class OrderPermissionSpecification
{
    public IQueryable<Order> Apply(
        IQueryable<Order> query,
        Guid userId,
        string role,
        Guid? warehouseId)
    {
        if (role == Role.ADMIN)
        {
            return query;
        }

        if (role == Role.MANAGER)
        {
            return query.Where(q =>
                q.WarehouseId == warehouseId);
        }

        return query.Where(x =>
            q.UserId == userId);
    }

    public bool CanAccess(
        Order order,
        CurrentUser currentUser)
    {
        return currentUser.Role switch
        {
            Roles.Admin => true,

            Roles.Staff =>
                order.WarehouseId == currentUser.WarehouseId,

            _ =>
                order.UserId == currentUser.UserId
        };
    }
}