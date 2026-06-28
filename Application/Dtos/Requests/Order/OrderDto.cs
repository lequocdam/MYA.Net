public sealed record OrderDto(
    Guid Id,
    Guid UserId,
    Guid WarehouseId,
    Guid ServiceId,
    string Code,
    DateTime Date,
    OrderStatus Status,
    decimal Total);