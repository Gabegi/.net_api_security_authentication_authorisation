using FluentValidation;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SecureApi.Infrastructure.Persistence;
using SecureApi.API.Endpoints;
using SecureApi.API.Extensions;
using SecureApi.API.Middleware;
using SecureApi.Infrastructure.Persistence.Models;
using SecureApi.Application.Services;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// ───────────────────────────────────────────────────────────────
// SERVICES CONFIGURATION
// ───────────────────────────────────────────────────────────────

// Database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Data Source=secureapi.db";
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));

// API Documentation
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Validation
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// Services
builder.Services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAuthResultHandler, AuthResultHandler>();

// Authentication
// Try environment variables first (for testing), then fall back to configuration
var secretKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY")
    ?? builder.Configuration.GetSection("Jwt")["SecretKey"]
    ?? throw new InvalidOperationException("JWT SecretKey not configured");

var jwtSettings = builder.Configuration.GetSection("Jwt");
var issuer = Environment.GetEnvironmentVariable("JWT_ISSUER")
    ?? jwtSettings["Issuer"];
var audience = Environment.GetEnvironmentVariable("JWT_AUDIENCE")
    ?? jwtSettings["Audience"];

builder.Services
    .AddAuthentication("Bearer")
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new()
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(secretKey)),
            ValidateIssuer = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidateAudience = true,
            ValidAudience = jwtSettings["Audience"],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(5) // Allow 5 second clock skew for testing
        };

        // Add event logging for debugging
        options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                var logger = context.HttpContext.RequestServices.GetService<ILogger<Program>>();
                logger?.LogError("Authentication failed: {Message}, Exception: {Exception}",
                    context.Exception?.Message, context.Exception?.ToString());
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                var logger = context.HttpContext.RequestServices.GetService<ILogger<Program>>();
                logger?.LogInformation("Token validated successfully for user: {Identity}",
                    context.Principal?.Identity?.Name);
                return Task.CompletedTask;
            },
            OnChallenge = context =>
            {
                var logger = context.HttpContext.RequestServices.GetService<ILogger<Program>>();
                logger?.LogWarning("Authentication challenge: {Error} - {ErrorDescription}",
                    context.Error, context.ErrorDescription);
                return Task.CompletedTask;
            }
        };
    });

// Authorization with Policies
builder.Services.AddAuthorizationPolicies();

// Rate Limiting (protects against brute force attacks)
if (!builder.Environment.IsEnvironment("Testing"))
{
    // Production: Use real rate limiting policies
    builder.Services.AddRateLimitingPolicies();
}
else
{
    // Testing: Use unlimited rate limiting to allow tests to run
    builder.Services.AddRateLimiter(options =>
    {
        options.AddFixedWindowLimiter("auth", opt =>
        {
            opt.PermitLimit = int.MaxValue;
            opt.Window = TimeSpan.FromMinutes(1);
            opt.AutoReplenishment = true;
        });
        options.AddFixedWindowLimiter("api", opt =>
        {
            opt.PermitLimit = int.MaxValue;
            opt.Window = TimeSpan.FromMinutes(1);
            opt.AutoReplenishment = true;
        });
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    });
}

// ───────────────────────────────────────────────────────────────
// HTTPS CONFIGURATION
// ───────────────────────────────────────────────────────────────

// HTTPS Redirection (redirect HTTP → HTTPS)
builder.Services.AddHttpsRedirection(options =>
{
    options.RedirectStatusCode = StatusCodes.Status307TemporaryRedirect;
    options.HttpsPort = 7012; // HTTPS port from launchSettings
});

// HSTS (HTTP Strict Transport Security)
// Forces browsers to ALWAYS use HTTPS for your domain
builder.Services.AddHsts(options =>
{
    options.MaxAge = TimeSpan.FromDays(365);        // Remember for 1 year
    options.IncludeSubDomains = true;               // Apply to all subdomains
    options.Preload = true;                         // Submit to browser preload list
});

// ───────────────────────────────────────────────────────────────
// CORS CONFIGURATION
// ───────────────────────────────────────────────────────────────
// NOTE: CORS is ONLY needed for browser-based frontends (React, Vue, Angular, etc.)
// Postman, curl, mobile apps, and server-to-server calls DON'T need CORS
// This is here for learning purposes - replace with your actual frontend domain

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(
                "https://myapp.com",           // Replace with your frontend domain
                "https://www.myapp.com",       // Include www version if needed
                "http://localhost:3000",       // Local development (React, Vue, etc.)
                "http://localhost:5173")       // Vite dev server
            .WithMethods("GET", "POST", "PUT", "DELETE")
            .WithHeaders("Content-Type", "Authorization")
            .AllowCredentials();               // Allow cookies/auth headers
    });
});

var app = builder.Build();

// ───────────────────────────────────────────────────────────────
// DATABASE INITIALIZATION & SEEDING
// ───────────────────────────────────────────────────────────────

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.EnsureCreatedAsync();

    // Seed sample products if empty
    try
    {
        if (!await db.Products.AnyAsync())
        {
            db.Products.AddRange(
                new Product { Name = "Laptop Pro", Description = "High-performance laptop", Price = 1299.99m, Category = "Electronics", StockQuantity = 50 },
                new Product { Name = "Wireless Mouse", Description = "Ergonomic wireless mouse", Price = 29.99m, Category = "Accessories", StockQuantity = 200 },
                new Product { Name = "Mechanical Keyboard", Description = "RGB mechanical keyboard", Price = 149.99m, Category = "Accessories", StockQuantity = 75 },
                new Product { Name = "Office Chair", Description = "Ergonomic office chair", Price = 299.99m, Category = "Furniture", StockQuantity = 30 },
                new Product { Name = "USB-C Hub", Description = "7-in-1 USB-C hub", Price = 49.99m, Category = "Accessories", StockQuantity = 100 }
            );
            await db.SaveChangesAsync();
        }
    }
    catch
    {
        // Tables may not exist yet, that's okay
    }
}

// Seed initial admin user
await app.SeedInitialAdminAsync();

// ───────────────────────────────────────────────────────────────
// MIDDLEWARE PIPELINE (Order matters!)
// ───────────────────────────────────────────────────────────────

// 1. Global exception handling (first - catch all errors)
app.UseGlobalExceptionHandler();

// 2. HTTPS redirection (HTTP → HTTPS) - MUST come before HSTS
app.UseHttpsRedirection();

// 3. HSTS (only in production, not on localhost)
// Strict-Transport-Security: forces HTTPS for 1 year
// MUST come after HTTPS redirection to only be sent on HTTPS responses
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

// 4. CORS middleware (must be before UseAuthentication and UseAuthorization)
// Allows browser-based frontends to make requests to this API
app.UseCors();

// 5. Swagger (development only)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// 6. Rate Limiting (must be before Authentication)
// Prevents brute force attacks on auth endpoints
// Skip in test environment to allow test execution
if (!app.Environment.IsEnvironment("Testing"))
{
    app.UseRateLimiter();
}

// 7. Authentication (verify JWT tokens)
app.UseAuthentication();

// 8. Authorization (check permissions)
app.UseAuthorization();

// ───────────────────────────────────────────────────────────────
// MAP ENDPOINTS
// ───────────────────────────────────────────────────────────────

app.MapAuthEndpoints();
app.MapProductEndpoints();

app.Run();

// Make Program accessible to test projects
public partial class Program { }
