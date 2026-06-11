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
}