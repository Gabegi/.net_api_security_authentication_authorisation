namespace SecureApi.Infrastructure.Persistence.Models;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Represents an API key for service authentication
/// </summary>
public class ApiKey
{
    /// <summary>
    /// Gets or sets the unique identifier for the API key.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The actual key (e.g., "sk_live_abc123...")
    /// </summary>
    [Required(ErrorMessage = "API Key is required")]
    [StringLength(512, MinimumLength = 32, ErrorMessage = "API Key must be between 32 and 512 characters")]
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Friendly name (e.g., "Stripe Webhook", "Partner XYZ")
    /// </summary>
    [Required(ErrorMessage = "Name is required")]
    [StringLength(255, MinimumLength = 1, ErrorMessage = "Name must be between 1 and 255 characters")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Owner of this key
    /// </summary>
    [Required(ErrorMessage = "Owner is required")]
    [StringLength(255, MinimumLength = 1, ErrorMessage = "Owner must be between 1 and 255 characters")]
    public string Owner { get; set; } = string.Empty;

    /// <summary>
    /// What this key can access (e.g., ["webhooks", "read:products"])
    /// Stored as JSON array
    /// </summary>
    [Required(ErrorMessage = "Scopes are required")]
    public string Scopes { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the date and time when the API key was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the expiration date of the API key.
    /// Null means the key never expires.
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Gets or sets whether this API key is currently active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets the date and time of the last usage of this API key.
    /// </summary>
    public DateTime? LastUsedAt { get; set; }
}
