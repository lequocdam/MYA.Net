public interface ILoginAttemptStore
{
    Task<Lockout> RegisterFailedAttemptAsync(Guid userId, CancellationToken ct);

    Task<Lockout> GetLockoutAsync(Guid userId, CancellationToken ct);

    Task ResetAsync(Guid userId, CancellationToken ct);
}