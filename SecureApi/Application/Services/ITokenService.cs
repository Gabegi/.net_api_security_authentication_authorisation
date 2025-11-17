using SecureApi.Application.DTOs.Responses;
using SecureApi.Infrastructure.Persistence.Models;

namespace SecureApi.Application.Services;

/// <summary>
/// Interface for JWT token generation and refresh token management.
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Generates a short-lived JWT access token for authenticated requests.
    /// </summary>
    /// <param name="userId">The user's unique identifier.</param>
    /// <param name="email">The user's email address.</param>
    /// <param name="role">The user's role.</param>
    /// <returns>The JWT access token.</returns>
    string GenerateAccessToken(int userId, string email, string role);

    /// <summary>
    /// Generates a long-lived refresh token for obtaining new access tokens.
    /// </summary>
    /// <param name="ipAddress">The IP address requesting the token.</param>
    /// <returns>A RefreshToken entity (not yet saved to database).</returns>
    RefreshToken GenerateRefreshToken(string ipAddress);

    /// <summary>
    /// Refreshes an access token using a valid refresh token.
    /// Revokes the old refresh token and issues new tokens.
    /// </summary>
    /// <param name="token">The refresh token value.</param>
    /// <param name="ipAddress">The IP address requesting the refresh.</param>
    /// <returns>TokenResponse with new access and refresh tokens.</returns>
    Task<TokenResponse> RefreshTokenAsync(string token, string ipAddress);

    /// <summary>
    /// Revokes a refresh token (invalidates it for logout).
    /// </summary>
    /// <param name="token">The refresh token to revoke.</param>
    Task RevokeTokenAsync(string token);
}
