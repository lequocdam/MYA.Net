public static class RolePermissions
{
    public static readonly IReadOnlyDictionary<Role, string[]> Map = new Dictionary<Role, string[]>
    {
        [Role.Manager] =
        [
            UserPermissions.Read,
            UserPermissions.Create,
            UserPermissions.Update,
            UserPermissions.Unlock,
            UserPermissions.Delete
        ]
    };
}