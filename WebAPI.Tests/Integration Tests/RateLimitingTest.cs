namespace WebAPI.Tests.Integration_Tests;
using System.Net;
using System.Net.Http.Json;

using FluentAssertions;
using Xunit;
using WebAPI.Domain;

public class RateLimitingTest
{
    
    [Fact]
    public async Task Login_Should_Return_429_After_Exceeding_Rate_Limit()
    {
        using var factory = new CustomWebApplicationFactory();
        var _client = factory.CreateClient();
        
        var loginDTO = new LoginDTO { Email = "test@gmail.com", Password = "testpassword" };

        HttpResponseMessage? lastResponse = null;

        for (int i = 0; i < 6; i++)
            lastResponse = await _client.PostAsJsonAsync("/api/auth/login", loginDTO);
        
        lastResponse!.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);

    }

    [Fact]
    public async Task Login_Should_Allow_Requests_Within_Rate_Limit()
    {
        using var factory = new CustomWebApplicationFactory();
        var _client = factory.CreateClient();
        var loginDTO = new LoginDTO { Email = "test@gmail.com", Password = "testpassword" };
        for (int i = 0; i < 5; i++)
        {
            var lastResponse = await _client.PostAsJsonAsync("/api/auth/login", loginDTO);
            lastResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            
        }
        
    }
    
    

}