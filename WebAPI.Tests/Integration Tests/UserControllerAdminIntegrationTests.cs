using System.Net;
using FluentAssertions;
using Xunit;
using WebAPI.Domain;

namespace WebAPI.Tests.Integration_Tests;


public class UserControllerAdminIntegrationTests : IClassFixture<AdminTestWebApplicationFactory>
{
    private readonly AdminTestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public UserControllerAdminIntegrationTests(AdminTestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private static StringContent ToJsonContent(object dto)
    {
        return new StringContent(System.Text.Json.JsonSerializer.Serialize(dto),
            System.Text.Encoding.UTF8, "application/json");
    }

    private string UniqueEmail() => $"user_{Guid.NewGuid().ToString().Substring(0, 8)}@mail.com";
    private string UniqueUsername() => $"user_{Guid.NewGuid().ToString().Substring(0, 8)}";

    [Fact]
    public async Task Delete_User_With_Admin_Role_Should_Return_NoContent()
    { 
        var createUserDto = new CreateUserDTO
        {
            Username = UniqueUsername(),
            Password = "testpassword123!",
            Email = UniqueEmail()
        };
        
        var createResponse = await _client.PostAsync("/api/User", ToJsonContent(createUserDto));
        var createContent = await createResponse.Content.ReadAsStringAsync();
        var jsonDoc = System.Text.Json.JsonDocument.Parse(createContent);
        var userId = jsonDoc.RootElement.GetProperty("id").GetInt32();
        
        
        var response = await _client.DeleteAsync($"/api/User/{userId}");
        
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Delete_User_With_NonExistent_Id_Should_Return_NotFound()
    {
        var response = await _client.DeleteAsync("/api/User/99999");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}

