using System.Security.Cryptography;
using FluentAssertions;
using WebAPI.Infrastructure;

namespace WebAPI.Tests;

public class Pbkdf2PasswordHasherTests
{
    private readonly Pbkdf2PasswordHasher hasher = new();

    [Fact]
    public void Hash_Should_Not_Return_Plain_Text()
    {
        var hash = hasher.Hash("plaintext");
        hash.Should().NotBe("plaintext");
    }

    [Fact]
    public void Verify_Should_Return_True_For_Correct_Password()
    {
        var hash = hasher.Hash("password");
        var result = hasher.Verify(hash, "password");
        result.Should().BeTrue();
    }


    [Fact]
    public void Verify_Should_Return_False_For_Incorrect_Password()
    {
        var hash = hasher.Hash("password");
        var result = hasher.Verify(hash, "plaintext");
        result.Should().BeFalse();
    }

    [Fact]
    public void Hash_Stores_The_Iteration_Count()
    {
        var hash = hasher.Hash("password");
        hash.Should().StartWith("v1.600000.");
    }

    [Fact]
    public void Verify_Accepts_A_Legacy_Hash_At_100000_Iterations()
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var subkey = Rfc2898DeriveBytes.Pbkdf2("password", salt, 100_000, HashAlgorithmName.SHA256, 32);
        var legacy = $"{Convert.ToBase64String(salt)}-{Convert.ToBase64String(subkey)}";

        hasher.Verify(legacy, "password").Should().BeTrue();
        hasher.Verify(legacy, "other-password").Should().BeFalse();
    }

    [Fact]
    public void Verify_Reads_Iterations_From_The_Stored_Hash()
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var subkey = Rfc2898DeriveBytes.Pbkdf2("password", salt, 1_000, HashAlgorithmName.SHA256, 32);
        var hash = $"v1.1000.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(subkey)}";

        hasher.Verify(hash, "password").Should().BeTrue();
    }

}