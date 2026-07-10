public sealed class UserRepository(AppDbContext db) : IUserRepository
{
    public async Task<bool> AnyAsync(string phone, string email,
        CancellationToken ct = default)
    {
        return await db.Users.AnyAsync(
            u => u.Phone == phone || u.Email == email,
            ct);
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