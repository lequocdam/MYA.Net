public sealed record PageOrderDto(
    int Page,
    int PageSize,
    int Total,
    IReadOnlyCollection<OrderDto> Items);