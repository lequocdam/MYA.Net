public class AuthService(
    IUserRepository userRepo,
    IOTPService otpService,
    ITokenService token,
    IRefreshService refresh,
    IUserService user,
    ILogger<AuthService> logger) : IAuthService,
{
    private const int Logininutes = 5;
    private const int RefreshDays     = 7;

    // REGISTER
    public async Task<object> RegisterAsync(RegisterDTO dto, CancellationToken ct)
    {
        var exist = await userRepo.AnyAsync(dto.Phone, dto.Email, ct);
        if (exist)
            throw new ConflictException("Phone or email registered");

        return await otpService.SendOTPAsync(dto, ct);
    }

    // RESEND OTP

    // VERIFY OTP
    public async Task<UserDTO> VerifyOTPAsync(OtpDTO dto, CancellationToken ct)
    {
        var userCache = await otpService.VerifyOTPAsync(dto, ct);

        var user = await userService.CreateAsync(
            new CreateUserDTO
            {
                Name = userCache.Name,
                Phone = userCache.Phone,
                Email = userCache.Email,
                Password = userCache.Password
            }, ct);

        await otpService.DeleteOTPAsync(user.Email);

        return user;
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