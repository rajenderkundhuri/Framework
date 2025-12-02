using System.Text.Json;
using Framework.Domain.Common.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace Framework.Api.Middleware;

/// <summary>
/// Global exception handling middleware
/// </summary>
public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public ExceptionMiddleware(
        RequestDelegate next,
        ILogger<ExceptionMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, problemDetails) = exception switch
        {
            ValidationException validationEx => (
                StatusCodes.Status400BadRequest,
                CreateValidationProblemDetails(context, validationEx)),

            EntityNotFoundException notFoundEx => (
                StatusCodes.Status404NotFound,
                CreateProblemDetails(context, StatusCodes.Status404NotFound, notFoundEx.Code, notFoundEx.Message)),

            BusinessRuleException businessEx => (
                StatusCodes.Status422UnprocessableEntity,
                CreateProblemDetails(context, StatusCodes.Status422UnprocessableEntity, businessEx.Code, businessEx.Message)),

            ConflictException conflictEx => (
                StatusCodes.Status409Conflict,
                CreateProblemDetails(context, StatusCodes.Status409Conflict, conflictEx.Code, conflictEx.Message)),

            ForbiddenException forbiddenEx => (
                StatusCodes.Status403Forbidden,
                CreateProblemDetails(context, StatusCodes.Status403Forbidden, forbiddenEx.Code, forbiddenEx.Message)),

            UnauthorizedException unauthorizedEx => (
                StatusCodes.Status401Unauthorized,
                CreateProblemDetails(context, StatusCodes.Status401Unauthorized, unauthorizedEx.Code, unauthorizedEx.Message)),

            DomainException domainEx => (
                StatusCodes.Status400BadRequest,
                CreateProblemDetails(context, StatusCodes.Status400BadRequest, domainEx.Code, domainEx.Message)),

            _ => (
                StatusCodes.Status500InternalServerError,
                CreateProblemDetails(context, StatusCodes.Status500InternalServerError, "INTERNAL_ERROR",
                    _environment.IsDevelopment() ? exception.Message : "An unexpected error occurred."))
        };

        _logger.LogError(exception, "Unhandled exception: {Message}", exception.Message);

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = statusCode;

        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        await context.Response.WriteAsync(JsonSerializer.Serialize(problemDetails, options));
    }

    private static ProblemDetails CreateProblemDetails(
        HttpContext context,
        int statusCode,
        string code,
        string detail)
    {
        return new ProblemDetails
        {
            Status = statusCode,
            Title = code,
            Detail = detail,
            Instance = context.Request.Path,
            Extensions = { { "traceId", context.TraceIdentifier } }
        };
    }

    private static ValidationProblemDetails CreateValidationProblemDetails(
        HttpContext context,
        ValidationException exception)
    {
        return new ValidationProblemDetails(exception.Errors)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = exception.Code,
            Detail = exception.Message,
            Instance = context.Request.Path,
            Extensions = { { "traceId", context.TraceIdentifier } }
        };
    }
}

/// <summary>
/// Extension methods for exception middleware
/// </summary>
public static class ExceptionMiddlewareExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ExceptionMiddleware>();
    }
}
