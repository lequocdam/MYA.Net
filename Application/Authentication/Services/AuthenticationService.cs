using BCrypt.Net;

public class AuthenticationService(
    IUserRepository userRepository,
    IRegistrationRepository registrationRepository,
    IOutboxRepository outboxRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService,
    IIdempotencyService idempotencyService,
    IOtpService otpService,
    ILoginAttemptStore loginAttemptStore,
    IEmailNormalizer emailNormalizer,
    IPhoneNormalizer phoneNormalizer
    IPasswordHasher passwordHasher) : IAuthenticationService
{
    public async Task<RegisterResponse> RegisterAsync(RegisterRequest request, CancellationToken ct)
    {
        var email = emailNormalizer.Normalize(request.Email);
        var phone = phoneNormalizer.Normalize(request.Phone);
        var existedUser = await userRepository.ExistAsync(email, phone, ct);

        if (existedUser)
            throw new ConflictException("User is registered.");

        var existedRegistration = await registrationRepository.ExistAsync(email, phone, ct);

        if (existedRegistration)
            throw new ConflictException("Registration is registered. Try again.");

        var hashedPassword = await passwordHasher.Hash(request.Password);

        await using var transaction = await unitOfWork.BeginTransactionAsync(ct);

        try
        {
            var registration = Registration.Create(
                request.name, 
                email, 
                phone, 
                hashedPassword);

            await registrationRepository.AddAsync(registration, ct);

            var @event = new RegistrationCreatedEvent
            {
                RegistrationId = registration.Id,
                Email = registration.Email
            };

            var payload = JsonSerializer.Serialize(@event);
            var message = OutboxMessage.Create(OutboxMessageType.RegistrationCreated, @event);

            await outboxRepository.AddAsync(message, ct);
            await unitOfWork.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            return new RegisterResponse
            {
                RegistrationId = registration.Id,
            };
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }

    public async Task<ConfirmResponse> ConfirmAsync(
        ConfirmRequest request,
        CancellationToken ct)
    {        
        var verified = await otpService.VerifyAsync(request.RegistrationId, request.Code, ct);

        if (!verified)
            throw new InvalidException("Invalid OTP.");

        await using var transaction = await unitOfWork.BeginTransactionAsync(ct);

        try
        {
            var registration = await registrationRepository.GetByIdAsync(request.RegistrationId, ct)
                ?? throw new NotFoundException("Registration not found.");

            var user = User.Create(
                registration.Name,
                registration.Email,
                registration.Phone,
                registration.HashedPassword);

            await userRepository.AddAsync(user, ct);

            registration.MarkConfirmed();

            var @event = new RegistrationOtpConsumedEvent
            {
                RegistrationId = registration.Id
            };

            var message = OutboxMessage.Create(OutboxMessageType.ConsumeRegistrationOtp, @event);

            await outboxRepository.AddAsync(message, ct);

            var accessToken = await tokenService.GenerateAccessToken(user);
            var refreshToken = await refreshTokenService.GenerateAsync(user.Id);

            await unitOfWork.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            return new TokenData
            {
                AccessToken = accessToken.Token, 
                AccessExpiresAt = accessToken.ExpiresAt, 
                RefreshToken = refreshToken.Token, 
                RefreshExpiresAt = refreshToken.ExpiresAt
            };
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }

    public async Task ResendAsync(
        ResendRequest request,
        CancellationToken ct)
    {
        var registration = await registrationRepository.GetByIdAsync(request.RegistrationId, ct)
            ?? throw new NotFoundException("Registration not found.");

        if (!registration.IsPending())
            throw new ConflictException("Registration is confirmed.");

        await using var transaction = await unitOfWork.BeginTransactionAsync(ct);

        try
        {
            var @event = new SendRegistrationOtpEvent
            {
                RegistrationId = registration.Id,
                Email = registration.Email
            };

            var message = OutboxMessage.Create(OutboxMessageType.SendRegistrationOtp, @event);

            await outboxRepository.AddAsync(message, ct);
            await unitOfWork.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }

    public async Task ForgotPasswordAsync(
        ForgotPasswordRequest request,
        CancellationToken ct)
    {
        var email = emailNormalizer.Normalize(request.Email);
        var phone = phoneNormalizer.Normalize(request.Phone);

        var user = await userRepository.GetByUniqueAsync(email, phone, ct)
            ?? throw new NotFoundException("Invalid email or phone.");

        var resetToken = await tokenService.GenerateResetToken(user);

        await ResetStore.SetAsync(resetToken, user.Id, TimeSpan.FromMinutes(10), ct);

        var @event = new ForgotPasswordEvent
        {
            UserId = user.Id,
            Email = user.Email,
            ResetToken = resetToken
        };

        var message = OutboxMessage.Create(OutboxMessageType.ForgotPassword, @event);

        await outboxRepository.AddAsync(message, ct);
        await unitOfWork.SaveChangesAsync(ct);
    }

    public async Task ForgotPasswordConfirmAsync(
        ForgotPasswordConfirmRequest request,
        CancellationToken ct)
    {
        var session = await resetStore.GetAsync(request.ResetToken, ct)
            ?? throw new InvalidException("Reset token is invalid or expired.");

        var user = await userRepository.GetByIdAsync(session.UserId, ct)
            ?? throw new NotFoundException("User not found.");

        await otpService.VerifyAsync(session.OtpId, request.Code, ct);

        var hashedPassword = await passwordHasher.Hash(request.NewPassword);

        await using var transaction = await unitOfWork.BeginTransactionAsync(ct);
        try
        {
            user.ResetPassword(hashedPassword);

            await sessionRepository.RevokeAllAsync(user.Id, ct);
            await unitOfWork.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
    }

    public async Task<TokenData> LoginAsync(
        LoginRequest request,
        CancellationToken ct)
    {
        var email = emailNormalizer.Normalize(request.Email);
        var phone = phoneNormalizer.Normalize(request.Phone);

        var user = await userRepository.GetByUniqueAsync(email, phone, ct)
            ?? throw new UnauthorizedException("Invalid email/phone or password.");

        if (user.IsDeleted)
            throw new ConflictException("User is deleted.");

        var lockout = await loginAttemptStore.GetLockoutAsync(user.Id, ct);

        if (lockout.IsLocked)
        {
            throw new UnauthorizedException($"User is locked. Try again after {lockout.LockedUntil}.");
        }

        var verified = await passwordHasher.Verify(request.Password, user.HashedPassword);

        if (!verified)
        {
            await loginAttemptStore.RegisterFailedAttemptAsync(user.Id, ct);

            throw new UnauthorizedException("Invalid email/phone or password.");
        }

        await loginAttemptStore.ResetAsync(user.Id, ct);

        await using var transaction = await unitOfWork.BeginTransactionAsync(ct);

        try
        {
            var session = Session.Create(
                user.Id,
                requestContext.Ip,
                requestContext.Agent,
                requestContext.Device);

            var refreshToken = await refreshTokenService.GenerateAsync(user.Id, session.Id);
            var accessToken = await tokenService.GenerateAccessToken(user, session.Id);

            await sessionRepository.AddAsync(session, ct);
            await unitOfWork.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }

        return new TokenData
        {
            AccessToken = accessToken.Token, 
            AccessExpiresAt = accessToken.ExpiresAt, 
            RefreshToken = refreshToken.Token, 
            RefreshExpiresAt = refreshToken.ExpiresAt
        };
    }

    public async Task<TokenData> RefreshAsync(RefreshRequest request, CancellationToken ct)
    {
        var data = await refreshTokenService.RotateAsync(request.RefreshToken, ct);
            ?? throw new UnauthorizedException("Invalid refresh token.");

        var user = await userRepository.GetByIdAsync(data.UserId, ct)
            ?? throw new UnauthorizedException("Invalid refresh token.");

        if (user.IsDeleted)
            throw new ConflictException("User is deleted.");

        var accessToken = await tokenService.GenerateAccessTokenAsync(user, refreshToken.SessionId, ct);

        return new TokenData
        {
            AccessToken = accessToken.Token, 
            AccessExpiresAt = accessToken.ExpiresAt, 
            RefreshToken = refreshToken.Token, 
            RefreshExpiresAt = refreshToken.ExpiresAt
        };
    }

    public async Task LogoutAsync(
        LogoutRequest request, 
        string? jti, 
        CancellationToken ct)
    {
        var data = await refreshTokenService.RevokeAsync(
            request.RefreshToken, 
            reason: "Logout", 
            ct);

        if (refreshToken == null)
        {
            return;
        }

        await refreshTokenService.RevokeSessionAsync(
            refreshToken.SessionId,
            reason: "Log out", 
            ct);

        if (!string.IsNullOrEmpty(jti))
        {
            await cacheService.SetBlacklistAsync(jti, expiredAt: TimeSpan.FromMinutes(15), ct);
        }

        await unitOfWork.SaveChangesAsync(ct);

        await auditService.LogAsync(
            UserId: refreshToken.UserId,
            SessionId: refreshToken.SessionId,
            Event: AuditEvent.LogoutSuccess,
            ct);
    }

    public async Task LogoutAllAsync(LogoutAllRequest request, string? Jti, CancellationToken ct = default)
    {
        await refreshTokenService.RevokeAllAsync(
            userId, 
            reason: "Log out all", 
            ct);

        if (!string.IsNullOrEmpty(jti))
        {
            await cacheService.SetBlacklistAsync(jti, expiredAt: TimeSpan.FromMinutes(15), ct);
        }

        await unitOfWork.SaveChangesAsync(ct)

        await auditService.LogAsync(
            UserId: userId,
            SessionId: null,
            Even: AuditEvent.LogoutAllSuccess,
            ct);
    }

    public async Task<UserProfileResponse> GetProfileAsync(CancellationToken ct)
    {
        return await userRepository.GetProfileAsync(userId, ct)
            ?? throw new NotFoundException("Profile not found.");
    }

    public async Task UpdateProfileAsync(
        UpdateProfileRequest request,
        CancellationToken ct)
    {
        var user = await userRepository.GetByIdAsync(userId, ct)
            ?? throw new NotFoundException("User not found.");

        user.UpdateProfile(name);
        await unitOfWork.SaveChangesAsync(ct);
    }

    public async Task ChangeEmailAsync(ChangeEmailRequest request, CancellationToken ct)
    {
        var existedUser = await userRepository.ExistAsync(request.NewEmail, ct);

        if (existedUser) 
            throw new ConflictException("Email is used.");

        var existedUserChange = await userChangeRepository.ExistAsync(request.NewEmail, ct);

        if (existedUserChange)
            throw new ConflictException("Email is used.");

        await userChangeRepository.CancelAsync(userId, ContactChangeType.Email, ct);

        await using var transaction = await unitOfWork.BeginTransactionAsync(ct);
        
        try
        {
            var userChange = UserChange.Create(
                userId, 
                UserChangeType.Email, 
                request.NewEmail);

            await userChangeRepository.AddAsync(userChange, ct);

            var @event = new EmailChangeRequestedEvent
            {
                UserId = user.Id,
                UserChangeId = userChange.Id,
                NewEmail = newEmail
            };

            var message = OutboxMessage.Create(OutboxMessageType.ChangeEmailRequestedEvent, @even);

            await outboxRepository.AddAsync(message, ct);
            await unitOfWork.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }

    public async Task EmailChangeConfirmAsync(
        EmailChangeConfirmRequest request,
        CancellationToken ct)
    {        
        var verified = await otpService.VerifyAsync(userId, request.Code, OtpPurpose.ChangeEmail, ct);

        if (!verified)
            throw new InvalidException("Invalid OTP.");

        await using var transaction = await unitOfWork.BeginTransactionAsync(ct);

        try
        {
            var user = await userRepository.GetByIdAsync(userId, ct)
                ?? throw new NotFoundException("User not found.");

            var userChange = await userChangeRepository.GetByIdAsync(request.UserChangeId, ct)
                ?? throw new NotFoundException("UserChange not found.");

            user.ChangeEmail(userChange.NewValue);
            userChange.MarkConfirm();

            var @event = new EmailChangeOtpConsumedEvent
            {
                UserId = user.Id
            };

            var message = OutboxMessage.Create(OutboxMessageType.ConsumeEmailChangeOtp, @event);

            await outboxRepository.AddAsync(message, ct);
            await unitOfWork.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }

    public async Task ChangePhoneAsync(
        ChangePhoneRequest request,
        CancellationToken ct)
    {
        var existedUser = await userRepository.ExistAsync(request.NewPhone, ct);

        if (existedUser) 
            throw new ConflictException("Phone is used.");

        var existedUserChange = await userChangeRepository.ExistAsync(request.NewPhone, ct);

        if (existedUserChange)
            throw new ConflictException("Phone is used.");

        await using var transaction = await unitOfWork.BeginTransactionAsync(ct);

        try
        {
            var userChange = UserChange.Create(userId, UserChangeType.Phone, request.NewPhone);

            await userChangeRepository.AddAsync(userChange, ct);

            var @event = new ChangePhoneRequestedEvent
            {
                UserId = user.Id,
                Email = user.Email
            };

            var message = OutboxMessage.Create(OutboxMessageType.ChangePhoneRequested, @event);

            await outboxRepository.AddAsync(message, ct);

            try
            {
                await unitOfWork.SaveChangesAsync(ct);
            }
            catch (DbUpdateException ex) when (ex.IsUniqueConstraintViolation())
            {
                throw new ConflictException("Phone is used. Please confirm again.");
            }

            await transaction.CommitAsync(ct);
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }

    public async Task ChangePhoneConfirmAsync(
        ChangePhoneConfirmRequest request,
        CancellationToken ct)
    {
        var userChange = await userChangeRepository.GetByIdentityAsync( request.UserChangeId, userId, ContactChangeType.Phone, ct)
            ?? throw new NotFoundException("UserChange not found.");

        if (!userChange.IsPending())
            throw new ConflictException("UserChange is confirmed.");
        
        var verified = await otpService.VerifyAsync(userId, request.Code, OtpPurpose.ChangePhone, ct);

        if (!verified)
            throw new InvalidException("Invalid OTP.");

        await using var transaction = await unitOfWork.BeginTransactionAsync(ct);

        try
        {
            var user = await userRepository.GetByIdAsync(UserId, ct)
                ?? throw new NotFoundException("User not found.");

            user.ChangePhone(userChange.NewValue);
            userChange.MarkConfirm();

            var @event = new ChangePhoneOtpConsumedEvent
            {
                UserId = user.Id
            };

            var message = OutboxMessage.Create(OutboxMessageType.ConsumeChangePhoneOtp, @event);

            await outboxRepository.AddAsync(message, ct);

            try
            {
                await unitOfWork.SaveChangesAsync(ct);
            }
            catch (DbUpdateException ex) when (ex.IsUniqueConstraintViolation())
            {
                throw new ConflictException("Phone is used.");
            }

            await transaction.CommitAsync(ct);
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }

    public async Task ChangePasswordAsync(
        ChangePasswordRequest request,
        CancellationToken ct)
    {
        var user = await userRepository.GetByIdAsync(userId, ct)
            ?? throw new NotFoundException("User not found.");

        var verifiedCurrentPassword = await passwordHasher.Verify(request.CurrentPassword, user.HashedPassword);

        if (verified)
            throw new ConflictException("Invalid current password.");

        var verifiedNewPassword = await passwordHasher.VerifyAsync(request.NewPassword, user.HashedPassword, ct);

        if (verifiedNewPassword)
            throw new ConflictException("New password is used.");

        var hashedPassword = await passwordHasher.Hash(request.NewPassword);

        await using var transaction = await unitOfWork.BeginTransactionAsync(ct);

        try
        {
            user.ChangePassword(newHashedPassword);

            var @event = new PasswordChangedEvent
            {
                UserId = user.Id
            };

            var message = OutboxMessage.Create(OutboxMessageType.PasswordChanged, @event);

            await outboxRepository.AddAsync(message, ct);
            await unitOfWork.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }

    public async Task VerifyEmailAsync(CancellationToken ct)
    {
        var user = await userRepository.GetByIdAsync(userId, ct)
            ?? throw new NotFoundException("User not found.");

        if (user.IsEmailVerified)
            throw new ConflictException("Email is verified.");

        var @event = new EmailVerificationRequestedEvent
        {
            UserId = user.Id,
            Email = user.Email
        };

        var message = OutboxMessage.Create(OutboxMessageType.SaveEmailVerificationOtp, @event);
        await outboxRepository.AddAsync(message, ct);
        await unitOfWork.SaveChangesAsync(ct);
    }

    public async Task EmailVerificationConfirmAsync(
        EmailVerificationConfirmRequest request,
        CancellationToken ct)
    {  
        var verified = await otpService.VerifyAsync(userId, request.Code, OtpPurpose.VerifyEmail, ct);

        if (!verified)
            throw new InvalidException("Invalid OTP.");

        await using var transaction = await unitOfWork.BeginTransactionAsync(ct);

        try
        {
            var user = await userRepository.GetByIdAsync(userId, ct)
                ?? throw new NotFoundException("User not found.");

            user.VerifyEmail();

            var @event = new EmailVerificationOtpConsumedEvent
            {
                UserId = user.Id
            };

            var message = OutboxMessage.Create(OutboxMessageType.ConsumeEmailVerificationOtp, @event);

            await outboxRepository.AddAsync(message, ct);
            await unitOfWork.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }

    public async Task VerifyPhoneAsync(CancellationToken ct)
    {
        var user = await userRepository.GetByIdAsync(userId, ct)
            ?? throw new NotFoundException("User not found.");

        if (user.IsPhoneVerified)
            throw new ConflictException("Phone is verified.");

        var @event = new PhoneVerificationRequestedEvent
        {
            UserId = user.Id,
            Email = user.Email
        };

        var message = OutboxMessage.Create(OutboxMessageType.SavePhoneVerificationOtp, @event);
        await outboxRepository.AddAsync(message, ct);
        await unitOfWork.SaveChangesAsync(ct);
    }

    public async Task PhoneVerificationConfirmAsync(
        PhoneVerificationConfirmRequest request,
        CancellationToken ct)
    {  
        var verified = await otpService.VerifyAsync(userId, request.Code, OtpPurpose.VerifyPhone, ct);

        if (!verified)
            throw new InvalidException("Invalid OTP.");

        await using var transaction = await unitOfWork.BeginTransactionAsync(ct);

        try
        {
            var user = await userRepository.GetByIdAsync(userId, ct)
                ?? throw new NotFoundException("User not found.");

            user.VerifyPhone();

            var @event = new PhoneVerificationOtpConsumedEvent
            {
                UserId = user.Id
            };

            var message = OutboxMessage.Create(OutboxMessageType.ConsumePhoneVerificationOtp, @event);

            await outboxRepository.AddAsync(message, ct);
            await unitOfWork.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }
}