namespace MYA.Domain.Services;

public class WeightCalculator : IWeightCalculator
{
    private const decimal RoadDivisor = 5000m; 

    public decimal CalculateAsync(IEnumerable<OrderItem> items) 
    {
        if (items == null || !items.Any()) return 0m;

        decimal actualWeight = items.Sum(i => i.WeightKg * i.Quantity);

        decimal volumeCubicCm = items.Sum(i => (i.LengthCm * i.WidthCm * i.HeightCm) * i.Quantity);

        decimal volumetricWeight = totalVolumeCubicCm / RoadDivisor;

        decimal chargeableWeightKg = Math.Max(actualWeight, volumetricWeight);

        return Math.Ceiling(chargeableWeightKg * 2m) / 2m;
    }
}
