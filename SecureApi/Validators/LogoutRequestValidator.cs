using FluentValidation;
using SecureApi.DTOs;

namespace SecureApi.Validators;

/// <summary>
/// Validator for LogoutRequest using FluentValidation.
/// </summary>
public class LogoutRequestValidator : AbstractValidator<LogoutRequest>
{
    /// <summary>
    /// Initializes a new instance of the LogoutRequestValidator class.
    /// </summary>
    public LogoutRequestValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage("Refresh token is required")
            .Length(32, 500).WithMessage("Refresh token must be between 32 and 500 characters");
    }
}
