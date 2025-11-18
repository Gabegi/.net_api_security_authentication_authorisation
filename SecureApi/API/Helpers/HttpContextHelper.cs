namespace SecureApi.API.Helpers;

/// <summary>
/// Helper methods for extracting information from HttpContext.
/// </summary>
public static class HttpContextHelper
{
    /// <summary>
    /// Gets the client's IP address from the HTTP request.
    /// Checks for proxy headers first (X-Forwarded-For), then falls back to direct connection IP.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>The client's IP address, or "unknown" if unable to determine.</returns>
    /// <remarks>
    /// This method is used to track where refresh tokens are created for security auditing.
    /// Order of checks:
    /// 1. X-Forwarded-For header (when behind proxy/load balancer)
    /// 2. RemoteIpAddress (direct connection)
    /// 3. "unknown" fallback
    /// </remarks>
    public static string GetClientIp(HttpContext context)
    {
        // Step 1: Check for X-Forwarded-For header
        // This header is set by proxies/load balancers and contains the real client IP
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();

        if (!string.IsNullOrEmpty(forwardedFor))
        {
            // X-Forwarded-For can contain multiple IPs (proxy chain)
            // The first one is the original client IP
            var ips = forwardedFor.Split(',');
            var clientIp = ips[0].Trim();

            if (!string.IsNullOrEmpty(clientIp))
            {
                return clientIp;
            }
        }

        // Step 2: Fall back to direct connection IP
        // This is used when not behind a proxy
        var remoteIp = context.Connection.RemoteIpAddress?.ToString();

        if (!string.IsNullOrEmpty(remoteIp))
        {
            return remoteIp;
        }

        // Step 3: Return "unknown" if unable to determine
        return "unknown";
    }

    /// <summary>
    /// Gets the user agent string from the HTTP request.
    /// Used to track which device/browser created the token.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>The user agent string, or "unknown" if not present.</returns>
    public static string GetUserAgent(HttpContext context)
    {
        return context.Request.Headers["User-Agent"].FirstOrDefault() ?? "unknown";
    }
}
