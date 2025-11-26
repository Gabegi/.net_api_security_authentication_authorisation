using SecureApi.Application.DTOs.Responses;

namespace SecureApi.Application.Services;

/// <summary>
/// Handles authentication operation results and maps exceptions to HTTP responses.
/// Centralizes HTTP result mapping for all auth endpoints.
/// Sets secure HTTP-only cookies for refresh tokens to prevent XSS attacks.
/// </summary>
public class AuthResultHandler : IAuthResultHandler
{
    /// <summary>
    /// Sets a secure HTTP-only cookie with the refresh token.
    /// Cookie flags protect against XSS and CSRF attacks.
    /// </summary>
    private static void SetRefreshTokenCookie(HttpContext httpContext, string refreshToken)
    {
        httpContext.Response.Cookies.Append(
            "refreshToken",
            refreshToken,
            new CookieOptions
            {
                HttpOnly = true,                          // JavaScript can't read it (prevents XSS)
                Secure = true,                            // HTTPS only (prevents MITM)
                SameSite = SameSiteMode.Strict,          // CSRF protection (don't send on cross-site requests)
                Expires = DateTimeOffset.UtcNow.AddDays(7)  // Matches refresh token expiry
            }
        );
    }

    /// <summary>
    /// Handles registration operation results.
    /// Maps InvalidOperationException to 409 Conflict, other exceptions to 400 BadRequest.
    /// Sets refresh token as secure HTTP-only cookie.
    /// </summary>
    public async Task<IResult> HandleRegisterAsync(Func<Task<TokenResponse>> operation, HttpContext httpContext)
    {
        try
        {
            var tokenResponse = await operation();
            SetRefreshTokenCookie(httpContext, tokenResponse.RefreshToken);
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
    /// Sets refresh token as secure HTTP-only cookie.
    /// </summary>
    public async Task<IResult> HandleLoginAsync(Func<Task<TokenResponse>> operation, HttpContext httpContext)
    {
        try
        {
            var tokenResponse = await operation();
            SetRefreshTokenCookie(httpContext, tokenResponse.RefreshToken);
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
    /// Sets new refresh token as secure HTTP-only cookie.
    /// </summary>
    public async Task<IResult> HandleRefreshAsync(Func<Task<TokenResponse>> operation, HttpContext httpContext)
    {
        try
        {
            var tokenResponse = await operation();
            SetRefreshTokenCookie(httpContext, tokenResponse.RefreshToken);
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
    /// Clears the refresh token cookie.
    /// </summary>
    public async Task<IResult> HandleLogoutAsync(Func<Task> operation, HttpContext httpContext)
    {
        try
        {
            await operation();
            // Clear the refresh token cookie by setting it to null with past expiration
            httpContext.Response.Cookies.Append(
                "refreshToken",
                "",
                new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTimeOffset.UnixEpoch  // Set to past date to delete cookie
                }
            );
            return Results.Ok(new { message = "Logged out successfully" });
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new { error = "Logout failed", details = ex.Message });
        }
    }
}
