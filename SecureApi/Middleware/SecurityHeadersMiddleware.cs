namespace SecureApi.Middleware;

/// <summary>
/// Middleware for adding security headers to all HTTP responses.
/// Protects against common web vulnerabilities and privacy concerns.
/// </summary>
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    /// <summary>
    /// Initializes a new instance of the SecurityHeadersMiddleware class.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline</param>
    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    /// <summary>
    /// Invokes the middleware to add security headers to the response.
    /// </summary>
    /// <param name="context">The HTTP context</param>
    public async Task InvokeAsync(HttpContext context)
    {
        // Remove identifying headers (don't advertise tech stack)
        context.Response.Headers.Remove("Server");
        context.Response.Headers.Remove("X-Powered-By");

        var headers = context.Response.Headers;

        // ───────────────────────────────────────────────────────────────
        // SECURITY HEADERS
        // ───────────────────────────────────────────────────────────────

        // X-Content-Type-Options: nosniff
        // Prevents browser from MIME-sniffing a response away from declared content-type
        headers.Append("X-Content-Type-Options", "nosniff");

        // X-Frame-Options: DENY
        // Prevents page from being embedded in iframes (clickjacking protection)
        headers.Append("X-Frame-Options", "DENY");

        // X-XSS-Protection: 1; mode=block
        // Enables browser XSS filtering and blocks page if attack is detected
        headers.Append("X-XSS-Protection", "1; mode=block");

        // Referrer-Policy: no-referrer
        // Don't send referrer information to other sites (privacy)
        headers.Append("Referrer-Policy", "no-referrer");

        // Content-Security-Policy: restrictive defaults
        // Only allow resources from same origin, prevent framing, restrict forms
        headers.Append("Content-Security-Policy",
            "default-src 'self'; " +
            "frame-ancestors 'none'; " +
            "base-uri 'self'; " +
            "form-action 'self'");

        // Permissions-Policy: disable unused features
        // Prevent JavaScript from accessing sensitive browser features
        // Without this: Any JS on your page can access camera 📷, microphone 🎤, location 📍, etc.
        headers.Append("Permissions-Policy",
            "camera=(), " +
            "microphone=(), " +
            "geolocation=(), " +
            "payment=(), " +
            "usb=(), " +
            "magnetometer=(), " +
            "gyroscope=(), " +
            "accelerometer=()");

        // ───────────────────────────────────────────────────────────────
        // Call next middleware
        // ───────────────────────────────────────────────────────────────

        await _next(context);
    }
}

/// <summary>
/// Extension method to add SecurityHeadersMiddleware to the application pipeline.
/// </summary>
public static class SecurityHeadersMiddlewareExtensions
{
    /// <summary>
    /// Adds the SecurityHeadersMiddleware to the application pipeline.
    /// Should be added early in the middleware pipeline.
    /// </summary>
    /// <param name="app">The application builder</param>
    /// <returns>The application builder</returns>
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
    {
        return app.UseMiddleware<SecurityHeadersMiddleware>();
    }
}
