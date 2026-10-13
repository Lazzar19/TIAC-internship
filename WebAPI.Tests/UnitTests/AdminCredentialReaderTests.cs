using FluentAssertions;
using Microsoft.Extensions.Configuration;
using WebAPI.Authorization;

namespace WebAPI.Tests.UnitTests;

public class AdminCredentialReaderTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "admin-seed-" + Guid.NewGuid().ToString("N"));

    public AdminCredentialReaderTests()
    {
        Directory.CreateDirectory(_directory);
    }

    [Fact]
    public void Password_In_AppSettings_Is_Ignored()
    {
        var configuration = Build(appSettingsPassword: "from-file");

        AdminCredentialReader.ReadPassword(configuration, production: false).Should().BeNull();
    }

    [Fact]
    public void Password_From_Non_File_Configuration_Is_Used_In_Development()
    {
        var configuration = Build(appSettingsPassword: "from-file", secret: "from-secret");

        AdminCredentialReader.ReadPassword(configuration, production: false).Should().Be("from-secret");
    }

    [Fact]
    public void Production_Reads_Only_The_Environment_Variable()
    {
        var configuration = Build(appSettingsPassword: "from-file", secret: "from-secret");
        Environment.SetEnvironmentVariable(AdminCredentialReader.PasswordEnvironmentVariable, "from-env");

        AdminCredentialReader.ReadPassword(configuration, production: true).Should().Be("from-env");
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable(AdminCredentialReader.PasswordEnvironmentVariable, null);
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, recursive: true);
    }

    private IConfigurationRoot Build(string? appSettingsPassword, string? secret = null)
    {
        var path = Path.Combine(_directory, "appsettings.json");
        File.WriteAllText(path, "{\"Admin\":{\"Password\":\"" + appSettingsPassword + "\"}}");

        var builder = new ConfigurationBuilder().AddJsonFile(path, optional: false);
        if (secret is not null)
        {
            builder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                [AdminCredentialReader.PasswordKey] = secret
            });
        }

        return builder.Build();
    }
}
