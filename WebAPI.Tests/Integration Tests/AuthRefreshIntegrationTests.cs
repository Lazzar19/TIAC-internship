using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using WebAPI.Domain;

namespace WebAPI.Tests.Integration_Tests;

public class AuthRefreshReuseIntegrationTests : IClassFixture<JwtWebApplicationFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _client;

    public AuthRefreshReuseIntegrationTests(JwtWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Reused_Refresh_Token_Revokes_The_Replacement()
    {
        var auth = await RegisterAndLoginAsync("reuse");

        var firstRefresh = await _client.PostAsJsonAsync("/api/auth/refresh", new RefreshRequestDTO
        {
            RefreshToken = auth.RefreshToken
        });
        firstRefresh.StatusCode.Should().Be(HttpStatusCode.OK);
        var rotated = await firstRefresh.Content.ReadFromJsonAsync<AuthResponseDTO>(JsonOptions);
        rotated.Should().NotBeNull();

        var reused = await _client.PostAsJsonAsync("/api/auth/refresh", new RefreshRequestDTO
        {
            RefreshToken = auth.RefreshToken
        });
        reused.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var replacement = await _client.PostAsJsonAsync("/api/auth/refresh", new RefreshRequestDTO
        {
            RefreshToken = rotated!.RefreshToken
        });
        replacement.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private async Task<AuthResponseDTO> RegisterAndLoginAsync(string name, string? email = null, string password = "testpassword123!")
    {
        email ??= $"{name}_{Guid.NewGuid():N}@mail.com";
        var register = await _client.PostAsJsonAsync("/api/auth/register", new CreateUserDTO
        {
            Username = name,
            Email = email,
            Password = password
        });
        register.StatusCode.Should().Be(HttpStatusCode.Created);

        var login = await _client.PostAsJsonAsync("/api/auth/login", new LoginDTO
        {
            Email = email,
            Password = password
        });
        login.StatusCode.Should().Be(HttpStatusCode.OK);
        var auth = await login.Content.ReadFromJsonAsync<AuthResponseDTO>(JsonOptions);
        auth.Should().NotBeNull();
        return auth!;
    }
}

public class AuthPasswordChangeIntegrationTests : IClassFixture<JwtWebApplicationFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _client;

    public AuthPasswordChangeIntegrationTests(JwtWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Password_Change_Revokes_The_Refresh_Token()
    {
        var email = $"password_{Guid.NewGuid():N}@mail.com";
        const string password = "testpassword123!";
        var auth = await RegisterAndLoginAsync("password", email, password);

        var me = new HttpRequestMessage(HttpMethod.Get, "/api/user/me");
        me.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);
        var meResponse = await _client.SendAsync(me);
        meResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var user = await meResponse.Content.ReadFromJsonAsync<UserDTO>(JsonOptions);

        var change = new HttpRequestMessage(HttpMethod.Put, $"/api/user/{user!.ID}/password");
        change.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);
        change.Content = JsonContent.Create(new ChangePasswordDTO
        {
            CurrentPassword = password,
            NewPassword = "newpassword123!"
        });
        var changed = await _client.SendAsync(change);
        changed.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var refresh = await _client.PostAsJsonAsync("/api/auth/refresh", new RefreshRequestDTO
        {
            RefreshToken = auth.RefreshToken
        });
        refresh.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private async Task<AuthResponseDTO> RegisterAndLoginAsync(string name, string? email = null, string password = "testpassword123!")
    {
        email ??= $"{name}_{Guid.NewGuid():N}@mail.com";
        var register = await _client.PostAsJsonAsync("/api/auth/register", new CreateUserDTO
        {
            Username = name,
            Email = email,
            Password = password
        });
        register.StatusCode.Should().Be(HttpStatusCode.Created);

        var login = await _client.PostAsJsonAsync("/api/auth/login", new LoginDTO
        {
            Email = email,
            Password = password
        });
        login.StatusCode.Should().Be(HttpStatusCode.OK);
        var auth = await login.Content.ReadFromJsonAsync<AuthResponseDTO>(JsonOptions);
        auth.Should().NotBeNull();
        return auth!;
    }
}
