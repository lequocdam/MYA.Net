public class LoginAttemptStore(IConnectionMultiplexer redis) : ILoginAttemptStore
{
    private readonly IDatabase db = redis.GetDatabase();
    private static readonly TimeSpan attemptWindow = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan lockoutDuration = TimeSpan.FromMinutes(15);
    private const int maxAttempts = 5;

    public async Task<Lockout> RegisterFailedAttemptAsync(Guid userId, CancellationToken ct)
    {
        var key = GetAttemptKey(userId);
        var lockKey = GetLockKey(userId);

        var attempCount = await db.StringIncrementAsync(key);

        if (attempCount == 1)
            await db.KeyExpireAsync(key, attemptWindow);

        if (attempCount >= maxAttempts)
        {
            await db.StringSetAsync(lockKey, "1", lockoutDuration);

            return new Lockout
            {
                IsLocked = true,
                LockedUntil = DateTime.UtcNow.Add(lockoutDuration),
                FailedAttempts = (int)attempCount,
            };
        }

        return new Lockout
        {
            IsLocked = false,
            FailedAttempts = (int)attempCount,
        };
    }

    public async Task<Lockout> GetLockoutAsync(Guid userId, CancellationToken ct)
    {
        var key = GetAttemptKey(userId);
        var lockKey = GetLockKey(userId);
        var locked = await db.KeyExistsAsync(lockKey);
        var ttl = locked ? await db.KeyTimeToLiveAsync(lockKey) : null;
        var lockedUntil = ttl.HasValue ? DateTime.UtcNow.Add(ttl.Value): null;
        var attempCount = await GetCountAttemptAsync(key);

        return new Lockout
        {
            IsLocked = locked,
            LockedUntil = lockedUntil,
            FailedAttempts = (int)attempCount,
        };
    }

    public async Task ResetAsync(Guid userId, CancellationToken ct)
    {
        var key = GetAttemptKey(userId);
        var lockKey = GetLockKey(userId);

        await db.KeyDeleteAsync(key);
        await db.KeyDeleteAsync(lockKey);
    }

    private async Task<int> GetCountAttemptAsync(string attemptKey)
    {
        var raw = await db.StringGetAsync(attemptKey);

        return raw.IsNull ? 0 : (int)raw;
    }

    private static string GetAttemptKey(Guid userId) => $"attempt:login:{userId}";
    private static string GetLockKey(Guid userId) => $"lock:login:{userId}";
}