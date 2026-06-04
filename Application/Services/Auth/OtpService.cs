public class OTPService(
    IRedisService redisService,
    ICryptoService crypto,
    IEmailService email,
    ILogger<OTPService> logger) : IOTPService
{
    public async Task<string> SendOTPAsync(RegisterDTO dto, CancellationToken ct)
    {
        var cooldownKey = $"register:cooldown:{dto.Email}";
        var exist = await redisService.SetWhenNotExistsAsync(cooldownKey, "1", TimeSpan.FromSeconds(60));
        if (!exist)
            throw new TooManyRequestsException("");

        var otp = GenerateOTP();
        var hash = BCrypt.HashPassword(otp),
        var key = $"otp:register:{dto.Email}";
        var value = new UserCache
        {
            Name = dto.Name,
            Phone = dto.Phone,
            Email = dto.Email,
            Password = dto.Password,
            OTP = hash,
        };
        var json = JsonSerializer.Serialize(value);
        var encrypt = crypto.Encrypt(json);

        await redisService.SetAsync(key, encrypt, TimeSpan.FromMinutes(5), ct);
        logger.LogInformation("OTP sent to cache");

        await email.SendOTPToEmailAsync(value.Email, value.Name, value.OTP, ct);
        logger.LogInformation($"OTP sent to {value.Email}");

        return MaskEmail(value.Email);
    }

    public async Task<UserCache> VerifyOTPAsync(OtpDTO dto, CancellationToken ct)
    {
        var attemptKey = $"otp:register:attempt:{dto.Email}";
        var attempts = await redisService.IncrementAsync(attemptKey, ct);
        if (attempts == 1)
            await redisService.ExpireAsync(attemptKey, TimeSpan.FromMinutes(15), ct);

        if (attempts > 5)
        {
            await redisService.DeleteOTPAsync(dto.Email, ct);
            throw new TooManyRequestsException("Too many attempts");
        }

        var key = $"otp:register:{dto.Email}";
        var decrypt = await redisService.GetAsync(key);
        if (decrypt is null)
            throw new BadRequestException("OTP not found or expired");

        var value = JsonSerializer.Deserialize<UserCache>(crypto.Decrypt(decrypt);) 
            ?? throw new BadRequestException("Invalid data");
        if (value.OTP != dto.OTP)
            throw new BadRequestException($"Invalid OTP. {5 - attempts} attempts");

        return value;
    }

    public async Task DeleteOTPAsync(string email, CancellationToken ct)
    {
        var key = $"otp:register:{email}";
        var attemptKey = $"otp:register:attempt:{email}";

        await redisService.DeleteAsync(key, ct);
        await redisService.DeleteAsync(attemptKey, ct);
    }

    private static string GenerateOTP()
    {
        var value = RandomNumberGenerator.GetInt32(0, (int)Math.Pow(10, 6));
        return value.ToString($"D{6}");
    }

    private static string MaskEmail(string email)
    {
        var parts = email.Split('@');
        var part = part[0];

        var mask = part.Length <= 2
            ? new string('*', part.Length)
            : part[0] + new string('*', part.Length - 1);

        return $"{mask}@{parts[1]}";
    }
}