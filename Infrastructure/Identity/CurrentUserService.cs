using Microsoft.AspNetCore.Http;
using MYA.Application.Common.Interfaces;
using MYA.Application.Common.Exceptions;
using MYA.Application.Common.Models;

namespace MYA.Infrastructure.Identity;

public sealed class CurrentUserService(
    IHttpContextAccessor httpContextAccessor): ICurrentUserService
{
    public CurrentUser GetCurrent()
    {
        var principal = httpContextAccessor.HttpContext?.User;

        if (principal?.Identity?.IsAuthenticated != true)
            throw new UnauthorizedException();

        return new CurrentUser(
            principal.GetId(),
            principal.GetWarehouseId(),
            principal.GetRole());
    }
}