public class OrderDto
{
    public Guid Id { get; set; }

    public string Code { get; set; }

    public double Total { get; set; }

    public string Status { get; set; }

    public DateTime Date { get; set; }

    public Guid FromAddressId { get; set; }

    public Guid ToAddressId { get; set; }

    public Guid ServiceId { get; set; }

    public Guid WarehouseId { get; set; }
}
