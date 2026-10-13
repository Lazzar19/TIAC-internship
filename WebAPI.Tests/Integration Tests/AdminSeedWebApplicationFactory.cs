using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WebAPI.Infrastructure;

namespace WebAPI.Tests.Integration_Tests;

public class AdminSeedWebApplicationFactory : WebApplicationFactory<Program>
{
    public const string Email = "seed-admin@example.com";
    public const string Password = "AdminPass123!";

    private readonly string _dbName = $"AdminSeedDB_{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "K7mPq29XvL8nR4zBwT1yHs6JfUc3AeN9Qd5MiGp0VkYr8CxZu2FoElW7hSn4",
                ["Jwt:Issuer"] = "WebAPI",
                ["Jwt:Audience"] = "WebApiUsers",
                ["Admin:Email"] = Email,
                ["Admin:Password"] = Password
            });
        });

        builder.ConfigureServices(ReplaceDatabase);
    }

    private void ReplaceDatabase(IServiceCollection services)
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
    }
}
