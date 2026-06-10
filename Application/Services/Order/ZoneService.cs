public class ZoneService(
    IZoneRepository zoneRepository,
    ILogger<ZoneService> logger) : IZoneService
{
    // Cache tránh query DB mỗi lần tính zone
    private List<Province>     _provinces;
    private List<ZoneOverride> _overrides;

    private async Task LoadAsync()
    {
        if (_provinces is not null) return;

        _provinces = await db.Provinces.ToListAsync();
        _overrides = await db.ZoneOverrides.ToListAsync();
    }

    public async Task<ZoneResult> GetAsync(AddressDto sender, AddressDto receiver)
    {
        await EnsureLoadedAsync();

        var from = _provinces.FirstOrDefault(p => p.Code == sender.ProvinceCode)
            ?? throw new NotFoundException("Province", sender.ProvinceCode);

        var to = _provinces.FirstOrDefault(p => p.Code == receiver.ProvinceCode)
            ?? throw new NotFoundException("Province", receiver.ProvinceCode);

        // Check restricted trước
        if (from.IsRestricted)
            return ZoneResult.Fail($"Không hỗ trợ lấy hàng tại {from.Name}. {from.RestrictedReason}");

        if (to.IsRestricted)
            return ZoneResult.Fail($"Không hỗ trợ giao hàng đến {to.Name}. {to.RestrictedReason}");

        // Check override
        var overrideZone = _overrides.FirstOrDefault(o =>
            o.FromProvinceId == from.Id && o.ToProvinceId == to.Id);

        if (overrideZone is not null)
            return ZoneResult.Ok(Enum.Parse<Zone>(overrideZone.Zone), from, to);

        // Logic mặc định
        var zone = (from.Code == to.Code) ? Zone.Local
                 : (from.Region == to.Region) ? Zone.SameRegion
                 : Zone.CrossRegion;

        return ZoneResult.Ok(zone, from, to);
    }
}