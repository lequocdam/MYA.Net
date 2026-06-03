using System.Security.Cryptography;
using System.Text.Json;

namespace AuthSystem.Services;

public class OtpService(
    IRedisService redis,
    ICryptoService crypto,
    ILogger<OtpService> logger) : IOtpService
{
    private const int OtpDigits = 6;

    public async Task<string> SendOtpAsync(RegisterDTO dto)
    {
        var cooldownKey = $"cooldown:{dto.Email}";
        var exist = await redis.SetWhenNotExistsAsync(
            cooldownKey, "1", TimeSpan.FromSeconds(CooldownSeconds));
        if (!exist)
            throw new TooManyRequestsException("Please wait");

        var otp = GenerateOTP();
        var key = $"register:{dto.Email}";
        var value = new
        {
            Name = dto.Name,
            Phone = dto.Phone,
            Email = dto.Email,
            Otp = otp,
            Password = dto.Password,
        };
        var json      = JsonSerializer.Serialize(value);
        var encrypt = crypto.Encrypt(json);

        logger.LogInformation("OTP sent");

        await redis.SetAsync(key, encrypt, TimeSpan.FromMinutes(5));
        await email.SendOtpToEmailAsync(dto.Email, dto.Name, otpCode);

        return value.Email;
    }

    public async Task<UserDTO> VerifyOtpAsync(OtpDTO dto)
    {
        var key = $"register:{dto.Email}";
        var decrypt = await redis.GetAsync(key);
        if (decrypt is null)
            throw new BadRequestException("OTP not found or expired");

        var json;
        try
        {
            json = crypto.Decrypt(value);
        }
        catch (CryptographicException)
        {
            throw new BadRequestException("...");
            logger.LogWarning("");
        }

        try
        {
            value = JsonSerializer.Deserialize<UserDTO>(json);
        }
        catch (JsonException)
        {
            throw new BadRequestException("");
            logger.LogWarning("");
        }

        if (!CryptographicOperations.FixedTimeEquals(
            System.Text.Encoding.UTF8.GetBytes(json.Otp),
            System.Text.Encoding.UTF8.GetBytes(dto.Otp)))
        {
            throw new BadRequestException("Invalid OTP");
        }

        await redis.DeleteAsync(key);

        return value;
    }

    private static string GenerateOTP()
    {
        var value = RandomNumberGenerator.GetInt32(0, (int)Math.Pow(10, OtpDigits));
        return value.ToString($"D{OtpDigits}");
    }
}