using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using WebAPI.Domain;

namespace WebAPI.Tests.Integration_Tests;

public class AdminSeedIntegrationTests : IClassFixture<AdminSeedWebApplicationFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _client;

    public AdminSeedIntegrationTests(AdminSeedWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Seeded_Admin_Can_Delete_User_And_Registration_Cannot_Grant_Admin()
    {
        var adminLogin = await _client.PostAsJsonAsync("/api/auth/login", new LoginDTO
        {
            Email = AdminSeedWebApplicationFactory.Email,
            Password = AdminSeedWebApplicationFactory.Password
        });
        adminLogin.StatusCode.Should().Be(HttpStatusCode.OK);
        var admin = await adminLogin.Content.ReadFromJsonAsync<AuthResponseDTO>(JsonOptions);
        admin.Should().NotBeNull();

        var list = await Send(HttpMethod.Get, "/api/user", admin!.Token);
        list.StatusCode.Should().Be(HttpStatusCode.OK);

        var email = $"member_{Guid.NewGuid():N}@mail.com";
        var register = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            username = "memberuser",
            email,
            password = "testpassword123!",
            role = "Admin"
        });
        register.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = JsonDocument.Parse(await register.Content.ReadAsStringAsync());
        var userId = created.RootElement.GetProperty("id").GetInt32();

        var memberLogin = await _client.PostAsJsonAsync("/api/auth/login", new LoginDTO
        {
            Email = email,
            Password = "testpassword123!"
        });
        memberLogin.StatusCode.Should().Be(HttpStatusCode.OK);
        var member = await memberLogin.Content.ReadFromJsonAsync<AuthResponseDTO>(JsonOptions);

        var forbiddenList = await Send(HttpMethod.Get, "/api/user", member!.Token);
        var forbiddenDelete = await Send(HttpMethod.Delete, $"/api/user/{userId}", member.Token);
        forbiddenList.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        forbiddenDelete.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var deleted = await Send(HttpMethod.Delete, $"/api/user/{userId}", admin.Token);
        deleted.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    private Task<HttpResponseMessage> Send(HttpMethod method, string url, string token)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return _client.SendAsync(request);
    }
}
