using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using WebAPI.Application.Interfaces;
using WebAPI.Application;

namespace WebAPI.Infrastructure;

public class Pbkdf2PasswordHasher : IPasswordHasher
{
    
    private const int SaltSize = 16;
    private const int HashSize = 32; 
    private const int Iterations = 100000;
    private static readonly HashAlgorithmName Alg = HashAlgorithmName.SHA256;

    public string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var subkey = Rfc2898DeriveBytes.Pbkdf2(
            password: password,
            salt: salt,
            iterations: Iterations,
            hashAlgorithm: Alg,
            outputLength: HashSize);

        return $"{Convert.ToBase64String(salt)}-{Convert.ToBase64String(subkey)}";
    }


    public bool Verify(string hash, string password)
    {
        var parts = hash.Split('-');
        if (parts.Length != 2) return false;

        var salt = Convert.FromBase64String(parts[0]);
        var expectedKey = Convert.FromBase64String(parts[1]);

        var actualSubkey = Rfc2898DeriveBytes.Pbkdf2(
            password: password,
            salt: salt,
            iterations: Iterations,
            hashAlgorithm: Alg,
            outputLength: HashSize);

        return CryptographicOperations.FixedTimeEquals(actualSubkey, expectedKey);
    }

}