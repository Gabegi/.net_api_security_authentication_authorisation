# API Key Authentication Implementation Guide 🔐

## Overview

This guide documents the complete implementation of API key authentication for service-to-service communication. API keys provide an alternative authentication method to JWT tokens, designed specifically for webhooks and partner integrations.

## What Was Implemented

### 1. **ApiKey Model** (`SecureApi/Infrastructure/Persistence/Models/ApiKey.cs`)

The `ApiKey` entity stores API key credentials and metadata:

```csharp
public class ApiKey
{
    public int Id { get; set; }
    public string Key { get; set; }              // The actual secret key
    public string Name { get; set; }             // Friendly name
    public string Owner { get; set; }            // Owner identifier
    public string Scopes { get; set; }           // JSON array of permissions
    public DateTime CreatedAt { get; set; }      // Creation timestamp
    public DateTime? ExpiresAt { get; set; }     // Optional expiration
    public bool IsActive { get; set; }           // Active/inactive flag
    public DateTime? LastUsedAt { get; set; }    // Last usage timestamp
}
```

**Key Features:**
- Unique index on `Key` for fast lookups
- Indexes on `Owner` and `ExpiresAt` for efficient queries
- Nullable `ExpiresAt` allows keys to never expire
- Automatic timestamps for audit trail

### 2. **Database Configuration** (`ApplicationDbContext.cs`)

The `ApiKey` DbSet and entity configuration was added to support:

- **Unique constraint** on the `Key` column to prevent duplicate keys
- **Indexing** on frequently queried columns (`Owner`, `ExpiresAt`)
- **Default values** for boolean and timestamp columns
- **Column type** specifications for SQLite compatibility

**Migration Applied:** `20251121182303_AddApiKeys`

### 3. **ApiKeyMiddleware** (`SecureApi/API/Middleware/ApiKeyMiddleware.cs`)

The middleware validates API keys for protected endpoints:

```csharp
public class ApiKeyMiddleware
{
    private const string API_KEY_HEADER = "X-API-Key";

    public async Task InvokeAsync(HttpContext context, ApplicationDbContext db)
    {
        // Only check specific paths (webhooks, partner endpoints)
        if (!RequiresApiKey(context.Request.Path))
        {
            await _next(context);
            return;
        }

        // Extract and validate API key
        if (!context.Request.Headers.TryGetValue(API_KEY_HEADER, out var extractedKey))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { error = "API Key required" });
            return;
        }

        // Database lookup
        var apiKey = await db.ApiKeys
            .FirstOrDefaultAsync(k => k.Key == extractedKey && k.IsActive);

        if (apiKey == null)
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { error = "Invalid API Key" });
            return;
        }

        // Check expiration
        if (apiKey.ExpiresAt.HasValue && apiKey.ExpiresAt.Value < DateTime.UtcNow)
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { error = "API Key expired" });
            return;
        }

        // Update last used timestamp
        apiKey.LastUsedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        // Store in context for endpoints
        context.Items["ApiKey"] = apiKey;

        await _next(context);
    }

    private static bool RequiresApiKey(PathString path)
    {
        return path.StartsWithSegments("/api/webhooks") ||
               path.StartsWithSegments("/api/partner");
    }
}
```

**Validation Steps:**
1. Check if endpoint requires API key
2. Extract key from `X-API-Key` header
3. Look up key in database
4. Verify key is active
5. Check expiration date
6. Update `LastUsedAt` timestamp
7. Store in `context.Items` for endpoint use

### 4. **Webhook Endpoints** (`SecureApi/API/Endpoints/WebhookEndpoints.cs`)

Two endpoint groups were created:

#### **Webhooks** (`/api/webhooks`)

```csharp
// POST /api/webhooks/stripe - Stripe payment webhooks
group.MapPost("/stripe", HandleStripeWebhook)

// POST /api/webhooks/generic - Generic webhook handler
group.MapPost("/generic", HandleGenericWebhook)
```

Request models:

```csharp
public class StripeWebhookRequest
{
    public string EventType { get; set; }      // e.g., "payment.success"
    public string EventId { get; set; }        // Unique event ID
    public string Data { get; set; }           // JSON payload
}

public class GenericWebhookRequest
{
    public string Action { get; set; }         // Action identifier
    public string Payload { get; set; }        // JSON payload
}
```

