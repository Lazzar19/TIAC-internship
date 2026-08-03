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


    [Fact]

    public async Task Get_All_Products_Should_Return_Ok()
    {
        var response = await _client.GetAsync("/api/Product");
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
    }

    [Fact]
    public async Task Create_Product_With_Valid_Data_Should_Return_Created()
    {
        var createProductDTO = new CreatedProductDTO
        {
            Name = "test",
            Description = "test description",
            Price = 100.10m,
            Stock = 10
        };

        var content = new StringContent(System.Text.Json.JsonSerializer.Serialize((createProductDTO)),
            System.Text.Encoding.UTF8, "application/json");
        
        var response = await _client.PostAsync("/api/Product", content);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

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
        
        var content = new StringContent(
            System.Text.Json.JsonSerializer.Serialize(createProductDTO),
            System.Text.Encoding.UTF8,
            "application/json");;

        var response = await _client.PostAsync("/api/Product", content);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
    


}