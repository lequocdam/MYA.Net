public class AuthService(
    IUserRepository userRepo,
    IOtpService otpService,
    ITokenService tokenService,
    IRefreshService refresh,
    IUserService user,
    ILogger<AuthService> logger) : IAuthService
{
    private const int RefreshDays     = 7;

    // REGISTER
    public async Task<EmailDto> RegisterAsync(RegisterDTO dto, CancellationToken ct)
    {
        var exist = await userRepo.AnyAsync(dto.Phone, dto.Email, ct);
        if (exist)
            throw new ConflictException("Phone or email registered");

        var maskEmail = await otpService.SendOtpAsync(dto, ct);

        return new EmailDto{
            Email  = email,
        };
    }

    // RESEND OTP
    public async Task ResendOtpAsync(ResendOtpDto dto, CancellationToken ct)
    {
        await otpService.ResendOtpAsync(dto, ct);
    }

    // VERIFY OTP
    public async Task<UserDto> VerifyOtpAsync(OtpDto dto, CancellationToken ct)
    {
        var userCache = await otpService.VerifyOtpAsync(dto, ct);
        var user = new User{
            Id = Guid.NewGuid(),
            Name = userCache.Name,
            Phone = userCache.Phone,
            Email = userCache.Email,
            Password = userCache.Password,
            Role = Role.USER,
        };

        await userRepo.Add(user, ct);

        try
        {
            await userRepo.SaveChangesAsync(ct);
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
    public async Task<TokenDto> LoginAsync(LoginDto dto, CancellationToken ct)
    {
        var user = await userRepo.FirstOrDefaultAsync(dto.Phone, ct);
        if (user is null)
        {
            throw new NotFoundException("Phone not found");
        }

        if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.Password))
        {
            throw new UnauthorizedException("Invalid password");
        }

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