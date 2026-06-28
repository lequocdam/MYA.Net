public sealed class FilterOrderDto
{
    public Guid? WarehouseId { get; init; }

    public Guid? ServiceId { get; init; }

    public string? Code { get; init; }

    public DateTime? FromDate { get; init; }

    public DateTime? ToDate { get; init; }

    public OrderStatus? Status { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 20;
}