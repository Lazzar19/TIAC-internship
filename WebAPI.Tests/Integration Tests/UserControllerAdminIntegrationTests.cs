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

    [Fact]
    public async Task Get_All_Users_With_Admin_Role_Should_Return_Ok()
    {
        var response = await _client.GetAsync("/api/User");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Update_Missing_User_With_Admin_Role_Should_Return_NotFound()
    {
        var response = await _client.PutAsync("/api/User/99999", ToJsonContent(new UpdateUserDTO
        {
            UserName = "missing",
            Email = UniqueEmail()
        }));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_User_Restores_Assigned_Stock()
    {
        var userId = await CreateUser();
        var productId = await CreateProduct(stock: 50);

        var assign = await _client.PostAsync($"/api/users/{userId}/products", ToJsonContent(new AssignProductToUserDTO
        {
            ProductID = productId,
            Quantity = 5
        }));
        assign.StatusCode.Should().Be(HttpStatusCode.OK);

        var beforeDelete = await ReadStock(productId);
        beforeDelete.Should().Be(45);

        var delete = await _client.DeleteAsync($"/api/User/{userId}");
        delete.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var afterDelete = await ReadStock(productId);
        afterDelete.Should().Be(50);

        var assignments = await _client.GetAsync($"/api/users/{userId}/products");
        assignments.StatusCode.Should().Be(HttpStatusCode.OK);
        (await assignments.Content.ReadAsStringAsync()).Should().Be("[]");
    }

    private async Task<int> CreateUser()
    {
        var response = await _client.PostAsync("/api/User", ToJsonContent(new CreateUserDTO
        {
            Username = UniqueUsername(),
            Password = "testpassword123!",
            Email = UniqueEmail()
        }));
        response.EnsureSuccessStatusCode();
        var json = System.Text.Json.JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.GetProperty("id").GetInt32();
    }

    private async Task<int> CreateProduct(int stock)
    {
        var response = await _client.PostAsync("/api/Product", ToJsonContent(new CreateProductDTO
        {
            Name = $"stock_{Guid.NewGuid().ToString()[..8]}",
            Description = "stock restore",
            Price = 15m,
            Stock = stock
        }));
        response.EnsureSuccessStatusCode();
        var json = System.Text.Json.JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.GetProperty("id").GetInt32();
    }

    private async Task<int> ReadStock(int productId)
    {
        var response = await _client.GetAsync($"/api/Product/{productId}");
        response.EnsureSuccessStatusCode();
        var json = System.Text.Json.JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.GetProperty("stock").GetInt32();
    }
}

