using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc.Testing;
using WebAPI.Infrastructure;

namespace WebAPI.Tests.Integration_Tests;


public class AdminTestWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = $"AdminTestDB_{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
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
            {
                services.Remove(coreDescriptor);
            }

            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseInMemoryDatabase(_dbName);
            });

           
            services.AddAuthentication("TestScheme")
                .AddScheme<AuthenticationSchemeOptions, AdminTestAuthenticationHandler>("TestScheme", options => { });

            var sp = services.BuildServiceProvider();
            using (var scope = sp.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                dbContext.Database.EnsureCreated();
            }
        });
    }
}

