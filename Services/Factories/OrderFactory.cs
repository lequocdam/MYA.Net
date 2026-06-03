public class OrderStrategyFactory
{
    public (IPricing pricing, IStatus status) Resolve(string serviceId)
    {
        return serviceId switch
        {
            "standard" => (new WeightPricing(), new StandardStatus()),
            "express" => (new WeightPricing(), new ExpressStatus()),
            "truck" => (new VolumePricing(), new TruckStatus()),
            _ => throw new Exception("Invalid service")
        };
    }
}