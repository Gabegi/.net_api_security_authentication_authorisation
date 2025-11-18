namespace SecureApi.Application.DTOs.Requests;

/// <summary>
/// Request for user registration.
/// </summary>
public class RegisterRequest
{
    /// <summary>
    /// User's email address.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// User's password.
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// User's full name.
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// User's date of birth (required for age verification).
    /// </summary>
    public DateTime BirthDate { get; set; }
}
