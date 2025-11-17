namespace SecureApi.Application.DTOs.Requests;

/// <summary>
/// Request to logout and revoke refresh token.
/// </summary>
public class LogoutRequest
{
    /// <summary>
    /// The refresh token to revoke.
    /// </summary>
    public string RefreshToken { get; set; } = string.Empty;
}
