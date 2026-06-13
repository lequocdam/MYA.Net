namespace MYAlog.Models
{
    public class Order
    {
        public string Id { get; set; }

        public string Code { get; set; }

        public string FromAddressId { get; set; }

        public string ToAddressId { get; set; }

        public string ServiceId { get; set; }

        public string Warehouse { get; set; }

        public string Note { get; set; }

        public double Cost { get; set; }

        public double Fee { get; set; }

        public double Total { get; set; }

        public void Update(
            Guid fromAddressId,
            Guid toAddressId,
            Guid serviceId)
        {
            FromAddressId = fromAddressId;
            ToAddressId = toAddressId;
            ServiceId = serviceId;
        }
    }
}