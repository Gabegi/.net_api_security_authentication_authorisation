namespace SecureApi.Models;

using System.ComponentModel.DataAnnotations;

// ===========================
// Request Models
// ===========================

/// <summary>
/// Represents a user registration request.
/// </summary>
public class RegisterRequest
{
    /// <summary>
    /// Gets or sets the user's email address.
    /// </summary>
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Email must be a valid email address")]
    [StringLength(254, MinimumLength = 5, ErrorMessage = "Email must be between 5 and 254 characters")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user's password.
    /// Password must be at least 8 characters long and should contain uppercase, lowercase, numbers, and special characters.
    /// </summary>
    [Required(ErrorMessage = "Password is required")]
    [StringLength(128, MinimumLength = 8, ErrorMessage = "Password must be between 8 and 128 characters")]
    [RegularExpression(
        @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$",
        ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter, one number, and one special character (@$!%*?&)")]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user's full name.
    /// </summary>
    [Required(ErrorMessage = "Full name is required")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Full name must be between 2 and 100 characters")]
    [RegularExpression(
        @"^[a-zA-Z\s'-]+$",
        ErrorMessage = "Full name can only contain letters, spaces, hyphens, and apostrophes")]
    public string FullName { get; set; } = string.Empty;
}

/// <summary>
/// Represents a user login request.
/// </summary>
public class LoginRequest
{
    /// <summary>
    /// Gets or sets the user's email address.
    /// </summary>
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Email must be a valid email address")]
    [StringLength(254, MinimumLength = 5, ErrorMessage = "Email must be between 5 and 254 characters")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user's password.
    /// </summary>
    [Required(ErrorMessage = "Password is required")]
    [StringLength(128, MinimumLength = 8, ErrorMessage = "Password must be between 8 and 128 characters")]
    public string Password { get; set; } = string.Empty;
}

// ===========================
// Response Models
// ===========================

/// <summary>
/// Represents a user response (safe to return in API responses - no sensitive data).
/// </summary>
public class UserResponse
{
    /// <summary>
    /// Gets or sets the user's unique identifier.
    /// </summary>
    [Required]
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the user's email address.
    /// </summary>
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user's full name.
    /// </summary>
    [Required]
    [StringLength(100)]
    public string FullName { get; set; } = string.Empty;
}

/// <summary>
/// Represents an authentication response.
/// </summary>
public class AuthResponse
{
    /// <summary>
    /// Gets or sets a value indicating whether the authentication was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets a message describing the result of the authentication operation.
    /// </summary>
    [Required]
    [StringLength(500)]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user information (only populated if authentication was successful).
    /// </summary>
    public UserResponse? User { get; set; }
}

/// <summary>
/// Represents a JWT token response.
/// </summary>
public class TokenResponse
{
    /// <summary>
    /// Gets or sets the JWT token.
    /// </summary>
    [Required(ErrorMessage = "Token is required")]
    [MinLength(20, ErrorMessage = "Token must be at least 20 characters")]
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the date and time when the token expires.
    /// </summary>
    [Required(ErrorMessage = "Expiration time is required")]
    public DateTime ExpiresAt { get; set; }
}
