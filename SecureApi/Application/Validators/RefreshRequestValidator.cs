using FluentValidation;
using SecureApi.Application.DTOs.Requests;

namespace SecureApi.Application.Validators;

/// <summary>
/// Validator for RefreshRequest using FluentValidation.
/// </summary>
public class RefreshRequestValidator : AbstractValidator<RefreshRequest>
{
    /// <summary>
    /// Initializes a new instance of the RefreshRequestValidator class.
    /// </summary>
    public RefreshRequestValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage("Refresh token is required")
            .Length(32, 500).WithMessage("Refresh token must be between 32 and 500 characters");
    }
}
