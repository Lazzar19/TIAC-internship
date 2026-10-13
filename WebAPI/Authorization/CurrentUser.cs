using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace WebAPI.Authorization;

public static class CurrentUser
{
    public const string AdminRole = "Admin";

    public static bool TryGetId(ClaimsPrincipal principal, out int userId)
    {
        var raw = principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? principal.FindFirstValue(JwtRegisteredClaimNames.Sub);

        return int.TryParse(raw, out userId) && userId > 0;
    }

    public static bool IsAdmin(ClaimsPrincipal principal) =>
        principal.IsInRole(AdminRole);

    public static bool CanAccessUser(ClaimsPrincipal principal, int userId) =>
        TryGetId(principal, out var currentId) && (IsAdmin(principal) || currentId == userId);
}
