using SecureApi.Infrastructure.Persistence;
using SecureApi.Application.DTOs.Requests;
using SecureApi.Application.DTOs.Responses;
using SecureApi.API.Filters;
using SecureApi.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace SecureApi.API.Endpoints;

/// <summary>
/// Extension methods to map authentication endpoints.
/// </summary>
public static class AuthEndpoints
{
    /// <summary>
    /// Maps all authentication-related endpoints.
    /// </summary>
    /// <param name="app">The web application builder.</param>
    public static void MapAuthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/auth")
            .WithTags("Authentication");

        // Login endpoint
        group.MapPost("/login", HandleLogin)
            .WithName("Login")
            .WithSummary("Login with email and password")
            .WithDescription("Authenticates a user and returns access + refresh tokens")
            .AddEndpointFilter<ValidationFilter<LoginRequest>>()
            .Produces<TokenResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized);

        // Refresh token endpoint
        group.MapPost("/refresh", HandleRefresh)
            .WithName("RefreshToken")
            .WithSummary("Refresh access token")
            .WithDescription("Uses a refresh token to get a new access token")
            .AddEndpointFilter<ValidationFilter<RefreshRequest>>()
            .Produces<TokenResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized);

        // Logout endpoint
        group.MapPost("/logout", HandleLogout)
            .WithName("Logout")
            .WithSummary("Logout and revoke token")
            .WithDescription("Revokes the refresh token and logs out the user")
            .AddEndpointFilter<ValidationFilter<LogoutRequest>>()
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized);
    }

    /// <summary>
    /// Handles user login.
    /// </summary>
    private static async Task<IResult> HandleLogin(
        LoginRequest request,
        ApplicationDbContext db)
    {
        // Validation is already done by ValidationFilter!
        // This is just a placeholder implementation

        return Results.Ok(new TokenResponse(
            AccessToken: "temp_access_token",
            RefreshToken: "temp_refresh_token",
            ExpiresIn: 900
        ));
    }

    /// <summary>
    /// Handles token refresh.
    /// </summary>
    private static async Task<IResult> HandleRefresh(
        RefreshRequest request,
        ApplicationDbContext db)
    {
        // Validation is already done by ValidationFilter!

        return Results.Ok(new TokenResponse(
            AccessToken: "new_access_token",
            RefreshToken: "new_refresh_token",
            ExpiresIn: 900
        ));
    }

    /// <summary>
    /// Handles user logout.
    /// </summary>
    private static async Task<IResult> HandleLogout(
        LogoutRequest request,
        ApplicationDbContext db)
    {
        // Validation is already done by ValidationFilter!

        return Results.Ok(new { message = "Logged out successfully" });
    }
}
