public sealed class QuoteService(
    IWeightService weightService,
    IZoneService zoneService,
    ITariffRepository tariffRepository,
    IPromotionService promotionService,
    PricingCalculator pricingCalculator)
    : IQuoteService
{
    public async Task<QuoteResult> CalculateAsync(QuoteRequest request, CancellationToken ct)
    {
        var chargeableWeight = weightCalculator.Calculate(request.Items);

        var zone = ZoneResolver.Resolve(request.FromAddress, request.ToAddress);

        var tariff = await tariffRepository.GetEffectiveAsync(
            request.ServiceId,
            zone.Id, ct); 
            ?? throw new NotFoundException("Tariff not found.");

        var price = priceCalculator.Calculate(
            tariff,
            chargeableWeight,
            input.CodAmount);

        // 5. Promotion
        var discount = await promotionService.CalculateAsync(
            input.PromotionId,
            price.TotalPrice,
            ct);

        var finalAmount = price.TotalPrice - discount.Amount;

        return new QuoteResult(
            price,
            discount,
            finalAmount);
    }
}