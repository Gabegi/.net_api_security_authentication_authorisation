namespace SecureApi.Application.DTOs.Responses;

/// <summary>
/// Standard error response for API errors.
/// </summary>
public class ErrorResponse
{
    /// <summary>
    /// Gets or sets the error message.
    /// </summary>
    public string Error { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the HTTP status code.
    /// </summary>
    public int StatusCode { get; set; }

    /// <summary>
    /// Gets or sets the trace ID for debugging and correlation.
    /// </summary>
    public string? TraceId { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the error occurred (UTC).
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets validation errors keyed by property name.
    /// </summary>
    public Dictionary<string, string[]>? Errors { get; set; }

    /// <summary>
    /// Gets or sets detailed error information (development only).
    /// </summary>
    public string? Details { get; set; }
}
