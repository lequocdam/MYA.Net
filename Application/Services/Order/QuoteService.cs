public class QuoteService(
    IZoneService zoneService,
    IWeightService weightService,
    IPricingService pricingService) : IShippingQuoteService
{
    public async Task<Quote> GetAsync(
        Guid serviceId,
        Address fromAddress,
        Address toAddress,
        decimal cod,
        List<Item> items,
        CancellationToken ct)
    {
        var zone = await zoneService.GetAsync(fromAddress, toAddress, ct);
        var weight = weightService.Calculate(items);
        var price = await pricingService.GetAsync(
            serviceId,
            zone.Id,
            weight,
            cod,
            ct);

        return new Quote(
            zone.Id,
            weight,
            price.Cost,
            price.Fee,
            price.Total);
    }
}