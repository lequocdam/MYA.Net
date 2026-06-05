public class WeightService : IWeightService
{
    public double Calculate(List<ItemDTO> items)
    {
        var actual = items.Sum(i => i.Weight * i.Quantity);

        var volumetricWeight = items.Sum(i =>
            ((i.Length * i.Width * i.Height) / divisor) * i.Quantity);

        var chargeable = Math.Max(actualWeight, volumetricWeight);

        return Math.Ceiling(actual * 2) / 2;
    }
}