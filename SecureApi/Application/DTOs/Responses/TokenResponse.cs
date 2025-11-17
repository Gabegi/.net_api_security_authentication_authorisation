namespace SecureApi.Application.DTOs.Responses;

/// <summary>
/// Response containing authentication tokens after successful login or refresh.
/// </summary>
/// <param name="AccessToken">JWT access token for API requests.</param>
/// <param name="RefreshToken">Refresh token for obtaining new access tokens.</param>
/// <param name="ExpiresIn">Access token expiration time in seconds.</param>
public record TokenResponse(
    string AccessToken,
    string RefreshToken,
    int ExpiresIn);
