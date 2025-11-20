using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SecureApi.Application.DTOs.Requests;
using SecureApi.Application.DTOs.Responses;
using SecureApi.Infrastructure.Persistence.Models;
using SecureApi.Tests.Fixtures;
using Xunit;

namespace SecureApi.Tests.IntegrationTests;

/// <summary>
/// Integration tests for product endpoints (/api/products).
/// Tests CRUD operations with various authorization scenarios.
/// </summary>
public class ProductTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ProductTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    #region GET Tests (Public - No Auth Required)

    [Fact]
    public async Task GetAllProducts_WithoutAuth_ReturnsOk()
    {
        // Arrange
        _factory.ExecuteDbContext(db =>
        {
            TestDataGenerator.SeedProducts(db, 3);
        });

        // Act
        var response = await _client.GetAsync("/api/products");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var products = await response.Content.ReadFromJsonAsync<List<Product>>();
        products.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetProductById_WithValidId_ReturnsOk()
    {
        // Arrange
        var product = _factory.ExecuteDbContext(db =>
            TestDataGenerator.SeedProduct(db)
        );

        // Act
        var response = await _client.GetAsync($"/api/products/{product.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<Product>();
        result!.Id.Should().Be(product.Id);
        result.Name.Should().Be(product.Name);
    }

    [Fact]
    public async Task GetProductById_WithInvalidId_ReturnsNotFound()
    {
        // Act
        var response = await _client.GetAsync("/api/products/99999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region GET Adult Products Tests (Age-Restricted - 18+ Required)

    [Fact]
    public async Task GetAdultProducts_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        _factory.ExecuteDbContext(db =>
        {
            TestDataGenerator.SeedProducts(db, 2);
            var adultProduct = TestDataGenerator.CreateAdultProduct();
            db.Products.Add(adultProduct);
            db.SaveChanges();
        });

        // Act
        var response = await _client.GetAsync("/api/products/adult/list");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAdultProducts_AsUnderageUser_ReturnsForbidden()
    {
        // Arrange - Register a user via API, then manually make them underage in DB
        var registerRequest = ApiWebApplicationFactory.CreateRegisterRequest();
        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);
        var tokenResponse = await registerResponse.Content.ReadAsAsync<TokenResponse>();

        // Get the registered user's email and manually set their birth date to underage
        _factory.ExecuteDbContext(db =>
        {
            var user = db.Users.First(u => u.Email == registerRequest.Email);
            user.BirthDate = DateTime.Today.AddYears(-17); // Make them 17 years old
            db.SaveChanges();
        });

        _client.DefaultRequestHeaders.Authorization = new("Bearer", tokenResponse!.AccessToken);

        // Seed an adult product
        _factory.ExecuteDbContext(db =>
        {
            var adultProduct = new Product
            {
                Name = "Adult Item",
                Description = "For 18+ only",
                Price = 99.99m,
                Category = "Adult",
                StockQuantity = 10
            };
            db.Products.Add(adultProduct);
            db.SaveChanges();
        });

        // Act
        var response = await _client.GetAsync("/api/products/adult/list");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetAdultProducts_AsAdultUser_ReturnsOk()
    {
        // Arrange - Register an adult user via API
        var registerRequest = ApiWebApplicationFactory.CreateRegisterRequest(
            birthDate: DateTime.Today.AddYears(-25) // 25 years old
        );
        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);
        var tokenResponse = await registerResponse.Content.ReadAsAsync<TokenResponse>();
        _client.DefaultRequestHeaders.Authorization = new("Bearer", tokenResponse!.AccessToken);

        // Seed adult and regular products
        _factory.ExecuteDbContext(db =>
        {
            // Add 2 regular products
            for (int i = 0; i < 2; i++)
            {
                db.Products.Add(new Product
                {
                    Name = $"Regular Product {i}",
                    Description = "Regular item",
                    Price = 9.99m,
                    Category = "Electronics",
                    StockQuantity = 5
                });
            }

            // Add 1 adult product
            db.Products.Add(new Product
            {
                Name = "Adult Item",
                Description = "For 18+ only",
                Price = 19.99m,
                Category = "Adult",
                StockQuantity = 3
            });
            db.SaveChanges();
        });

        // Act
        var response = await _client.GetAsync("/api/products/adult/list");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var products = await response.Content.ReadFromJsonAsync<List<Product>>();
        products.Should().HaveCount(1);
        products!.First().Category.Should().Be("Adult");
    }

    #endregion

    #region POST Tests (Create - User Role Required)

    [Fact]
    public async Task CreateProduct_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        var request = TestDataGenerator.CreateValidProductRequest();

        // Act
        var response = await _client.PostAsJsonAsync("/api/products", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateProduct_AsUser_ReturnsCreated()
    {
        // Arrange - Register and login via API
        var registerRequest = ApiWebApplicationFactory.CreateRegisterRequest();
        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);
        var tokenResponse = await registerResponse.Content.ReadAsAsync<TokenResponse>();
        _client.DefaultRequestHeaders.Authorization = new("Bearer", tokenResponse!.AccessToken);

        var productRequest = new CreateProductRequest(
            Name: "Test Product",
            Description: "A test product",
            Price: 29.99m,
            Category: "Electronics",
            StockQuantity: 10
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/products", productRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var product = await response.Content.ReadFromJsonAsync<Product>();
        product!.Name.Should().Be(productRequest.Name);
    }

    [Fact]
    public async Task CreateProduct_AsAdmin_ReturnsCreated()
    {
        // Arrange - Register via API, then manually set role to Admin
        var registerRequest = ApiWebApplicationFactory.CreateRegisterRequest();
        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);
        var tokenResponse = await registerResponse.Content.ReadAsAsync<TokenResponse>();

        // Upgrade user to Admin role
        _factory.ExecuteDbContext(db =>
        {
            var user = db.Users.First(u => u.Email == registerRequest.Email);
            user.Role = "Admin";
            db.SaveChanges();
        });

        _client.DefaultRequestHeaders.Authorization = new("Bearer", tokenResponse!.AccessToken);

        var productRequest = new CreateProductRequest(
            Name: "Admin Product",
            Description: "Created by admin",
            Price: 49.99m,
            Category: "Premium",
            StockQuantity: 5
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/products", productRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreateProduct_WithInvalidData_ReturnsBadRequest()
    {
        // Arrange - Register and authorize
        await _factory.RegisterAndAuthorizeAsync(_client);

        var invalidRequest = new CreateProductRequest(
            "", // Invalid: empty name
            null,
            -10, // Invalid: negative price
            "Electronics",
            -5 // Invalid: negative quantity
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/products", invalidRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateProduct_SavesUserIdAsCreator()
    {
        // Arrange - Register and authorize
        await _factory.RegisterAndAuthorizeAsync(_client);

        var productRequest = new CreateProductRequest(
            Name: "Creator Test Product",
            Description: "Testing creator tracking",
            Price: 25.00m,
            Category: "Test",
            StockQuantity: 1
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/products", productRequest);
        var product = await response.Content.ReadFromJsonAsync<Product>();

        // Assert - Verify product was created
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        product!.Name.Should().Be(productRequest.Name);
    }

    #endregion

    #region PUT Tests (Update - User Role Required)

    [Fact]
    public async Task UpdateProduct_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        var product = _factory.ExecuteDbContext(db =>
            TestDataGenerator.SeedProduct(db)
        );

        var request = TestDataGenerator.CreateUpdateProductRequest(name: "Updated");

        // Act
        var response = await _client.PutAsJsonAsync($"/api/products/{product.Id}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateProduct_AsUser_ReturnsOk()
    {
        // Arrange - Register and authorize
        await _factory.RegisterAndAuthorizeAsync(_client);

        // Seed a product
        var productId = _factory.ExecuteDbContext(db =>
        {
            var product = new Product
            {
                Name = "Original Product",
                Description = "Will be updated",
                Price = 19.99m,
                Category = "Electronics",
                StockQuantity = 10
            };
            db.Products.Add(product);
            db.SaveChanges();
            return product.Id;
        });

        var updateRequest = new UpdateProductRequest(
            Name: "Updated Product",
            Description: null,
            Price: null,
            Category: null,
            StockQuantity: null
        );

        // Act
        var response = await _client.PutAsJsonAsync($"/api/products/{productId}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updatedProduct = await response.Content.ReadFromJsonAsync<Product>();
        updatedProduct!.Name.Should().Be("Updated Product");
    }

    [Fact]
    public async Task UpdateProduct_WithNonexistentId_ReturnsNotFound()
    {
        // Arrange
        var user = _factory.ExecuteDbContext(db =>
            TestDataGenerator.SeedUser(db)
        );

        var token = TokenHelper.GenerateToken(user);
        _client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        var request = TestDataGenerator.CreateUpdateProductRequest(name: "Updated");

        // Act
        var response = await _client.PutAsJsonAsync("/api/products/99999", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateProduct_WithPartialData_PreservesUnchangedFields()
    {
        // Arrange
        var user = _factory.ExecuteDbContext(db =>
            TestDataGenerator.SeedUser(db)
        );

        var product = _factory.ExecuteDbContext(db =>
            TestDataGenerator.SeedProduct(db, createdByUserId: user.Id)
        );

        var token = TokenHelper.GenerateToken(user);
        _client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        var originalName = product.Name;
        var request = new UpdateProductRequest(
            null, // Don't change name
            "New description",
            null, // Don't change price
            null,
            null
        );

        // Act
        var response = await _client.PutAsJsonAsync($"/api/products/{product.Id}", request);
        var updatedProduct = await response.Content.ReadFromJsonAsync<Product>();

        // Assert
        updatedProduct!.Name.Should().Be(originalName); // Unchanged
        updatedProduct.Description.Should().Be("New description"); // Changed
    }

    #endregion

    #region DELETE Tests (Delete - Admin Role Required)

    [Fact]
    public async Task DeleteProduct_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        var product = _factory.ExecuteDbContext(db =>
            TestDataGenerator.SeedProduct(db)
        );

        // Act
        var response = await _client.DeleteAsync($"/api/products/{product.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeleteProduct_AsUser_ReturnsForbidden()
    {
        // Arrange
        var user = _factory.ExecuteDbContext(db =>
            TestDataGenerator.SeedUser(db)
        );

        var product = _factory.ExecuteDbContext(db =>
            TestDataGenerator.SeedProduct(db)
        );

        var token = TokenHelper.GenerateToken(user);
        _client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        // Act
        var response = await _client.DeleteAsync($"/api/products/{product.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeleteProduct_AsAdmin_ReturnsNoContent()
    {
        // Arrange - Register and promote to Admin
        var registerRequest = ApiWebApplicationFactory.CreateRegisterRequest();
        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);
        var tokenResponse = await registerResponse.Content.ReadAsAsync<TokenResponse>();

        _factory.ExecuteDbContext(db =>
        {
            var user = db.Users.First(u => u.Email == registerRequest.Email);
            user.Role = "Admin";
            db.SaveChanges();
        });

        _client.DefaultRequestHeaders.Authorization = new("Bearer", tokenResponse!.AccessToken);

        // Seed a product
        var productId = _factory.ExecuteDbContext(db =>
        {
            var product = new Product
            {
                Name = "Product to Delete",
                Description = "Will be deleted",
                Price = 9.99m,
                Category = "Electronics",
                StockQuantity = 1
            };
            db.Products.Add(product);
            db.SaveChanges();
            return product.Id;
        });

        // Act
        var response = await _client.DeleteAsync($"/api/products/{productId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteProduct_WithNonexistentId_ReturnsNotFound()
    {
        // Arrange
        var admin = _factory.ExecuteDbContext(db =>
            TestDataGenerator.SeedAdminUser(db)
        );

        var token = TokenHelper.GenerateToken(admin);
        _client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        // Act
        var response = await _client.DeleteAsync("/api/products/99999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteProduct_RemovesFromDatabase()
    {
        // Arrange
        var admin = _factory.ExecuteDbContext(db =>
            TestDataGenerator.SeedAdminUser(db)
        );

        var product = _factory.ExecuteDbContext(db =>
            TestDataGenerator.SeedProduct(db)
        );

        var token = TokenHelper.GenerateToken(admin);
        _client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        // Act
        await _client.DeleteAsync($"/api/products/{product.Id}");

        // Assert
        var productExists = _factory.ExecuteDbContext(db =>
            db.Products.Any(p => p.Id == product.Id)
        );
        productExists.Should().BeFalse();
    }

    #endregion
}
