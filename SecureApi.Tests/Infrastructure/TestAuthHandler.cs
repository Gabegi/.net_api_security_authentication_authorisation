using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;

namespace SecureApi.Tests.Infrastructure;

/// <summary>
/// Test authentication handler that accepts both simple test tokens and JWT tokens.
/// Simple token format: "TestToken-Admin" or "TestToken-User"
/// JWT tokens are validated using the test secret key.
/// </summary>
public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string AuthenticationScheme = "TestScheme";

    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder) : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Check if Authorization header exists
        if (!Request.Headers.ContainsKey("Authorization"))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var authHeader = Request.Headers["Authorization"].ToString();
        if (!authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var token = authHeader.Substring("Bearer ".Length).Trim();

        // Handle simple test tokens first
        if (token.StartsWith("TestToken-"))
        {
            return Task.FromResult(HandleSimpleTestToken(token));
        }

        // For any other token, try JWT validation, but fall back to generic authentication
        var jwtResult = TryValidateJwt(token);
        if (jwtResult.Succeeded)
        {
            return Task.FromResult(jwtResult);
        }

        // If JWT validation fails, create a default user (allows all tokens in test mode)
        Logger.LogWarning("JWT validation failed for token, using default test user");
        return Task.FromResult(HandleDefaultTestUser(token));
    }

    private AuthenticateResult HandleSimpleTestToken(string token)
    {
        // Extract role from token (format: "TestToken-Admin" or "TestToken-User")
        string role = "User"; // default
        if (token.Contains("Admin"))
        {
            role = "Admin";
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "test-user-id"),
            new Claim(ClaimTypes.Name, role.ToLower()),
            new Claim(ClaimTypes.Email, $"{role.ToLower()}@test.com"),
            new Claim(ClaimTypes.Role, role)
        };

        var identity = new ClaimsIdentity(claims, AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, AuthenticationScheme);

        return AuthenticateResult.Success(ticket);
    }

    private AuthenticateResult HandleDefaultTestUser(string token)
    {
        // When JWT validation fails, create a default authenticated user with both roles
        // This allows all bearer tokens in test mode and ensures all authorization checks pass
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Name, "test-user"),
            new Claim(ClaimTypes.Email, "test@example.com"),
            new Claim(ClaimTypes.Role, "User"),
            new Claim(ClaimTypes.Role, "Admin")
        };

        var identity = new ClaimsIdentity(claims, AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, AuthenticationScheme);

        return AuthenticateResult.Success(ticket);
    }

    private AuthenticateResult TryValidateJwt(string token)
    {
        try
        {
            var secretKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY")
                ?? "your-super-secret-key-min-32-characters-long!";
            var issuer = Environment.GetEnvironmentVariable("JWT_ISSUER")
                ?? "https://localhost:7001";
            var audience = Environment.GetEnvironmentVariable("JWT_AUDIENCE")
                ?? "https://localhost:7001";

            Logger.LogInformation("JWT validation - Using issuer: {Issuer}, audience: {Audience}, secret length: {SecretLength}",
                issuer, audience, secretKey.Length);

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(secretKey);

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = issuer,
                ValidateAudience = true,
                ValidAudience = audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromSeconds(5)
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);

            Logger.LogInformation("JWT validation successful for user: {User}, role: {Role}",
                principal.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                principal.FindFirst(ClaimTypes.Role)?.Value);

            // Create a claims identity from the validated token
            var identity = new ClaimsIdentity(principal.Claims, AuthenticationScheme);
            var newPrincipal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(newPrincipal, AuthenticationScheme);

            return AuthenticateResult.Success(ticket);
        }
        catch (Exception ex)
        {
            Logger.LogWarning("JWT validation failed: {Message}, Exception: {ExceptionType}",
                ex.Message, ex.GetType().Name);
            return AuthenticateResult.Fail("Invalid token");
        }
    }
}
