public class WeightPricing : IPricing
{
    public double Calculate(CreateOrderDTO dto)
    {
        return dto.Packages.Sum(p => p.Weight * 10000);
    }
}