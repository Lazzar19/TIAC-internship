using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using WebAPI.Domain;

namespace WebAPI.Tests.Integration_Tests;

public class AuthLogoutIntegrationTests : IClassFixture<JwtWebApplicationFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _client;

    public AuthLogoutIntegrationTests(JwtWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Logout_With_Real_Jwt_Revokes_Refresh_Token()
    {
        var email = $"logout_{Guid.NewGuid():N}@mail.com";
        var password = "testpassword123!";

        var register = await _client.PostAsJsonAsync("/api/auth/register", new CreateUserDTO
        {
            Username = "logoutuser",
            Email = email,
            Password = password
        });
        register.StatusCode.Should().Be(HttpStatusCode.OK);

        var login = await _client.PostAsJsonAsync("/api/auth/login", new LoginDTO
        {
            Email = email,
            Password = password
        });
        login.StatusCode.Should().Be(HttpStatusCode.OK);

        var auth = await login.Content.ReadFromJsonAsync<AuthResponseDTO>(JsonOptions);
        auth.Should().NotBeNull();
        auth!.Token.Should().NotBeNullOrWhiteSpace();
        auth.RefreshToken.Should().NotBeNullOrWhiteSpace();

        var logout = new HttpRequestMessage(HttpMethod.Post, "/api/auth/logout");
        logout.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);
        var logoutResponse = await _client.SendAsync(logout);
        logoutResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var refresh = await _client.PostAsJsonAsync("/api/auth/refresh", new RefreshRequestDTO
        {
            RefreshToken = auth.RefreshToken
        });
        refresh.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
