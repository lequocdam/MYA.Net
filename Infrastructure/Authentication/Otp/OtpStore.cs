using System.Text.Json;

public sealed class OtpStore(
    IRedisService redis,
    ICryptoService crypto): IOtpStore
{
    public async Task SaveAsync(
        OtpModel model,
        CancellationToken ct)
    {
        var key = GetKey(model.Id);

        var jsonValue = JsonSerializer.Serialize(model);

        var encryptValue = crypto.Encrypt(jsonValue);

        var ttl = model.ExpiredAt - DateTimeOffset.UtcNow;

        await redis.SetAsync(key, encryptValue, ttl, ct);
    }

    public async Task<OtpCache?> GetAsync(
        string requestId,
        CancellationToken ct)
    {
        var json = await redisService.GetAsync(
            GetKey(requestId),
            ct);

        return json is null
            ? null
            : JsonSerializer.Deserialize<OtpCache>(json);
    }

    public async Task UpdateAsync(
        OtpCache cache,
        CancellationToken ct)
    {
        var ttl = cache.ExpiredAt - DateTimeOffset.UtcNow;

        await redisService.SetAsync(
            GetKey(cache.RequestId),
            JsonSerializer.Serialize(cache),
            ttl,
            ct);
    }

    public Task RemoveAsync(
        string requestId,
        CancellationToken ct)
    {
        return redisService.RemoveAsync(
            GetKey(requestId),
            ct);
    }

    private static string GetKey(string requestId)
        => $"otp:{requestId}";
}