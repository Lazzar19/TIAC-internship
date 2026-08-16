using System.Net;
using FluentAssertions;
using Xunit;
using WebAPI.Domain;

namespace WebAPI.Tests.Integration_Tests;

public class UserProductControllerIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public UserProductControllerIntegrationTests(CustomWebApplicationFactory factory)
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
    private string UniqueProductName() => $"product_{Guid.NewGuid().ToString().Substring(0, 8)}";


    private async Task<int> CreateTestUser()
    {
        var userDto = new CreateUserDTO
        {
            Username = UniqueUsername(),
            Password = "testpassword123!",
            Email = UniqueEmail()
        };

        var response = await _client.PostAsync("/api/User", ToJsonContent(userDto));
        var content = await response.Content.ReadAsStringAsync();
        var jsonDoc = System.Text.Json.JsonDocument.Parse(content);
        return jsonDoc.RootElement.GetProperty("id").GetInt32();
    }

    private async Task<int> CreateTestProduct()
    {
        var productDto = new CreatedProductDTO
        {
            Name = UniqueProductName(),
            Description = "test product",
            Price = 100.00m,
            Stock = 50
        };

        var response = await _client.PostAsync("/api/Product", ToJsonContent(productDto));
        var content = await response.Content.ReadAsStringAsync();
        var jsonDoc = System.Text.Json.JsonDocument.Parse(content);
        return jsonDoc.RootElement.GetProperty("id").GetInt32();
    }

    [Fact]
    public async Task Get_Products_For_User_Should_Return_Ok()
    {

        var userId = await CreateTestUser();
        var response = await _client.GetAsync($"/api/users/{userId}/products");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Assign_Product_To_User_Should_Return_Ok()
    {

        var userId = await CreateTestUser();
        var productId = await CreateTestProduct();

        var assignDto = new AsigningUserToProductDTO
        {
            ProductID = productId,
            Quantity = 5
        };


        var response = await _client.PostAsync(
            $"/api/users/{userId}/products",
            ToJsonContent(assignDto)
        );


        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("\"quantity\"");
    }

    [Fact]
    public async Task Assigning_NonExistent_Product_Should_Return_NotFound()
    {
        var userID = await CreateTestUser();
        var assignDTO = new AsigningUserToProductDTO
        {
            ProductID = 9999,
            Quantity = 5
        };

        var response = await _client.PostAsync(
            $"/api/users/{userID}/products", ToJsonContent(assignDTO));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

    }

    [Fact]
    public async Task Assigning_Product_With_Insufficient_Stock_Should_Return_BadRequest()
    {
        var userID = await CreateTestUser();
        var productID =  await CreateTestProduct();

        var assignDTO = new AsigningUserToProductDTO
        {
            ProductID = productID,
            Quantity = 999
        };
        
        var response = await _client.PostAsync(
            $"/api/users/{userID}/products", ToJsonContent(assignDTO));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
    
    [Fact]
    public async Task Delete_Product_Assignment_Should_Return_NoContent()
    {
        var userId = await CreateTestUser();
        var productId = await CreateTestProduct();

        var assignDto = new AsigningUserToProductDTO
        {
            ProductID = productId,
            Quantity = 5
        };

        await _client.PostAsync(
            $"/api/users/{userId}/products",
            ToJsonContent(assignDto)
        );

        var response = await _client.DeleteAsync($"/api/users/{userId}/products/{productId}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Delete_NonExistent_Product_Assignment_Should_Return_NotFound()
    {
        var userId = await CreateTestUser();
        var response = await _client.DeleteAsync($"/api/users/{userId}/products/99999");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Get_Products_For_NonExistent_User_Should_Return_Empty_List()
    {

        var response = await _client.GetAsync("/api/users/99999/products");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Be("[]");
    }
}

