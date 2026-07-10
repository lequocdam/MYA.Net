namespace MYA.Application.Common.Models;

public sealed record CurrentUser(
    Guid Id,
    Guid? WarehouseId,
    string Role);