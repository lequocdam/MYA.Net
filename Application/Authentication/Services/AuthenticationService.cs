public class AuthenticationService(
    IUserRepository repository,
    IRegisterService register,
    IOtpService otp) : IAuthService
{
    public async Task<RegisterResponse> RegisterAsync(
        RegisterRequest request,
        CancellationToken ct)
    {
        var exists = await repository.ExistsByPhoneOrEmailAsync(
            request.Phone,
            request.Email,
            ct);

        UserPolicy.CanRegister(exists);

        var id = Guid.NewGuid();

        await register.SaveAsync(new RegisterModel{
            Id = id,
            Name = request.Name,
            Phone = request.Phone,
            Email = request.Email,
            HashPassword = passwordHasher.Hash(request.Password)
        }, ct);

        try
        {
            await otp.SendAsync(new SendModel{
                Id = id,
                Target = request.Email,
                Channel = OtpChannel.Email, 
                Purpose = OtpPurpose.Register
            }, ct);
        }
        catch
        {
            await register.RemoveAsync(model.Id, ct);
            throw;
        }

        return new RegisterResponse
        {
            Id = id,
            ExpiresIn = 300
        };
    }

    public async Task<LoginResponse> ConfirmAsync(
        ConfirmRequest request,
        CancellationToken ct)
    {
        var r = await register.GetAsync(request.Id, ct);
            ?? throw new RegisterExpiredException();

        if (r.IsVerified)
        {
             var user = await CreateAsync(r, ct);

            await register.RemoveAsync(r.Id, ct);

            return await token.GenerateAsync(user, ct);
        }

        await otp.VerifyAsync(new VerifyModel
        {
            Id = request.Id,
            Code = request.Code
        }, ct);

        r.IsVerified = true;
        await register.UpdateAsync(r, ct);

        var user = await CreateAsync(r, ct);
    
        await register.RemoveAsync(request.Id, ct);

        return await token.GenerateAsync(user, ct);
    }

    private async Task<LoginResponse> CreateAsync(RegisterModel r, CancellationToken ct)
    {
        var user = User.Create(r.Name, r.Phone, r.Email, r.HashPassword);
        
        await userRepository.AddAsync(user, ct);
        await userRepository.SaveChangesAsync(ct);
    }

    // RESEND OTP
    public async Task ResendOtpAsync(ResendOtpDto dto, CancellationToken ct)
    {
        await otpService.ResendOtpAsync(dto, ct);
    }

    public async Task<UserDto> VerifyAsync(VerifyRequest request, CancellationToken ct)
    {
        var userCache = await otpService.VerifyOtpAsync(request.Otp, ct);

        var user = User.Create(new CreateContext(
            userCache.Name,
            userCache.Phone,
            userCache.Email,
            userCache.Password,
        ));

        await repository.Add(user, ct);

        try
        {
            await unitOfWork.SaveChangesAsync(ct);
        }
        catch (DbUpdateException e)
        {
            logger.LogWarning("{Phone} or {Email} duplicated", user.Phone, user.Email, e);
            throw new ConflictException("Phone or email registered");
        }
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