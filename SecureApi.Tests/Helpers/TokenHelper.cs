using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using SecureApi.Infrastructure.Persistence.Models;

namespace SecureApi.Tests.Helpers;

/// <summary>
/// Helper class for generating JWT tokens in integration tests.
/// Provides methods for creating valid, expired, and invalid tokens.
/// </summary>
public static class TokenHelper
{
    // Test JWT configuration - MUST match appsettings.json values
    private const string TestSecretKey = "your-super-secret-key-min-32-characters-long!";
    private const string TestIssuer = "https://localhost:7001";
    private const string TestAudience = "https://localhost:7001";
    private const int DefaultExpirationMinutes = 15;

    /// <summary>
    /// Generates a valid JWT token for a given user.
    /// Uses the same claim structure as the app's TokenService.GenerateAccessToken()
    /// </summary>
    public static string GenerateToken(User user, int expirationMinutes = DefaultExpirationMinutes)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),      // Subject (user ID) - matches TokenService
            new(JwtRegisteredClaimNames.Email, user.Email),            // Email
            new(ClaimTypes.Role, user.Role),                           // Role (for authorization)
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()) // Token ID (unique)
        };

        return GenerateToken(claims, DateTime.UtcNow.AddMinutes(expirationMinutes));
    }

    /// <summary>
    /// Generates a valid JWT token for a user with role.
    /// Quick helper for common test scenarios.
    /// </summary>
    public static string GenerateTokenForRole(int userId, string email, string role)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),       // Matches TokenService
            new(JwtRegisteredClaimNames.Email, email),
            new(ClaimTypes.Role, role),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        return GenerateToken(claims, DateTime.UtcNow.AddMinutes(DefaultExpirationMinutes));
    }

    /// <summary>
    /// Generates an expired JWT token (expired 5 minutes ago).
    /// Useful for testing 401 Unauthorized responses.
    /// </summary>
    public static string GenerateExpiredToken(User user)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),      // Matches TokenService
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(ClaimTypes.Role, user.Role),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestSecretKey));
        var signingCredentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        // Create token that expired 5 minutes ago (notBefore 10 minutes ago)
        var token = new JwtSecurityToken(
            issuer: TestIssuer,
            audience: TestAudience,
            claims: claims,
            notBefore: DateTime.UtcNow.AddMinutes(-10),
            expires: DateTime.UtcNow.AddMinutes(-5),
            signingCredentials: signingCredentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Generates a token without a role claim.
    /// Useful for testing authorization policies that require roles.
    /// </summary>
    public static string GenerateTokenWithoutRole(User user)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),      // Matches TokenService
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            // No Role claim
        };

        return GenerateToken(claims, DateTime.UtcNow.AddMinutes(DefaultExpirationMinutes));
    }

    /// <summary>
    /// Generates a token without a user ID claim.
    /// Useful for testing scenarios where user ID is required.
    /// </summary>
    public static string GenerateTokenWithoutUserId(string email, string role)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Email, email),
            new(ClaimTypes.Role, role),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            // No Sub (user ID) claim
        };

        return GenerateToken(claims, DateTime.UtcNow.AddMinutes(DefaultExpirationMinutes));
    }

    /// <summary>
    /// Generates a token with custom claims.
    /// Useful for testing edge cases and specific scenarios.
    /// </summary>
    public static string GenerateTokenWithCustomClaims(
        IEnumerable<Claim> claims,
        int expirationMinutes = DefaultExpirationMinutes)
    {
        return GenerateToken(claims, DateTime.UtcNow.AddMinutes(expirationMinutes));
    }

    /// <summary>
    /// Returns a completely invalid token string.
    /// Useful for testing malformed token handling.
    /// </summary>
    public static string GenerateInvalidToken()
    {
        return "this.is.not.a.valid.jwt.token";
    }

    /// <summary>
    /// Generates a token signed with a different secret key.
    /// Useful for testing token validation with wrong signature.
    /// </summary>
    public static string GenerateTokenWithWrongSignature(User user)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),      // Matches TokenService
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(ClaimTypes.Role, user.Role),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var wrongSecretKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes("wrong-secret-key-for-testing-signature-validation-minimum-32-chars"));

        var signingCredentials = new SigningCredentials(
            wrongSecretKey,
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: TestIssuer,
            audience: TestAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(DefaultExpirationMinutes),
            signingCredentials: signingCredentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Core method to generate a JWT token with specified claims and expiration.
    /// </summary>
    private static string GenerateToken(IEnumerable<Claim> claims, DateTime expires)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestSecretKey));
        var signingCredentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: TestIssuer,
            audience: TestAudience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expires,
            signingCredentials: signingCredentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Gets the test JWT configuration.
    /// Use this to configure your test WebApplicationFactory.
    /// </summary>
    public static Dictionary<string, string> GetTestJwtConfiguration()
    {
        return new Dictionary<string, string>
        {
            ["Jwt:SecretKey"] = TestSecretKey,
            ["Jwt:Issuer"] = TestIssuer,
            ["Jwt:Audience"] = TestAudience,
            ["Jwt:ExpirationInMinutes"] = DefaultExpirationMinutes.ToString()
        };
    }
}
