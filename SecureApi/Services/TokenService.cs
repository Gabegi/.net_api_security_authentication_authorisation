using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SecureApi.Data;
using SecureApi.DTOs;
using SecureApi.Models;

namespace SecureApi.Services;

/// <summary>
/// Service for JWT token generation and refresh token management.
/// </summary>
public class TokenService : ITokenService
{
    private const string DEFAULT_ROLE = "User";
    private readonly IConfiguration _configuration;
    private readonly ApplicationDbContext _context;

    /// <summary>
    /// Initializes a new instance of the TokenService class.
    /// </summary>
    /// <param name="configuration">Application configuration for JWT settings.</param>
    /// <param name="context">Database context for token operations.</param>
    public TokenService(IConfiguration configuration, ApplicationDbContext context)
    {
        _configuration = configuration;
        _context = context;
    }

    /// <summary>
    /// Generates a short-lived JWT access token.
    /// Input: User data (ID, email, role)
    /// Output: JWT string (e.g., "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...")
    /// Database: No interaction
    /// </summary>
    public string GenerateAccessToken(int userId, string email, string role)
    {
        // Step 1: Get JWT configuration from appsettings.json
        var jwtSettings = _configuration.GetSection("Jwt");

        // Step 2: Get secret key (throws if missing)
        var secretKey = jwtSettings["SecretKey"]
            ?? throw new InvalidOperationException("JWT SecretKey not configured in appsettings.json");

        // Step 3: Get expiry time (default 15 minutes)
        var expiryMinutes = int.Parse(jwtSettings["AccessTokenExpiryMinutes"] ?? "15");

        // Step 4: Create claims (data embedded in JWT)
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),          // Subject (user ID)
            new(JwtRegisteredClaimNames.Email, email),                    // Email
            new(ClaimTypes.Role, role),                                   // Role (for authorization)
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())   // Token ID (unique)
        };

        // Step 5: Create signing key from secret
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));

        // Step 6: Create signing credentials (HMAC-SHA256 algorithm)
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // Step 7: Create the actual JWT token
        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],      // Who created it
            audience: jwtSettings["Audience"],  // Who it's for
            claims: claims,                     // User data
            expires: DateTime.UtcNow.AddMinutes(expiryMinutes),  // When it expires
            signingCredentials: credentials     // Signature
        );

        // Step 8: Serialize token to string
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Generates a long-lived refresh token (NOT a JWT, just a random string).
    /// Input: IP address
    /// Output: RefreshToken object (NOT saved yet)
    /// Database: No interaction
    /// Note: Token is just random bytes, NOT a JWT!
    /// </summary>
    public RefreshToken GenerateRefreshToken(string ipAddress)
    {
        // Step 1: Get expiry configuration
        var jwtSettings = _configuration.GetSection("Jwt");
        var expiryDays = int.Parse(jwtSettings["RefreshTokenExpiryDays"] ?? "7");

        // Step 2: Generate cryptographically secure random bytes
        var randomBytes = new byte[64];                      // 64 bytes = 512 bits
        using var rng = RandomNumberGenerator.Create();      // Crypto-safe random
        rng.GetBytes(randomBytes);                           // Fill with random data

        // Step 3: Create RefreshToken object
        return new RefreshToken
        {
            // Convert bytes to Base64 string
            // Result: "xK8j2mN9pQ3rT5vW7yZ0aB4cD6eF8gH1iJ3kL5mN7oP9qR1sT3uV5wX7yZ0=="
            Token = Convert.ToBase64String(randomBytes),

            // Calculate expiration date (7 days from now)
            ExpiresAt = DateTime.UtcNow.AddDays(expiryDays),

            // Track where token was created (for audit/security)
            CreatedByIp = ipAddress

            // UserId NOT set here - caller must set it!
        };
    }

    /// <summary>
    /// Refreshes an access token using a valid refresh token.
    /// </summary>
    public async Task<TokenResponse> RefreshTokenAsync(string token, string ipAddress)
    {
        // Find the refresh token
        var refreshToken = await _context.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == token);

        // Validate token exists and is active
        if (refreshToken == null || !refreshToken.IsActive)
        {
            throw new UnauthorizedAccessException("Invalid or expired refresh token");
        }

        // Revoke old token
        refreshToken.RevokedAt = DateTime.UtcNow;

        // Generate new tokens
        var newAccessToken = GenerateAccessToken(
            refreshToken.User.Id,
            refreshToken.User.Email,
            refreshToken.User.Role ?? DEFAULT_ROLE);

        var newRefreshToken = GenerateRefreshToken(ipAddress);
        newRefreshToken.UserId = refreshToken.UserId;

        // Save new refresh token
        _context.RefreshTokens.Add(newRefreshToken);
        await _context.SaveChangesAsync();

        return new TokenResponse(
            AccessToken: newAccessToken,
            RefreshToken: newRefreshToken.Token,
            ExpiresIn: int.Parse(_configuration.GetSection("Jwt")["AccessTokenExpiryMinutes"] ?? "15") * 60
        );
    }

    /// <summary>
    /// Revokes a refresh token (for logout).
    /// </summary>
    public async Task RevokeTokenAsync(string token)
    {
        var refreshToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == token);

        if (refreshToken != null && refreshToken.IsActive)
        {
            refreshToken.RevokedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }
}
