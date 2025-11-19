using FluentValidation;
using SecureApi.Application.DTOs.Requests;

namespace SecureApi.Application.Validators;

/// <summary>
/// Validator for UpdateProductRequest using FluentValidation.
/// </summary>
public class UpdateProductRequestValidator : AbstractValidator<UpdateProductRequest>
{
    /// <summary>
    /// Initializes a new instance of the UpdateProductRequestValidator class.
    /// </summary>
    public UpdateProductRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MinimumLength(2)
            .MaximumLength(200)
            .When(x => x.Name != null)
            .WithMessage("Name must be between 2 and 200 characters when provided");

        RuleFor(x => x.Description)
            .MaximumLength(1000)
            .When(x => x.Description != null)
            .WithMessage("Description must not exceed 1000 characters");

        RuleFor(x => x.Price)
            .GreaterThan(0)
            .When(x => x.Price.HasValue)
            .WithMessage("Price must be greater than 0 when provided");

        RuleFor(x => x.Category)
            .NotEmpty()
            .MaximumLength(100)
            .When(x => x.Category != null)
            .WithMessage("Category must not exceed 100 characters when provided");

        RuleFor(x => x.StockQuantity)
            .GreaterThanOrEqualTo(0)
            .When(x => x.StockQuantity.HasValue)
            .WithMessage("Stock quantity cannot be negative");
    }
}
