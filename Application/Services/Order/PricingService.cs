public class PricingService(
    IPricingRepository pricingRepository,
    IZoneService zoneService) : IPricingService
{
    public async Task<PriceResult> CalculateAsync(
        Guid      serviceId,
        Address   fromAddress,
        Address   toAddress,
        decimal   weight,
        decimal   cod,
        CancellationToken ct)
    {
        var zone = await zoneService.GetAsync(
            fromAddress,
            toAddress,
            ct);

        var pricing = await pricingRepository
            .Query()
            .FirstOrDefaultAsync(
                p => p.ServiceId == serviceIdserviceId
                  && p.Zone == zone, ct)
            ?? throw new NotFoundException("Pricing not found");

        return CalculatePrice(weight, cod, pricing);
    }

    private static Price Calculate(Pricing pricing, decimal weight, decimal cod)
    {
        var cost = CalculateCost(pricing, weight);
        var codFee = CalculateCodFee(pricing, cod);
        var fee    = codFee;
        var total  = cost + fee;

        return new Price
        {
            Cost = cost,
            CodFee = codFee,
            Fee = fee,
            Total = total
        };
    }

    private static decimal CalculateCost(decimal weight, Pricing pricing)
    {
        if (weight <= pricing.BaseWeight)
            return pricing.BaseCost;

        var extraWeight = weight - pricing.BaseWeight;
        var extraSteps  = Math.Ceiling(extraWeight / pricing.Step);

        return pricing.BaseCost + (extraSteps * pricing.ExtraCost);
    }

    private static decimal CalculateCodFee(decimal cod, Pricing pricing)
    {
        if (cod == 0) return 0;

        var codFee = cod * pricing.CodFeeRate;
        return Math.Max(codFee, pricing.MinCodFee);
    }
}