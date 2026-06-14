public class WarehouseService(
    IWarehouseRepository warehouseRepository,
    ILogger<WarehouseService> logger) : IWarehouseService
{
    public async Task<List<WarehouseDto>> GetAllAsync(CancellationToken ct)
    {
        return await warehouseRepository.Query()
            .Where(w => w.IsActive)
            .Select(w => new WarehouseDto(
                w.Id,
                w.Name,
                w.Address,
                w.Province,
                w.District,
                w.IsDefault,
                w.IsActive))
            .ToListAsync(ct);
    }

    public async Task<WarehouseDto> GetByIdAsync(Guid id, CancellationToken ct)
    {
        return await warehouseRepository.Query()
            .Where(w => w.Id == id)
            .Select(w => new WarehouseDto(
                w.Id,
                w.Name,
                w.Address,
                w.Province,
                w.District,
                w.IsDefault,
                w.IsActive))
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("Warehouse", id);
    }

    public async Task<WarehouseDto> CreateAsync(
        CreateWarehouseDto dto,
        CancellationToken ct)
    {
        // Nếu set IsDefault thì bỏ default của kho cũ
        if (dto.IsDefault)
            await ClearDefaultAsync(ct);

        var warehouse = new Warehouse
        {
            Id        = Guid.NewGuid(),
            Name      = dto.Name,
            Address   = dto.Address,
            Province  = dto.Province,
            District  = dto.District,
            IsDefault = dto.IsDefault,
            IsActive  = true,
        };

        await warehouseRepository.AddAsync(warehouse, ct);
        await warehouseRepository.SaveChangesAsync(ct);

        return new WarehouseDto(
            warehouse.Id,
            warehouse.Name,
            warehouse.Address,
            warehouse.Province,
            warehouse.District,
            warehouse.IsDefault,
            warehouse.IsActive);
    }

    public async Task UpdateAsync(
        Guid id,
        UpdateWarehouseDto dto,
        CancellationToken ct)
    {
        var warehouse = await warehouseRepository.Query()
            .FirstOrDefaultAsync(w => w.Id == id, ct)
            ?? throw new NotFoundException("Warehouse", id);

        if (dto.IsDefault && !warehouse.IsDefault)
            await ClearDefaultAsync(ct);

        warehouse.Name      = dto.Name;
        warehouse.Address   = dto.Address;
        warehouse.Province  = dto.Province;
        warehouse.District  = dto.District;
        warehouse.IsDefault = dto.IsDefault;
        warehouse.IsActive  = dto.IsActive;

        await warehouseRepository.SaveChangesAsync(ct);
    }

    public async Task<List<WarehouseCoverageDto>> GetCoveragesAsync(
        Guid warehouseId,
        CancellationToken ct)
    {
        var exists = await warehouseRepository.Query()
            .AnyAsync(w => w.Id == warehouseId, ct);

        if (!exists)
            throw new NotFoundException("Warehouse", warehouseId);

        return await warehouseRepository.QueryCoverage()
            .Where(c => c.WarehouseId == warehouseId)
            .Select(c => new WarehouseCoverageDto(c.Id, c.Province, c.District))
            .ToListAsync(ct);
    }

    public async Task UpsertCoveragesAsync(
        Guid warehouseId,
        UpsertCoverageDto dto,
        CancellationToken ct)
    {
        var warehouse = await warehouseRepository.Query()
            .Include(w => w.Coverages)
            .FirstOrDefaultAsync(w => w.Id == warehouseId, ct)
            ?? throw new NotFoundException("Warehouse", warehouseId);

        // Kiểm tra district đã được kho khác phụ trách chưa
        var districts = dto.Items.Select(i => new { i.Province, i.District }).ToList();

        var conflicts = await warehouseRepository.QueryCoverage()
            .Where(c => c.WarehouseId != warehouseId
                && districts.Any(d => d.Province == c.Province
                                   && d.District  == c.District))
            .Select(c => new { c.Province, c.District, c.Warehouse.Name })
            .ToListAsync(ct);

        if (conflicts.Any())
        {
            var detail = string.Join(", ", conflicts.Select(c => $"{c.District} ({c.Name})"));
            throw new BusinessException($"Khu vực đã được phụ trách bởi kho khác: {detail}");
        }

        // Xoá hết rồi insert lại (upsert đơn giản)
        warehouse.Coverages.Clear();

        foreach (var item in dto.Items)
        {
            warehouse.Coverages.Add(new WarehouseCoverage
            {
                Id          = Guid.NewGuid(),
                WarehouseId = warehouseId,
                Province    = item.Province,
                District    = item.District,
            });
        }

        await warehouseRepository.SaveChangesAsync(ct);
    }

    public async Task<Guid> ResolveAsync(
        string province,
        string district,
        double latitude,
        double longitude,
        CancellationToken ct)
    {
        var warehouse = await warehouseRepository.Query()
            .Where(w => w.Province == province && w.District == district)
            .FirstOrDefaultAsync(ct);
            ?? throw new NotFoundException("Warehouse not found");

        return warehouse.Id;

        warehouse = await warehouseRepository.Query()
            .Where(w => w.Province == province)
            .FirstOrDefaultAsync(ct);
            ?? throw new NotFoundException("Warehouse not found");

        return warehouse.Id;

        // Bước 4: fallback — kho gần nhất theo tọa độ
        var warehouses = await warehouseRepository.Query()
            .Where(w => w.IsActive)
            .Select(w => new
            {
                w.Id,
                w.Latitude,
                w.Longitude
            })
            .ToListAsync(ct);

        if (!warehouses.Any())
            throw new BusinessException("Hệ thống chưa có kho nào hoạt động");

        var nearest = warehouses
            .OrderBy(w => HaversineDistance(
                latitude, longitude,
                w.Latitude, w.Longitude))
            .First();

        return nearest.Id;
    }

    // Công thức Haversine tính khoảng cách giữa 2 tọa độ (km)
    private static double HaversineDistance(
        double lat1, double lon1,
        double lat2, double lon2)
    {
        const double R = 6371;

        var dLat = ToRad(lat2 - lat1);
        var dLon = ToRad(lon2 - lon1);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
            + Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2))
            * Math.Sin(dLon / 2)   * Math.Sin(dLon / 2);

        return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    private static double ToRad(double deg) => deg * Math.PI / 180;

    // ─────────────────────────────────────────────
    // PRIVATE
    // ─────────────────────────────────────────────
    private async Task ClearDefaultAsync(CancellationToken ct)
    {
        var current = await warehouseRepository.Query()
            .Where(w => w.IsDefault)
            .FirstOrDefaultAsync(ct);

        if (current is not null)
        {
            current.IsDefault = false;
            await warehouseRepository.SaveChangesAsync(ct);
        }
    }
}
