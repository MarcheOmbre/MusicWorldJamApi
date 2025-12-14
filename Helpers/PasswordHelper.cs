using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

namespace WorldMusicJam.Helpers;

public static class PasswordHelper
{
    private const int Iterations = 100000;
    private const int HashSize = 256 / 8;
    private const int SaltSize = 128 / 8;

    public static byte[] GenerateSalt()
    {
        var salt = new byte[SaltSize];
        
        using RandomNumberGenerator rng = RandomNumberGenerator.Create();
        rng.GetNonZeroBytes(salt);

        return salt;
    }

    public static byte[] GetPasswordHash(IConfiguration configuration, string password, byte[] salt)
    {
        var secretKey = configuration["AppSettings:SecretKey"] ?? throw new Exception("No secret found");

        // Add secret
        var secretKeyPlusSalt = secretKey + Convert.ToBase64String(salt);

        // Hash
        return KeyDerivation.Pbkdf2(password, Encoding.ASCII.GetBytes(secretKeyPlusSalt), KeyDerivationPrf.HMACSHA256,
            Iterations, HashSize);
    }
}