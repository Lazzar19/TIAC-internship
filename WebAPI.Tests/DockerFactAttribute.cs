using System.Net.Sockets;

namespace WebAPI.Tests;

public sealed class DockerFactAttribute : FactAttribute
{
    public DockerFactAttribute()
    {
        if (!IsDockerAvailable())
            Skip = "Docker is not available, so the PostgreSQL container test is skipped.";
    }

    private static bool IsDockerAvailable()
    {
        if (File.Exists("/var/run/docker.sock"))
            return true;

        var dockerHost = Environment.GetEnvironmentVariable("DOCKER_HOST");
        if (string.IsNullOrWhiteSpace(dockerHost))
            return false;

        if (!dockerHost.StartsWith("tcp://", StringComparison.OrdinalIgnoreCase))
            return true;

        var endpoint = dockerHost["tcp://".Length..];
        var colon = endpoint.LastIndexOf(':');
        if (colon <= 0 || !int.TryParse(endpoint[(colon + 1)..], out var port))
            return false;

        try
        {
            using var client = new TcpClient();
            var connect = client.ConnectAsync(endpoint[..colon], port);
            return connect.Wait(TimeSpan.FromMilliseconds(300)) && client.Connected;
        }
        catch (SocketException)
        {
            return false;
        }
    }
}
