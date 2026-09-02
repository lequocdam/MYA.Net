using Ardalis.Specification;

public class UserPermissionSpecification : Specification<User>
{
    public UserPermissionSpecification(ICurrentUser currentUser)
    {
        if (currentUser.IsAdmin)
        {
            return;
        }

        if (currentUser.Role == "Manager")
        {
            Query.Where(x => x.HubId == currentUser.HubId);
        }
    }
}