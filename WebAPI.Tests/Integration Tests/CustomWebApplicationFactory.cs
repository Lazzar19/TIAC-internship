using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Hosting;

namespace WebAPI.Tests.Integration_Tests;

using FluentAssertions;
using Xunit;

using WebAPI.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;


public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    
    private readonly string _dbName = $"IntegrationTestDB_{Guid.NewGuid()}";

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

            // Add an in-memory database for testing - 
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseInMemoryDatabase(_dbName);
            });

            
            services.AddAuthentication("TestScheme")
                .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>("TestScheme", options => { });

          

        });
        
       


    }
    
        
    
    
}