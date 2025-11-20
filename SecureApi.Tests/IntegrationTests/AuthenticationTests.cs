using System.Net;
using System.Net.Http.Json;
using BCrypt.Net;
using FluentAssertions;
using SecureApi.Application.DTOs.Requests;
using SecureApi.Application.DTOs.Responses;
using SecureApi.Tests.Fixtures;
using Xunit;

namespace SecureApi.Tests.IntegrationTests;

/// <summary>
/// Integration tests for authentication endpoints (/api/auth).
/// Tests registration, login, token refresh, and logout functionality.
/// </summary>
public class AuthenticationTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public AuthenticationTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    #region Register Tests

    [Fact]
    public async Task Register_WithValidData_ReturnsCreatedWithTokens()
    {
        // Arrange
        var request = TestDataGenerator.CreateValidRegisterRequest();

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var token = await response.Content.ReadFromJsonAsync<TokenResponse>();
        token.Should().NotBeNull();
        token!.AccessToken.Should().NotBeNullOrEmpty();
        token.RefreshToken.Should().NotBeNullOrEmpty();
        token.ExpiresIn.Should().Be(900); // 15 minutes in seconds
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ReturnConflict()
    {
        // Arrange
        var email = TestDataGenerator.GenerateUniqueEmail();
        var request1 = TestDataGenerator.CreateValidRegisterRequest(email: email);
        var request2 = TestDataGenerator.CreateValidRegisterRequest(email: email);

        // Act
        await _client.PostAsJsonAsync("/api/auth/register", request1);
        var response = await _client.PostAsJsonAsync("/api/auth/register", request2);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Register_WithInvalidEmail_ReturnsBadRequest()
    {
        // Arrange
        var request = TestDataGenerator.CreateValidRegisterRequest(email: "not-an-email");

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_WithWeakPassword_ReturnsBadRequest()
    {
        // Arrange
        var request = TestDataGenerator.CreateValidRegisterRequest(password: "weak");

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_WithUnderageBirthDate_ReturnsBadRequest()
    {
        // Arrange
        var tooYoung = DateTime.UtcNow.AddYears(-10); // 10 years old
        var request = TestDataGenerator.CreateValidRegisterRequest(birthDate: tooYoung);

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_SavesUserToDatabase()
    {
        // Arrange
        var request = TestDataGenerator.CreateValidRegisterRequest();

        // Act
        await _client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        var userExists = _factory.ExecuteDbContext(db =>
            db.Users.Any(u => u.Email == request.Email)
        );
        userExists.Should().BeTrue();
    }

    #endregion

    #region Login Tests

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsOkWithTokens()
    {
        // Arrange - Register a user first
        var registerRequest = TestDataGenerator.CreateValidRegisterRequest();
        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);
        registerResponse.StatusCode.Should().Be(HttpStatusCode.Created,
            "Registration must succeed before testing login. Response: " + await registerResponse.Content.ReadAsStringAsync());

        // Verify the user was actually saved to the database
        var userInDb = _factory.ExecuteDbContext(db =>
            db.Users.FirstOrDefault(u => u.Email == registerRequest.Email));
        userInDb.Should().NotBeNull($"User with email '{registerRequest.Email}' should exist in database after registration");

        // Verify password hashing works
        var passwordVerifies = BCrypt.Net.BCrypt.Verify(registerRequest.Password, userInDb!.PasswordHash);
        passwordVerifies.Should().BeTrue($"Password should verify against stored hash. Plain: {registerRequest.Password}, Hash: {userInDb.PasswordHash}");

        // Login with the registered credentials
        var loginRequest = TestDataGenerator.CreateLoginRequest(registerRequest.Email, registerRequest.Password);

        // Verify the credentials we're using
        loginRequest.Email.Should().NotBeNullOrEmpty();
        loginRequest.Password.Should().NotBeNullOrEmpty();

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Debug: Show response
        var responseBody = await response.Content.ReadAsStringAsync();

        // If login failed, we want to know why
        if (response.StatusCode != HttpStatusCode.OK)
        {
            throw new InvalidOperationException($"Login failed!\nStatus: {response.StatusCode}\nEmail: {loginRequest.Email}\nPassword: {loginRequest.Password}\nResponse: {responseBody}");
        }

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var token = await response.Content.ReadFromJsonAsync<TokenResponse>();
        token.Should().NotBeNull();
        token!.AccessToken.Should().NotBeNullOrEmpty();
        token.RefreshToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_WithNonexistentEmail_ReturnsUnauthorized()
    {
        // Arrange
        var loginRequest = TestDataGenerator.CreateLoginRequest("nonexistent@test.com");

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized()
    {
        // Arrange
        var registerRequest = TestDataGenerator.CreateValidRegisterRequest();
        await _client.PostAsJsonAsync("/api/auth/register", registerRequest);

        var loginRequest = TestDataGenerator.CreateLoginRequest(registerRequest.Email, "WrongPassword123!");

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_StoresRefreshTokenInDatabase()
    {
        // Arrange
        var registerRequest = TestDataGenerator.CreateValidRegisterRequest();
        await _client.PostAsJsonAsync("/api/auth/register", registerRequest);

        var loginRequest = TestDataGenerator.CreateLoginRequest(registerRequest.Email, registerRequest.Password);

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponse>();

        // Assert
        var refreshTokenExists = _factory.ExecuteDbContext(db =>
            db.RefreshTokens.Any(rt => rt.Token == tokenResponse!.RefreshToken)
        );
        refreshTokenExists.Should().BeTrue();
    }

    #endregion

    #region Refresh Token Tests

    [Fact]
    public async Task RefreshToken_WithValidToken_ReturnsNewTokens()
    {
        // Arrange
        var registerRequest = TestDataGenerator.CreateValidRegisterRequest();
        await _client.PostAsJsonAsync("/api/auth/register", registerRequest);

        var loginRequest = TestDataGenerator.CreateLoginRequest(registerRequest.Email, registerRequest.Password);
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        var loginToken = await loginResponse.Content.ReadFromJsonAsync<TokenResponse>();

        var refreshRequest = new RefreshRequest { RefreshToken = loginToken!.RefreshToken };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/refresh", refreshRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var newToken = await response.Content.ReadFromJsonAsync<TokenResponse>();
        newToken.Should().NotBeNull();
        newToken!.AccessToken.Should().NotBe(loginToken.AccessToken); // New token issued
    }

    [Fact]
    public async Task RefreshToken_WithInvalidToken_ReturnsUnauthorized()
    {
        // Arrange
        var refreshRequest = new RefreshRequest { RefreshToken = "invalid-token" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/refresh", refreshRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RefreshToken_RevokesPreviousToken()
    {
        // Arrange
        var registerRequest = TestDataGenerator.CreateValidRegisterRequest();
        await _client.PostAsJsonAsync("/api/auth/register", registerRequest);

        var loginRequest = TestDataGenerator.CreateLoginRequest(registerRequest.Email, registerRequest.Password);
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        var loginToken = await loginResponse.Content.ReadFromJsonAsync<TokenResponse>();
        var oldRefreshToken = loginToken!.RefreshToken;

        var refreshRequest = new RefreshRequest { RefreshToken = oldRefreshToken };

        // Act
        await _client.PostAsJsonAsync("/api/auth/refresh", refreshRequest);

        // Assert - Try to use old token again, should fail
        var secondRefreshRequest = new RefreshRequest { RefreshToken = oldRefreshToken };
        var response = await _client.PostAsJsonAsync("/api/auth/refresh", secondRefreshRequest);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Logout Tests

    [Fact]
    public async Task Logout_WithValidToken_ReturnsOk()
    {
        // Arrange
        var registerRequest = TestDataGenerator.CreateValidRegisterRequest();
        await _client.PostAsJsonAsync("/api/auth/register", registerRequest);

        var loginRequest = TestDataGenerator.CreateLoginRequest(registerRequest.Email, registerRequest.Password);
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        var loginToken = await loginResponse.Content.ReadFromJsonAsync<TokenResponse>();

        var logoutRequest = new LogoutRequest { RefreshToken = loginToken!.RefreshToken };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/logout", logoutRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Logout_RevokesRefreshToken()
    {
        // Arrange
        var registerRequest = TestDataGenerator.CreateValidRegisterRequest();
        await _client.PostAsJsonAsync("/api/auth/register", registerRequest);

        var loginRequest = TestDataGenerator.CreateLoginRequest(registerRequest.Email, registerRequest.Password);
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        var loginToken = await loginResponse.Content.ReadFromJsonAsync<TokenResponse>();

        var logoutRequest = new LogoutRequest { RefreshToken = loginToken!.RefreshToken };

        // Act
        await _client.PostAsJsonAsync("/api/auth/logout", logoutRequest);

        // Assert - Try to refresh with the logged-out token
        var refreshRequest = new RefreshRequest { RefreshToken = loginToken.RefreshToken };
        var response = await _client.PostAsJsonAsync("/api/auth/refresh", refreshRequest);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion
}
