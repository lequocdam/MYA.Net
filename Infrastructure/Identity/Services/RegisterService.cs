using System.Text.Json;

public sealed class RegisterService(
    IRedisService redis,
    ICryptoService crypto) : IRegisterService
{
    private static readonly TimeSpan Expiration = TimeSpan.FromMinutes(5);

    public async Task SaveAsync(
        RegisterModel model,
        CancellationToken ct)
    {
        var key = GetKey(model.Id);

        var jsonValue = JsonSerializer.Serialize(model);

        var encryptValue = crypto.Encrypt(jsonValue);

        await redis.SetAsync(key, encryptValue, Expiration, ct);
    }

    public async Task<RegisterCache?> GetAsync(
        string requestId,
        CancellationToken ct)
    {
        var key = GetKey(requestId);

        var encrypted = await redisService.GetAsync(key, ct);

        if (encrypted is null)
            return null;

        var json = cryptoService.Decrypt(encrypted);

        return JsonSerializer.Deserialize<RegisterCache>(json);
    }

    public async Task RemoveAsync(
        string requestId,
        CancellationToken ct)
    {
        await redisService.RemoveAsync(
            GetKey(requestId),
            ct);
    }

    private static string GetKey(string requestId)
        => $"register:{requestId}";
}