namespace WebAPI.Tests;

using System.IdentityModel.Tokens.Jwt;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using WebAPI.Domain;
using WebAPI.Infrastructure;
using Xunit;

public class TokenServiceTests
{
    private readonly TokenService _tokenService;

    public TokenServiceTests()
    {
        var configVal = new Dictionary<string, string?>()
        {
            { "Jwt:Issuer", "TestIssuer" },
            { "Jwt:Audience", "TestAudience" },
            { "Jwt:Key", "K7mPq29XvL8nR4zBwT1yHs6JfUc3AeN9Qd5MiGp0VkYr8CxZu2FoElW7hSn4" }
        };

        IConfiguration configuration = new ConfigurationManager()
            .AddInMemoryCollection(configVal).Build();

        _tokenService = new TokenService(configuration);
    }

    [Fact]
    public void GenerateToken_Should_Contain_Correct_Claims()
    {
        var newUser = new User { ID = 15, Username = "joedoe", Email = "joe@test.com", PasswordHash = "hash" };
        var token = _tokenService.GenerateToken(newUser);

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        jwt.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == "15");
        jwt.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Email && c.Value == "joe@test.com");
        jwt.Issuer.Should().Be("TestIssuer");
    }

    [Fact]
    public void GenerateToken_Should_Set_Expiration_Date()
    {
        var newUser = new User { ID = 24, Username = "joedoe", Email = "test@test.com", PasswordHash = "hash" };
        var token = _tokenService.GenerateToken(newUser);

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);
        jwt.ValidTo.Should().BeOnOrAfter(DateTime.UtcNow.AddMinutes(29));
        jwt.ValidTo.Should().BeOnOrBefore(DateTime.UtcNow.AddMinutes(31));
    }

    [Fact]
    public void GenerateRefreshToken_Should_Return_Base64_Encoded_64_Bytes()
    {
        var refreshToken = _tokenService.GenerateRefreshToken();

        var bytes = Convert.FromBase64String(refreshToken);

        bytes.Should().HaveCount(64);
        refreshToken.Should().NotBeNullOrWhiteSpace();
    }

     [Fact]
    public void GenerateRefreshToken_Should_Return_Different_Values_On_Subsequent_Calls()
    {
        var firstToken = _tokenService.GenerateRefreshToken();
        var secondToken = _tokenService.GenerateRefreshToken();

        firstToken.Should().NotBe(secondToken);
    }

    [Fact]
    public void GenerateToken_Should_Contain_Role_Claim()
    {
        var newUser = new User { ID = 1, Username = "testuser", Email = "test@test.com", PasswordHash = "hash", Role = Role.Admin };
        var token = _tokenService.GenerateToken(newUser);

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        jwt.Claims.Should().Contain(c => 
            c.Type == System.Security.Claims.ClaimTypes.Role && 
            c.Value == Role.Admin.ToString());
    }

    [Fact]
    public void GenerateToken_Should_Contain_UniqueName_Claim()
    {
        var username = "johndoe";
        var newUser = new User{ ID = 2, Username = username, Email = "john@test.com", PasswordHash = "hash" };
        var token = _tokenService.GenerateToken(newUser);

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        jwt.Claims.Should().Contain(c => 
            c.Type == JwtRegisteredClaimNames.UniqueName && 
            c.Value == username);
    }
    

    [Fact]
    public void GenerateToken_Should_Use_Configured_Audience()
    {
        var newUser = new User { ID = 4, Username = "testuser", Email = "test@test.com", PasswordHash = "hash" };
        var token = _tokenService.GenerateToken(newUser);

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        jwt.Audiences.Should().Contain("TestAudience");
    }

    [Fact]
    public void GenerateRefreshToken_Should_Return_Base64String()
    {
        var refreshToken = _tokenService.GenerateRefreshToken();

        Action act = () => Convert.FromBase64String(refreshToken);
        act.Should().NotThrow();
        refreshToken.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void GenerateRefreshToken_Should_Return_64_Bytes()
    {
        var refreshToken = _tokenService.GenerateRefreshToken();

        var bytes = Convert.FromBase64String(refreshToken);

        bytes.Should().HaveCount(64);
    }

    [Fact]
    public void GenerateRefreshToken_Should_Generate_Different_Values()
    {
        var firstToken = _tokenService.GenerateRefreshToken();
        var secondToken = _tokenService.GenerateRefreshToken();

        firstToken.Should().NotBe(secondToken);
    }
}