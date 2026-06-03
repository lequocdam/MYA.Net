public class StandardFlow : IStatus
{
    public List<string> GetFlow() => new()
    {
        "Pending", "Processing", "Shipping", "Delivered"
    };
}