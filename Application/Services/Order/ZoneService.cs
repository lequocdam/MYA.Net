public class ZoneService(
    IZoneRepository zoneRepository,
    IAddressService addressService,
    ILogger<ZoneService> logger) : IZoneService
{
    // Cache tránh query DB mỗi lần tính zone
    private List<Province>     _provinces;
    private List<ZoneOverride> _overrides;

    public async Task<ZoneResult> GetAsync(Guid FromAddressId, Guid ToAddressId)
    {
        await LoadAsync();

        var fromAddress = await addressService.GetByIdAsync(FromAddressId);
        var toAddress   = await addressService.GetByIdAsync(ToAddressId);

        var fromProvince = provinces.FirstOrDefault(p => p.Code == fromAddress.ProvinceCode)
            ?? throw new NotFoundException("From province not found", fromAddress.ProvinceCode);

        var toProvince   = provinces.FirstOrDefault(p => p.Code == toAddress.ProvinceCode)
            ?? throw new NotFoundException("To province not found", toAddress.ProvinceCode);

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

    private async Task LoadAsync()
    {
        if (provinces is not null) return;

        provinces = await db.Provinces.ToListAsync();
        overrides = await db.ZoneOverrides.ToListAsync();
    }
}