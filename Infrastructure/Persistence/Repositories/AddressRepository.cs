public class AddressRepository(
    AppDbContext db) : IAddressRepository
{
    public async Task ClearDefaultAsync(
        CurrentUser currentUser,
        CancellationToken ct)
    {
        await db.Addresses
            .Where(x =>
                x.UserId == currentUser.UserId &&
                x.IsDefault &&
                x.IsActive)
            .ExecuteUpdateAsync(x =>
                x.SetProperty(x =>
                    x.IsDefault, false), ct);
    }

    public async Task FirstOrDefaultAsync(
        Guid id,
        CurrentUser currentUser,
        CancellationToken ct)
    {
        await db.Addresses
            .Where(x =>
                x.UserId == request.UserId &&
                x.IsDefault &&
                x.IsActive)
            .FirstOrDefaultAsync(ct);
    }

    public async Task AddAsync(
        AddressEntity addressEntity,
        CancellationToken ct)
    {
        await db.Addresses.AddAsync(addressEntity, ct);
    }

    public Task SaveChangesAsync(
        CancellationToken ct)
    {
        return db.SaveChangesAsync(ct);
    }
}