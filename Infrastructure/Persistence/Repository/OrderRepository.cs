public sealed class UserRepository(AppDbContext db) : IUserRepository
{
    public IQueryable<Order> Query()
    {
        return db.Orders
            .AsQueryable();
            .AsNoTracking();
    }

    public async Task<User> FirstOrDefaultAsync(Guid userId,
        CancellationToken ct = default)
    {
        return await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
    }

    public async Task AddAsync(User user, CancellationToken ct = default)
    {
        await db.Users.AddAsync(user, ct);
    }

    public Task SaveChangesAsync(CancellationToken ct = default)
        => db.SaveChangesAsync(ct);

    public async Task<OrderPageDto> GetPageAsync(
        FilterOrderDto filter,
        CurrentUser currentUser,
        CancellationToken ct)
    {
        var query = db.Orders.AsNoTracking();

        query = orderPermissionSpec.Apply(query, currentUser);

        query = orderFilterSpec.Apply(query, filter);

        var total = await query.CountAsync(ct);

        var orders = await query
            .OrderByDescending(x => x.Date)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(x => new OrderDto(
                x.Id,
                x.UserId,
                x.WarehouseId,
                x.ServiceId,
                x.Code,
                x.Date,
                x.Status,
                x.Total))
            .ToListAsync(ct);

        return new OrderPageDto(
            filter.Page,
            filter.PageSize,
            total,
            orders);
    }

    public async Task<OrderByIdDto?> GetByIdlAsync(
        Guid orderId,
        CurrentUser user,
        CancellationToken ct)
    {
        return await db.Orders
        .AsNoTracking()
        .Include(x => x.FromAddressSnapshot)
        .Include(x => x.ToAddressSnapshot)
        .Include(x => x.Items)
        .FirstOrDefaultAsync(x => x.Id == orderId, ct);
    }
}