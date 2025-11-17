using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SecureApi.Infrastructure.Persistence;
using SecureApi.Application.DTOs.Requests;
using SecureApi.Application.DTOs.Responses;
using SecureApi.Infrastructure.Persistence.Models;

namespace SecureApi.Application.Services;

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
    /// Refreshes an access token using a valid refresh token (Most Complex!).
    /// Input: Refresh token string, IP
    /// Output: New access token + new refresh token
    /// Database: Read old token, save new token, revoke old token (TOKEN ROTATION)
    /// </summary>
    public async Task<TokenResponse> RefreshTokenAsync(string token, string ipAddress)
    {
        // ===== STEP 1: Find token in database =====
        var refreshToken = await _context.RefreshTokens
            .Include(rt => rt.User)  // Load associated User data
            .FirstOrDefaultAsync(rt => rt.Token == token);

        // Database query:
        // SELECT * FROM RefreshTokens
        // JOIN Users ON RefreshTokens.UserId = Users.Id
        // WHERE Token = 'abc123'

        // ===== STEP 2: Validate =====
        if (refreshToken == null || !refreshToken.IsActive)
        {
            // refreshToken == null → Token doesn't exist
            // !IsActive → Token expired or already revoked
            throw new UnauthorizedAccessException("Invalid or expired refresh token");
        }

        // IsActive checks: RevokedAt == null && DateTime.UtcNow < ExpiresAt

        // ===== STEP 3: Revoke old token (security!) =====
        refreshToken.RevokedAt = DateTime.UtcNow;
        // Mark as revoked - can't be used again
        // This is TOKEN ROTATION - prevents reuse

        // ===== STEP 4: Generate new access token =====
        var newAccessToken = GenerateAccessToken(
            refreshToken.User.Id,              // From DB
            refreshToken.User.Email,           // From DB
            refreshToken.User.Role ?? DEFAULT_ROLE);  // From DB (default if null)
        // Calls GenerateAccessToken() above
        // Returns: "eyJ..." (new JWT)

        // ===== STEP 5: Generate new refresh token =====
        var newRefreshToken = GenerateRefreshToken(ipAddress);
        // Calls GenerateRefreshToken() above
        // Returns: RefreshToken object with new random string

        newRefreshToken.UserId = refreshToken.UserId;
        // Link to user

        // ===== STEP 6: Save new refresh token to DB =====
        _context.RefreshTokens.Add(newRefreshToken);
        await _context.SaveChangesAsync();

        // Database now has:
        // Old token: RevokedAt = 2025-11-16 (can't use)
        // New token: RevokedAt = null (active)

        // ===== STEP 7: Return response =====
        return new TokenResponse(
            AccessToken: newAccessToken,           // "eyJ..." (15 min)
            RefreshToken: newRefreshToken.Token,   // "xyz789..." (7 days)
            ExpiresIn: int.Parse(_configuration.GetSection("Jwt")["AccessTokenExpiryMinutes"] ?? "15") * 60
            // 15 min * 60 sec = 900 seconds
        );
    }

    /// <summary>
    /// Revokes a refresh token (for logout).
    /// Input: Refresh token string
    /// Output: Nothing (void)
    /// Database: Update token (set RevokedAt)
    /// </summary>
    public async Task RevokeTokenAsync(string token)
    {
        // ===== STEP 1: Find token in database =====
        var refreshToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == token);

        // Query: SELECT * FROM RefreshTokens WHERE Token = 'xyz789'

        // ===== STEP 2: Check if exists and active =====
        if (refreshToken != null && refreshToken.IsActive)
        {
            // Only revoke if:
            // - Token exists (not null)
            // - Token is active (not already revoked, not expired)

            // ===== STEP 3: Revoke it =====
            refreshToken.RevokedAt = DateTime.UtcNow;

            // ===== STEP 4: Save to database =====
            await _context.SaveChangesAsync();

            // UPDATE RefreshTokens
            // SET RevokedAt = '2025-11-16 10:30:00'
            // WHERE Token = 'xyz789'
        }

        // If token doesn't exist or already revoked → silently do nothing
        // This is safe - already logged out or invalid token
    }
}
