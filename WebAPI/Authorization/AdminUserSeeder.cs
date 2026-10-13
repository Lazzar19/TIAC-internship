using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WebAPI.Application.Interfaces;
using WebAPI.Application.Validators;
using WebAPI.Domain;

namespace WebAPI.Authorization;

public static class AdminUserSeeder
{
    public const string EmailKey = "Admin:Email";

    public static async Task SeedAsync(
        IServiceProvider services,
        IHostEnvironment environment,
        IConfiguration configuration,
        ILogger logger)
    {
        var email = configuration[EmailKey];
        var password = AdminCredentialReader.ReadPassword(configuration, environment.IsProduction());

        if (string.IsNullOrWhiteSpace(email) || password is null)
        {
            logger.LogWarning(
                "Admin seed skipped. Set {EmailKey} and {PasswordKey} with user-secrets or environment variables. Do not put the password in appsettings.json. In Production the password is read only from {PasswordVariable}.",
                EmailKey,
                AdminCredentialReader.PasswordKey,
                AdminCredentialReader.PasswordEnvironmentVariable);
            return;
        }

        var users = services.GetRequiredService<IUserRepository>();
        var existing = await users.GetByEmailAsync(email);
        if (existing is not null)
        {
            if (existing.Role != Role.Admin)
            {
                logger.LogWarning(
                    "Admin seed skipped. {Email} already belongs to a non-admin account.",
                    email);
            }

            return;
        }

        var hasher = services.GetRequiredService<IPasswordHasher>();
        await users.AddAsync(new User
        {
            Username = "admin",
            Email = email,
            PasswordHash = hasher.Hash(password),
            Role = Role.Admin
        });

        logger.LogInformation("Seeded admin account for {Email}.", email);
    }
}
