using SecureApi.Data;
using SecureApi.Endpoints;
using SecureApi.Models;
using Microsoft.EntityFrameworkCore;

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
// MIDDLEWARE PIPELINE
// ───────────────────────────────────────────────────────────────

// HTTPS Redirection (HTTP → HTTPS)
app.UseHttpsRedirection();

// HSTS (only in production, not on localhost)
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

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
