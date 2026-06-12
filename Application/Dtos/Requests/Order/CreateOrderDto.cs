public class CreateOrderDto
{
    public Guid FromAddressId { get; set; }

    public Guid ToAddressId { get; set; }

    public Guid ServiceId { get; set; }

    public Guid WarehouseId { get; set; }

    public List<CreateItemDto> Items { get; set; }
}
