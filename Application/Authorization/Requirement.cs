public sealed class Requirement(string permission): IAuthorizationRequirement
{
    public string Permission { get; } = permission;
}