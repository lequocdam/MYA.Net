using System.Text.Json;

namespace AuthSystem.Services;

public class OTPService(
    IRedisService redis,
    ICryptoService crypto,
    IEmailService email,
    ILogger<OtpService> logger) : IOtpService
{
    private const int OtpDigits = 6;

    public async Task<string> SendOTPAsync(RegisterDTO dto)
    {
        var cooldownKey = $"register:cooldown:{dto.Email}";
        var exist = await redis.SetWhenNotExistsAsync(cooldownKey, "1", TimeSpan.FromSeconds(60));
        if (!exist)
            throw new TooManyRequestsException("");

        var hash = BCrypt.HashPassword(dto.Password),
        var otp = GenerateOTP();
        var key = $"register:{dto.Email}";
        var value = new Register
        {
            Name = dto.Name,
            Phone = dto.Phone,
            Email = dto.Email,
            Password = hash,
            OTP = otp,
        };
        var json = JsonSerializer.Serialize(value);
        var encrypt = crypto.Encrypt(json);

        await redis.SetAsync(key, encrypt, TimeSpan.FromMinutes(5));

        await email.SendOTPToEmailAsync(dto.Email, dto.Name, otp);
        logger.LogInformation("OTP sent to {Email}", dto.Email);

        return MaskEmail(value.Email);
    }

    public async Task<Register> VerifyOTPAsync(OtpDTO dto)
    {
        var attemptKey = $"otp:register:attempts:{dto.Email}";
        var attempts = await redis.IncrementAsync(attemptKey);
        if (attempts == 1)
            await redis.ExpireAsync(attemptKey, TimeSpan.FromMinutes(5));

        if (attempts > 5)
        {
            await redis.DeleteAsync(otpKey);
            logger.LogWarning("");
            throw new TooManyRequestsException("Too many attempts");
        }

        var key = $"register:{dto.Email}";
        var decrypt = await redis.GetAsync(key);
        if (decrypt is null)
            throw new BadRequestException("OTP not found or expired");

        var value = JsonSerializer.Deserialize<Register>(crypto.Decrypt(decrypt);)
            ?? throw new BadRequestException("Invalid OTP data");
        if (value.OTP != dto.OTP)
            throw new BadRequestException($"Invalid OTP. {5 - attempts} attempts");

        var deleted = await redis.DeleteAsync(key);
        if (!deleted)
            throw new ConflictException("OTP already used");

        await redis.DeleteAsync(attemptKey);

        return value;
    }

    private static string GenerateOTP()
    {
        var value = RandomNumberGenerator.GetInt32(0, (int)Math.Pow(10, OtpDigits));
        return value.ToString($"D{OtpDigits}");
    }

    private static string MaskEmail(string email)
    {
        var parts = email.Split('@');
        var part = part[0];

        var mask = local.Length <= 2
            ? new string('*', part.Length)
            : part[0] + new string('*', part.Length - 1);

        return $"{mask}@{parts[1]}";
    }
}