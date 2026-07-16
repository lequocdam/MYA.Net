using System.Security.Cryptography;

public sealed class OtpGenerator : IOtpGenerator
{
    public string Generate(int length = 6)
    {
        Span<byte> bytes = stackalloc byte[length];
        RandomNumberGenerator.Fill(bytes);

        Span<char> chars = stackalloc char[length];

        for (var i = 0; i < length; i++)
            chars[i] = (char)('0' + (bytes[i] % 10));

        return new string(chars);
    }
}