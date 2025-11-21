using Microsoft.EntityFrameworkCore;
using SecureApi.Infrastructure.Persistence;
using SecureApi.Infrastructure.Persistence.Models;

namespace SecureApi.API.Endpoints;

/// <summary>
/// Webhook and partner API endpoints that require API key authentication.
/// These endpoints do not require JWT authentication.
/// </summary>
public static class WebhookEndpoints
{
    /// <summary>
    /// Maps webhook-related endpoints to the application.
    /// </summary>
    /// <param name="app">The web application</param>
    public static void MapWebhookEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/webhooks")
            .WithTags("Webhooks")
            .WithOpenApi();

        // POST /api/webhooks/stripe - Stripe payment webhook
        group.MapPost("/stripe", HandleStripeWebhook)
            .WithName("StripeWebhook")
            .WithSummary("Handle Stripe payment webhook")
            .Produces(200)
            .Produces(401)
            .WithDescription("Receives and processes Stripe webhook events. Requires X-API-Key header.");

        // POST /api/webhooks/generic - Generic webhook endpoint
        group.MapPost("/generic", HandleGenericWebhook)
            .WithName("GenericWebhook")
            .WithSummary("Handle generic webhook")
            .Produces(200)
            .Produces(401)
            .WithDescription("Receives and processes generic webhook events. Requires X-API-Key header.");
    }

    /// <summary>
    /// Maps partner API endpoints that require API key authentication.
    /// </summary>
    /// <param name="app">The web application</param>
    public static void MapPartnerEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/partner")
            .WithTags("Partner API")
            .WithOpenApi();

        // GET /api/partner/status - Check partner API status
        group.MapGet("/status", GetPartnerStatus)
            .WithName("PartnerStatus")
            .WithSummary("Get partner API status")
            .Produces(200)
            .Produces(401)
            .WithDescription("Returns the status of the partner API. Requires X-API-Key header.");

        // GET /api/partner/products - Get products available to partner
        group.MapGet("/products", GetPartnerProducts)
            .WithName("PartnerProducts")
            .WithSummary("Get partner products")
            .Produces(200)
            .Produces(401)
            .WithDescription("Returns products available to the partner. Requires X-API-Key header.");
    }

    /// <summary>
    /// Handles Stripe webhook events.
    /// </summary>
    /// <param name="context">The HTTP context containing the API key in context items</param>
    /// <param name="db">The database context</param>
    /// <param name="body">The webhook payload</param>
    /// <returns>A webhook receipt response</returns>
    private static async Task<IResult> HandleStripeWebhook(
        HttpContext context,
        ApplicationDbContext db,
        StripeWebhookRequest body)
    {
        // API key already validated by middleware
        var apiKey = context.Items["ApiKey"] as ApiKey;
        if (apiKey == null)
        {
            return Results.Unauthorized();
        }

        // Log the webhook event (in a real application, you would process it)
        Console.WriteLine($"Stripe webhook received from {apiKey.Owner}: {body.EventType}");

        // Simulate processing
        await Task.Delay(100);

        return Results.Ok(new { received = true, eventType = body.EventType });
    }

    /// <summary>
    /// Handles generic webhook events.
    /// </summary>
    /// <param name="context">The HTTP context containing the API key in context items</param>
    /// <param name="db">The database context</param>
    /// <param name="body">The webhook payload</param>
    /// <returns>A webhook receipt response</returns>
    private static async Task<IResult> HandleGenericWebhook(
        HttpContext context,
        ApplicationDbContext db,
        GenericWebhookRequest body)
    {
        // API key already validated by middleware
        var apiKey = context.Items["ApiKey"] as ApiKey;
        if (apiKey == null)
        {
            return Results.Unauthorized();
        }

        // Log the webhook event
        Console.WriteLine($"Generic webhook received from {apiKey.Owner}: {body.Action}");

        // Simulate processing
        await Task.Delay(100);

        return Results.Ok(new { received = true, action = body.Action, timestamp = DateTime.UtcNow });
    }

    /// <summary>
    /// Gets the partner API status.
    /// </summary>
    /// <param name="context">The HTTP context containing the API key in context items</param>
    /// <param name="db">The database context</param>
    /// <returns>The API status</returns>
    private static async Task<IResult> GetPartnerStatus(
        HttpContext context,
        ApplicationDbContext db)
    {
        var apiKey = context.Items["ApiKey"] as ApiKey;
        if (apiKey == null)
        {
            return Results.Unauthorized();
        }

        // Return partner-specific status
        var status = new
        {
            status = "operational",
            partner = apiKey.Owner,
            apiKeyName = apiKey.Name,
            lastUsed = apiKey.LastUsedAt,
            expiresAt = apiKey.ExpiresAt,
            timestamp = DateTime.UtcNow
        };

        return Results.Ok(status);
    }

    /// <summary>
    /// Gets products available to the partner.
    /// </summary>
    /// <param name="context">The HTTP context containing the API key in context items</param>
    /// <param name="db">The database context</param>
    /// <returns>A list of available products</returns>
    private static async Task<IResult> GetPartnerProducts(
        HttpContext context,
        ApplicationDbContext db)
    {
        var apiKey = context.Items["ApiKey"] as ApiKey;
        if (apiKey == null)
        {
            return Results.Unauthorized();
        }

        // In a real application, you would filter products based on partner permissions
        // For now, return all products
        var products = await db.Products.ToListAsync();

        return Results.Ok(new
        {
            partner = apiKey.Owner,
            productCount = products.Count,
            products = products
        });
    }
}

/// <summary>
/// Represents a Stripe webhook request.
/// </summary>
public class StripeWebhookRequest
{
    /// <summary>
    /// Gets or sets the Stripe event type.
    /// </summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Stripe event ID.
    /// </summary>
    public string EventId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the event data (JSON serialized).
    /// </summary>
    public string Data { get; set; } = string.Empty;
}

/// <summary>
/// Represents a generic webhook request.
/// </summary>
public class GenericWebhookRequest
{
    /// <summary>
    /// Gets or sets the action being performed.
    /// </summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the payload data (JSON serialized).
    /// </summary>
    public string Payload { get; set; } = string.Empty;
}