#### **Partner API** (`/api/partner`)

```csharp
// GET /api/partner/status - Check API status
group.MapGet("/status", GetPartnerStatus)

// GET /api/partner/products - Get available products
group.MapGet("/products", GetPartnerProducts)
```

### 5. **Program.cs Integration**

Middleware and endpoints registered in the request pipeline:

```csharp
// In middleware pipeline section:
app.UseAuthentication();      // JWT validation
app.UseApiKeyAuthentication(); // API key validation
app.UseAuthorization();

// In endpoint registration:
app.MapAuthEndpoints();
app.MapProductEndpoints();
app.MapWebhookEndpoints();    // Webhook endpoints
app.MapPartnerEndpoints();    // Partner endpoints
```

**Important:** API key middleware is registered AFTER `UseAuthentication()` to avoid conflict with JWT processing.

### 6. **Comprehensive Tests** (`SecureApi.Tests/IntegrationTests/WebhookTests.cs`)

Full test coverage including:

- **API Key Creation Tests**
  - Valid API key creation
  - API keys with expiration dates

- **Webhook Endpoint Tests**
  - Missing API key (401)
  - Invalid API key (401)
  - Valid API key (200)
  - Expired API key (401)
  - Inactive API key (401)
  - Generic webhook handling

- **Partner Endpoint Tests**
  - Missing API key (401)
  - Valid API key (200)
  - Product retrieval with partner key

- **API Key Tracking Tests**
  - `LastUsedAt` timestamp updates on usage

## Usage Examples

### Creating an API Key

```csharp
var apiKey = new ApiKey
{
    Key = "sk_live_" + GenerateRandomString(32),
    Name = "Stripe Webhook",
    Owner = "stripe",
    Scopes = "[\"webhooks\"]",
    IsActive = true,
    ExpiresAt = DateTime.UtcNow.AddYears(1)
};

context.ApiKeys.Add(apiKey);
await context.SaveChangesAsync();
```

### Seeding API Keys

Use the provided `ApiKeySeeder.cs` script:

```bash
dotnet run --project SecureApi ApiKeySeeder.cs
```

This creates sample API keys for testing:
- **Stripe Key**: `sk_live_...` (Owner: stripe)
- **Partner Key**: `pk_live_...` (Owner: partner_xyz)

### Making Authenticated Requests

#### Using cURL

```bash
# Without API key (fails)
curl -X POST http://localhost:5000/api/webhooks/stripe \
  -H "Content-Type: application/json" \
  -d '{"eventType":"payment.success"}'

# With API key (succeeds)
curl -X POST http://localhost:5000/api/webhooks/stripe \
  -H "X-API-Key: sk_live_abc123..." \
  -H "Content-Type: application/json" \
  -d '{"eventType":"payment.success"}'
```

#### Using HttpClient (C#)

```csharp
var client = new HttpClient();
var apiKey = "sk_live_abc123...";

client.DefaultRequestHeaders.Add("X-API-Key", apiKey);

var response = await client.PostAsJsonAsync(
    "http://localhost:5000/api/webhooks/stripe",
    new StripeWebhookRequest
    {
        EventType = "payment.success",
        EventId = "evt_123",
        Data = "{}"
    });
```

#### Using .NET RestSharp

```csharp
var client = new RestClient("http://localhost:5000");
var request = new RestRequest("/api/webhooks/stripe", Method.Post)
    .AddHeader("X-API-Key", "sk_live_abc123...")
    .AddJsonBody(new StripeWebhookRequest { /* ... */ });

var response = await client.ExecuteAsync(request);
```

## Security Best Practices

### 1. **Key Storage**
- Never commit API keys to version control
- Store in environment variables or secure vaults (Azure Key Vault, AWS Secrets Manager)
- Rotate keys periodically

### 2. **Key Expiration**
Set expiration dates for temporary keys:
```csharp
apiKey.ExpiresAt = DateTime.UtcNow.AddMonths(3);
```

### 3. **Scope Limitation**
Use scopes to limit what each key can access:
```csharp
apiKey.Scopes = "[\"webhooks:write\", \"products:read\"]";
```

### 4. **Key Deactivation**
Deactivate keys instead of deleting:
```csharp
apiKey.IsActive = false;
await context.SaveChangesAsync();
```

