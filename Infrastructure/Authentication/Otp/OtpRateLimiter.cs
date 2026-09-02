using StackExchange.Redis;

public class OtpRateLimiter(IConnectionMultiplexer redis)
{
    private readonly IDatabase db = redis.GetDatabase();

    public async Task RateLimitAsync(string email, CancellationToken ct)
    {
        var cooldownKey = $"ratelimit:otp:cooldown:{email}";
        var limitKey = $"ratelimit:otp:limit:{email}";

        var exists = await db.KeyExistsAsync(cooldownKey);
        if (exists)
        {
            throw new TooManyRequestsException("");
        }

        var count = await db.StringIncrementAsync(limitKey);
        if (count == 1)
        {
            await db.KeyExpireAsync(limitKey, TimeSpan.FromMinutes(10));
        }

        if (count > 5)
        {
            throw new TooManyRequestsException("");
        }

        await db.StringSetAsync(cooldownKey, "1", TimeSpan.FromSeconds(60));
    }
}