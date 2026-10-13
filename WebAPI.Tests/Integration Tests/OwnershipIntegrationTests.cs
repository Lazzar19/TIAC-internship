using System.Net;
using System.Text.Json;
using FluentAssertions;
using WebAPI.Domain;

namespace WebAPI.Tests.Integration_Tests;

public class OwnershipIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public OwnershipIntegrationTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private static StringContent ToJsonContent(object dto)
    {
        return new StringContent(JsonSerializer.Serialize(dto),
            System.Text.Encoding.UTF8, "application/json");
    }

    private string UniqueEmail() => $"user_{Guid.NewGuid():N}"[..16] + "@mail.com";
    private string UniqueUsername() => $"user_{Guid.NewGuid():N}"[..12];

    private async Task<int> CreateUserAsAdmin()
    {
        var response = await TestClients.AsAdminAsync(_client, HttpMethod.Post, "/api/User", ToJsonContent(new CreateUserDTO
        {
            Username = UniqueUsername(),
            Password = "testpassword123!",
            Email = UniqueEmail()
        }));
        response.EnsureSuccessStatusCode();
        var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.GetProperty("id").GetInt32();
    }

    private async Task<int> CreateProductAsAdmin()
    {
        var response = await TestClients.AsAdminAsync(_client, HttpMethod.Post, "/api/Product", ToJsonContent(new CreatedProductDTO
        {
            Name = $"product_{Guid.NewGuid():N}"[..16],
            Description = "owned product",
            Price = 20m,
            Stock = 10
        }));
        response.EnsureSuccessStatusCode();
        var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.GetProperty("id").GetInt32();
    }

    [Fact]
    public async Task User_Cannot_Update_Or_Read_Another_User()
    {
        var userA = await CreateUserAsAdmin();
        var userB = await CreateUserAsAdmin();
        var update = ToJsonContent(new UpdateUserDTO
        {
            UserName = "takenname",
            Email = UniqueEmail()
        });

        var getResponse = await TestClients.AsUserAsync(_client, userA, HttpMethod.Get, $"/api/User/{userB}");
        var putResponse = await TestClients.AsUserAsync(_client, userA, HttpMethod.Put, $"/api/User/{userB}", update);

        getResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        putResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task User_Cannot_Change_Another_Users_Password()
    {
        var userA = await CreateUserAsAdmin();
        var userB = await CreateUserAsAdmin();

        var response = await TestClients.AsUserAsync(_client, userA, HttpMethod.Put, $"/api/User/{userB}/password", ToJsonContent(new ChangePasswordDTO
        {
            CurrentPassword = "testpassword123!",
            NewPassword = "NewPassword123!"
        }));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task User_Cannot_Touch_Another_Users_Products()
    {
        var userA = await CreateUserAsAdmin();
        var userB = await CreateUserAsAdmin();
        var productId = await CreateProductAsAdmin();

        var list = await TestClients.AsUserAsync(_client, userA, HttpMethod.Get, $"/api/users/{userB}/products");
        var assign = await TestClients.AsUserAsync(_client, userA, HttpMethod.Post, $"/api/users/{userB}/products", ToJsonContent(new AsigningUserToProductDTO
        {
            ProductID = productId,
            Quantity = 1
        }));
        var remove = await TestClients.AsUserAsync(_client, userA, HttpMethod.Delete, $"/api/users/{userB}/products/{productId}");

        list.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        assign.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        remove.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task NonAdmin_Cannot_Create_Users_Or_Products()
    {
        var userId = await CreateUserAsAdmin();

        var createUser = await TestClients.AsUserAsync(_client, userId, HttpMethod.Post, "/api/User", ToJsonContent(new CreateUserDTO
        {
            Username = UniqueUsername(),
            Password = "testpassword123!",
            Email = UniqueEmail()
        }));
        var createProduct = await TestClients.AsUserAsync(_client, userId, HttpMethod.Post, "/api/Product", ToJsonContent(new CreatedProductDTO
        {
            Name = "blocked",
            Description = "blocked product",
            Price = 10m,
            Stock = 1
        }));

        createUser.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        createProduct.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task User_Can_Read_Own_Account()
    {
        var userId = await CreateUserAsAdmin();

        var byId = await TestClients.AsUserAsync(_client, userId, HttpMethod.Get, $"/api/User/{userId}");
        var me = await TestClients.AsUserAsync(_client, userId, HttpMethod.Get, "/api/User/me");

        byId.StatusCode.Should().Be(HttpStatusCode.OK);
        me.StatusCode.Should().Be(HttpStatusCode.OK);
        var meJson = JsonDocument.Parse(await me.Content.ReadAsStringAsync());
        meJson.RootElement.GetProperty("id").GetInt32().Should().Be(userId);
    }

    [Fact]
    public async Task Admin_Can_Update_Another_User_Change_Password_And_Assign_Product()
    {
        var userId = await CreateUserAsAdmin();
        var productId = await CreateProductAsAdmin();

        var update = await TestClients.AsAdminAsync(_client, HttpMethod.Put, $"/api/User/{userId}", ToJsonContent(new UpdateUserDTO
        {
            UserName = "adminedit",
            Email = UniqueEmail()
        }));
        var password = await TestClients.AsAdminAsync(_client, HttpMethod.Put, $"/api/User/{userId}/password", ToJsonContent(new ChangePasswordDTO
        {
            CurrentPassword = "testpassword123!",
            NewPassword = "NewPassword123!"
        }));
        var assign = await TestClients.AsAdminAsync(_client, HttpMethod.Post, $"/api/users/{userId}/products", ToJsonContent(new AsigningUserToProductDTO
        {
            ProductID = productId,
            Quantity = 1
        }));

        update.StatusCode.Should().Be(HttpStatusCode.NoContent);
        password.StatusCode.Should().Be(HttpStatusCode.NoContent);
        assign.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
