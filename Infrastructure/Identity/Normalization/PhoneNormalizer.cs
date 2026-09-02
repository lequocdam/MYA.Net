public sealed class PhoneNormalizer : IPhoneNormalizer
{
    public string Normalize(string phone)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(phone);

        var trimmedPhone = phone.Trim();

        return trimmedPhone.StartsWith('0') ? $"+84{trimmedPhone[1..]}" : trimmedPhone;
    }
}