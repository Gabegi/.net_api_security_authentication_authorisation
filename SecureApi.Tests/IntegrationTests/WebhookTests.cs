using System.Net;
using System.Text.Json;
using FluentAssertions;
using SecureApi.API.Endpoints;
using SecureApi.Infrastructure.Persistence.Models;
using SecureApi.Tests.Fixtures;
using Xunit;

namespace SecureApi.Tests.IntegrationTests;

/// <summary>
/// Integration tests for API key authentication and webhook endpoints.
/// Tests the ApiKeyMiddleware and webhook/partner endpoints.
/// </summary>
public class WebhookTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public WebhookTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    #region API Key Creation Tests

    [Fact]
    public async Task CreateApiKey_WithValidData_Succeeds()
    {
        // Arrange
        var apiKey = new ApiKey
        {
            Key = "sk_test_" + Guid.NewGuid().ToString().Substring(0, 20),
            Name = "Test API Key",
            Owner = "test-owner",
            Scopes = "[\"webhooks\"]",
            IsActive = true
        };

        // Act
        _factory.ExecuteDbContext(db =>
        {
            db.ApiKeys.Add(apiKey);
            db.SaveChanges();
        });

        // Assert
        var createdKey = _factory.ExecuteDbContext(db =>
            db.ApiKeys.FirstOrDefault(k => k.Owner == "test-owner")
        );

        createdKey.Should().NotBeNull();
        createdKey!.Name.Should().Be("Test API Key");
        createdKey.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task CreateApiKey_WithExpiration_Succeeds()
    {
        // Arrange
        var expiresAt = DateTime.UtcNow.AddDays(30);
        var apiKey = new ApiKey
        {
            Key = "sk_test_" + Guid.NewGuid().ToString().Substring(0, 20),
            Name = "Expiring API Key",
            Owner = "expiring-owner",
            Scopes = "[\"webhooks\"]",
            IsActive = true,
            ExpiresAt = expiresAt
        };

        // Act
        _factory.ExecuteDbContext(db =>
        {
            db.ApiKeys.Add(apiKey);
            db.SaveChanges();
        });

        // Assert
        var createdKey = _factory.ExecuteDbContext(db =>
            db.ApiKeys.FirstOrDefault(k => k.Owner == "expiring-owner")
        );

        createdKey.Should().NotBeNull();
        createdKey!.ExpiresAt.Should().Be(expiresAt);
    }

    #endregion

    #region Webhook Endpoint Tests

    [Fact]
    public async Task StripeWebhook_WithoutApiKey_Returns401()
    {
        // Arrange
        var request = new StripeWebhookRequest
        {
            EventType = "payment.success",
            EventId = "evt_test_123",
            Data = "{}"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/webhooks/stripe", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("API Key required");
    }

    [Fact]
    public async Task StripeWebhook_WithInvalidApiKey_Returns401()
    {
        // Arrange
        var request = new StripeWebhookRequest
        {
            EventType = "payment.success",
            EventId = "evt_test_123",
            Data = "{}"
        };

        _client.DefaultRequestHeaders.Add("X-API-Key", "sk_invalid_key_12345");

        // Act
        var response = await _client.PostAsJsonAsync("/api/webhooks/stripe", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Invalid or inactive API Key");
    }

    [Fact]
    public async Task StripeWebhook_WithValidApiKey_Returns200()
    {
        // Arrange - Create a valid API key
        var validKey = "sk_test_" + Guid.NewGuid().ToString().Substring(0, 20);
        _factory.ExecuteDbContext(db =>
        {
            db.ApiKeys.Add(new ApiKey
            {
                Key = validKey,
                Name = "Test Stripe Key",
                Owner = "stripe",
                Scopes = "[\"webhooks\"]",
                IsActive = true
            });
            db.SaveChanges();
        });

        _client.DefaultRequestHeaders.Add("X-API-Key", validKey);

        var request = new StripeWebhookRequest
        {
            EventType = "payment.success",
            EventId = "evt_test_123",
            Data = "{}"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/webhooks/stripe", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("received");
    }

    [Fact]
    public async Task StripeWebhook_WithExpiredApiKey_Returns401()
    {
        // Arrange - Create an expired API key
        var expiredKey = "sk_expired_" + Guid.NewGuid().ToString().Substring(0, 20);
        _factory.ExecuteDbContext(db =>
        {
            db.ApiKeys.Add(new ApiKey
            {
                Key = expiredKey,
                Name = "Expired Stripe Key",
                Owner = "stripe_expired",
                Scopes = "[\"webhooks\"]",
                IsActive = true,
                ExpiresAt = DateTime.UtcNow.AddDays(-1)  // Expired yesterday
            });
            db.SaveChanges();
        });

        _client.DefaultRequestHeaders.Add("X-API-Key", expiredKey);

        var request = new StripeWebhookRequest
        {
            EventType = "payment.success",
            EventId = "evt_test_123",
            Data = "{}"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/webhooks/stripe", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("expired");
    }

    [Fact]
    public async Task StripeWebhook_WithInactiveApiKey_Returns401()
    {
        // Arrange - Create an inactive API key
        var inactiveKey = "sk_inactive_" + Guid.NewGuid().ToString().Substring(0, 20);
        _factory.ExecuteDbContext(db =>
        {
            db.ApiKeys.Add(new ApiKey
            {
                Key = inactiveKey,
                Name = "Inactive Stripe Key",
                Owner = "stripe_inactive",
                Scopes = "[\"webhooks\"]",
                IsActive = false  // Inactive
            });
            db.SaveChanges();
        });

        _client.DefaultRequestHeaders.Add("X-API-Key", inactiveKey);

        var request = new StripeWebhookRequest
        {
            EventType = "payment.success",
            EventId = "evt_test_123",
            Data = "{}"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/webhooks/stripe", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GenericWebhook_WithValidApiKey_Returns200()
    {
        // Arrange - Create a valid API key
        var validKey = "sk_generic_" + Guid.NewGuid().ToString().Substring(0, 20);
        _factory.ExecuteDbContext(db =>
        {
            db.ApiKeys.Add(new ApiKey
            {
                Key = validKey,
                Name = "Test Generic Key",
                Owner = "generic_service",
                Scopes = "[\"webhooks\"]",
                IsActive = true
            });
            db.SaveChanges();
        });

        _client.DefaultRequestHeaders.Add("X-API-Key", validKey);

        var request = new GenericWebhookRequest
        {
            Action = "order.created",
            Payload = "{\"orderId\": 123}"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/webhooks/generic", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("received");
    }

    #endregion

    #region Partner Endpoint Tests

    [Fact]
    public async Task PartnerStatus_WithoutApiKey_Returns401()
    {
        // Act
        var response = await _client.GetAsync("/api/partner/status");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PartnerStatus_WithValidApiKey_Returns200()
    {
        // Arrange - Create a valid partner API key
        var partnerKey = "pk_test_" + Guid.NewGuid().ToString().Substring(0, 20);
        _factory.ExecuteDbContext(db =>
        {
            db.ApiKeys.Add(new ApiKey
            {
                Key = partnerKey,
                Name = "Partner API Key",
                Owner = "partner_acme",
                Scopes = "[\"products:read\", \"orders:read\"]",
                IsActive = true
            });
            db.SaveChanges();
        });

        _client.DefaultRequestHeaders.Add("X-API-Key", partnerKey);

        // Act
        var response = await _client.GetAsync("/api/partner/status");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("partner_acme");
    }

    [Fact]
    public async Task PartnerProducts_WithValidApiKey_ReturnsProducts()
    {
        // Arrange - Create a valid partner API key and some products
        var partnerKey = "pk_test_" + Guid.NewGuid().ToString().Substring(0, 20);
        _factory.ExecuteDbContext(db =>
        {
            db.ApiKeys.Add(new ApiKey
            {
                Key = partnerKey,
                Name = "Partner API Key",
                Owner = "partner_xyz",
                Scopes = "[\"products:read\"]",
                IsActive = true
            });

            // Ensure products exist
            if (!db.Products.Any())
            {
                db.Products.Add(new Product
                {
                    Name = "Test Product",
                    Description = "Test",
                    Price = 99.99m,
                    Category = "Test",
                    StockQuantity = 10
                });
            }

            db.SaveChanges();
        });

        _client.DefaultRequestHeaders.Add("X-API-Key", partnerKey);

        // Act
        var response = await _client.GetAsync("/api/partner/products");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("partner_xyz");
        content.Should().Contain("productCount");
    }

    #endregion

    #region API Key Tracking Tests

    [Fact]
    public async Task ApiKey_LastUsedAt_UpdatesOnUsage()
    {
        // Arrange - Create a valid API key with no LastUsedAt
        var validKey = "sk_track_" + Guid.NewGuid().ToString().Substring(0, 20);
        ApiKey? createdKey = null;

        _factory.ExecuteDbContext(db =>
        {
            createdKey = new ApiKey
            {
                Key = validKey,
                Name = "Tracking Key",
                Owner = "tracker",
                Scopes = "[\"webhooks\"]",
                IsActive = true,
                LastUsedAt = null
            };
            db.ApiKeys.Add(createdKey);
            db.SaveChanges();
        });

        createdKey!.LastUsedAt.Should().BeNull();

        _client.DefaultRequestHeaders.Add("X-API-Key", validKey);

        var request = new StripeWebhookRequest
        {
            EventType = "test.event",
            EventId = "evt_123",
            Data = "{}"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/webhooks/stripe", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var updatedKey = _factory.ExecuteDbContext(db =>
            db.ApiKeys.FirstOrDefault(k => k.Key == validKey)
        );

        updatedKey.Should().NotBeNull();
        updatedKey!.LastUsedAt.Should().NotBeNull();
        updatedKey.LastUsedAt.Should().BeAfter(DateTime.UtcNow.AddSeconds(-5));
    }

    #endregion
}
