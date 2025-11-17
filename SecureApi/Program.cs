using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SecureApi.Data;
using SecureApi.Endpoints;
using SecureApi.Middleware;
using SecureApi.Models;
using SecureApi.Services;

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

// Authentication
var jwtSettings = builder.Configuration.GetSection("Jwt");
var secretKey = jwtSettings["SecretKey"]
    ?? throw new InvalidOperationException("JWT SecretKey not configured");

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
            ClockSkew = TimeSpan.Zero
        };
    });

// Authorization
builder.Services.AddAuthorization();

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

// ───────────────────────────────────────────────────────────────
// MIDDLEWARE PIPELINE (Order matters!)
// ───────────────────────────────────────────────────────────────

// 1. Exception handling (first - catch all errors)
app.UseExceptionHandler("/error");

// 2. Security headers middleware (before HTTPS redirect)
app.UseSecurityHeaders();

// 3. HSTS (only in production, not on localhost)
// Strict-Transport-Security: forces HTTPS for 1 year
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

// 4. HTTPS redirection (HTTP → HTTPS)
app.UseHttpsRedirection();

// 5. CORS middleware (must be before UseAuthentication and UseAuthorization)
// Allows browser-based frontends to make requests to this API
app.UseCors();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// ───────────────────────────────────────────────────────────────
// MAP ENDPOINTS
// ───────────────────────────────────────────────────────────────

app.MapProductEndpoints();

app.Run();
