public sealed class Handler : AuthorizationHandler<Requirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        Requirement requirement)
    {
        var role = context.User.FindFirst("Role");
            
        if (role is null)
            return Task.CompletedTask;

        if (!Enum.TryParse<Role>(role.Value, out var role))
        {
            return Task.CompletedTask;
        }

        var permissions = RolePermissions.Map.GetValueOrDefault(role)
            ?? [];

        if (permissions.Contains(requirement.Permission))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}