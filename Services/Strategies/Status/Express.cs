public class ExpressStatus : IStatus
{
    public List<string> GetFlow() => new()
    {
        "Pending", "Shipping", "Delivered"
    };
}