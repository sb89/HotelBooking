using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;

namespace Web.Extensions;

public static class ValidationExtensions
{
    public static IActionResult ToValidationProblem(this ValidationResult result, ControllerBase controller)
    {
        var errors = result.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.ErrorMessage).ToArray()
            );

        return new BadRequestObjectResult(new ValidationProblemDetails(errors)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Validation Error",
            Detail = "One or more validation errors occurred",
            Instance = controller.HttpContext.Request.Path
        });
    }
}