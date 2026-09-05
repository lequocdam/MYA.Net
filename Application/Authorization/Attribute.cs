public sealed class Attribute : AuthorizeAttribute
{
    public Attribute(string permission)
    {
        Policy = $"{PolicyProvider.Prefix}{permission}";
    }
}