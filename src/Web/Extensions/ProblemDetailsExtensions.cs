using Microsoft.AspNetCore.Mvc;

namespace Web.Extensions;

public static class ProblemDetailsExtensions
{
    public static IActionResult NotFoundProblem(this ControllerBase controller, string title, string detail)
    {
        return controller.NotFound(new ProblemDetails
        {
            Status = StatusCodes.Status404NotFound,
            Title = title,
            Detail = detail,
            Instance = controller.HttpContext.Request.Path
        });
    }

    public static IActionResult ConflictProblem(this ControllerBase controller, string title, string detail)
    {
        return controller.Conflict(new ProblemDetails
        {
            Status = StatusCodes.Status409Conflict,
            Title = title,
            Detail = detail,
            Instance = controller.HttpContext.Request.Path
        });
    }

    public static IActionResult UnprocessableEntityProblem(this ControllerBase controller, string title, string detail)
    {
        return controller.UnprocessableEntity(new ProblemDetails
        {
            Status = StatusCodes.Status422UnprocessableEntity,
            Title = title,
            Detail = detail,
            Instance = controller.HttpContext.Request.Path
        });
    }
}
