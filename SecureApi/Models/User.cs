namespace SecureApi.Models;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Represents a user entity in the system.
/// </summary>
public class User
{
    /// <summary>
    /// Gets or sets the unique identifier for the user.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the user's email address.
    /// </summary>
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Email must be a valid email address")]
    [StringLength(254, MinimumLength = 5, ErrorMessage = "Email must be between 5 and 254 characters")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user's password hash (never store plain text passwords).
    /// </summary>
    [Required(ErrorMessage = "Password hash is required")]
    [StringLength(512, MinimumLength = 60, ErrorMessage = "Password hash must be between 60 and 512 characters")]
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user's full name.
    /// </summary>
    [Required(ErrorMessage = "Full name is required")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Full name must be between 2 and 100 characters")]
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the date and time when the user was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time of the user's last login.
    /// </summary>
    public DateTime? LastLoginAt { get; set; }

    /// <summary>
    /// Gets or sets the user's role (e.g., "User", "Admin").
    /// </summary>
    public string Role { get; set; } = "User";
}
