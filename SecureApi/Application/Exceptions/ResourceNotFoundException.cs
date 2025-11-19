namespace SecureApi.Application.Exceptions;

/// <summary>
/// Exception thrown when a requested resource is not found.
/// </summary>
public class ResourceNotFoundException : Exception
{
    /// <summary>
    /// Initializes a new instance of the ResourceNotFoundException class.
    /// </summary>
    /// <param name="resourceName">The name of the resource (e.g., "Product", "User").</param>
    /// <param name="key">The identifier of the resource that was not found.</param>
    public ResourceNotFoundException(string resourceName, object key)
        : base($"{resourceName} with key '{key}' not found")
    {
    }
}
