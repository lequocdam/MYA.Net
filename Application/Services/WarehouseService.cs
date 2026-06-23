public class WarehouseService(
    IWarehouseRepository warehouseRepository,
    IMapper mapper,
    ILogger<WarehouseService> logger) : IWarehouseService
{
    public async Task<List<WarehouseDto>> GetAllAsync(CancellationToken ct)
    {
        return await warehouseRepository.Query()
            .Where(w => w.IsActive)
            .Select(w => new WarehouseDto(
                w.Id,
                w.Name,
                w.Street,
                w.WardId,
                w.CityId))
            .ToListAsync(ct);
    }

    public async Task<WarehouseDto> CreateAsync(
        CreateWarehouseDto dto,
        CancellationToken ct)
    {
        var warehouse = Warehouse.Create(
            dto.Name,
            dto.Street,
            dto.WardId,
            dto.CityId,
            dto.Latitude,
            dto.Longitude);

        await warehouseRepository.AddAsync(warehouse, ct);
        await warehouseRepository.SaveChangesAsync(ct);

        return mapper.Map<WarehouseDto>(warehouse);
    }

    public async Task<WarehouseDto> UpdateAsync(
        Guid id,
        UpdateWarehouseDto dto,
        CancellationToken ct)
    {
        var warehouse = await warehouseRepository.Query()
            .FirstOrDefaultAsync(w => w.Id == id, ct)
            ?? throw new NotFoundException("Warehouse not found");

        warehouse.Update(
            dto.Name,
            dto.Street,
            dto.WardId,
            dto.CityId,
            dto.Latitude,
            dto.Longitude);

        await warehouseRepository.SaveChangesAsync(ct);

        return mapper.Map<WarehouseDto>(warehouse);
    }

    public async Task<Guid> GetByAddressAsync(
        AddressDto dto,
        CancellationToken ct)
    {
        var coverageWarehouseId = await warehouseCoverageRepository.Query()
            .Where(c => c.CityId == address.CityId && c.WardId == address.WardId)
            .Select(c => c.WarehouseId)
            .FirstOrDefaultAsync(ct);

        if (coverageWarehouseId != null)
            return coverageWarehouseId;

        var warehouses = await warehouseRepository.Query()
            .Where(x => x.IsActive)
            .Select(x => new
            {
                x.Id,
                x.Latitude,
                x.Longitude
            })
            .ToListAsync(ct);

        if (!warehouses.Any())
            throw new BusinessException("Warehouses not found");

        var nearestWarehouse = warehouses.MinBy(x =>
            HaversineDistance(  
                address.Latitude,
                address.Longitude,
                x.Latitude,
                x.Longitude));

        return nearest.Id;
    }

    private static double CalculateDistance(
        double latFrom, double lonFrom,
        double latTo, double lonTo)
    {
        const double R = 6371;

        var dLat = ToRad(latTo - latFrom);
        var dLon = ToRad(lonTo - lonFrom);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
            + Math.Cos(ToRad(latFrom)) * Math.Cos(ToRad(latTo))
            * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    private static double ToRad(double deg) => deg * Math.PI / 180;
}
