    public Price Calculate(Pricing pricing, decimal weight, decimal cod)
    {
        var cost = CalculateCost(pricing, weight);
        var fee = CalculateCod(pricing, cod);
        var surcharge = CalculateSurcharge(pricing, cost)
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
        if (weight <= pricing.FirstWeight)
            return pricing.FirstCost;

        var extraWeight = weight - pricing.BaseWeight;
        var extraSteps  = Math.Ceiling(extraWeight / pricing.Step);

        return pricing.BaseCost + (extraSteps * pricing.ExtraCost);
    }

    private static decimal CalculateCod( Pricing pricing, decimal cod)
    {
        if (cod == 0) return 0;
        var fee = Math.Max((cod * pricing.CodRate;), pricing.MinCod);

        return fee;
    }

    private static decimal CalculateSurcharge(
        Pricing pricing,
        decimal shippingCost)
    {
        decimal total = 0;

        foreach (var surcharge in pricing.Surcharges)
        {
            if (!surcharge.IsActive)
                continue;

            total += surcharge.IsPercentage
                ? cost * surcharge.Value / 100
                : surcharge.Value;
        }

        return total;
    }