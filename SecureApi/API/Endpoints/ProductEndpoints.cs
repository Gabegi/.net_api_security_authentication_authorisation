namespace SecureApi.API.Endpoints;

using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using SecureApi.Infrastructure.Persistence;
using SecureApi.Infrastructure.Persistence.Models;
using SecureApi.Application.DTOs.Requests;

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

        // GET /api/products - Public endpoint
        group.MapGet("/", async (ApplicationDbContext db) =>
            Results.Ok(await db.Products.ToListAsync()))
            .WithName("GetAllProducts")
            .WithSummary("Get all products")
            .AllowAnonymous();

        // GET /api/products/{id} - Public endpoint
        group.MapGet("/{id}", async (int id, ApplicationDbContext db) =>
        {
            var product = await db.Products.FindAsync(id);
            return product is null ? Results.NotFound() : Results.Ok(product);
        })
            .WithName("GetProductById")
            .WithSummary("Get product by ID")
            .AllowAnonymous();

        // GET /api/products/adult - Age-restricted content (18+ only)
        group.MapGet("/adult/list", async (ApplicationDbContext db) =>
            Results.Ok(await db.Products.Where(p => p.Category == "Adult").ToListAsync()))
            .WithName("GetAdultProducts")
            .WithSummary("Get adult products (18+ only)")
            .RequireAuthorization("MustBeOver18");

        // POST /api/products - Requires User role
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
            .RequireAuthorization("UserOnly");

        // PUT /api/products/{id} - Requires User role
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
            .WithSummary("Update a product")
            .RequireAuthorization("UserOnly");

        // DELETE /api/products/{id} - Requires Admin role
        group.MapDelete("/{id}", async (int id, ApplicationDbContext db) =>
        {
            var product = await db.Products.FindAsync(id);
            if (product is null) return Results.NotFound();

            db.Products.Remove(product);
            await db.SaveChangesAsync();
            return Results.NoContent();
        })
            .WithName("DeleteProduct")
            .WithSummary("Delete a product")
            .RequireAuthorization("AdminOnly");
    }
}
