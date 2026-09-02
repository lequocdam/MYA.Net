public interface IUserRepository
{
    Task<Registration?> GetByIdAsync(
        Guid id,
        CancellationToken ct);

    Task<User?> GetByContactAsync(
        string email,
        string phone,
        CancellationToken ct);

    Task<bool> ExistAsync(
        string email,
        string phone,
        CancellationToken ct);

    Task AddAsync(
        User user,
        CancellationToken ct);
}