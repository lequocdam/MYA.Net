namespace MYAlog.Models
{
    public class Order
    {
        public string Id { get; set; }

        public string Code { get; set; }

        public string SenderId { get; set; }

        public string ReceiverId { get; set; }

        public string Service { get; set; }

        public string Warehouse { get; set; }

        public string Note { get; set; }

        public double Cost { get; set; }

        public double Fee { get; set; }

        public double Total { get; set; }
    }
}