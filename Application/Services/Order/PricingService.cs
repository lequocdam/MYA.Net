public class PricingService : IPricingService
{
    private readonly IPricingRepository _pricingRepository;

    public PricingService(IPricingRepository pricingRepository)
    {
        _pricingRepository = pricingRepository;
    }

    public async Task<PriceDto> GetAsync(
        Guid serviceId,
        Guid zoneId,
        decimal weight,
        CancellationToken ct)
    {
        var pricing = await pricingRepository.GetAsync(serviceId, zoneId, ct)
            ?? throw new NotFoundException("Pricing not found");

        var cost = CalculateCost(pricing, weight);

        var fee = CalculateFee();

        return new PriceDto
        {
            Cost = cost,
            Fee = fee,
            Total = cost + fee
        };
    }

    private static decimal CalculateCost(
        Pricing pricing,
        decimal weight)
    {
        if (weight <= pricing.BaseWeight)
            return pricing.BaseCost;

        var extraWeight = weight - pricing.BaseWeight;

        var extraSteps = Math.Ceiling(
            extraWeight / pricing.NextWeight);

        return pricing.BaseCost
             + extraSteps * pricing.AddedCost;
    }

    private static decimal CalculateFee()
    {
        // Sau này cộng Fuel, COD, Insurance...
        return 0;
    }
}