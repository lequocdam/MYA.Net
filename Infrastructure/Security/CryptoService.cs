using System.Security.Cryptography;
using System.Text;

public class CryptoService(IConfiguration config) : ICryptoService
{
    private readonly byte[] _key = Convert.FromBase64String(
        config["Crypto:Key"] ?? throw new InvalidOperationException("Crypto:Key is not configured")
    );

    public string Encrypt(string plainText)
    {
        var nonce = new byte[AesGcm.NonceByteSizes.MaxSize];      // 12 bytes
        var tag   = new byte[AesGcm.TagByteSizes.MaxSize];        // 16 bytes
        var plainBytes  = Encoding.UTF8.GetBytes(plainText);
        var cipherBytes = new byte[plainBytes.Length];

        RandomNumberGenerator.Fill(nonce);

        using var aes = new AesGcm(_key, AesGcm.TagByteSizes.MaxSize);
        aes.Encrypt(nonce, plainBytes, cipherBytes, tag);

        return $"{Convert.ToBase64String(nonce)}.{Convert.ToBase64String(tag)}.{Convert.ToBase64String(cipherBytes)}";
    }

    public string Decrypt(string cipherText)
    {
        var parts = cipherText.Split('.');
        if (parts.Length != 3)
            throw new FormatException("Invalid cipher format");

        var nonce       = Convert.FromBase64String(parts[0]);
        var tag         = Convert.FromBase64String(parts[1]);
        var cipherBytes = Convert.FromBase64String(parts[2]);
        var plainBytes  = new byte[cipherBytes.Length];

        using var aes = new AesGcm(_key, AesGcm.TagByteSizes.MaxSize);
        aes.Decrypt(nonce, cipherBytes, tag, plainBytes);

        return Encoding.UTF8.GetString(plainBytes);
    }
}