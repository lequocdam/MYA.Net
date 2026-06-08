using Microsoft.EntityFrameworkCore;

public sealed class UserRepository(AppDbContext db): IUserRepository
{
    public IQueryable<User> Query()
    {
        return db.Users.AsQueryable();
    }

    public async Task<User?> SelectByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);
    }

    public async Task<bool> SelectByPhoneOrEmailAsync(string phone, string email, CancellationToken ct = default)
    {
        return await db.Users.AnyAsync(u => u.Phone == phone || u.Email == email, ct);
    }

    public async Task AddAsync(User user, CancellationToken ct = default)
    {
        await db.Users.AddAsync(user, ct);
    }

    public Task SaveChangesAsync(CancellationToken ct = default)
    {
        return db.SaveChangesAsync(ct);
    }
}