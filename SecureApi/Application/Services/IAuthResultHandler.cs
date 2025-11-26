using SecureApi.Application.DTOs.Requests;
using SecureApi.Application.DTOs.Responses;

namespace SecureApi.Application.Services;

/// <summary>
/// Service interface for handling authentication operation results and mapping to HTTP responses.
/// Encapsulates exception handling and HTTP status code mapping for auth operations.
/// Sets secure HTTP-only cookies for refresh tokens to prevent XSS attacks.
/// </summary>
public interface IAuthResultHandler
{
    /// <summary>
    /// Handles the result of a registration operation and returns appropriate HTTP response.
    /// Sets refresh token as secure HTTP-only cookie.
    /// </summary>
    /// <param name="operation">The async operation to execute.</param>
    /// <param name="httpContext">The HTTP context to set cookies.</param>
    /// <returns>An IResult containing the HTTP response.</returns>
    Task<IResult> HandleRegisterAsync(Func<Task<TokenResponse>> operation, HttpContext httpContext);

    /// <summary>
    /// Handles the result of a login operation and returns appropriate HTTP response.
    /// Sets refresh token as secure HTTP-only cookie.
    /// </summary>
    /// <param name="operation">The async operation to execute.</param>
    /// <param name="httpContext">The HTTP context to set cookies.</param>
    /// <returns>An IResult containing the HTTP response.</returns>
    Task<IResult> HandleLoginAsync(Func<Task<TokenResponse>> operation, HttpContext httpContext);

    /// <summary>
    /// Handles the result of a token refresh operation and returns appropriate HTTP response.
    /// Sets new refresh token as secure HTTP-only cookie.
    /// </summary>
    /// <param name="operation">The async operation to execute.</param>
    /// <param name="httpContext">The HTTP context to set cookies.</param>
    /// <returns>An IResult containing the HTTP response.</returns>
    Task<IResult> HandleRefreshAsync(Func<Task<TokenResponse>> operation, HttpContext httpContext);

    /// <summary>
    /// Handles the result of a logout operation and returns appropriate HTTP response.
    /// Clears the refresh token cookie.
    /// </summary>
    /// <param name="operation">The async operation to execute.</param>
    /// <param name="httpContext">The HTTP context to clear cookies.</param>
    /// <returns>An IResult containing the HTTP response.</returns>
    Task<IResult> HandleLogoutAsync(Func<Task> operation, HttpContext httpContext);
}
