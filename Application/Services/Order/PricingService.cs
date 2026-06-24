public class PricingService(
    IPricingRepository pricingRepository,
    IPriceEngine priceEngine) : IPricingService
{
    public async Task<PriceResult> CalculateAsync(
        Guid serviceId,
        Guid zoneId
        decimal weight,
        decimal cod,
        CancellationToken ct)
    {
        var pricing = await pricingRepository
            .Query()
            .Where(p => p.ServiceId == serviceId && p.Zone == zoneId)
            .Select(p => new Pricing(
                p.Id,
                p.FirstWeight,
                p.FirstCost,
                p.NextWeight,
                p.NextCost))
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("Pricing not found");

        var price = priceEngine.Calculate(pricing, weight, cod);

        return price;
    }
}