public class AuthService(
    IUserRepository userRepo,
    IOtpService otpService,
    ITokenService tokenService,
    IRefreshService refresh,
    IUserService user,
    ILogger<AuthService> logger) : IAuthService,
{
    private const int Logininutes = 5;
    private const int RefreshDays     = 7;

    // REGISTER
    public async Task RegisterAsync(RegisterDTO dto, CancellationToken ct)
    {
        var exist = await userRepo.AnyAsync(dto.Phone, dto.Email, ct);
        if (exist)
            throw new ConflictException("Phone or email registered");

        await otpService.SendOTPAsync(dto, ct);
    }

    // RESEND OTP

    // VERIFY OTP
    public async Task<UserDto> VerifyOtpAsync(OtpDto dto, CancellationToken ct)
    {
        var userCache = await otpService.VerifyOtpAsync(dto, ct);
        await userRepo.Add(new User{
            Id = Guid.NewGuid(),
            Name = userCache.Name,
            Phone = userCache.Phone,
            Email = userCache.Email,
            Password = userCache.Password,
            Role = Role.USER,
        }, ct);

        try
        {
            var user = await userRepo.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            logger.LogWarning(ex,"Duplicate user for {Phone} or {Email}",dto.Phone, dto.Email);

            throw new ConflictException("Phone or email registered");
        }

        return new UserDto{
            Id = user.Id,
            Role = user.Role,
        };
    }

    // LOGIN
    public async Task<TokenDto> LoginAsync(LoginDto dto, string Ip, CancellationToken ct)
    {
        var user = await userRepo.FirstOrDefaultAsync(dto.Phone, ct);
        if (user is null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.Password))
            throw new UnauthorizedException("Invalid phone or password");

        var accessToken  = tokenService.GenerateAccessToken(user);
        var refreshToken = await refresh.CreateAsync(user.Id, ct);

        return new TokenDto{
            AccessToken  = accessToken,
            RefreshToken = refreshToken,
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