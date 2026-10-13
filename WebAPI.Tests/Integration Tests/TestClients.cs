namespace WebAPI.Tests.Integration_Tests;

public static class TestClients
{
    public const string UserIdHeader = "X-Test-User-Id";
    public const string RoleHeader = "X-Test-Role";

    public static Task<HttpResponseMessage> SendAsync(
        HttpClient client,
        HttpMethod method,
        string url,
        HttpContent? content = null,
        int? userId = null,
        string? role = null)
    {
        var request = new HttpRequestMessage(method, url) { Content = content };
        if (userId.HasValue)
            request.Headers.Add(UserIdHeader, userId.Value.ToString());
        if (!string.IsNullOrWhiteSpace(role))
            request.Headers.Add(RoleHeader, role);

        return client.SendAsync(request);
    }

    public static Task<HttpResponseMessage> AsAdminAsync(
        HttpClient client,
        HttpMethod method,
        string url,
        HttpContent? content = null) =>
        SendAsync(client, method, url, content, role: "Admin");

    public static Task<HttpResponseMessage> AsUserAsync(
        HttpClient client,
        int userId,
        HttpMethod method,
        string url,
        HttpContent? content = null) =>
        SendAsync(client, method, url, content, userId, role: "User");
}
