using BCrypt.Net;

public class OtpService(
    IOtpStore otpStore,
    IOtpRateLimiter otpRateLimiter) : IOtpService
{
    public async Task<Otp> SaveOtpAsync(RegistrationCreatedEvent @event, CancellationToken ct)
    {
        await otpRateLimiter.RateLimitAsync(request.Target, ct);

        var code = generator.Generate();

        await otpStore.AddAsync(new Otp
        {
            Id = @event.Id,
            Target = request.Target,
            Purpose = request.Purpose,
            HashedCode = codeHasher.Hash(code),
            ExpiredAt = DateTime.UtcNow.AddMinutes(5)
        }, ct);

        return new SaveOtpData(code, expiredAt);
    }

    public async Task<bool> VerifyAsync(Guid id, string code, CancellationToken ct)
    {
        var lockKey = $"otp-lock:{id}";
        var lockToken = Guid.NewGuid().ToString();

        var acquired = await otpStore.SaveAsync(lockKey, lockToken, TimeSpan.FromSeconds(30), When.NotExists);

        if (!acquired)
            throw new ConflictException("Please try again.");

        try
        {
            var otp = await otpStore.GetAsync(id, ct)
                ?? throw new NotFoundException("OTP not found or is expired.");
            
            var verified = await codeHasher.Verify(code, otp.HashedCode);

            if (!verified)
            {
                var countAttempt = await otpStore.FailedAttemptAsync(id, ct);

                if (countAttempt >= MaxAttempts)
                {
                    await otpStore.DeleteAsync(id, ct);
                    throw new TooManyRequestsException("Too many verification requests.");
                }

                return true;
            }

            return false;
        }
        finally
        {
            await ReleaseLockAsync(lockKey, lockToken);
        }
    }

    private static string GenerateOTP()
    {
        var value = RandomNumberGenerator.GetInt32(0, (int)Math.Pow(10, 6));
        return value.ToString($"D{6}");
    }
}