using StackExchange.Redis;

namespace AuthSystem.Services;

public interface IRedisService
{
    Task<bool> SetAsync(string key, string value, TimeSpan? expiry = null);
    Task<bool> SetIfNotExistsAsync(string key, string value, TimeSpan expiry);
    Task<string?> GetAsync(string key);
    Task<bool> DeleteAsync(string key);
    Task<bool> ExistsAsync(string key);
    Task<long> IncrementAsync(string key);
    Task<bool> ExpireAsync(string key, TimeSpan expiry);
    Task<bool> SetWithAtomicExpireAsync(string key, string value, TimeSpan expiry);
}

public class RedisService(IConnectionMultiplexer cmr) : IRedisService
{
    private readonly IDatabase _db = cmr.GetDatabase();

    public Task<bool> SetAsync(string key, string value, TimeSpan? expiry = null)
        => _db.StringSetAsync(key, value, expiry);

    /// <summary>
    /// SET NX EX — atomic set-if-not-exists với expiry. Dùng cho cooldown / rate limit.
    /// </summary>
    public Task<bool> SetIfNotExistsAsync(string key, string value, TimeSpan expiry)
        => _db.StringSetAsync(key, value, expiry, When.NotExists);

    public async Task<string?> GetAsync(string key)
    {
        var value = await _db.StringGetAsync(key);
        return value.IsNullOrEmpty ? null : value.ToString();
    }

    public Task<bool> DeleteAsync(string key)
        => _db.KeyDeleteAsync(key);

    public Task<bool> ExistsAsync(string key)
        => _db.KeyExistsAsync(key);

    public Task<long> IncrementAsync(string key)
        => _db.StringIncrementAsync(key);

    public Task<bool> ExpireAsync(string key, TimeSpan expiry)
        => _db.KeyExpireAsync(key, expiry);

    /// <summary>
    /// Increment + set expiry chỉ khi là lần đầu (atomic via Lua).
    /// Tránh race condition của increment-then-expire tách biệt.
    /// </summary>
    public async Task<bool> SetWithAtomicExpireAsync(string key, string value, TimeSpan expiry)
    {
        const string lua = @"
            local current = redis.call('INCR', KEYS[1])
            if current == 1 then
                redis.call('EXPIRE', KEYS[1], ARGV[1])
            end
            return current";

        var result = await _db.ScriptEvaluateAsync(lua,
            keys: [key],
            values: [(int)expiry.TotalSeconds]);

        return (long)result <= 5; // trả false nếu vượt giới hạn
    }
}