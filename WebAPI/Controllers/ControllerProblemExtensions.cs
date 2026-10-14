using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers;

public static class ControllerProblemExtensions
{
    public static ActionResult ApiProblem(this ControllerBase controller, int statusCode, string detail)
        => controller.Problem(detail: detail, statusCode: statusCode);
}
