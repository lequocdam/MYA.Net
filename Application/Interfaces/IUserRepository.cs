public interface IUserRepository
{
    Task<bool> AnyAsync(string phone, string email,
        CancellationToken ct = default);

    Task<User> FirstOrDefaultAsync(Guid userId,
        CancellationToken ct = default);

    Task AddAsync(User user, CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);
}