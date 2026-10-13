using Microsoft.Extensions.Configuration;

namespace WebAPI.Authorization;

public static class AdminCredentialReader
{
    public const string PasswordKey = "Admin:Password";
    public const string PasswordEnvironmentVariable = "Admin__Password";

    public static string? ReadPassword(IConfiguration configuration, bool production)
    {
        if (production)
            return NullIfEmpty(Environment.GetEnvironmentVariable(PasswordEnvironmentVariable));

        if (configuration is not IConfigurationRoot root)
            return null;

        foreach (var provider in root.Providers.Reverse())
        {
            if (IsAppSettingsFile(provider))
                continue;

            if (provider.TryGet(PasswordKey, out var value))
            {
                var password = NullIfEmpty(value);
                if (password is not null)
                    return password;
            }
        }

        return null;
    }

    private static bool IsAppSettingsFile(IConfigurationProvider provider)
    {
        var source = provider.GetType().GetProperty("Source")?.GetValue(provider);
        var path = source?.GetType().GetProperty("Path")?.GetValue(source) as string;
        if (string.IsNullOrWhiteSpace(path))
            return false;

        var name = Path.GetFileName(path);
        return name.StartsWith("appsettings", StringComparison.OrdinalIgnoreCase)
            && name.EndsWith(".json", StringComparison.OrdinalIgnoreCase);
    }

    private static string? NullIfEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value;
}
