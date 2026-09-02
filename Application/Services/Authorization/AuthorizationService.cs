using System.Linq.Expressions;
using MyProject.Application.Common.Interfaces;
using MyProject.Domain.Interfaces;

namespace MyProject.Application.Services;

public class AuthorizationService : IAuthorizationService
{
    private readonly ICurrentUser _currentUser;

    public AuthorizationService(ICurrentUser currentUser)
    {
        _currentUser = currentUser;
    }

    public Expression<Func<T, bool>> GetScopeExpression<T>() where T : class
    {
        if (_currentUser.IsAdmin)
            return x => true;

        if (typeof(IHasHub).IsAssignableFrom(typeof(T)))
        {
            if (_currentUser.HubId.HasValue)
            {
                var hubId = _currentUser.HubId.Value;
                return x => ((IHasHub)x).HubId == hubId;
            }
        }

        // 3. Nếu Entity T triển khai IHasOwner (Dữ liệu cá nhân)
        if (typeof(IHasOwner).IsAssignableFrom(typeof(T)))
        {
            var userId = _currentUser.UserId;
            return x => ((IHasOwner)x).OwnerId == userId;
        }

        // 4. Default Deny / Default Allow tùy chính sách doanh nghiệp
        return x => true;
    }
}