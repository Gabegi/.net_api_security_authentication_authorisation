namespace SecureApi.Infrastructure.Persistence.Models;

/// <summary>
/// Product entity - represents a product in the inventory system.
/// </summary>
public class Product
{
    /// <summary>
    /// Gets or sets the unique identifier for the product.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the product name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the product description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the product price.
    /// </summary>
    public decimal Price { get; set; }

    /// <summary>
    /// Gets or sets the product category.
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the stock quantity available.
    /// </summary>
    public int StockQuantity { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the product was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the user ID of the user who created this product.
    /// Null for products created before this field was added (seeded products).
    /// </summary>
    public int? CreatedByUserId { get; set; }
}
