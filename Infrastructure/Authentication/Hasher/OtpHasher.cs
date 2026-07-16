using System.Security.Cryptography;
using System.Text;

public sealed class OtpHasher : IOtpHasher
{
    public string Hash(string otp)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(otp));

        return Convert.ToHexString(bytes);
    }

    public bool Verify(string otp, string hash)
        => Hash(otp).Equals(hash, StringComparison.OrdinalIgnoreCase);
}