using System.Net.Http.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SecureApi.Application.DTOs.Requests;
using SecureApi.Application.DTOs.Responses;
using SecureApi.Infrastructure.Persistence;
using SecureApi.Tests.Infrastructure;

namespace SecureApi.Tests.Fixtures;

/// <summary>
/// Custom WebApplicationFactory that configures the test server
/// with in-memory SQLite database.
/// Keeps the database connection open for the lifetime of the factory.
/// Overrides JWT configuration to use test-specific secrets.
/// </summary>
public class ApiWebApplicationFactory : WebApplicationFactory<Program>
{
    private SqliteConnection? _connection;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Set environment to Testing to disable rate limiting
        builder.UseEnvironment("Testing");

        // Set JWT configuration via environment variables BEFORE app starts
        // Environment variables are read before ConfigureAppConfiguration, ensuring
        // the JWT middleware gets the correct values at initialization time
        Environment.SetEnvironmentVariable("JWT_SECRET_KEY", "your-super-secret-key-min-32-characters-long!");
        Environment.SetEnvironmentVariable("JWT_ISSUER", "https://localhost:7001");
        Environment.SetEnvironmentVariable("JWT_AUDIENCE", "https://localhost:7001");

        // Also set in configuration for TokenService and other components that read config
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string>
            {
                ["Jwt:SecretKey"] = "your-super-secret-key-min-32-characters-long!",
                ["Jwt:Issuer"] = "https://localhost:7001",
                ["Jwt:Audience"] = "https://localhost:7001",
                ["Jwt:AccessTokenExpiryMinutes"] = "15",
                ["Jwt:RefreshTokenExpiryDays"] = "7"
            }!);
        });

        // CRITICAL: Use ConfigureTestServices instead of ConfigureServices
        // This runs AFTER Program.cs configures services, allowing us to override authentication
        builder.ConfigureTestServices(services =>
        {
            // 1. Remove the real DbContext registration
            var dbDescriptor = services.SingleOrDefault(d =>
                d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));

            if (dbDescriptor != null)
            {
                services.Remove(dbDescriptor);
            }

            // 2. Create and open a connection to SQLite in-memory database
            // This connection must stay open for the database to persist
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            // Add DbContext with the persistent connection
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseSqlite(_connection);
            });

            // 3. Replace JWT authentication with test authentication handler
            // This allows tests to use simple tokens without JWT validation
            services.Configure<AuthenticationOptions>(options =>
            {
                options.DefaultAuthenticateScheme = TestAuthHandler.AuthenticationScheme;
                options.DefaultChallengeScheme = TestAuthHandler.AuthenticationScheme;
            });

            // Remove the old JWT Bearer scheme and its configuration
            var jwtSchemeDescriptors = services
                .Where(d => d.ImplementationType?.Name.Contains("JwtBearer") == true)
                .ToList();
            foreach (var descriptor in jwtSchemeDescriptors)
            {
                services.Remove(descriptor);
            }

            // Add test authentication handler
            services.AddAuthentication(TestAuthHandler.AuthenticationScheme)
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                    TestAuthHandler.AuthenticationScheme, options => { });

            // 4. Create the database schema
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Database.EnsureCreated();
        });
    }

    /// <summary>
    /// Helper to execute database operations in a scope.
    /// Automatically handles scope disposal.
    /// </summary>
    public void ExecuteDbContext(Action<ApplicationDbContext> action)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        action(db);
    }

    /// <summary>
    /// Helper to execute database operations with a return value.
    /// Automatically handles scope disposal.
    /// </summary>
    public T ExecuteDbContext<T>(Func<ApplicationDbContext, T> func)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return func(db);
    }

    /// <summary>
    /// Helper to execute database operations and automatically save changes.
    /// Use this when you need to add/modify data and persist it to the database.
    /// Automatically handles scope disposal.
    /// </summary>
    public void ExecuteDbContextWithSave(Action<ApplicationDbContext> action)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        action(db);
        db.SaveChanges();
    }

    /// <summary>
    /// Helper to execute database operations with a return value and automatically save changes.
    /// Use this when you need to add/modify data and persist it to the database.
    /// Automatically handles scope disposal.
    /// </summary>
    public T ExecuteDbContextWithSave<T>(Func<ApplicationDbContext, T> func)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var result = func(db);
        db.SaveChanges();
        return result;
    }

    /// <summary>
    /// Helper to generate a unique test email.
    /// </summary>
    public static string GenerateTestEmail() =>
        $"test_{Guid.NewGuid().ToString()[..8]}@example.com";

    /// <summary>
    /// Helper to generate a unique test username.
    /// </summary>
    public static string GenerateTestUsername() =>
        $"user_{Guid.NewGuid().ToString()[..8]}";

    /// <summary>
    /// Helper to create a valid register request for testing.
    /// </summary>
    public static RegisterRequest CreateRegisterRequest(
        string? email = null,
        string password = "ValidPass123!",
        string? fullName = null,
        DateTime? birthDate = null)
    {
        return new RegisterRequest
        {
            Email = email ?? GenerateTestEmail(),
            Password = password,
            FullName = fullName ?? GenerateTestUsername(),
            BirthDate = birthDate ?? new DateTime(1990, 1, 1)
        };
    }

    /// <summary>
    /// Helper to create a valid login request for testing.
    /// </summary>
    public static LoginRequest CreateLoginRequest(string email, string password = "ValidPass123!")
    {
        return new LoginRequest
        {
            Email = email,
            Password = password
        };
    }

    /// <summary>
    /// Helper to register a user and set authorization header in one call.
    /// Usage: await factory.RegisterAndAuthorizeAsync(client, birthDate, role);
    /// </summary>
    public async Task<string> RegisterAndAuthorizeAsync(
        HttpClient client,
        string? email = null,
        string password = "ValidPass123!",
        string? fullName = null,
        DateTime? birthDate = null,
        string? roleOverride = null)
    {
        var registerRequest = CreateRegisterRequest(email, password, fullName, birthDate);
        var response = await client.PostAsJsonAsync("/api/auth/register", registerRequest);
        response.EnsureSuccessStatusCode();
        var tokenResponse = (await response.Content.ReadFromJsonAsync<TokenResponse>())!;

        // If role override requested, update in database
        if (roleOverride != null)
        {
            ExecuteDbContext(db =>
            {
                var user = db.Users.First(u => u.Email == registerRequest.Email);
                user.Role = roleOverride;
                db.SaveChanges();
            });
        }

        client.DefaultRequestHeaders.Authorization = new("Bearer", tokenResponse.AccessToken);
        return tokenResponse.AccessToken;
    }

    /// <summary>
    /// Helper to authenticate HTTP client with test Admin token.
    /// Uses simple token format that TestAuthHandler recognizes.
    /// </summary>
    public static void AuthenticateAsAdmin(HttpClient client)
    {
        client.DefaultRequestHeaders.Authorization = new("Bearer", "TestToken-Admin");
    }

    /// <summary>
    /// Helper to authenticate HTTP client with test User token.
    /// Uses simple token format that TestAuthHandler recognizes.
    /// </summary>
    public static void AuthenticateAsUser(HttpClient client)
    {
        client.DefaultRequestHeaders.Authorization = new("Bearer", "TestToken-User");
    }

    /// <summary>
    /// Helper to clear authentication from HTTP client.
    /// </summary>
    public static void ClearAuthentication(HttpClient client)
    {
        client.DefaultRequestHeaders.Authorization = null;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _connection?.Close();
            _connection?.Dispose();
        }
        base.Dispose(disposing);
    }
}
