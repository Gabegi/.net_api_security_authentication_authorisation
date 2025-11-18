using SecureApi.Application.DTOs.Requests;
using SecureApi.Application.DTOs.Responses;

namespace SecureApi.Application.Services;

/// <summary>
/// Service interface for handling authentication operation results and mapping to HTTP responses.
/// Encapsulates exception handling and HTTP status code mapping for auth operations.
/// </summary>
public interface IAuthResultHandler
{
    /// <summary>
    /// Handles the result of a registration operation and returns appropriate HTTP response.
    /// </summary>
    /// <param name="operation">The async operation to execute.</param>
    /// <returns>An IResult containing the HTTP response.</returns>
    Task<IResult> HandleRegisterAsync(Func<Task<TokenResponse>> operation);

    /// <summary>
    /// Handles the result of a login operation and returns appropriate HTTP response.
    /// </summary>
    /// <param name="operation">The async operation to execute.</param>
    /// <returns>An IResult containing the HTTP response.</returns>
    Task<IResult> HandleLoginAsync(Func<Task<TokenResponse>> operation);

    /// <summary>
    /// Handles the result of a token refresh operation and returns appropriate HTTP response.
    /// </summary>
    /// <param name="operation">The async operation to execute.</param>
    /// <returns>An IResult containing the HTTP response.</returns>
    Task<IResult> HandleRefreshAsync(Func<Task<TokenResponse>> operation);

    /// <summary>
    /// Handles the result of a logout operation and returns appropriate HTTP response.
    /// </summary>
    /// <param name="operation">The async operation to execute.</param>
    /// <returns>An IResult containing the HTTP response.</returns>
    Task<IResult> HandleLogoutAsync(Func<Task> operation);
}
