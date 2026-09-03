public class UserChangeRepository(AppDbContext context) : RepositoryBase<UserChange>, IUserChangeRepository
{
    public async Task<bool> ExistAsync(
        string email,
        string phone,
        CancellationToken ct)
    {
        return await context.UserChanges.AnyAsync(u =>
            u.Email == email ||
            u.Phone == phone,
            ct);
    }

    public async Task AddAsync(
        UserChange userChange,
        CancellationToken ct)
    {
        await context.UserChanges.AddAsync(userChange, ct);
    }
}