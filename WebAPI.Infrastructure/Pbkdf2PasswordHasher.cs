using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using WebAPI.Application.Interfaces;

namespace WebAPI.Infrastructure;

public class Pbkdf2PasswordHasher : IPasswordHasher
{
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int CurrentIterations = 600_000;
    private const int LegacyIterations = 100_000;
    private static readonly HashAlgorithmName Alg = HashAlgorithmName.SHA256;

    public string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var subkey = Derive(password, salt, CurrentIterations);
        return $"v1.{CurrentIterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(subkey)}";
    }

    public bool Verify(string hash, string password)
    {
        if (string.IsNullOrWhiteSpace(hash))
            return false;

        if (hash.StartsWith("v1.", StringComparison.Ordinal))
            return VerifyVersioned(hash, password);

        return VerifyLegacy(hash, password);
    }

    private static bool VerifyVersioned(string hash, string password)
    {
        var parts = hash.Split('.');
        if (parts.Length != 4 || !int.TryParse(parts[1], out var iterations) || iterations < 1)
            return false;

        if (!TryDecode(parts[2], out var salt) || !TryDecode(parts[3], out var expectedKey))
            return false;

        var actualSubkey = Derive(password, salt, iterations);
        return CryptographicOperations.FixedTimeEquals(actualSubkey, expectedKey);
    }

    private static bool VerifyLegacy(string hash, string password)
    {
        var parts = hash.Split('-');
        if (parts.Length != 2)
            return false;

        if (!TryDecode(parts[0], out var salt) || !TryDecode(parts[1], out var expectedKey))
            return false;

        var actualSubkey = Derive(password, salt, LegacyIterations);
        return CryptographicOperations.FixedTimeEquals(actualSubkey, expectedKey);
    }

    private static bool TryDecode(string value, out byte[] bytes)
    {
        try
        {
            bytes = Convert.FromBase64String(value);
            return true;
        }
        catch (FormatException)
        {
            bytes = [];
            return false;
        }
    }

    private static byte[] Derive(string password, byte[] salt, int iterations)
        => Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, Alg, HashSize);
}
