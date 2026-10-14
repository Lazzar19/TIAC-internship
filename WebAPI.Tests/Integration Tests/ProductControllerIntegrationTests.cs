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
        var createProductDTO = new CreateProductDTO
        {
            Name = UniqueName(),
            Description = "test description",
            Price = 100.10m,
            Stock = 10
        };

        var createResponse = await TestClients.AsAdminAsync(_client, HttpMethod.Post, "/api/Product", ToJsonContent(createProductDTO));
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
        var createProductDTO = new CreateProductDTO
        {
            Name = UniqueName(),
            Description = "test description",
            Price = 100.10m,
            Stock = 10
        };

        var content = ToJsonContent(createProductDTO);
        
        var response = await TestClients.AsAdminAsync(_client, HttpMethod.Post, "/api/Product", content);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var responseContent = await response.Content.ReadAsStringAsync();
        responseContent.Should().Contain(createProductDTO.Name);
    }

    [Fact]
    public async Task Create_Product_With_Invalid_Data_Should_Return_BadRequest()
    {
        var createProductDTO = new CreateProductDTO
        {
            Name = "",
            Description = "",
            Price = -15,
            Stock = -10
        };
        
        var content = ToJsonContent(createProductDTO);

        var response = await TestClients.AsAdminAsync(_client, HttpMethod.Post, "/api/Product", content);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_Product_With_Negative_Price_Should_Return_BadRequest()
    {
        var createProductDTO = new CreateProductDTO
        {
            Name = UniqueName(),
            Description = "test description",
            Price = -50.00m,
            Stock = 10
        };

        var content = ToJsonContent(createProductDTO);
        var response = await TestClients.AsAdminAsync(_client, HttpMethod.Post, "/api/Product", content);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_Product_With_Negative_Stock_Should_Return_BadRequest()
    {
        var createProductDTO = new CreateProductDTO
        {
            Name = UniqueName(),
            Description = "test description",
            Price = 100.00m,
            Stock = -5
        };

        var content = ToJsonContent(createProductDTO);
        var response = await TestClients.AsAdminAsync(_client, HttpMethod.Post, "/api/Product", content);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Update_Product_With_Valid_Data_Should_Return_NoContent()
    {
        var createProductDTO = new CreateProductDTO
        {
            Name = UniqueName(),
            Description = "original description",
            Price = 50.00m,
            Stock = 5
        };

        var createResponse = await TestClients.AsAdminAsync(_client, HttpMethod.Post, "/api/Product", ToJsonContent(createProductDTO));
        var createContent = await createResponse.Content.ReadAsStringAsync();
        var jsonDoc = System.Text.Json.JsonDocument.Parse(createContent);
        var productId = jsonDoc.RootElement.GetProperty("id").GetInt32();

        var updateDto = new UpdateProductDTO
        {
            Name = "updated name",
            Description = "updated description",
            Price = 75.00m
        };

        var response = await TestClients.AsAdminAsync(_client, HttpMethod.Put, $"/api/Product/{productId}", ToJsonContent(updateDto));

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Update_Product_With_Invalid_Id_Should_Return_NotFound()
    {
        var updateDto = new UpdateProductDTO
        {
            Name = "updated name",
            Description = "updated description",
            Price = 75.00m
        };

        var response = await TestClients.AsAdminAsync(_client, HttpMethod.Put, "/api/Product/99999", ToJsonContent(updateDto));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Update_Product_With_Invalid_Data_Should_Return_BadRequest()
    {
        var createProductDTO = new CreateProductDTO
        {
            Name = UniqueName(),
            Description = "original description",
            Price = 50.00m,
            Stock = 5
        };

        var createResponse = await TestClients.AsAdminAsync(_client, HttpMethod.Post, "/api/Product", ToJsonContent(createProductDTO));
        var createContent = await createResponse.Content.ReadAsStringAsync();
        var jsonDoc = System.Text.Json.JsonDocument.Parse(createContent);
        var productId = jsonDoc.RootElement.GetProperty("id").GetInt32();

        var updateDto = new UpdateProductDTO
        {
            Name = "", 
            Description = "",
            Price = -50.00m
        };

        var response = await TestClients.AsAdminAsync(_client, HttpMethod.Put, $"/api/Product/{productId}", ToJsonContent(updateDto));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Update_Product_Does_Not_Change_Stock()
    {
        var createResponse = await TestClients.AsAdminAsync(_client, HttpMethod.Post, "/api/Product", ToJsonContent(new CreateProductDTO
        {
            Name = UniqueName(),
            Description = "original description",
            Price = 50.00m,
            Stock = 5
        }));
        var created = System.Text.Json.JsonDocument.Parse(await createResponse.Content.ReadAsStringAsync());
        var productId = created.RootElement.GetProperty("id").GetInt32();

        var update = await TestClients.AsAdminAsync(_client, HttpMethod.Put, $"/api/Product/{productId}", ToJsonContent(new UpdateProductDTO
        {
            Name = "renamed",
            Description = "updated description",
            Price = 60m
        }));
        update.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var get = await _client.GetAsync($"/api/Product/{productId}");
        var body = System.Text.Json.JsonDocument.Parse(await get.Content.ReadAsStringAsync());
        body.RootElement.GetProperty("stock").GetInt32().Should().Be(5);
    }

    [Fact]
    public async Task Create_Product_With_Zero_Stock_Should_Return_Created()
    {
        var response = await TestClients.AsAdminAsync(_client, HttpMethod.Post, "/api/Product", ToJsonContent(new CreateProductDTO
        {
            Name = UniqueName(),
            Description = "empty shelf",
            Price = 10m,
            Stock = 0
        }));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Delete_Product_Without_Assignments_Should_Return_NoContent()
    {
        var createResponse = await TestClients.AsAdminAsync(_client, HttpMethod.Post, "/api/Product", ToJsonContent(new CreateProductDTO
        {
            Name = UniqueName(),
            Description = "to delete",
            Price = 10m,
            Stock = 3
        }));
        var created = System.Text.Json.JsonDocument.Parse(await createResponse.Content.ReadAsStringAsync());
        var productId = created.RootElement.GetProperty("id").GetInt32();

        var response = await TestClients.AsAdminAsync(_client, HttpMethod.Delete, $"/api/Product/{productId}");
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Delete_Assigned_Product_Should_Return_Conflict()
    {
        var userResponse = await TestClients.AsAdminAsync(_client, HttpMethod.Post, "/api/User", ToJsonContent(new CreateUserDTO
        {
            Username = $"owner_{Guid.NewGuid().ToString()[..8]}",
            Password = "testpassword123!",
            Email = $"owner_{Guid.NewGuid().ToString()[..8]}@mail.com"
        }));
        var userJson = System.Text.Json.JsonDocument.Parse(await userResponse.Content.ReadAsStringAsync());
        var userId = userJson.RootElement.GetProperty("id").GetInt32();

        var createResponse = await TestClients.AsAdminAsync(_client, HttpMethod.Post, "/api/Product", ToJsonContent(new CreateProductDTO
        {
            Name = UniqueName(),
            Description = "assigned",
            Price = 10m,
            Stock = 4
        }));
        var created = System.Text.Json.JsonDocument.Parse(await createResponse.Content.ReadAsStringAsync());
        var productId = created.RootElement.GetProperty("id").GetInt32();

        var assign = await TestClients.AsAdminAsync(_client, HttpMethod.Post, $"/api/users/{userId}/products", ToJsonContent(new AssignProductToUserDTO
        {
            ProductID = productId,
            Quantity = 1
        }));
        assign.StatusCode.Should().Be(HttpStatusCode.OK);

        var response = await TestClients.AsAdminAsync(_client, HttpMethod.Delete, $"/api/Product/{productId}");
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Delete_Product_Should_Return_Forbidden_For_NonAdmin()
    {
        var response = await _client.DeleteAsync("/api/Product/1");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}

