public interface IAuthRepository
{
    Task<RefreshToken?> FindByHashAsync(string tokenHash, CancellationToken ct = default);

    Task<List<RefreshToken>> GetActiveFamilyAsync(Guid familyId, CancellationToken ct = default);

    Task AddAsync(RefreshToken token, CancellationToken ct = default);

    Task RevokeFamilyAsync(Guid familyId, string reason, CancellationToken ct = default);

    Task RevokeAllUserTokensAsync(Guid userId, string reason, CancellationToken ct = default);

    Task<int> DeleteExpiredAsync(CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);
}

public sealed class AuthRepository(AppDbContext db) : IAuthRepository
{
    public async Task<User> FirstOrDefaultAsync(string Phone)
    {
        => db.Users.FirstOrDefaultAsync(u => u.Phone == Phone);
    }

    public Task<RefreshToken?> FindByHashAsync(string tokenHash, CancellationToken ct = default)
        => db.RefreshTokens
             .Include(t => t.User)
             .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct);

    public Task<List<RefreshToken>> GetActiveFamilyAsync(Guid familyId, CancellationToken ct = default)
        => db.RefreshTokens
             .Where(t => t.FamilyId == familyId && !t.IsRevoked)
             .ToListAsync(ct);

    public async Task AddAsync(RefreshToken token, CancellationToken ct = default)
    {
        await db.RefreshTokens.AddAsync(token, ct);
    }

    public Task RevokeFamilyAsync(Guid familyId, string reason, CancellationToken ct = default)
        => db.Refreshs
             .Where(r => r.FamilyId == familyId && !r.IsRevoked)
             .ExecuteUpdateAsync(s => s
                 .SetProperty(r => r.IsRevoked,    true)
                 .SetProperty(r => r.Reason, reason)
                 .SetProperty(r => r.RevokedAt, DateTime.UtcNow),
             ct);

    public Task RevokeAllUserTokensAsync(Guid userId, string reason, CancellationToken ct = default)
        => db.Refreshs
             .Where(r => r.UserId == userId && !t.IsRevoked)
             .ExecuteUpdateAsync(s => s
                 .SetProperty(r => r.IsRevoked,    true)
                 .SetProperty(r => r.Reason, reason)
                 .SetProperty(r => r.RevokedAt,    DateTime.UtcNow),
             ct);

    public Task<int> DeleteExpiredAsync(CancellationToken ct = default)
        => db.RefreshTokens
             .Where(t => t.ExpiresAt < DateTime.UtcNow || (t.IsUsed && t.UsedAt < DateTime.UtcNow.AddDays(-1)))
             .ExecuteDeleteAsync(ct);

    public Task SaveChangesAsync(CancellationToken ct = default)
        => db.SaveChangesAsync(ct);
}