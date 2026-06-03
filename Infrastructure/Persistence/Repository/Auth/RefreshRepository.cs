using AuthSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace AuthSystem.Data;

public interface IRefreshRepository
{
    Task<Refresh> FindByHashAsync(string hash, CancellationToken ct = default);

    Task AddAsync(RefreshToken token, CancellationToken ct = default);

    Task RevokeFamilyAsync(Guid familyId, string reason, CancellationToken ct = default);

    Task RemoveAllAsync(Guid userId, string reason, CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);
}

public sealed class RefreshRepository(AppDbContext db) : IRefreshRepository
{
    public Task<Refresh> FindByHashAsync(string hash, CancellationToken Ct = default)
        => db.Refreshs
             .Include(r => r.User)
             .FirstOrDefaultAsync(r => r.hash == hash, ct);

    public async Task AddAsync(Refresh Refresh, CancellationToken Ct = default)
    {
        => db.Refreshs.AddAsync(Refresh, Ct);
    }

    public Task DeleteAsync(Guid familyId, string reason, CancellationToken Ct = default)
        => db.Refreshs
             .Where(r => r.FamilyId == familyId && !r.IsRevoked)
             .ExecuteUpdateAsync(r => r
                 .SetProperty(r => r.IsRevoked, true)
                 .SetProperty(r => r.Reason, reason),
             Ct);

    public Task DeleteAllAsync(Guid userId, string reason, CancellationToken Ct = default)
        => db.Refreshs
             .Where(r => r.UserId == userId && !r.IsRevoked)
             .ExecuteUpdateAsync(r => r
                 .SetProperty(r => r.IsRevoked, true)
                 .SetProperty(r => r.Reason, reason),
             Ct);

    public Task SaveChangesAsync(CancellationToken Ct = default)
        => db.SaveChangesAsync(Ct);
}