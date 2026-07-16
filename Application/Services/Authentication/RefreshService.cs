public interface IRefreshService
{
    Task<string> CreateAsync(Guid UserId, TokenContext Ctx, CancellationToken Ct = default);

    Task<string> RefreshAsync(string refreshToken, TokenContext Ctx, CancellationToken Ct = default);

    Task DeleteAsync(string refreshToken, CancellationToken Ct = default, string? Jti);

    Task RevokeAllAsync(Guid userId, string reason, CancellationToken ct = default);
}

public sealed record TokenContext(string IpAddress, string? Agent, string? Device);

public sealed class RefreshService(
    IRefreshTokenRepository repo,
    ITokenService token,
    ILogger<RefreshService> logger) : IRefreshTokenService
{
    private static readonly TimeSpan TokenDays = TimeSpan.FromDays(7);
    private static readonly TimeSpan AbsoluteDays  = TimeSpan.FromDays(90);

    // CREATE
    public async Task<string> CreateAsync(Guide UserId, TokenContext Ctx, CancellationToken Ct = default)
    {
        var refreshToken = token.GenerateRefreshToken();
        var hash     = token.HashToken(refreshToken);
        var refresh = new Refresh
        {
            FamilyId  = Guid.NewGuid(),    
            RefreshToken = hash,
            Expire = DateTime.UtcNow.Add(TokenDays),
            Date = refresh.Date,
            Agent = Ctx.Agent,
            Device  = Ctx.Device,
            Ip = Ctx.Ip,
            UserId    = UserId,
        };

        await repo.AddAsync(refresh, Ct);
        await repo.SaveChangesAsync(Ct);

        logger.LogInformation("Token refreshed");

        return refresh.refreshToken;
    }

    // REFRESH
    public async Task<string> RefreshAsync(string refreshToken, TokenContext Ctx, CancellationToken Ct = default)
    {
        var hash   = token.HashToken(Dto.RefreshToken);
        var refresh = await repo.FindByHashAsync(hash, Ct);
        if (refresh is null)
        {
            logger.LogWarning("Refresh token not found");
            throw new UnauthorizedException("Invalid refresh token");
        }

        if (refresh.IsUsed)
        {
            await repo.DeleteAsync(refresh.FamilyId, "Replay attack detected", Ct);
            await repo.SaveChangesAsync(ct);

            logger.LogCritical(
                "Replay attack detected: TokenId={TokenId} FamilyId={FamilyId}",
                refresh.Id, refresh.FamilyId);
            throw new UnauthorizedException("Replay attack detected. Please log in again");
        }

        if (refresh.IsRevoked)
        {
            logger.LogWarning(
                "Token revoked: TokenId={TokenId} Reason={Reason}",
                refresh.Id, refresh.Reason);
            throw new UnauthorizedException("Invalid refresh token");
        }

        if (refresh.IsExpired)
        {
            logger.LogInformation("Token expired: {Id}", refresh.Id);
            throw new UnauthorizedException("Refresh token expired");
        }

        var age = DateTime.UtcNow - refresh.Date;
        if (age > AbsoluteDays)
        {
            await repo.DeleteAsync(refresh.FamilyId, "Absolute days exceeded", Ct);
            await repo.SaveChangesAsync(Ct);

            logger.LogInformation(
                "Absolute days exceeded: FamilyId={FamilyId} Age={Days}d",
                refresh.FamilyId, age.Days);
            throw new UnauthorizedException("Session expired. Please log in again");
        }

        refresh.IsUsed = true;

        var newRefreshToken = token.GenerateRefreshToken();
        var newHash = token.HashToken(newRefreshToken);
        var newRefresh = new Refresh
        {
            ParentId  = refresh.Id,
            FamilyId  = refresh.FamilyId,
            RefreshToken = refresh.newHash,
            Date = refresh.Date,
            Expire = DateTime.UtcNow.Add(TokenDays),
            Agent = Ctx.Agent,
            Device  = Ctx.Device,
            Ip = Ctx.Ip,
            UserId    = refresh.UserId,
        };

        await repo.AddAsync(newRefresh, Ct);
        await repo.SaveChangesAsync(Ct);

        logger.LogInformation(
            "Token refreshed: NewRefresh={NewId} Refresh={OldId} FamilyId={FamilyId}",
            newRefresh.Id, refresh.Id, refresh.FamilyId);

        return newRefresh.RefreshToken;
    }

    // LOGOUT
    public async Task DeleteAsync(string refreshToken, CancellationToken Ct = default, string? Jti)
    {
        var hash = tokenService.HashToken(refreshToken);
        var refresh = await repo.FindByHashAsync(hash, Ct);
        if (refresh is null || refresh.IsRevoked) return;

        if (Jti is not null)
        {
            await redis.SetAsync($"Blacklist:{Jti}", "1", TimeSpan.FromMinutes(15));
        }

        await repo.LogoutAsync(refresh.FamilyId, "logout", Ct);
        await repo.SaveChangesAsync(Ct);

        logger.LogInformation(
            "Family revoked: FamilyId={FamilyId}", refresh.FamilyId);
    }

    // LOGOUT ALL
    public async Task DeleteAllAsync(Guid userId, string reason, CancellationToken Ct = default)
    {
        await repo.LogoutAllAsync(userId, reason, Ct);
        await repo.SaveChangesAsync(Ct);

        logger.LogInformation(
            "All tokens revoked: UserId={UserId} Reason={Reason}", userId, reason);
    }
}