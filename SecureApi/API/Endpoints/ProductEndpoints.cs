namespace SecureApi.API.Endpoints;

using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using SecureApi.Infrastructure.Persistence;
using SecureApi.Infrastructure.Persistence.Models;

/// <summary>
/// Product API endpoints - manages product CRUD operations
/// </summary>
public static class ProductEndpoints
{
    /// <summary>
    /// Maps all product endpoints to the application
    /// </summary>
    public static void MapProductEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/products")
            .WithTags("Products")
            .WithOpenApi();

        // GET /api/products
        group.MapGet("/", async (ApplicationDbContext db) =>
            Results.Ok(await db.Products.ToListAsync()))
            .WithName("GetAllProducts")
            .WithSummary("Get all products");

        // GET /api/products/{id}
        group.MapGet("/{id}", async (int id, ApplicationDbContext db) =>
        {
            var product = await db.Products.FindAsync(id);
            return product is null ? Results.NotFound() : Results.Ok(product);
        })
            .WithName("GetProductById")
            .WithSummary("Get product by ID");

        // POST /api/products
        group.MapPost("/", async (CreateProductRequest request, ApplicationDbContext db) =>
        {
            var product = new Product
            {
                Name = request.Name,
                Description = request.Description ?? string.Empty,
                Price = request.Price,
                Category = request.Category ?? "Uncategorized",
                StockQuantity = request.StockQuantity
            };
            db.Products.Add(product);
            await db.SaveChangesAsync();
            return Results.Created($"/api/products/{product.Id}", product);
        })
            .WithName("CreateProduct")
            .WithSummary("Create a new product")
            .RequireAuthorization();

        // PUT /api/products/{id}
        group.MapPut("/{id}", async (int id, UpdateProductRequest request, ApplicationDbContext db) =>
        {
            var product = await db.Products.FindAsync(id);
            if (product is null) return Results.NotFound();

            if (!string.IsNullOrWhiteSpace(request.Name)) product.Name = request.Name;
            if (!string.IsNullOrWhiteSpace(request.Description)) product.Description = request.Description;
            if (request.Price.HasValue) product.Price = request.Price.Value;
            if (!string.IsNullOrWhiteSpace(request.Category)) product.Category = request.Category;
            if (request.StockQuantity.HasValue) product.StockQuantity = request.StockQuantity.Value;

            await db.SaveChangesAsync();
            return Results.Ok(product);
        })
            .WithName("UpdateProduct")
            .WithSummary("Update a product");

        // DELETE /api/products/{id}
        group.MapDelete("/{id}", async (int id, ApplicationDbContext db) =>
        {
            var product = await db.Products.FindAsync(id);
            if (product is null) return Results.NotFound();

            db.Products.Remove(product);
            await db.SaveChangesAsync();
            return Results.NoContent();
        })
            .WithName("DeleteProduct")
            .WithSummary("Delete a product");
    }
}

/// <summary>
/// Request model for creating a new product
/// </summary>
public record CreateProductRequest(
    string Name,
    string? Description,
    decimal Price,
    string? Category,
    int StockQuantity
);

/// <summary>
/// Request model for updating a product
/// </summary>
public record UpdateProductRequest(
    string? Name,
    string? Description,
    decimal? Price,
    string? Category,
    int? StockQuantity
);
