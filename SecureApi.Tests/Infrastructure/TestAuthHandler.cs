using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace SecureApi.Tests.Infrastructure;

/// <summary>
/// Test authentication handler that accepts bearer tokens without JWT validation.
/// Supports simple test token format: "TestToken-Admin" or "TestToken-User"
/// All other bearer tokens are accepted in test mode.
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

        // For any other bearer token, accept it without validation (test mode)
        // This includes JWT tokens from the real token service
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
        // For any bearer token in test mode, create an authenticated user with both roles
        // This allows JWT tokens from the auth service, test tokens, and any other bearer tokens
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
}
