using Microsoft.EntityFrameworkCore;

public class AuthService(
    IAuthRepository repo,
    IOtpService otp,
    ITokenService token,
    IRefreshService refresh,
    ILogger<AuthService> logger) : IAuthService,
{
    private const int Logininutes = 5;
    private const int RefreshDays     = 7;

    // REGISTER
    public async Task<string> RegisterAsync(RegisterDTO dto)
    {
        var exist = await repo.AnyAsync(dto.Phone, dto.Email);
        if (exist)
            throw new ConflictException("Phone or email registered");

        return await otp.SendOTPAsync(dto);
    }

    // RESEND OTP

    // VERIFY
    public async Task<UserDTO> VerifyAsync(OtpDTO dto)
    {
        var register = await otp.VerifyOtpAsync(dto);
        var hash = BCrypt.Net.BCrypt.HashPassword(register.Password);
        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = register.Name,
            Phone = register.Phone,
            Email = register.Email,
            Password = hash,
            Role = "user",
        };

        await repo.Add(user);

        try
        {
            await repo.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            throw new ConflictException("Phone or email registered");
            logger.LogWarning("Duplicate user for {Phone} or {Email}", dto.Phone, dto.Email);
        }

        return new UserDTO{
            Message = "Account created",
            Data = {
                user.Id, 
                user.Name,
                user.Phone,
                user.Email,
                user.Role,
            },
        };
    }

    // LOGIN
    public async Task<TokensDTO> LoginAsync(LoginDTO Dto, string Ip)
    {
        var rateLimitKey = $"login:{Dto.Phone}:{Ip}";
        var allow = await redis.SetWithAtomicExpireAsync(
            rateLimitKey, "1", TimeSpan.FromMinutes(LoginMinutes));
        if (!allow)
            throw new TooManyRequestsException(
                $"Too many attempts. Try again in {LoginMinutes} minutes");

        var user = await repo.FirstOrDefaultAsync(Dto.Phone);
        if (user is null || !BCrypt.Net.BCrypt.Verify(user.Password, dto.Password))
            throw new UnauthorizedException("Invalid phone or password");

        var accessToken  = token.GenerateAccessToken(user);
        var refreshToken = refresh.CreateAsync(user.UserId);

        return new TokensDTO{
            accessToken, 
            refreshToken,
        };
    }

    // REFRESH
    public async Task<TokensDTO> RefreshAsync(RefreshDTO Dto, Guid UserId)
    {
        var user = await repo.FirstOrDefaultAsync(UserId);

        var accessToken  = token.GenerateAccessToken(user);
        var refreshToken = refresh.RefreshAsync(Dto.RefreshToken);

        return new TokensDTO{
            accessToken, 
            refreshToken,
        };
    }

    // LOGOUT
    public async Task LogoutAsync(RefreshDTO Dto, string? Jti)
    {
        await refresh.LogoutAsync(Dto.RefreshToken, Jti);
    }

    // LOGOUT ALL
    public async Task LogoutAllAsync(Guid userId, string reason, string? Jti)
    {
        await refresh.DeleteAllAsync(userId, Jti);
    }
}