### 5. **Audit Trail**
Monitor `LastUsedAt` for suspicious activity:
```csharp
var unusedKeys = await context.ApiKeys
    .Where(k => k.LastUsedAt == null || k.LastUsedAt < DateTime.UtcNow.AddMonths(-3))
    .ToListAsync();
```

### 6. **Rate Limiting**
Combine with rate limiting for extra protection:
```csharp
// Rate limit by API key owner
var key = context.Items["ApiKey"] as ApiKey;
await RateLimiter.CheckLimitAsync(key.Owner);
```

## Configuration

### Endpoint Protection

Modify which endpoints require API keys in `ApiKeyMiddleware.cs`:

```csharp
private static bool RequiresApiKey(PathString path)
{
    return path.StartsWithSegments("/api/webhooks") ||
           path.StartsWithSegments("/api/partner") ||
           path.StartsWithSegments("/api/external");  // Add more
}
```

### Header Name

Change the header name (default: `X-API-Key`):

```csharp
private const string API_KEY_HEADER = "Authorization-Key";  // Custom header
```

## Comparison: API Keys vs JWT

| Feature | API Keys | JWT |
|---------|----------|-----|
| **Use Case** | Service-to-service, webhooks | User authentication |
| **Storage** | Database | Token itself |
| **Validation** | Database lookup | Cryptographic signature |
| **Expiration** | Optional, checked in DB | Embedded in token |
| **Stateless** | No (requires DB) | Yes |
| **Revocation** | Immediate | Requires blacklist |
| **Scope** | String array | Claim-based |

## Troubleshooting

### Issue: 401 Unauthorized despite providing API key

**Solutions:**
1. Verify key exists in database: `SELECT * FROM ApiKeys WHERE Key = '...'`
2. Check `IsActive` flag is `true`
3. Verify expiration: `SELECT ExpiresAt FROM ApiKeys WHERE Key = '...'`
4. Ensure header is `X-API-Key` (case-sensitive)

### Issue: `X-API-Key` header not being read

**Solution:** Verify middleware is registered BEFORE `UseAuthorization()`:
```csharp
app.UseAuthentication();
app.UseApiKeyAuthentication();  // ← Must be here
app.UseAuthorization();
```

### Issue: Performance degradation

**Solutions:**
1. Add database indexes (already configured)
2. Implement caching: `IMemoryCache` for frequently used keys
3. Use read-only replicas for key lookups

## Testing

Run the comprehensive test suite:

```bash
# Run all webhook tests
dotnet test SecureApi.Tests -k WebhookTests

# Run specific test
dotnet test SecureApi.Tests -k "StripeWebhook_WithValidApiKey"

# Run with verbose output
dotnet test SecureApi.Tests -k WebhookTests -v normal
```

## Files Modified/Created

### Created Files
- `SecureApi/Infrastructure/Persistence/Models/ApiKey.cs` - Model definition
- `SecureApi/API/Middleware/ApiKeyMiddleware.cs` - Middleware implementation
- `SecureApi/API/Endpoints/WebhookEndpoints.cs` - Endpoint definitions
- `SecureApi.Tests/IntegrationTests/WebhookTests.cs` - Test suite
- `ApiKeySeeder.cs` - CLI seeding script

### Modified Files
- `SecureApi/Infrastructure/Persistence/ApplicationDbContext.cs` - Added DbSet and entity config
- `SecureApi/Program.cs` - Registered middleware and endpoints
- Database - Migration `20251121182303_AddApiKeys` applied

## Next Steps

1. **Test the implementation** using provided test suite
2. **Seed initial API keys** using `ApiKeySeeder.cs`
3. **Integrate with external services** (Stripe, webhooks, partners)
4. **Implement scope validation** based on endpoint requirements
5. **Add monitoring and alerting** for unusual API key usage
6. **Implement key rotation policy** for security compliance

## References

- [HTTP Header - Authorization](https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Authorization)
- [API Key Best Practices](https://cheatsheetseries.owasp.org/cheatsheets/API_Key_Extraction_Cheat_Sheet.html)
- [Stripe Webhook Security](https://stripe.com/docs/webhooks/best-practices)
- [ASP.NET Core Middleware](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/middleware)
