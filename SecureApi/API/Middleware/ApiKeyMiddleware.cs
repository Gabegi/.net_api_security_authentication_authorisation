using Microsoft.EntityFrameworkCore;
using SecureApi.Infrastructure.Persistence;

namespace SecureApi.API.Middleware;

/// <summary>
/// Validates API keys for service-to-service authentication.
/// Checks the X-API-Key header and validates against the database.
/// </summary>
public class ApiKeyMiddleware
{
    private readonly RequestDelegate _next;
    private const string API_KEY_HEADER = "X-API-Key";

    /// <summary>
    /// Initializes a new instance of the <see cref="ApiKeyMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline</param>
    public ApiKeyMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    /// <summary>
    /// Invokes the middleware to validate API keys.
    /// </summary>
    /// <param name="context">The HTTP context</param>
    /// <param name="db">The database context</param>
    public async Task InvokeAsync(HttpContext context, ApplicationDbContext db)
    {
        // Only apply to specific endpoints that require API key authentication
        if (!RequiresApiKey(context.Request.Path))
        {
            await _next(context);
            return;
        }

        // Extract API key from header
        if (!context.Request.Headers.TryGetValue(API_KEY_HEADER, out var extractedKey))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { error = "API Key required in X-API-Key header" });
            return;
        }

        // Validate API key in database
        var apiKey = await db.ApiKeys
            .FirstOrDefaultAsync(k => k.Key == extractedKey.ToString() && k.IsActive);

        if (apiKey == null)
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { error = "Invalid or inactive API Key" });
            return;
        }

        // Check if the API key has expired
        if (apiKey.ExpiresAt.HasValue && apiKey.ExpiresAt.Value < DateTime.UtcNow)
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { error = "API Key has expired" });
            return;
        }

        // Update last used timestamp
        apiKey.LastUsedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        // Store API key in context items for use in endpoints
        context.Items["ApiKey"] = apiKey;

        await _next(context);
    }

    /// <summary>
    /// Determines which paths require API key authentication.
    /// </summary>
    /// <param name="path">The request path</param>
    /// <returns>True if the path requires API key authentication; otherwise, false</returns>
    private static bool RequiresApiKey(PathString path)
    {
        return path.StartsWithSegments("/api/webhooks", StringComparison.OrdinalIgnoreCase) ||
               path.StartsWithSegments("/api/partner", StringComparison.OrdinalIgnoreCase);
    }
}

/// <summary>
/// Extension methods for registering the API Key middleware.
/// </summary>
public static class ApiKeyMiddlewareExtensions
{
    /// <summary>
    /// Adds API key authentication middleware to the application.
    /// </summary>
    /// <param name="builder">The application builder</param>
    /// <returns>The application builder for chaining</returns>
    public static IApplicationBuilder UseApiKeyAuthentication(
        this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ApiKeyMiddleware>();
    }
}
