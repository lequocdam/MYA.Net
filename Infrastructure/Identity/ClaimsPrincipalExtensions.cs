using System.Security.Claims;
using MYA.Application.Common.Exceptions;

namespace MYA.Infrastructure.Identity;

internal static class ClaimsPrincipalExtensions
{
    public static Guid GetId(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(value, out var id))
            throw new UnauthorizedException();

        return id;
    }

    public static Guid? GetWarehouseId(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(CustomClaimTypes.WarehouseId);

        if (string.IsNullOrWhiteSpace(value))
            return null;

        if (!Guid.TryParse(value, out var warehouseId))
            throw new UnauthorizedException();

        return warehouseId;
    }

    public static string GetRole(this ClaimsPrincipal principal)
    {
        return principal.FindFirstValue(ClaimTypes.Role)
            ?? throw new UnauthorizedException();
    }
}