ivate static Price Calculate(Pricing pricing, decimal weight, decimal cod)
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