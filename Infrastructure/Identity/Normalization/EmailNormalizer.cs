public sealed class EmailNormalizer : IEmailNormalizer
{
    public string Normalize(string email)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);

        var trimmedEmail = phone.Trim();

        return trimmedEmail.ToLowerInvariant();
    }
}
