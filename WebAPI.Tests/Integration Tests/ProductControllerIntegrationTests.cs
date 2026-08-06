using System.Net;

namespace WebAPI.Tests.Integration_Tests;
using FluentAssertions;
using Xunit;
using WebAPI.Domain;

public class ProductControllerIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public ProductControllerIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private static StringContent ToJsonContent(object dto)
    {
        return new StringContent(System.Text.Json.JsonSerializer.Serialize(dto),
            System.Text.Encoding.UTF8, "application/json");
    }

    private string UniqueName() => $"product_{Guid.NewGuid().ToString().Substring(0, 8)}";

    [Fact]
    public async Task Get_All_Products_Should_Return_Ok()
    {
        var response = await _client.GetAsync("/api/Product");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Get_Product_By_Nonexistent_Id_Should_Return_NotFound()
    {
        var response = await _client.GetAsync("/api/Product/99999");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Get_Product_By_Valid_Id_Should_Return_Ok()
    {
        var createProductDTO = new CreatedProductDTO
        {
            Name = UniqueName(),
            Description = "test description",
            Price = 100.10m,
            Stock = 10
        };

        var createResponse = await _client.PostAsync("/api/Product", ToJsonContent(createProductDTO));
        var createContent = await createResponse.Content.ReadAsStringAsync();
        var jsonDoc = System.Text.Json.JsonDocument.Parse(createContent);
        var productId = jsonDoc.RootElement.GetProperty("id").GetInt32();

        var response = await _client.GetAsync($"/api/Product/{productId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain(createProductDTO.Name);
    }

    [Fact]
    public async Task Create_Product_With_Valid_Data_Should_Return_Created()
    {
        var createProductDTO = new CreatedProductDTO
        {
            Name = UniqueName(),
            Description = "test description",
            Price = 100.10m,
            Stock = 10
        };

        var content = ToJsonContent(createProductDTO);
        
        var response = await _client.PostAsync("/api/Product", content);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var responseContent = await response.Content.ReadAsStringAsync();
        responseContent.Should().Contain(createProductDTO.Name);
    }

    [Fact]
    public async Task Create_Product_With_Invalid_Data_Should_Return_BadRequest()
    {
        var createProductDTO = new CreatedProductDTO
        {
            Name = "",
            Description = "",
            Price = -15,
            Stock = -10
        };
        
        var content = ToJsonContent(createProductDTO);

        var response = await _client.PostAsync("/api/Product", content);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_Product_With_Negative_Price_Should_Return_BadRequest()
    {
        var createProductDTO = new CreatedProductDTO
        {
            Name = UniqueName(),
            Description = "test description",
            Price = -50.00m,
            Stock = 10
        };

        var content = ToJsonContent(createProductDTO);
        var response = await _client.PostAsync("/api/Product", content);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_Product_With_Negative_Stock_Should_Return_BadRequest()
    {
        var createProductDTO = new CreatedProductDTO
        {
            Name = UniqueName(),
            Description = "test description",
            Price = 100.00m,
            Stock = -5
        };

        var content = ToJsonContent(createProductDTO);
        var response = await _client.PostAsync("/api/Product", content);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Update_Product_With_Valid_Data_Should_Return_NoContent()
    {
        var createProductDTO = new CreatedProductDTO
        {
            Name = UniqueName(),
            Description = "original description",
            Price = 50.00m,
            Stock = 5
        };

        var createResponse = await _client.PostAsync("/api/Product", ToJsonContent(createProductDTO));
        var createContent = await createResponse.Content.ReadAsStringAsync();
        var jsonDoc = System.Text.Json.JsonDocument.Parse(createContent);
        var productId = jsonDoc.RootElement.GetProperty("id").GetInt32();

        var updateDto = new UpdateProductDTO
        {
            Name = "updated name",
            Description = "updated description",
            Price = 75.00m,
            Stock = 15
        };

        var response = await _client.PutAsync($"/api/Product/{productId}", ToJsonContent(updateDto));

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Update_Product_With_Invalid_Id_Should_Return_NotFound()
    {
        var updateDto = new UpdateProductDTO
        {
            Name = "updated name",
            Description = "updated description",
            Price = 75.00m,
            Stock = 15
        };

        var response = await _client.PutAsync("/api/Product/99999", ToJsonContent(updateDto));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Update_Product_With_Invalid_Data_Should_Return_BadRequest()
    {
        var createProductDTO = new CreatedProductDTO
        {
            Name = UniqueName(),
            Description = "original description",
            Price = 50.00m,
            Stock = 5
        };

        var createResponse = await _client.PostAsync("/api/Product", ToJsonContent(createProductDTO));
        var createContent = await createResponse.Content.ReadAsStringAsync();
        var jsonDoc = System.Text.Json.JsonDocument.Parse(createContent);
        var productId = jsonDoc.RootElement.GetProperty("id").GetInt32();

        var updateDto = new UpdateProductDTO
        {
            Name = "", 
            Description = "",
            Price = -50.00m, // invalid - negative
            Stock = -5 // invalid - negative
        };

        var response = await _client.PutAsync($"/api/Product/{productId}", ToJsonContent(updateDto));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}

