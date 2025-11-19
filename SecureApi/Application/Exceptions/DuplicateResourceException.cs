namespace SecureApi.Application.Exceptions;

/// <summary>
/// Exception thrown when attempting to create a resource that already exists.
/// Typically used for unique constraint violations.
/// </summary>
public class DuplicateResourceException : Exception
{
    /// <summary>
    /// Initializes a new instance of the DuplicateResourceException class.
    /// </summary>
    /// <param name="message">The error message describing the duplicate resource.</param>
    public DuplicateResourceException(string message) : base(message)
    {
    }
}
