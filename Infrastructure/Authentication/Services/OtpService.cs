public class OtpService(
    IOtpStore store) : IOTPService
{
    private static readonly TimeSpan Expiration = TimeSpan.FromMinutes(5);

    public async Task SendAsync(
        SendModel model,
        CancellationToken ct)
    {
        var code = generator.Generate();

        await store.SaveAsync(new OtpModel
        {
            Id = model.Id,
            Target = model.Target,
            Purpose = model.Purpose,
            HashCode = codeHasher.Hash(code),
            Attempt = 0,
            Expiration,
        }, ct);

        await sender.SendAsync(
            model.Channel,
            model.Target,
            code,
            ct);
    }

    public async Task VerifyAsync(
        VerifyModel model,
        CancellationToken ct)
    {
        var otp = await store.GetAsync(model.Id, ct);
            ?? throw new OtpExpiredException();

        if (otp.Attempt >= 5)
            throw new OtpLockedException();

        if (!otpHasher.Verify(model.Code, otp.HashCode))
        {
            otp.Attempt++;

            await store.UpdateAsync(otp, ct);
                throw new InvalidOtpException();
        }

        await store.RemoveAsync(model.Id, ct);
    }

    // RESEND OTP
    public async Task ResendOtpAsync(ResendOtpDTO dto, CancellationToken ct)
    {
        var cooldownKey = $"otp:register:cooldown:{dto.Email}";
        var allow = await redisService.SetWhenNotExistsAsync(cooldownKey, "1", TimeSpan.FromSeconds(60));
        if (!allow)
            throw new TooManyRequestsException("Vui lòng chờ 60 giây trước khi gửi lại");

        var key     = $"otp:register:{dto.Email}";

        var decryptValue = await redisService.GetAsync(key, ct);
        if (decryptValue is null)
            throw new BadRequestException("Phiên đăng ký đã hết hạn. Vui lòng đăng ký lại");
        var value = JsonSerializer.Deserialize<UserCache>(cryptoService.Decrypt(decryptValue));

        var newOtp         = GenerateOTP();
        value.Otp          = newOtp;

        var newEncryptValue = cryptoService.Encrypt(JsonSerializer.Serialize(value));
        await redisService.SetAsync(key, newEncryptValue, TimeSpan.FromMinutes(5), ct);

        // 6. Reset attempt counter để user có 5 lần thử mới
        var attemptKey = $"otp:register:attempt:{dto.Email}";
        await redisService.DeleteAsync(attemptKey, ct);

        await email.SendOTPToEmailAsync(value.Email, value.Otp , ct);
        logger.LogInformation($"OTP resent to {value.Email}");
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