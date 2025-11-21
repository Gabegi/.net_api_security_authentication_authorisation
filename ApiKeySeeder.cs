using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using SecureApi.Infrastructure.Persistence;
using SecureApi.Infrastructure.Persistence.Models;

// Quick script to seed test API keys
var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
optionsBuilder.UseSqlite("Data Source=secureapi.db");

using var db = new ApplicationDbContext(optionsBuilder.Options);

// Create API keys for testing
var stripeKey = new ApiKey
{
    Key = "sk_live_" + Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)),
    Name = "Stripe Webhook",
    Owner = "stripe",
    Scopes = "[\"webhooks\"]",
    IsActive = true,
    CreatedAt = DateTime.UtcNow,
    ExpiresAt = DateTime.UtcNow.AddYears(1)
};

var partnerKey = new ApiKey
{
    Key = "pk_live_" + Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)),
    Name = "Partner API Access",
    Owner = "partner_xyz",
    Scopes = "[\"products:read\", \"orders:read\"]",
    IsActive = true,
    CreatedAt = DateTime.UtcNow,
    ExpiresAt = DateTime.UtcNow.AddYears(1)
};

// Check if keys already exist
var existingStripe = await db.ApiKeys.FirstOrDefaultAsync(k => k.Owner == "stripe");
var existingPartner = await db.ApiKeys.FirstOrDefaultAsync(k => k.Owner == "partner_xyz");

if (existingStripe == null)
{
    db.ApiKeys.Add(stripeKey);
    Console.WriteLine($"✓ Created Stripe API Key: {stripeKey.Key}");
}
else
{
    Console.WriteLine("✓ Stripe API Key already exists");
}

if (existingPartner == null)
{
    db.ApiKeys.Add(partnerKey);
    Console.WriteLine($"✓ Created Partner API Key: {partnerKey.Key}");
}
else
{
    Console.WriteLine("✓ Partner API Key already exists");
}

await db.SaveChangesAsync();

Console.WriteLine("\n✓ API Keys seeded successfully!");
Console.WriteLine("\nUsage Examples:");
Console.WriteLine("────────────────────────────────────────");
Console.WriteLine("\nStripe Webhook (no authentication required):");
Console.WriteLine("POST /api/webhooks/stripe");
Console.WriteLine("X-API-Key: " + (existingStripe?.Key ?? stripeKey.Key));
Console.WriteLine("\nPartner API Status:");
Console.WriteLine("GET /api/partner/status");
Console.WriteLine("X-API-Key: " + (existingPartner?.Key ?? partnerKey.Key));
