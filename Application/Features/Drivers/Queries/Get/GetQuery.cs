public sealed record GetQuery(
    string? Keyword,
    DriverStatus? Status,
    DriverType? Type,
    Guid? WarehouseId,
    int Page = 1,
    int PageSize = 20
) : IRequest<PagedResult<DriverDto>>;