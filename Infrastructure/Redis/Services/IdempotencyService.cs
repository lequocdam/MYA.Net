using StackExchange.Redis;
using System.Text.Json;

public sealed class IdempotencyService(
    IConnectionMultiplexer redis,
    ILogger<IdempotencyService> logger) : IIdempotencyService
{
    private readonly IDatabase db = redis.GetDatabase();

    private const string processing = "Processing";

    private static readonly TimeSpan processingTtl = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan ttl = TimeSpan.FromHours(24);

    public async Task<TResponse?> GetAsync<TResponse>(
        string operation,
        string idempotencyKey,
        CancellationToken ct)
    {
        var key = GetKey(operation, idempotencyKey);

        try
        {
            var value = await db.StringGetAsync(key);

            if (value.HasValue)
            {
                if (value == processing)
                {
                    throw new ConflictException("This idempotency is processed.");
                }

                return JsonSerializer.Deserialize<TResponse>(value);
            }

            await db.StringSetAsync(
                key,
                processing,
                processingTtl,
                When.NotExists);

            return default;
        }
        catch (RedisException ex)
        {
            logger.LogError(ex, "Service unavailable to get idempotency for key {Key}", key);
            throw new ServiceUnavailableException("Service is unavailable.", ex);
        }
    }

    public async Task SetAsync<TResponse>(
        string operation,
        string idempotencyKey,
        TResponse response,
        CancellationToken ct)
    {
        var key = GetKey(operation, idempotencyKey);
        var value = JsonSerializer.Serialize(response);

        try
        {
            await db.StringSetAsync(key, value, ttl);
        }
        catch (RedisException ex)
        {
            logger.LogError(ex, "Failed to add idempotency for key {Key}", key);
        }
    }

    public async Task DeleteAsync(string operation, string idempotencyKey, CancellationToken ct)
    {
        var key = GetKey(operation, idempotencyKey);

        try
        {
            await db.KeyDeleteAsync(key);
        }
        catch (RedisException ex)
        {
            logger.LogError(ex, "Failed to delete idempotency for key {Key}", key);
        }
    }

    private static string GetKey(string operation, string idempotencyKey) =>
        $"idempotency:{operation}:{idempotencyKey}";
}