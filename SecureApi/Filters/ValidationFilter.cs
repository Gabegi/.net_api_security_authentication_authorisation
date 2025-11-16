using FluentValidation;

namespace SecureApi.Filters;

/// <summary>
/// Endpoint filter for automatic validation of request objects using FluentValidation.
/// </summary>
/// <typeparam name="T">The type of request to validate.</typeparam>
public class ValidationFilter<T> : IEndpointFilter
{
    /// <summary>
    /// Invokes the validation filter.
    /// </summary>
    /// <param name="context">The endpoint filter invocation context.</param>
    /// <param name="next">The next filter in the pipeline.</param>
    /// <returns>The result from the next filter or a validation problem response.</returns>
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        // Get the validator for this request type
        var validator = context.HttpContext.RequestServices
            .GetService<IValidator<T>>();

        // If no validator is registered, skip validation
        if (validator == null)
            return await next(context);

        // Get the request object from the invocation arguments
        var request = context.Arguments.OfType<T>().FirstOrDefault();
        if (request == null)
            return await next(context);

        // Validate the request
        var validationResult = await validator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            // Return validation errors as a problem response
            return Results.ValidationProblem(validationResult.ToDictionary());
        }

        // Validation passed, continue to the next filter/handler
        return await next(context);
    }
}
