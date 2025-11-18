using SecureApi.Application.DTOs.Requests;
using SecureApi.Application.DTOs.Responses;

namespace SecureApi.Application.Services;

/// <summary>
/// Service interface for authentication operations (register, login, refresh, logout).
/// Encapsulates all business logic related to user authentication and token management.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Registers a new user with the provided credentials.
    /// </summary>
    /// <param name="request">The registration request containing email, password, and name.</param>
    /// <param name="ipAddress">The client IP address for security auditing.</param>
    /// <returns>A token response containing access token, refresh token, and expiry.</returns>
    /// <exception cref="InvalidOperationException">Thrown if user with email already exists.</exception>
    Task<TokenResponse> RegisterAsync(RegisterRequest request, string ipAddress);

    /// <summary>
    /// Authenticates a user with email and password credentials.
    /// </summary>
    /// <param name="request">The login request containing email and password.</param>
    /// <param name="ipAddress">The client IP address for security auditing.</param>
    /// <returns>A token response containing access token, refresh token, and expiry.</returns>
    /// <exception cref="UnauthorizedAccessException">Thrown if credentials are invalid.</exception>
    Task<TokenResponse> LoginAsync(LoginRequest request, string ipAddress);

    /// <summary>
    /// Refreshes an expired access token using a valid refresh token.
    /// Implements token rotation: old refresh token is revoked, new one is issued.
    /// </summary>
    /// <param name="request">The refresh request containing the refresh token.</param>
    /// <param name="ipAddress">The client IP address for security auditing.</param>
    /// <returns>A token response with new access and refresh tokens.</returns>
    /// <exception cref="UnauthorizedAccessException">Thrown if refresh token is invalid or expired.</exception>
    Task<TokenResponse> RefreshTokenAsync(RefreshRequest request, string ipAddress);

    /// <summary>
    /// Logs out a user by revoking their refresh token.
    /// </summary>
    /// <param name="request">The logout request containing the refresh token.</param>
    /// <returns>A completed task.</returns>
    Task LogoutAsync(LogoutRequest request);
}
