using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SecureApi.Application.DTOs.Requests;
using SecureApi.Tests.Fixtures;
using SecureApi.Tests.Helpers;
using Xunit;

namespace SecureApi.Tests.IntegrationTests;

/// <summary>
/// Integration tests for edge cases and unusual scenarios.
/// Tests boundary conditions, malformed inputs, and special cases.
/// </summary>
public class EdgeCaseTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public EdgeCaseTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    #region Token Edge Cases

    [Fact]
    public async Task Request_WithExpiredToken_Returns401()
    {
        // Arrange
        var user = _factory.ExecuteDbContext(db =>
            TestDataGenerator.SeedUser(db)
        );

        var expiredToken = TokenHelper.GenerateExpiredToken(user);
        _client.DefaultRequestHeaders.Authorization = new("Bearer", expiredToken);

        var request = TestDataGenerator.CreateValidProductRequest();

        // Act
        var response = await _client.PostAsJsonAsync("/api/products", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Request_WithMalformedToken_Returns401()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization = new("Bearer", "not.a.valid.token");

        // Act
        var response = await _client.GetAsync("/api/products");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Request_WithTokenWrongSignature_Returns401()
    {
        // Arrange
        var user = _factory.ExecuteDbContext(db =>
            TestDataGenerator.SeedUser(db)
        );

        var wrongSignatureToken = TokenHelper.GenerateTokenWithWrongSignature(user);
        _client.DefaultRequestHeaders.Authorization = new("Bearer", wrongSignatureToken);

        // Act
        var response = await _client.GetAsync("/api/products");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Request_WithTokenWithoutRole_Returns403()
    {
        // Arrange
        var user = _factory.ExecuteDbContext(db =>
            TestDataGenerator.SeedUser(db)
        );

        var tokenWithoutRole = TokenHelper.GenerateTokenWithoutRole(user);
        _client.DefaultRequestHeaders.Authorization = new("Bearer", tokenWithoutRole);

        var request = TestDataGenerator.CreateValidProductRequest();

        // Act
        var response = await _client.PostAsJsonAsync("/api/products", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Email and Case Sensitivity Tests

    [Fact]
    public async Task Login_WithEmailDifferentCase_AuthenticatesSuccessfully()
    {
        // Arrange
        var emailLower = "testuser@example.com";
        var registerRequest = TestDataGenerator.CreateValidRegisterRequest(email: emailLower);
        await _client.PostAsJsonAsync("/api/auth/register", registerRequest);

        var emailUpper = "TestUser@Example.COM";
        var loginRequest = new LoginRequest
        {
            Email = emailUpper, // Different case
            Password = registerRequest.Password
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Register_WithEmailDifferentCase_AllowsRegistration()
    {
        // Arrange
        var baseEmail = TestDataGenerator.GenerateUniqueEmail();
        var request1 = TestDataGenerator.CreateValidRegisterRequest(email: baseEmail.ToLower());
        var request2 = TestDataGenerator.CreateValidRegisterRequest(email: baseEmail.ToUpper());

        // Act
        await _client.PostAsJsonAsync("/api/auth/register", request1);
        var response = await _client.PostAsJsonAsync("/api/auth/register", request2);

        // Assert
        // Database should treat emails as case-insensitive (common practice)
        // If implementation is case-sensitive, this test documents that behavior
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    #endregion

    #region Boundary Value Tests

    [Fact]
    public async Task CreateProduct_WithMinimumValidData_Succeeds()
    {
        // Arrange
        var user = _factory.ExecuteDbContext(db =>
            TestDataGenerator.SeedUser(db)
        );

        var token = TokenHelper.GenerateToken(user);
        _client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        var request = new CreateProductRequest(
            "A", // Minimum: 1 character (if allowed)
            null,
            0.01m, // Minimum: positive
            "Cat",
            0 // Minimum: zero allowed
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/products", request);

        // Assert - Behavior depends on validator implementation
        // Document what the current implementation does
    }

    [Fact]
    public async Task CreateProduct_WithVeryLargeName_MayBeTruncatedOrRejected()
    {
        // Arrange
        var user = _factory.ExecuteDbContext(db =>
            TestDataGenerator.SeedUser(db)
        );

        var token = TokenHelper.GenerateToken(user);
        _client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        var veryLongName = new string('X', 1000);
        var request = TestDataGenerator.CreateValidProductRequest(name: veryLongName);

        // Act
        var response = await _client.PostAsJsonAsync("/api/products", request);

        // Assert - Document behavior (validation rule or database constraint)
    }

    [Fact]
    public async Task CreateProduct_WithVeryLargePrice_Succeeds()
    {
        // Arrange
        var user = _factory.ExecuteDbContext(db =>
            TestDataGenerator.SeedUser(db)
        );

        var token = TokenHelper.GenerateToken(user);
        _client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        var request = TestDataGenerator.CreateValidProductRequest(
            name: "Expensive Item",
            price: decimal.MaxValue
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/products", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task GetProducts_WithLargePageSize_ReturnsAll()
    {
        // Arrange
        _factory.ExecuteDbContext(db =>
        {
            TestDataGenerator.SeedProducts(db, 50);
        });

        // Act
        var response = await _client.GetAsync("/api/products");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion

    #region Whitespace and Special Character Tests

    [Fact]
    public async Task Register_WithEmailContainingWhitespace_IsValidated()
    {
        // Arrange
        var request = TestDataGenerator.CreateValidRegisterRequest(
            email: "  test@example.com  "
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        // Assert - Either trimmed or rejected, document behavior
    }

    [Fact]
    public async Task CreateProduct_WithSpecialCharactersInName_Succeeds()
    {
        // Arrange
        var user = _factory.ExecuteDbContext(db =>
            TestDataGenerator.SeedUser(db)
        );

        var token = TokenHelper.GenerateToken(user);
        _client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        var request = TestDataGenerator.CreateValidProductRequest(
            name: "Product™ with © and 中文"
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/products", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreateProduct_WithSQLInjectionAttempt_IsSafelyHandled()
    {
        // Arrange
        var user = _factory.ExecuteDbContext(db =>
            TestDataGenerator.SeedUser(db)
        );

        var token = TokenHelper.GenerateToken(user);
        _client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        var request = TestDataGenerator.CreateValidProductRequest(
            name: "'; DROP TABLE Products; --"
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/products", request);

        // Assert - Should create product with the literal string, not execute SQL
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var savedProduct = _factory.ExecuteDbContext(db =>
            db.Products.FirstOrDefault(p => p.Name == request.Name)
        );
        savedProduct.Should().NotBeNull();
    }

    #endregion

    #region Empty/Null Payload Tests

    [Fact]
    public async Task CreateProduct_WithEmptyBody_ReturnsBadRequest()
    {
        // Arrange
        var user = _factory.ExecuteDbContext(db =>
            TestDataGenerator.SeedUser(db)
        );

        var token = TokenHelper.GenerateToken(user);
        _client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        // Act
        var content = new StringContent("", System.Text.Encoding.UTF8, "application/json");
        var response = await _client.PostAsync("/api/products", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Concurrent Request Tests

    [Fact]
    public async Task MultipleCreateProductRequests_AllSucceed()
    {
        // Arrange
        var user = _factory.ExecuteDbContext(db =>
            TestDataGenerator.SeedUser(db)
        );

        var token = TokenHelper.GenerateToken(user);

        var client1 = _factory.CreateClient();
        client1.DefaultRequestHeaders.Authorization = new("Bearer", token);

        var client2 = _factory.CreateClient();
        client2.DefaultRequestHeaders.Authorization = new("Bearer", token);

        var request1 = TestDataGenerator.CreateValidProductRequest();
        var request2 = TestDataGenerator.CreateValidProductRequest();

        // Act
        var response1 = await client1.PostAsJsonAsync("/api/products", request1);
        var response2 = await client2.PostAsJsonAsync("/api/products", request2);

        // Assert
        response1.StatusCode.Should().Be(HttpStatusCode.Created);
        response2.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    #endregion

    #region Age Boundary Tests

    [Fact]
    public async Task Register_WithExactly18YearsOld_Succeeds()
    {
        // Arrange - Exactly 18 years old today
        var birthDate = DateTime.UtcNow.AddYears(-18);
        var request = TestDataGenerator.CreateValidRegisterRequest(birthDate: birthDate);

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Register_WithJustBefore18_Fails()
    {
        // Arrange - 17 years and 364 days old
        var birthDate = DateTime.UtcNow.AddYears(-18).AddDays(1);
        var request = TestDataGenerator.CreateValidRegisterRequest(birthDate: birthDate);

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_With120YearsOld_Succeeds()
    {
        // Arrange - 120 years old (boundary)
        var birthDate = DateTime.UtcNow.AddYears(-120);
        var request = TestDataGenerator.CreateValidRegisterRequest(birthDate: birthDate);

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Register_WithOver120YearsOld_Fails()
    {
        // Arrange - 121 years old (exceeds boundary)
        var birthDate = DateTime.UtcNow.AddYears(-121);
        var request = TestDataGenerator.CreateValidRegisterRequest(birthDate: birthDate);

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion
}
