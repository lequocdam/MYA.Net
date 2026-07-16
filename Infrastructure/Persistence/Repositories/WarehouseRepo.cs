public sealed class WarehouseRepository(AppDbContext db)
    : IWarehouseRepository
{
    public async Task<IReadOnlyList<Warehouse>> GetPickupCandidatesAsync(WarehouseCandidateQuery query, CancellationToken ct)
    {
        return await db.Warehouses
            .AsNoTracking()
            .Where(x => x.IsActive)
            .Where(x => x.WarehouseCoverages.Any(y => y.CityId == query.CityId && y.WardId == query.WardId))
            .Where(x => x.WarehouseCapabilities.Any(z => z.ServiceId == query.ServiceId))
            .OrderBy(x => x.Location.Distance(query.Location))
            .Take(20)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Warehouse>> GetDeliveryCandidatesAsync(WarehouseCandidateQuery query, CancellationToken ct)
    {
        return await db.Warehouses
            .AsNoTracking()
            .Where(x => x.IsActive)
            .Where(x => x.WarehouseCoverages.Any(y => y.CityId == query.CityId && y.WardId == query.WardId))
            .Where(x => x.WarehouseCapabilities.Any(z => z.ServiceId == query.ServiceId))
            .OrderBy(x => x.Location.Distance(query.Location))
            .Take(20)
            .ToListAsync(ct);
    }

    public Task<Warehouse?> GetByIdAsync(
        Guid id,
        CancellationToken ct)
    {
        return db.Warehouses
            .FirstOrDefaultAsync(x => x.Id == id, ct);
    }
}