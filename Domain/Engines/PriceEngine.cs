public sealed class PriceCalculator
{
    public Price Calculate(
        Guid serviceId
        Zone zone,
        decimal chargeableWeightKg,
        decimal codAmount)
    {
        var tariff = await tariffRepository.GetEffectiveAsync(
            request.ServiceId,
            zone.Id, ct); 
            ?? throw new NotFoundException("Tariff not found.")

        var cost = CalculateCost(tariff, chargeableWeight);

        var codFee = CalculateCodFee(tariff, codAmount);

        var surchargeFee = CalculateSurcharge(
            tariff,
            baseCost);

        return new DeliveryPrice(
            baseCost,
            codFee,
            surchargeFee,
            baseCost + codFee + surchargeFee);
    }

    private static decimal CalculateCost(Tariff tariff, decimal chargeableWeight)
    {
        if (chargeableWeight <= tariff.BaseWeight)
            return tariff.BaseCost;

        var extraWeight = chargeableWeight - tariff.BaseWeight;

        var steps = Math.Ceiling(extraWeight / tariff.WeightStepKg);

        return tariff.BaseCost + (steps * tariff.ExtraStepCost);
    }

    private static decimal CalculateCodFee(Tariff tariff, decimal codAmount)
    {
        if (codAmount <= 0)
            return 0;

        var fee = codAmount * tariff.CodRatePercentage / 100m;

        return Math.Max(
            fee,
            tariff.MinCodFee);
    }

    private static decimal CalculateSurcharge(
        Tariff tariff,
        decimal baseCost)
    {
        decimal total = 0;

        foreach (var surcharge in tariff.Surcharges)
        {
            if (!surcharge.IsActive)
                continue;

            total += surcharge.IsPercentage
                ? baseCost * surcharge.ValuePercentage / 100m
                : surcharge.FixedValue;
        }

        return total;
    }
}