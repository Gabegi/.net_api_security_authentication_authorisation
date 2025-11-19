using Microsoft.AspNetCore.Authorization;
using SecureApi.Application.Authorization.Requirements;
using SecureApi.Application.Authorization.Handlers;

namespace SecureApi.API.Extensions;

/// <summary>
/// Extension methods for configuring authorization policies and handlers.
/// </summary>
public static class AuthorizationServiceExtensions
{
    /// <summary>
    /// Adds authorization policies and custom handlers to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddAuthorizationPolicies(
        this IServiceCollection services)
    {
        // Authorization with Policies
        services.AddAuthorization(options =>
        {
            // Policy 1: Admin Only - requires Admin role
            options.AddPolicy("AdminOnly", policy =>
            {
                policy.RequireRole("Admin");
            });

            // Policy 2: User Only - requires User role
            options.AddPolicy("UserOnly", policy =>
            {
                policy.RequireRole("User");
            });

            // Policy 3: Must be 18+ - custom requirement with DB lookup
            options.AddPolicy("MustBeOver18", policy =>
            {
                policy.Requirements.Add(new MustBeOver18Requirement());
            });

            // Policy 4: Admin OR User - either role is accepted
            options.AddPolicy("AdminOrUser", policy =>
            {
                policy.RequireRole("Admin", "User");
            });
        });

        // Register the custom authorization handler (scoped because it uses DbContext)
        services.AddScoped<IAuthorizationHandler, MustBeOver18Handler>();

        return services;
    }
}
