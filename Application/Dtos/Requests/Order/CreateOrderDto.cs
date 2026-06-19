public class CreateOrderDto
{
    private Guid FromAddressId { get; set; }

    private Guid ToAddressId { get; set; }

    private decimal? Cod { get; set; }

    private List<CreateItemDto> Items { get; set; } = [];
}
