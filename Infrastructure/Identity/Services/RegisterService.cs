using System.Text.Json;

public sealed class RegisterStore(
    IRedisService redisService,
    ICryptoService cryptoService) : IRegisterService
{
    private static readonly TimeSpan expiration = TimeSpan.FromMinutes(5);

    public async Task SaveAsync(Register register, CancellationToken ct)
    {
        var key = GetKey(register.Id);

        var jsonValue = JsonSerializer.Serialize(register);

        var EncryptedValue = cryptoService.Encrypt(jsonValue);

        await redisService.SetAsync(key, EncryptedValue, expiration, ct);
    }

    public async Task SaveAsync(PendingRegisterDto pendingRegister, CancellationToken ct)
    {
        var db = redisConnection.GetDatabase();

        // Chuẩn bị dữ liệu cho Redis Stream Event
        var streamEntries = new NameValueEntry[]
        {
            new("registerId", pendingRegister.RegisterId.ToString()),
            new("target", pendingRegister.Email),
            new("otpCode", pendingRegister.OtpCode),
            new("createdAt", DateTime.UtcNow.ToString("o"))
        };

        var transaction = db.CreateTransaction();

        // 1. Lưu Register pending data
        _ = tran.StringSetAsync(
            $"register:{pendingRegister.RegisterId}",
            JsonSerializer.Serialize(pendingRegister),
            RegistrationExpiration
        );

        // 2. Lưu OTP Hash (Nếu muốn tách rời OTP để verify)
        _ = tran.StringSetAsync(
            $"otp:{pendingRegister.RegisterId}",
            pendingRegister.OtpCode, // Hoặc Hash của OtpCode
            OtpExpiration
        );

        // 3. Đẩy Event vào Redis Stream (Tương đương XADD)
        _ = tran.StreamAddAsync(StreamKey, streamEntries);

        // Thực thi transaction
        bool committed = await tran.ExecuteAsync();
        if (!committed)
        {
            throw new InvalidOperationException("Failed to commit pending registration in Redis.");
        }
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

    private static string GetKey(string id)
        => $"register:{id}";
}