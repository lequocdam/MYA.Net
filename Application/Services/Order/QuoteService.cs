public class QuoteService(
    IZoneService zoneService,
    IWeightService weightService,
    IPricingService pricingService) : IQuoteService
{
    public async Task<Quote> GetAsync(
        Guid serviceId,
        Area fromArea,
        Area toArea,
        decimal cod,
        List<Item> items,
        CancellationToken ct)
    {
        var zone = await zoneService.GetAsync(fromArea, toArea, ct);
        var weight = await weightService.Calculate(items);
        var cost = await costService.GetAsync(serviceId, zone.Id, ct);
        var fee = await feeService.GetAsync(weight, cod, ct);
        var cod = await codService.GetAsync(cod, ct);

        return new Quote(
            ServiceId
            zoneId,
            weight,
            price);
    }
}