public class PricingService(
    IPricingRepository pricingRepository
) : IPricingService
{
    public async Task<PriceDto> CalculateAsync(
        Guid      serviceId,
        Zone      zone,
        double    weight,
        decimal?  cod,
        CancellationToken ct)
    {
        var pricing = await pricingRepository.GetAsync(serviceId, zone, ct)
            ?? throw new NotFoundException("Pricing not found");

        var cost      = CalculateCost(pricing, weight);
        var codFee    = CalculateCodFee(pricing, cod);
        var remoteFee = CalculateRemoteFee(pricing, zone);
        var fee       = codFee + remoteFee;

        return new PriceDto(
            Cost      : cost,
            CodFee    : codFee,
            RemoteFee : remoteFee,
            Fee       : fee,
            Total     : cost + fee
        );
    }

    private static decimal CalculateCost(Pricing pricing, double weight)
    {
        if (weight <= pricing.BaseWeight)
            return pricing.BaseCost;

        var extraWeight = weight - pricing.BaseWeight;
        var extraSteps  = Math.Ceiling(extraWeight / pricing.NextWeight);

        return pricing.BaseCost + (decimal)extraSteps * pricing.AddedCost;
    }

    private static decimal CalculateCodFee(Pricing pricing, decimal? codAmount)
    {
        if (codAmount is null or 0) return 0;

        // Phí COD = % trên giá trị thu hộ, tối thiểu MinCodFee
        var fee = codAmount.Value * pricing.CodFeeRate;
        return Math.Max(fee, pricing.MinCodFee);
    }

    private static decimal CalculateRemoteFee(Pricing pricing, Zone zone) =>
        zone == Zone.Remote ? pricing.RemoteFee : 0;
}