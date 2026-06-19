public class PriceEngine : IPriceEngine
{
    public decimal Calculate(decimal weight, decimal cod, Pricing pricing)
    {
        var cost = CalculateCost(weight, pricing);
        var codFee = CalculateCodFee(cod, pricing);
        var fee = codFee + codFee;
        var total = cost + fee;

        return
    }

    private decimal CalculateCost(decimal weight, Pricing pricing)
    {
        if (weight <= pricing.BaseWeight)
            return pricing.BaseCost;

        var extraWeights = weight - pricing.BaseWeight;
        var extraSteps = Math.Ceiling(extraWeight / pricing.Step);

        return pricing.BaseCost + (extraSteps * pricing.ExtraCost);
    }

    private decimal CalculateCodFee(Pricing pricing, decimal cod)
    {
        if (cod is null or 0) return 0;

        var codFee = cod * pricing.Rate;
        return Math.Max(codFee, pricing.MinCodFee);
    }
}