public class PricingService(
    IPricingRepository pricingRepository,
    IZoneService zoneService) : IPricingService
{
    public async Task<PriceResult> CalculateAsync(
        Guid serviceId,
        Guid zoneId
        decimal   weight,
        decimal   cod,
        CancellationToken ct)
    {
        var zone = await zoneService.GetAsync(
            fromAddress,
            toAddress,
            ct);

        var pricing = await pricingRepository.Query()
            .Where(p => p.ServiceId == serviceId && p.Zone == zoneId)
            .Select(p => new Pricing(
                p.Id,
                a.WardId,
                a.CityId,
                a.Latitude,
                a.Longitude))
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("Pricing not found");

        return CalculatePrice(weight, cod, pricing);
    }
}