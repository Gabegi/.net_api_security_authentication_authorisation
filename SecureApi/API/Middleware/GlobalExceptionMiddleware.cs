using System.Diagnostics;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using SecureApi.Application.DTOs.Responses;
using SecureApi.Application.Exceptions;

namespace SecureApi.API.Middleware;

/// <summary>
/// Global exception handling middleware that catches all unhandled exceptions
/// and returns standardized error responses.
/// </summary>
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly IHostEnvironment _env;

    /// <summary>
    /// Initializes a new instance of the GlobalExceptionMiddleware class.
    /// </summary>
    public GlobalExceptionMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionMiddleware> logger,
        IHostEnvironment env)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _env = env ?? throw new ArgumentNullException(nameof(env));
    }

    /// <summary>
    /// Invokes the middleware to handle the request or catch exceptions.
    /// </summary>
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

    /// <summary>
    /// Handles exceptions by mapping them to appropriate HTTP responses.
    /// </summary>
    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        // Create error response based on exception type
        var errorResponse = exception switch
        {
            // Custom domain exceptions
            ResourceNotFoundException ex => CreateErrorResponse(404, "Resource not found", ex),
            DuplicateResourceException ex => CreateErrorResponse(409, ex.Message, ex),

            // FluentValidation errors - special handling for multiple errors
            ValidationException ex => CreateValidationErrorResponse(ex),

            // Database exceptions (unique constraint violations)
            DbUpdateException ex when ex.InnerException?.Message.Contains("UNIQUE constraint") == true
                => CreateErrorResponse(409, "A resource with this value already exists", ex),

            // Authorization exceptions
            UnauthorizedAccessException ex => CreateErrorResponse(401, "Unauthorized", ex),

            // Standard exceptions
            ArgumentException ex => CreateErrorResponse(400, ex.Message, ex),
            InvalidOperationException ex => CreateErrorResponse(400, ex.Message, ex),

            // Catch-all for unexpected errors
            _ => CreateErrorResponse(500, "An unexpected error occurred", exception)
        };

        // Log the exception with appropriate level
        LogException(exception, errorResponse.StatusCode);

        // Write error response
        context.Response.StatusCode = errorResponse.StatusCode;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(errorResponse);
    }

    /// <summary>
    /// Creates a standard error response.
    /// </summary>
    private ErrorResponse CreateErrorResponse(int statusCode, string message, Exception exception)
    {
        return new ErrorResponse
        {
            Error = message,
            StatusCode = statusCode,
            TraceId = Activity.Current?.Id ?? "unknown",
            Timestamp = DateTime.UtcNow,
            Details = _env.IsDevelopment() ? exception.ToString() : null
        };
    }

    /// <summary>
    /// Creates an error response specifically for validation errors.
    /// Includes all validation failures grouped by property.
    /// </summary>
    private ErrorResponse CreateValidationErrorResponse(ValidationException exception)
    {
        // Group validation errors by property name
        var errors = exception.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.ErrorMessage).ToArray()
            );

        return new ErrorResponse
        {
            Error = "Validation failed",
            StatusCode = 400,
            TraceId = Activity.Current?.Id ?? "unknown",
            Timestamp = DateTime.UtcNow,
            Errors = errors
        };
    }

    /// <summary>
    /// Logs exceptions with appropriate severity levels.
    /// </summary>
    private void LogException(Exception exception, int statusCode)
    {
        if (statusCode >= 500)
        {
            // Server errors - log as error with full exception
            _logger.LogError(exception, "Unhandled server error: {Message}", exception.Message);
        }
        else if (statusCode == 401 || statusCode == 403)
        {
            // Authorization failures - log as warning without full stack
            _logger.LogWarning("Authorization failed: {Message}", exception.Message);
        }
        else if (statusCode == 400)
        {
            // Client errors - log as information
            _logger.LogInformation(exception, "Client error: {Message}", exception.Message);
        }
        else
        {
            // Other errors - log as information
            _logger.LogInformation(exception, "Request error: {Message}", exception.Message);
        }
    }
}

/// <summary>
/// Extension methods for registering global exception middleware.
/// </summary>
public static class GlobalExceptionMiddlewareExtensions
{
    /// <summary>
    /// Adds the global exception handling middleware to the pipeline.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <returns>The application builder for chaining.</returns>
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
    {
        return app.UseMiddleware<GlobalExceptionMiddleware>();
    }
}
