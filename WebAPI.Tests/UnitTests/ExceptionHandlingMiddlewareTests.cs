using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using WebAPI.Middleware;

namespace WebAPI.Tests.UnitTests;

public class ExceptionHandlingMiddlewareTests
{
    [Fact]
    public async Task Unhandled_Exception_Returns_Problem_Details_Without_The_Exception_Text()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        context.TraceIdentifier = "trace-123";
        var middleware = new ExceptionHandlingMiddleware(
            _ => throw new InvalidOperationException("database password"),
            NullLogger<ExceptionHandlingMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        context.Response.Body.Position = 0;
        using var json = await JsonDocument.ParseAsync(context.Response.Body);
        json.RootElement.GetProperty("status").GetInt32().Should().Be(500);
        json.RootElement.GetProperty("detail").GetString().Should().Be("Unexpected error occurred, try again.");
        json.RootElement.GetProperty("detail").GetString().Should().NotContain("database password");
        json.RootElement.GetProperty("traceId").GetString().Should().Be("trace-123");
    }
}
