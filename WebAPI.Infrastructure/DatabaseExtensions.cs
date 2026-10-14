namespace WebAPI.Infrastructure;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

public static class DatabaseExtensions
{
    public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");
        var environmentName = configuration["ASPNETCORE_ENVIRONMENT"]
            ?? configuration["DOTNET_ENVIRONMENT"]
            ?? Environments.Production;

        services.AddDbContext<ApplicationDbContext>(options =>
        {
            if (string.Equals(environmentName, Environments.Development, StringComparison.OrdinalIgnoreCase))
            {
                options.UseSqlite(connectionString)
                    .ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning));
                // The migration snapshot is the PostgreSQL model. SQLite reports that same model as pending because identity and timestamp types differ by provider.
            }
            else
            {
                options.UseNpgsql(connectionString);
            }
        });

        return services;
    }

    public static IServiceCollection AddApplicationCache(this IServiceCollection services, IConfiguration configuration)
    {
        var redisConfiguration = configuration["Redis:Configuration"]
            ?? configuration.GetConnectionString("Redis");

        if (!string.IsNullOrWhiteSpace(redisConfiguration))
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConfiguration;
                options.InstanceName = "webapi:";
            });
        }
        else
        {
            services.AddDistributedMemoryCache();
        }

        return services;
    }


}