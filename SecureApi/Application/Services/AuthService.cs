using Microsoft.EntityFrameworkCore;
using SecureApi.Application.DTOs.Requests;
using SecureApi.Application.DTOs.Responses;
using SecureApi.Application.Exceptions;
using SecureApi.Infrastructure.Persistence;
using SecureApi.Infrastructure.Persistence.Models;

namespace SecureApi.Application.Services;

/// <summary>
/// Authentication service implementation.
/// Handles user registration, login, token refresh, and logout with all business logic.
/// </summary>
public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;

    /// <summary>
    /// Initializes a new instance of the AuthService class.
    /// </summary>
    public AuthService(
        ApplicationDbContext db,
        IPasswordHasher passwordHasher,
        ITokenService tokenService)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
        _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
    }

    /// <summary>
    /// Registers a new user with the provided credentials.
    /// Creates user, hashes password, generates tokens, and stores refresh token.
    /// </summary>
    public async Task<TokenResponse> RegisterAsync(RegisterRequest request, string ipAddress)
    {
        // Check if user already exists
        var existingUser = await _db.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email);

        if (existingUser != null)
        {
            throw new DuplicateResourceException($"User with email '{request.Email}' already exists");
        }

        // Hash password using BCrypt with work factor 12
        var hashedPassword = _passwordHasher.HashPassword(request.Password);

        // Create new user entity
        var user = new User
        {
            Email = request.Email,
            FullName = request.FullName,
            BirthDate = request.BirthDate,
            PasswordHash = hashedPassword,
            Role = "User" // Default role for new users
        };

        // Save user to database
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        // Generate JWT access token (15 minutes)
        var accessToken = _tokenService.GenerateAccessToken(user.Id, user.Email, user.Role);

        // Generate refresh token (7 days, random base64 string)
        var refreshToken = _tokenService.GenerateRefreshToken(ipAddress);

        // Save refresh token to database for revocation support
        var refreshTokenEntity = new RefreshToken
        {
            Token = refreshToken.Token,
            UserId = user.Id,
            ExpiresAt = refreshToken.ExpiresAt,
            CreatedByIp = ipAddress
        };

        _db.RefreshTokens.Add(refreshTokenEntity);
        await _db.SaveChangesAsync();

        // Return tokens to client
        return new TokenResponse(
            AccessToken: accessToken,
            RefreshToken: refreshToken.Token,
            ExpiresIn: 900 // 15 minutes in seconds
        );
    }

    /// <summary>
    /// Authenticates a user with email and password.
    /// Verifies credentials, generates tokens, and stores refresh token.
    /// </summary>
    public async Task<TokenResponse> LoginAsync(LoginRequest request, string ipAddress)
    {
        // Find user by email
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email);

        if (user == null)
        {
            throw new UnauthorizedAccessException("Invalid email or password");
        }

        // Verify password using BCrypt
        var isPasswordValid = _passwordHasher.VerifyPassword(request.Password, user.PasswordHash);

        if (!isPasswordValid)
        {
            throw new UnauthorizedAccessException("Invalid email or password");
        }

        // Generate JWT access token (15 minutes)
        var accessToken = _tokenService.GenerateAccessToken(user.Id, user.Email, user.Role);

        // Generate refresh token (7 days, random base64 string)
        var refreshToken = _tokenService.GenerateRefreshToken(ipAddress);

        // Save refresh token to database for revocation support
        var refreshTokenEntity = new RefreshToken
        {
            Token = refreshToken.Token,
            UserId = user.Id,
            ExpiresAt = refreshToken.ExpiresAt,
            CreatedByIp = ipAddress
        };

        _db.RefreshTokens.Add(refreshTokenEntity);
        await _db.SaveChangesAsync();

        // Return tokens to client
        return new TokenResponse(
            AccessToken: accessToken,
            RefreshToken: refreshToken.Token,
            ExpiresIn: 900 // 15 minutes in seconds
        );
    }

    /// <summary>
    /// Refreshes an expired access token using a valid refresh token.
    /// Implements token rotation: validates old token, revokes it, and issues new tokens.
    /// This prevents replay attacks by ensuring old refresh tokens cannot be reused.
    /// </summary>
    public async Task<TokenResponse> RefreshTokenAsync(RefreshRequest request, string ipAddress)
    {
        // Delegate token refresh to TokenService which handles:
        // - Validation of refresh token
        // - Revocation of old refresh token
        // - Generation of new tokens
        return await _tokenService.RefreshTokenAsync(request.RefreshToken, ipAddress);
    }

    /// <summary>
    /// Logs out a user by revoking their refresh token.
    /// Once revoked, the token cannot be used to generate new access tokens.
    /// </summary>
    public async Task LogoutAsync(LogoutRequest request)
    {
        // Revoke the refresh token to prevent further use
        await _tokenService.RevokeTokenAsync(request.RefreshToken);
    }
}
