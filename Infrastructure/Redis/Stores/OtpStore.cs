public sealed class OtpStore(IConnectionMultiplexer redis) : IOtpStore
{
    private readonly IDatabase db = redis.GetDatabase();

    public async Task<Otp> GetAsync(
        string id,
        CancellationToken ct)
    {
        var key = $"register-otp:{id}";

        var value = await db.HashGetAllAsync(key);

        if (value.Length == 0)
            return null;

        var codeHash = value
            .FirstOrDefault(x => x.Name == "codeHash")
            .Value
            .ToString();

        var attemptValue = value
            .FirstOrDefault(x => x.Name == "attempt")
            .Value
            .ToString();

        return new Otp
        {
            CodeHash = codeHash,
            Attempt = int.TryParse(
                attemptValue,
                out var attempt)
                    ? attempt
                    : 0
        };
    }

    public async Task IncrementAttemptAsync(
        string id,
        CancellationToken ct)
    {
        await db.HashIncrementAsync(
            GetKey(id),
            "attempt",
            1);
    }

    public async Task<int> IncrementAttemptAsync(
        string id,
        CancellationToken ct)
    {
        var key = $"register-otp:{id}:attempt";

        var attempt = await db.StringIncrementAsync(key);

        if (attempt == 1)
        {
            await db.KeyExpireAsync(key, TimeSpan.FromMinutes(5));
        }

        return (int)attempt;
    }

    public async Task<bool> TryConsumeAsync(
        string id,
        CancellationToken ct)
    {
        var key = $"register-otp:{id}";

        return deleted = await db.KeyDeleteAsync(key);
    }

    public async Task<IAsyncDisposable> AcquireVerifyLockAsync(
        string id,
        CancellationToken ct)
    {
        var lockKey = $"otp:lock:{id}";

        var lockValue = Guid.NewGuid().ToString();

        while (!ct.IsCancellationRequested)
        {
            var acquired = await db.StringSetAsync(
                lockKey,
                lockValue,
                TimeSpan.FromSeconds(10),
                When.NotExists);

            if (acquired)
            {
                return new RedisLock(
                    db,
                    lockKey,
                    lockValue);
            }

            await Task.Delay(
                TimeSpan.FromMilliseconds(50),
                ct);
        }

        ct.ThrowIfCancellationRequested();

        throw new OperationCanceledException(ct);
    }

    private async Task ReleaseLockAsync(string lockKey, string lockToken)
    {
        const string script = @"
            if redis.call('get', KEYS[1]) == ARGV[1] then
                return redis.call('del', KEYS[1])
            else
                return 0
            end";

        await db.ScriptEvaluateAsync(script, new RedisKey[] { lockKey }, new RedisValue[] { lockToken });
    }

    private static string GetKey(string id)
    {
        return $"otp:{id}";
    }
}