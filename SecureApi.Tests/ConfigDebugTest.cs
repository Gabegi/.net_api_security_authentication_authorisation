using Microsoft.Extensions.Configuration;
using SecureApi.Tests.Fixtures;
using Xunit;
using Xunit.Abstractions;

namespace SecureApi.Tests;

public class ConfigDebugTest : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;
    private readonly ITestOutputHelper _output;

    public ConfigDebugTest(ApiWebApplicationFactory factory, ITestOutputHelper output)
    {
        _factory = factory;
        _output = output;
    }

    [Fact]
    public void Test_JWTConfigurationInTest()
    {
        // Get the services from the factory to access IConfiguration
        var configuration = _factory.Services.GetService<IConfiguration>();
        Assert.NotNull(configuration);

        // Read the JWT configuration
        var jwtSettings = configuration!.GetSection("Jwt");
        var secretKey = jwtSettings["SecretKey"];
        var issuer = jwtSettings["Issuer"];
        var audience = jwtSettings["Audience"];
        var accessTokenExpiry = jwtSettings["AccessTokenExpiryMinutes"];
        var refreshTokenExpiry = jwtSettings["RefreshTokenExpiryDays"];

        _output.WriteLine($"JWT Configuration in Test Factory:");
        _output.WriteLine($"  SecretKey: {secretKey}");
        _output.WriteLine($"  Issuer: {issuer}");
        _output.WriteLine($"  Audience: {audience}");
        _output.WriteLine($"  AccessTokenExpiryMinutes: {accessTokenExpiry}");
        _output.WriteLine($"  RefreshTokenExpiryDays: {refreshTokenExpiry}");

        // Verify the values
        Assert.NotNull(secretKey);
        Assert.NotNull(issuer);
        Assert.NotNull(audience);
        Assert.NotNull(accessTokenExpiry);
        Assert.NotNull(refreshTokenExpiry);

        Assert.Equal("your-super-secret-key-min-32-characters-long!", secretKey);
        Assert.Equal("https://localhost:7001", issuer);
        Assert.Equal("https://localhost:7001", audience);
    }
}
