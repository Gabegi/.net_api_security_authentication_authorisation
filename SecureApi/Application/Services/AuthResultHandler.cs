using SecureApi.Application.DTOs.Responses;

namespace SecureApi.Application.Services;

/// <summary>
/// Handles authentication operation results and maps exceptions to HTTP responses.
/// Centralizes HTTP result mapping for all auth endpoints.
/// </summary>
public class AuthResultHandler : IAuthResultHandler
{
    /// <summary>
    /// Handles registration operation results.
    /// Maps InvalidOperationException to 409 Conflict, other exceptions to 400 BadRequest.
    /// </summary>
    public async Task<IResult> HandleRegisterAsync(Func<Task<TokenResponse>> operation)
    {
        try
        {
            var tokenResponse = await operation();
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
    /// Handles login operation results.
    /// Maps UnauthorizedAccessException to 401 Unauthorized, other exceptions to 400 BadRequest.
    /// </summary>
    public async Task<IResult> HandleLoginAsync(Func<Task<TokenResponse>> operation)
    {
        try
        {
            var tokenResponse = await operation();
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
    /// Handles token refresh operation results.
    /// Maps UnauthorizedAccessException to 401 Unauthorized, other exceptions to 400 BadRequest.
    /// </summary>
    public async Task<IResult> HandleRefreshAsync(Func<Task<TokenResponse>> operation)
    {
        try
        {
            var tokenResponse = await operation();
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
    /// Handles logout operation results.
    /// Returns 200 OK on success, 400 BadRequest on any exception.
    /// </summary>
    public async Task<IResult> HandleLogoutAsync(Func<Task> operation)
    {
        try
        {
            await operation();
            return Results.Ok(new { message = "Logged out successfully" });
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new { error = "Logout failed", details = ex.Message });
        }
    }
}
