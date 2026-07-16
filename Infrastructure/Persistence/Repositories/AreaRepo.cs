public sealed class WarehouseRepository(AppDbContext db)
    : IWarehouseRepository
{
    public async Task<IReadOnlyList<Area>> GetAsync(AreaQuery query, CancellationToken ct)
    {
        return await db.Areas
            .AsNoTracking()
            .Where(x => x.AreaCoverages.Any(y => 
                y.CityId == query.CityId && 
                y.WardId == query.WardId &&
                y.ServiceId == query.ServiceId))
            .FirstOrDefaultAsync(ct);
    }

    public Task<Warehouse?> GetByIdAsync(
        Guid id,
        CancellationToken ct)
    {
        return db.Warehouses
            .FirstOrDefaultAsync(x => x.Id == id, ct);
    }
}