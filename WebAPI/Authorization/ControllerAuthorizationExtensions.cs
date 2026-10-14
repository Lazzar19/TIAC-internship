using Microsoft.AspNetCore.Mvc;
using WebAPI.Controllers;

namespace WebAPI.Authorization;

public static class ControllerAuthorizationExtensions
{
    public static ActionResult? DenyUnlessOwnerOrAdmin(this ControllerBase controller, int userId)
    {
        if (!CurrentUser.TryGetId(controller.User, out _))
            return controller.ApiProblem(StatusCodes.Status401Unauthorized, "Authentication is required.");

        if (!CurrentUser.CanAccessUser(controller.User, userId))
            return controller.ApiProblem(StatusCodes.Status403Forbidden, "You cannot access this user's data.");

        return null;
    }
}
