public class WeightEngine : IWeightEngine
{
    private const decimal divisor = 5000;

    public double Calculate(List<CreateItemDto> items)
    {
        var actualWeight = items.Sum(i => i.Weight * i.Quantity);

        var volumetricWeight = items.Sum(i => ((i.Length * i.Width * i.Height) / divisor) * i.Quantity);

        var chargeableWeight = Math.Max(actualWeight, volumetricWeight);

        return Math.Ceiling(chargeableWeight * 2) / 2;
    }
}