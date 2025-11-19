using SecureApi.Application.DTOs.Requests;
using SecureApi.Infrastructure.Persistence;
using SecureApi.Infrastructure.Persistence.Models;

namespace SecureApi.Tests.Helpers;

/// <summary>
/// Helper class to generate consistent test data.
/// Provides methods for creating users, products, and requests with unique values.
/// </summary>
public static class TestDataGenerator
{
    // Constants for test credentials
    public const string DefaultPassword = "Password123!";
    public const int TestBcryptWorkFactor = 4; // Fast for tests, still secure

    #region Unique Value Generators

    /// <summary>
    /// Generates a unique email address for testing.
    /// </summary>
    public static string GenerateUniqueEmail() =>
        $"test_{Guid.NewGuid().ToString()[..8]}@example.com";

    /// <summary>
    /// Generates a unique username for testing.
    /// </summary>
    public static string GenerateUniqueUsername() =>
        $"user_{Guid.NewGuid().ToString()[..8]}";

    #endregion

    #region Request DTOs

    /// <summary>
    /// Creates a valid RegisterRequest with unique email.
    /// </summary>
    public static RegisterRequest CreateValidRegisterRequest(
        DateTime? birthDate = null,
        string? email = null,
        string password = DefaultPassword)
    {
        return new RegisterRequest
        {
            Email = email ?? GenerateUniqueEmail(),
            Password = password,
            FullName = GenerateUniqueUsername(),
            BirthDate = birthDate ?? new DateTime(1990, 1, 1)
        };
    }

    /// <summary>
    /// Creates a LoginRequest.
    /// </summary>
    public static LoginRequest CreateLoginRequest(
        string email,
        string password = DefaultPassword)
    {
        return new LoginRequest
        {
            Email = email,
            Password = password
        };
    }

    /// <summary>
    /// Creates a valid CreateProductRequest.
    /// </summary>
    public static CreateProductRequest CreateValidProductRequest(
        string? name = null,
        decimal price = 99.99m,
        string? category = null,
        int stockQuantity = 50)
    {
        return new CreateProductRequest(
            name ?? $"Product_{Guid.NewGuid().ToString()[..8]}",
            "Test product description",
            price,
            category ?? "Electronics",
            stockQuantity
        );
    }

    /// <summary>
    /// Creates an UpdateProductRequest.
    /// </summary>
    public static UpdateProductRequest CreateUpdateProductRequest(
        string? name = null,
        decimal? price = null,
        string? category = null)
    {
        return new UpdateProductRequest(
            name,
            "Updated description",
            price,
            category,
            null
        );
    }

    #endregion

    #region User Entities

    /// <summary>
    /// Creates a User entity for database seeding.
    /// </summary>
    public static User CreateUser(
        string? email = null,
        string fullName = "Test User",
        DateTime? birthDate = null,
        string role = "User",
        string password = DefaultPassword)
    {
        return new User
        {
            Email = email ?? GenerateUniqueEmail(),
            FullName = fullName,
            BirthDate = birthDate ?? new DateTime(1990, 1, 1),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(
                password,
                workFactor: TestBcryptWorkFactor),
            Role = role,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates an admin User for testing admin endpoints.
    /// </summary>
    public static User CreateAdminUser(
        string? email = null,
        string password = DefaultPassword)
    {
        return CreateUser(
            email,
            "Admin User",
            role: "Admin",
            password: password);
    }

    /// <summary>
    /// Creates an underage User (under 18 years old) for testing age restrictions.
    /// </summary>
    public static User CreateUnderageUser(
        string? email = null,
        string password = DefaultPassword)
    {
        // Born less than 18 years ago (using DateTime.Today for validator compatibility)
        var birthDate = DateTime.Today.AddYears(-17); // 17 years old
        return CreateUser(
            email,
            "Young User",
            birthDate,
            password: password);
    }

    /// <summary>
    /// Creates an adult User (over 18 years old) for testing age-restricted endpoints.
    /// </summary>
    public static User CreateAdultUser(
        string? email = null,
        string password = DefaultPassword)
    {
        // Born more than 18 years ago (using DateTime.Today for validator compatibility)
        var birthDate = DateTime.Today.AddYears(-25); // 25 years old
        return CreateUser(
            email,
            "Adult User",
            birthDate,
            password: password);
    }

    #endregion

    #region Product Entities

    /// <summary>
    /// Creates a Product entity for database seeding.
    /// </summary>
    public static Product CreateProduct(
        string? name = null,
        decimal price = 99.99m,
        string category = "Electronics",
        int stockQuantity = 50,
        int? createdByUserId = null)
    {
        return new Product
        {
            Name = name ?? $"Product_{Guid.NewGuid().ToString()[..8]}",
            Description = "Test product description",
            Price = price,
            Category = category,
            StockQuantity = stockQuantity,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = createdByUserId
        };
    }

    /// <summary>
    /// Creates an adult-category product.
    /// </summary>
    public static Product CreateAdultProduct(int? createdByUserId = null)
    {
        return CreateProduct(
            name: $"Adult_Product_{Guid.NewGuid().ToString()[..8]}",
            category: "Adult",
            createdByUserId: createdByUserId);
    }

    #endregion

    #region Database Seeding Helpers

    /// <summary>
    /// Seeds a user to the database and returns it with its generated ID.
    /// </summary>
    public static User SeedUser(
        ApplicationDbContext db,
        string? email = null,
        string role = "User",
        DateTime? birthDate = null)
    {
        var user = CreateUser(email, role: role, birthDate: birthDate);
        db.Users.Add(user);
        db.SaveChanges();
        return user;
    }

    /// <summary>
    /// Seeds an admin user to the database and returns it.
    /// </summary>
    public static User SeedAdminUser(ApplicationDbContext db, string? email = null)
    {
        var admin = CreateAdminUser(email);
        db.Users.Add(admin);
        db.SaveChanges();
        return admin;
    }

    /// <summary>
    /// Seeds a product to the database and returns it with its generated ID.
    /// </summary>
    public static Product SeedProduct(
        ApplicationDbContext db,
        string? name = null,
        int? createdByUserId = null)
    {
        var product = CreateProduct(name, createdByUserId: createdByUserId);
        db.Products.Add(product);
        db.SaveChanges();
        return product;
    }

    /// <summary>
    /// Seeds multiple products to the database.
    /// </summary>
    public static List<Product> SeedProducts(
        ApplicationDbContext db,
        int count,
        int? createdByUserId = null)
    {
        var products = Enumerable.Range(0, count)
            .Select(_ => CreateProduct(createdByUserId: createdByUserId))
            .ToList();

        db.Products.AddRange(products);
        db.SaveChanges();
        return products;
    }

    /// <summary>
    /// Creates a test user and returns both the user entity and login credentials.
    /// Useful for tests that need to create a user and then login.
    /// </summary>
    public static (User User, string Email, string Password) CreateUserWithCredentials(
        string? email = null,
        string password = DefaultPassword,
        string role = "User")
    {
        var userEmail = email ?? GenerateUniqueEmail();
        var user = CreateUser(userEmail, role: role, password: password);
        return (user, userEmail, password);
    }

    #endregion
}
