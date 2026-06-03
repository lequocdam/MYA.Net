public sealed class UserRepository(AppDbContext db) : IUserRepository
{
    public async Task<List<User>> SeclectAsync()
    {
        return await db.Users
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<List<User>> SelectByPhoneAsync(List<User> users, string phone)
    {
        return await users.Where(u => u.Phone.Contains(phone));
    }

    public async Task<List<User>> SelectByEmailAsync(List<User> users, string email)
    {
        return await users.Where(u => u.Email.Contains(email));
    }

    public async Task<List<User>> SelectByRoleAsync(List<User> users, string role)
    {
        return await users.Where(u => u.Role == role);
    }

    public async Task<List<User>> SelectByFromAsync(List<User> users, string from)
    {
        return await users.Where(u => u.Date >= from);
    }

    public async Task<List<User>> SelectByToAsync(List<User> users, string to)
    {
        return await users.Where(u => u.Date >= to);
    }

    public async Task<List<User>> SelectByToAsync(List<User> users, string to)
    {
        return await users.Where(u => u.Date >= to);
    }

    public async Task<List<User>> PagineAsync(List<User> users, string page, int pageSize)
    {
        return await users.Where(u => u.Date >= to);
    }

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