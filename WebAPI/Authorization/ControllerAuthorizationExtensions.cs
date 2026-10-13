using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Authorization;

public static class ControllerAuthorizationExtensions
{
    public static ActionResult? DenyUnlessOwnerOrAdmin(this ControllerBase controller, int userId)
    {
        if (!CurrentUser.TryGetId(controller.User, out _))
            return controller.Unauthorized();

        if (!CurrentUser.CanAccessUser(controller.User, userId))
            return controller.StatusCode(StatusCodes.Status403Forbidden);

        return null;
    }
}
