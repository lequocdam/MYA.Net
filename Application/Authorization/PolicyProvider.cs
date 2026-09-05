public sealed class PolicyProvider : DefaultAuthorizationPolicyProvider
{
    public const string Prefix = "Permission:";

    public PolicyProvider(IOptions<AuthorizationOptions> options): base(options){}

    public override Task<AuthorizationPolicy?>GetPolicyAsync(string policyName)
    {
        if (!policyName.StartsWith(Prefix))
            return base.GetPolicyAsync(policyName);

        var permission = policyName[Prefix.Length..];

        var policy = new AuthorizationPolicyBuilder()
            .AddRequirements(new Requirement(permission))
            .Build();

        return Task.FromResult<AuthorizationPolicy?>(policy);
    }
}