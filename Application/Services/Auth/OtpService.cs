public class OtpService(
    IRedisService redisService,
    ICryptoService crypto,
    IEmailService email,
    ILogger<OTPService> logger) : IOTPService
{
    public async Task SendOtpAsync(RegisterDTO dto, CancellationToken ct)
    {
        var cooldownKey = $"register:cooldown:{dto.Email}";
        var exist = await redisService.SetWhenNotExistsAsync(cooldownKey, "1", TimeSpan.FromSeconds(60));
        if (!exist)
            throw new TooManyRequestsException("");

        var otp = GenerateOTP();
        
        var hash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
        var key = $"otp:register:{dto.Email}";
        var value = new UserCache
        {
            Name = dto.Name,
            Phone = dto.Phone,
            Email = dto.Email,
            Password = hash,
            Otp = otp,
        };

        var encrypt = crypto.Encrypt(JsonSerializer.Serialize(value));

        await redisService.SetAsync(key, encrypt, TimeSpan.FromMinutes(5), ct);
        logger.LogInformation("OTP sent to cache");

        await email.SendOTPToEmailAsync(value.Email, value.Name, value.OTP, ct);
        logger.LogInformation($"OTP sent to {value.Email}");
    }

    public async Task<UserCache> VerifyOtpAsync(OtpDto dto, CancellationToken ct)
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
        var decrypt = await redisService.GetAsync(key, ct);
        if (decrypt is null)
            throw new BadRequestException("OTP expired");

        var value = JsonSerializer.Deserialize<UserCache>(crypto.Decrypt(decrypt)); 
        if (value.Otp != dto.Otp)
            throw new BadRequestException($"Invalid OTP");

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