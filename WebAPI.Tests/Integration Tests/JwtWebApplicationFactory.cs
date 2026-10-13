using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WebAPI.Infrastructure;

namespace WebAPI.Tests.Integration_Tests;

public class JwtWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = $"JwtTestDB_{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "K7mPq29XvL8nR4zBwT1yHs6JfUc3AeN9Qd5MiGp0VkYr8CxZu2FoElW7hSn4",
                ["Jwt:Issuer"] = "WebAPI",
                ["Jwt:Audience"] = "WebApiUsers"
            });
        });

        builder.ConfigureServices(services =>
        {
            var descriptor =
                services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));

            if (descriptor != null)
                services.Remove(descriptor);

            var coreServiceDescriptors = services
                .Where(d => d.ServiceType.FullName?.StartsWith("Microsoft.EntityFrameworkCore") == true)
                .ToList();

            foreach (var coreDescriptor in coreServiceDescriptors)
                services.Remove(coreDescriptor);

            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseInMemoryDatabase(_dbName);
            });
        });
    }
}
