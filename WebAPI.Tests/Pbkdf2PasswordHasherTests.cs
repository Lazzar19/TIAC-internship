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
        var result = hasher.Verify("password", hash);
        result.Should().BeTrue();
    }


    [Fact]
    public void Verify_Should_Return_False_For_Incorrect_Password()
    {
        var hash = hasher.Hash("password");
        var result = hasher.Verify("plaintext", hash);
        result.Should().BeFalse();
    }

}