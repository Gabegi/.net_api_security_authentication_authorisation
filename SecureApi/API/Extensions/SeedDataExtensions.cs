using SecureApi.Application.Services;
using SecureApi.Infrastructure.Persistence;
using SecureApi.Infrastructure.Persistence.Models;

namespace SecureApi.API.Extensions;

/// <summary>
/// Extension methods for seeding initial data into the database.
/// </summary>
public static class SeedDataExtensions
{
    /// <summary>
    /// Seeds initial admin user if no admins exist.
    /// Uses configuration from appsettings or defaults to sensible values.
    /// </summary>
    /// <param name="app">The web application.</param>
    /// <returns>A completed task.</returns>
    public static async Task SeedInitialAdminAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        // Check if any admin user already exists
        if (!await db.Users.AnyAsync(u => u.Role == "Admin"))
        {
            // Read from configuration (allows per-environment customization)
            var adminEmail = config["AdminUser:Email"] ?? "admin@secureapi.com";
            var adminPassword = config["AdminUser:Password"] ?? "Admin@123!";

            var admin = new User
            {
                Email = adminEmail,
                FullName = "System Administrator",
                PasswordHash = passwordHasher.HashPassword(adminPassword),
                Role = "Admin",
                BirthDate = new DateTime(1990, 1, 1),
                CreatedAt = DateTime.UtcNow
            };

            db.Users.Add(admin);
            await db.SaveChangesAsync();

            // Only log credentials in development (security best practice)
            if (app.Environment.IsDevelopment())
            {
                Console.WriteLine("✅ Admin user created:");
                Console.WriteLine($"   Email: {adminEmail}");
                Console.WriteLine($"   Password: {adminPassword}");
                Console.WriteLine("   ⚠️  Change this password in production!");
            }
        }
    }
}
