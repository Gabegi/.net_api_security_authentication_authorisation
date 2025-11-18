namespace SecureApi.Application.DTOs.Requests;

/// <summary>
/// Request model for creating a new product.
/// </summary>
public record CreateProductRequest(
    string Name,
    string? Description,
    decimal Price,
    string? Category,
    int StockQuantity
);
