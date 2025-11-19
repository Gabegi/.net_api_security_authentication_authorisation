using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SecureApi.Application.DTOs.Requests;
using SecureApi.Application.DTOs.Responses;
using SecureApi.Tests.Fixtures;
using SecureApi.Tests.Helpers;
using Xunit;

namespace SecureApi.Tests.IntegrationTests;

/// <summary>
/// Integration tests for exception handling and error responses.
/// Verifies that various error scenarios return appropriate HTTP status codes and response formats.
/// </summary>
public class ExceptionHandlingTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ExceptionHandlingTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    #region Validation Error Tests

    [Fact]
    public async Task Register_WithValidationErrors_ReturnsBadRequestWithGroupedErrors()
    {
        // Arrange
        var invalidRequest = new RegisterRequest
        {
            Email = "invalid-email",
            Password = "weak",
            FullName = "",
            BirthDate = DateTime.UtcNow.AddYears(1) // Future date
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", invalidRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var errorResponse = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        errorResponse.Should().NotBeNull();
        errorResponse!.Error.Should().Be("Validation failed");
        errorResponse.Errors.Should().NotBeNull();
        errorResponse.Errors!.Keys.Should().Contain(new[] { "Email", "Password", "FullName", "BirthDate" });
    }

    [Fact]
    public async Task CreateProduct_WithInvalidPrice_ReturnsBadRequest()
    {
        // Arrange
        var user = _factory.ExecuteDbContext(db =>
            TestDataGenerator.SeedUser(db)
        );

        var token = TokenHelper.GenerateToken(user);
        _client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        var invalidRequest = new CreateProductRequest(
            "Product",
            null,
            -100, // Invalid: negative price
            "Electronics",
            10
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/products", invalidRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var errorResponse = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        errorResponse!.Errors.Should().ContainKey("Price");
    }

    #endregion

    #region Resource Not Found Tests (404)

    [Fact]
    public async Task GetProduct_WithInvalidId_Returns404WithCorrectFormat()
    {
        // Act
        var response = await _client.GetAsync("/api/products/99999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var errorResponse = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        errorResponse.Should().NotBeNull();
        errorResponse!.StatusCode.Should().Be(404);
        errorResponse.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task UpdateProduct_WithInvalidId_Returns404()
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
    public async Task DeleteProduct_WithInvalidId_Returns404()
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

    #endregion

    #region Duplicate Resource Tests (409 Conflict)

    [Fact]
    public async Task Register_WithDuplicateEmail_Returns409WithCorrectFormat()
    {
        // Arrange
        var email = TestDataGenerator.GenerateUniqueEmail();
        var request1 = TestDataGenerator.CreateValidRegisterRequest(email: email);
        var request2 = TestDataGenerator.CreateValidRegisterRequest(email: email);

        await _client.PostAsJsonAsync("/api/auth/register", request1);

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", request2);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var errorResponse = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        errorResponse.Should().NotBeNull();
        errorResponse!.StatusCode.Should().Be(409);
        errorResponse.Error.Should().Contain("already exists");
    }

    #endregion

    #region Unauthorized/Forbidden Tests

    [Fact]
    public async Task Login_WithInvalidCredentials_Returns401()
    {
        // Arrange
        var loginRequest = TestDataGenerator.CreateLoginRequest("test@test.com", "WrongPassword");

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var errorResponse = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        errorResponse!.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task DeleteProduct_AsNonAdmin_Returns403()
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
    public async Task GetAdultProducts_WithUnderageToken_Returns403()
    {
        // Arrange
        var underageUser = _factory.ExecuteDbContext(db =>
        {
            var user = TestDataGenerator.CreateUnderageUser();
            db.Users.Add(user);
            db.SaveChanges();
            return user;
        });

        var token = TokenHelper.GenerateToken(underageUser);
        _client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/products/adult/list");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Error Response Format Tests

    [Fact]
    public async Task ErrorResponse_IncludesTraceId()
    {
        // Act
        var response = await _client.GetAsync("/api/products/99999");

        // Assert
        var errorResponse = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        errorResponse!.TraceId.Should().NotBeNullOrEmpty();
        errorResponse.TraceId.Should().NotBe("unknown");
    }

    [Fact]
    public async Task ErrorResponse_IncludesTimestamp()
    {
        // Act
        var response = await _client.GetAsync("/api/products/99999");

        // Assert
        var errorResponse = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        errorResponse!.Timestamp.Should().NotBe(default);
        errorResponse.Timestamp.Should().BeBefore(DateTime.UtcNow.AddSeconds(1));
        errorResponse.Timestamp.Should().BeAfter(DateTime.UtcNow.AddSeconds(-10));
    }

    [Fact]
    public async Task ErrorResponse_ValidationErrors_AreGroupedByProperty()
    {
        // Arrange
        var invalidRequest = new RegisterRequest
        {
            Email = "invalid-email",
            Password = "weak",
            FullName = "",
            BirthDate = DateTime.UtcNow.AddYears(1)
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", invalidRequest);
        var errorResponse = await response.Content.ReadFromJsonAsync<ErrorResponse>();

        // Assert
        errorResponse!.Errors.Should().NotBeNull();
        foreach (var (propertyName, messages) in errorResponse.Errors!)
        {
            messages.Should().BeAssignableTo<IEnumerable<string>>();
            messages.Should().NotBeEmpty();
        }
    }

    #endregion

    #region Missing Authorization Header Tests

    [Fact]
    public async Task CreateProduct_WithoutAuthHeader_Returns401()
    {
        // Arrange
        var request = TestDataGenerator.CreateValidProductRequest();
        // No Authorization header set

        // Act
        var response = await _client.PostAsJsonAsync("/api/products", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateProduct_WithInvalidBearerToken_Returns401()
    {
        // Arrange
        var request = TestDataGenerator.CreateValidProductRequest();
        _client.DefaultRequestHeaders.Authorization = new("Bearer", "invalid.token.here");

        // Act
        var response = await _client.PostAsJsonAsync("/api/products", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion
}
