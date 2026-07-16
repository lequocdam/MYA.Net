public sealed class ShippingZoneRepository(AppDbContext db)
    : IShippingZoneRepository
{
    public async Task<ShippingZone> ResolveAsync(
        Address fromAddress,
        Address toAddress,
        Guid serviceId,
        CancellationToken ct)
    {
       var  fromArea = await AreaRepository.GetAsync(new AreaQuery(
            fromAddress.CityId,
            fromAddress.WardId,
            serviceId), ct);

        var toArea = await AreaRepository.GetAsync(new AreaQuery(
            fromAddress.CityId,
            fromAddress.WardId,
            serviceId);
            ct);

        return zone = await zoneRepository.GetAsync(new ZoneQuery(
            fromArea.Id,
            toArea.Id,
            serviceId);
            ct);
    }
}