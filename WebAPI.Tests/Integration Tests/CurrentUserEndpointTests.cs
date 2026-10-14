using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

namespace WebAPI.Tests.Integration_Tests;

public class CurrentUserEndpointTests : IClassFixture<JwtWebApplicationFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _client;

    public CurrentUserEndpointTests(JwtWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Me_Returns_The_User_From_The_Access_Token()
    {
        var ana = await RegisterAndLoginAsync("ana");
        var boris = await RegisterAndLoginAsync("boris");

        var anaMe = await GetMeAsync(ana.Token);
        var borisMe = await GetMeAsync(boris.Token);

        anaMe.StatusCode.Should().Be(HttpStatusCode.OK);
        borisMe.StatusCode.Should().Be(HttpStatusCode.OK);

        var anaUser = await anaMe.Content.ReadFromJsonAsync<UserDTO>(JsonOptions);
        var borisUser = await borisMe.Content.ReadFromJsonAsync<UserDTO>(JsonOptions);
        anaUser!.ID.Should().Be(ana.Id);
        anaUser.Email.Should().Be(ana.Email);
        borisUser!.ID.Should().Be(boris.Id);
        borisUser.Email.Should().Be(boris.Email);
        anaUser.ID.Should().NotBe(borisUser.ID);

        var anonymous = await _client.GetAsync("/api/user/me");
        anonymous.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private async Task<HttpResponseMessage> GetMeAsync(string token)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/user/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await _client.SendAsync(request);
    }

    private async Task<(int Id, string Email, string Token)> RegisterAndLoginAsync(string name)
    {
        var email = $"{name}_{Guid.NewGuid():N}@mail.com";
        const string password = "testpassword123!";
        var register = await _client.PostAsJsonAsync("/api/auth/register", new CreateUserDTO
        {
            Username = name,
            Email = email,
            Password = password
        });
        register.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await register.Content.ReadFromJsonAsync<UserDTO>(JsonOptions);

        var login = await _client.PostAsJsonAsync("/api/auth/login", new LoginDTO
        {
            Email = email,
            Password = password
        });
        login.StatusCode.Should().Be(HttpStatusCode.OK);
        var auth = await login.Content.ReadFromJsonAsync<AuthResponseDTO>(JsonOptions);

        return (created!.ID, email, auth!.Token);
    }
}
