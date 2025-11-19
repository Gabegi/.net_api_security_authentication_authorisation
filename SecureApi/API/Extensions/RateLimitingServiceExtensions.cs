using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace SecureApi.API.Extensions;

/// <summary>
/// Extension methods for configuring rate limiting policies.
/// </summary>
public static class RateLimitingServiceExtensions
{
    /// <summary>
    /// Adds rate limiting policies to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddRateLimitingPolicies(
        this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            // Auth endpoints: 5 requests per minute
            // Prevents brute force attacks on login/register
            options.AddFixedWindowLimiter("auth", opt =>
            {
                opt.PermitLimit = 5;
                opt.Window = TimeSpan.FromMinutes(1);
                opt.AutoReplenishment = true;
            });

            // API endpoints: 100 requests per minute
            // Prevents general DoS attacks
            options.AddFixedWindowLimiter("api", opt =>
            {
                opt.PermitLimit = 100;
                opt.Window = TimeSpan.FromMinutes(1);
                opt.AutoReplenishment = true;
            });

            // Global fallback: 1000 requests per minute
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        });

        return services;
    }
}
