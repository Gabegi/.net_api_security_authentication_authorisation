using SecureApi.Application.DTOs.Requests;
using SecureApi.Application.DTOs.Responses;
using SecureApi.API.Filters;
using SecureApi.API.Helpers;
using SecureApi.Application.Services;

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

        // Register endpoint
        group.MapPost("/register", HandleRegister)
            .WithName("Register")
            .WithSummary("Register a new user")
            .WithDescription("Creates a new user account and returns access + refresh tokens")
            .AddEndpointFilter<ValidationFilter<RegisterRequest>>()
            .Produces<TokenResponse>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status409Conflict);

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
    /// Handles user registration.
    /// Delegates business logic to AuthService, returns appropriate HTTP responses.
    /// </summary>
    private static async Task<IResult> HandleRegister(
        RegisterRequest request,
        IAuthService authService,
        HttpContext httpContext)
    {
        try
        {
            var tokenResponse = await authService.RegisterAsync(
                request,
                HttpContextHelper.GetClientIp(httpContext)
            );

            return Results.Created("/api/auth/register", tokenResponse);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Conflict(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new { error = "Registration failed", details = ex.Message });
        }
    }

    /// <summary>
    /// Handles user login.
    /// Delegates business logic to AuthService, returns appropriate HTTP responses.
    /// </summary>
    private static async Task<IResult> HandleLogin(
        LoginRequest request,
        IAuthService authService,
        HttpContext httpContext)
    {
        try
        {
            var tokenResponse = await authService.LoginAsync(
                request,
                HttpContextHelper.GetClientIp(httpContext)
            );

            return Results.Ok(tokenResponse);
        }
        catch (UnauthorizedAccessException)
        {
            return Results.Unauthorized();
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new { error = "Login failed", details = ex.Message });
        }
    }

    /// <summary>
    /// Handles token refresh.
    /// Delegates business logic to AuthService, returns appropriate HTTP responses.
    /// </summary>
    private static async Task<IResult> HandleRefresh(
        RefreshRequest request,
        IAuthService authService,
        HttpContext httpContext)
    {
        try
        {
            var tokenResponse = await authService.RefreshTokenAsync(
                request,
                HttpContextHelper.GetClientIp(httpContext)
            );

            return Results.Ok(tokenResponse);
        }
        catch (UnauthorizedAccessException)
        {
            return Results.Unauthorized();
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new { error = "Token refresh failed", details = ex.Message });
        }
    }

    /// <summary>
    /// Handles user logout.
    /// Delegates business logic to AuthService, returns appropriate HTTP responses.
    /// </summary>
    private static async Task<IResult> HandleLogout(
        LogoutRequest request,
        IAuthService authService)
    {
        try
        {
            await authService.LogoutAsync(request);
            return Results.Ok(new { message = "Logged out successfully" });
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new { error = "Logout failed", details = ex.Message });
        }
    }
}
