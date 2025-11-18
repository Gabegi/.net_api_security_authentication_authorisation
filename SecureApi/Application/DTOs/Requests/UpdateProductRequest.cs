namespace SecureApi.Application.DTOs.Requests;

/// <summary>
/// Request model for updating a product.
/// </summary>
public record UpdateProductRequest(
    string? Name,
    string? Description,
    decimal? Price,
    string? Category,
    int? StockQuantity
);
