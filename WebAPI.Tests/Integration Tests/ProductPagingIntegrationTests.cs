using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using WebAPI.Domain;

namespace WebAPI.Tests.Integration_Tests;

public class ProductPagingIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _client;

    public ProductPagingIntegrationTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Page_Below_One_Returns_The_First_Page(int value)
    {
        var response = await _client.GetAsync($"/api/product?pageNumber={value}&pageSize={value}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = await response.Content.ReadFromJsonAsync<PageResult<ProductDTO>>(JsonOptions);
        page!.PageNumber.Should().Be(1);
        page.PageSize.Should().Be(1);
    }

    [Fact]
    public async Task Page_Size_Of_10000_Is_Capped_At_50()
    {
        var response = await _client.GetAsync("/api/product?pageSize=10000");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = await response.Content.ReadFromJsonAsync<PageResult<ProductDTO>>(JsonOptions);
        page!.PageSize.Should().Be(50);
    }
}
