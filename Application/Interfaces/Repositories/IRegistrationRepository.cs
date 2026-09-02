public interface IRegistrationRepository
{
    Task<Registration?> GetByIdAsync(
        Guid id,
        CancellationToken ct);

    Task<bool> ExistAsync(
        string email,
        string phone,
        CancellationToken ct);

    Task AddAsync(Registration registration, CancellationToken ct);
}