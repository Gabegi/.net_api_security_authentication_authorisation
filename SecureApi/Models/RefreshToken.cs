namespace SecureApi.Models;

using System.ComponentModel.DataAnnotations.Schema;

/// <summary>
/// Represents a refresh token for obtaining new access tokens without re-authentication.
/// </summary>
public class RefreshToken
{
    /// <summary>
    /// Unique identifier for the refresh token.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The actual refresh token value (cryptographically secure random string).
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// ID of the user who owns this refresh token.
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// Navigation property to the associated user.
    /// </summary>
    public User User { get; set; } = null!;

    /// <summary>
    /// When this refresh token expires.
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// When this refresh token was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When this refresh token was revoked (null if still active).
    /// </summary>
    public DateTime? RevokedAt { get; set; }

    /// <summary>
    /// IP address from which the token was created.
    /// </summary>
    public string CreatedByIp { get; set; } = string.Empty;

    /// <summary>
    /// Whether this refresh token is still active (not revoked and not expired).
    /// </summary>
    [NotMapped]
    public bool IsActive => RevokedAt == null && DateTime.UtcNow < ExpiresAt;
}
