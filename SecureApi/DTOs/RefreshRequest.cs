namespace SecureApi.DTOs;

/// <summary>
/// Request to refresh access token using a refresh token.
/// </summary>
public class RefreshRequest
{
    /// <summary>
    /// The refresh token value.
    /// </summary>
    public string RefreshToken { get; set; } = string.Empty;
}
