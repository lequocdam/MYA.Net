public sealed class UserRepository(AppDbContext dbContext) : IUserRepository
{
    public async Task<bool> CheckExistsByContactAsync(
        string email, 
        string phone,
        CancellationToken ct = default)
    {
        return await dbContext.Users.AnyAsync(u =>
            u.Phone == phone || 
            u.Email == email,
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

    public Task CheckExistsByContactAsync(
        string email,
        string phone,
        CancellationToken ct = default)
        => db.SaveChangesAsync(ct);
}