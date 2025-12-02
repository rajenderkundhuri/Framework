using System.Security.Claims;
using Framework.Application.Common.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Framework.Api.Controllers;

/// <summary>
/// Base controller for API endpoints
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[Produces("application/json")]
public abstract class ApiControllerBase : ControllerBase
{
    private ISender? _mediator;

    /// <summary>
    /// Gets the MediatR sender
    /// </summary>
    protected ISender Mediator => _mediator ??= HttpContext.RequestServices.GetRequiredService<ISender>();

    /// <summary>
    /// Gets the current user's ID from claims
    /// </summary>
    protected Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    /// <summary>
    /// Gets the current user's email from claims
    /// </summary>
    protected string? GetCurrentUserEmail()
    {
        return User.FindFirst(ClaimTypes.Email)?.Value;
    }

    /// <summary>
    /// Checks if current user has a specific permission
    /// </summary>
    protected bool HasPermission(string permission)
    {
        return User.Claims
            .Where(c => c.Type == "permission")
            .Any(c => c.Value == permission);
    }

    /// <summary>
    /// Gets all permissions for the current user
    /// </summary>
    protected IEnumerable<string> GetCurrentUserPermissions()
    {
        return User.Claims
            .Where(c => c.Type == "permission")
            .Select(c => c.Value);
    }

    /// <summary>
    /// Gets all roles for the current user
    /// </summary>
    protected IEnumerable<string> GetCurrentUserRoles()
    {
        return User.Claims
            .Where(c => c.Type == ClaimTypes.Role)
            .Select(c => c.Value);
    }

    /// <summary>
    /// Handles a Result and returns appropriate ActionResult
    /// </summary>
    protected IActionResult HandleResult(Result result)
    {
        if (result.IsSuccess)
        {
            return Ok();
        }

        return HandleFailure(result);
    }

    /// <summary>
    /// Handles a Result with value and returns appropriate ActionResult
    /// </summary>
    protected IActionResult HandleResult<T>(Result<T> result)
    {
        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        return HandleFailure(result);
    }

    /// <summary>
    /// Handles a created Result and returns 201 Created
    /// </summary>
    protected IActionResult HandleCreatedResult<T>(Result<T> result, string actionName, object? routeValues = null)
    {
        if (result.IsSuccess)
        {
            return CreatedAtAction(actionName, routeValues, result.Value);
        }

        return HandleFailure(result);
    }

    /// <summary>
    /// Handles a Result for DELETE operations (returns 204 No Content on success)
    /// </summary>
    protected IActionResult HandleDeleteResult(Result result)
    {
        if (result.IsSuccess)
        {
            return NoContent();
        }

        return HandleFailure(result);
    }

    private IActionResult HandleFailure(Result result)
    {
        return result.ErrorCode switch
        {
            "NOT_FOUND" => NotFound(CreateProblemDetails(result, StatusCodes.Status404NotFound)),
            "UNAUTHORIZED" => Unauthorized(CreateProblemDetails(result, StatusCodes.Status401Unauthorized)),
            "FORBIDDEN" => Forbid(),
            "CONFLICT" => Conflict(CreateProblemDetails(result, StatusCodes.Status409Conflict)),
            "VALIDATION_ERROR" => BadRequest(CreateValidationProblemDetails(result)),
            _ => BadRequest(CreateProblemDetails(result, StatusCodes.Status400BadRequest))
        };
    }

    private ProblemDetails CreateProblemDetails(Result result, int statusCode)
    {
        return new ProblemDetails
        {
            Status = statusCode,
            Title = result.ErrorCode ?? "Error",
            Detail = result.Error,
            Instance = HttpContext.Request.Path
        };
    }

    private ValidationProblemDetails CreateValidationProblemDetails(Result result)
    {
        var problemDetails = new ValidationProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Validation Error",
            Detail = result.Error,
            Instance = HttpContext.Request.Path
        };

        if (result.ValidationErrors != null)
        {
            foreach (var error in result.ValidationErrors)
            {
                problemDetails.Errors.Add(error.Key, error.Value);
            }
        }

        return problemDetails;
    }
}
