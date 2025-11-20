using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using SecureApi.Application.DTOs.Responses;
using SecureApi.Tests.Fixtures;
using Xunit;
using Xunit.Abstractions;

namespace SecureApi.Tests;

public class JWTValidationDebugTest : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private readonly ITestOutputHelper _output;

    public JWTValidationDebugTest(ApiWebApplicationFactory factory, ITestOutputHelper output)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _output = output;
    }

    [Fact]
    public async Task Test_ManualJWTValidation()
    {
        // Register and get token
        var registerRequest = ApiWebApplicationFactory.CreateRegisterRequest();
        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);
        var tokenResponse = await registerResponse.Content.ReadFromJsonAsync<TokenResponse>();
        var token = tokenResponse!.AccessToken;

        _output.WriteLine($"Token: {token}");

        // Now manually validate the token with the same parameters as the JWT Bearer middleware
        var handler = new JwtSecurityTokenHandler();

        // These are the exact same values used in Program.cs
        var secretKey = "your-super-secret-key-min-32-characters-long!";
        var issuer = "https://localhost:7001";
        var audience = "https://localhost:7001";

        _output.WriteLine($"Secret Key: {secretKey}");
        _output.WriteLine($"Issuer: {issuer}");
        _output.WriteLine($"Audience: {audience}");

        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            ValidateIssuer = true,
            ValidIssuer = issuer,
            ValidateAudience = true,
            ValidAudience = audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(5)
        };

        try
        {
            var principal = handler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);
            _output.WriteLine($"Token validated successfully!");
            _output.WriteLine($"Principal Identity Name: {principal?.Identity?.Name}");
            _output.WriteLine($"Principal Claims:");
            if (principal != null)
            {
                foreach (var claim in principal.Claims)
                {
                    _output.WriteLine($"  - {claim.Type}: {claim.Value}");
                }
            }

            Assert.NotNull(principal);
        }
        catch (Exception ex)
        {
            _output.WriteLine($"Token validation failed: {ex.Message}");
            _output.WriteLine($"Exception type: {ex.GetType().Name}");
            throw;
        }
    }

    [Fact]
    public async Task Test_CompareIssuersInToken()
    {
        // Register and get token
        var registerRequest = ApiWebApplicationFactory.CreateRegisterRequest();
        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);
        var tokenResponse = await registerResponse.Content.ReadFromJsonAsync<TokenResponse>();
        var token = tokenResponse!.AccessToken;

        // Decode the token to see what issuer is actually in it
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        _output.WriteLine($"Token Issuer: {jwtToken.Issuer}");
        _output.WriteLine($"Expected Issuer: https://localhost:7001");
        _output.WriteLine($"Issuer Match: {jwtToken.Issuer == "https://localhost:7001"}");

        var audienceClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "aud");
        _output.WriteLine($"Token Audience (from claims): {audienceClaim?.Value}");
        _output.WriteLine($"Token Audiences (from Audiences): {string.Join(", ", jwtToken.Audiences)}");
        _output.WriteLine($"Expected Audience: https://localhost:7001");

        Assert.Equal("https://localhost:7001", jwtToken.Issuer);
        Assert.Contains(jwtToken.Audiences, a => a == "https://localhost:7001");
    }
}
