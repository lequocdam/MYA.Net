public class VolumePricing : IPricingStrategy
{
    public double Calculate(CreateOrderDTO dto)
    {
        return dto.Packages.Sum(p => p.Length * p.Width * p.Height * 2000);
    }
}