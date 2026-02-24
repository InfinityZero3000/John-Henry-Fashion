using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using JohnHenryFashionWeb.Data;

namespace JohnHenryFashionWeb.Tests;

/// <summary>
/// Custom WebApplicationFactory cho integration tests.
/// Sử dụng môi trường "Testing" → Program.cs sẽ tự dùng InMemory database.
/// </summary>
public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        // Override các cấu hình cần thiết cho testing
        builder.ConfigureAppConfiguration((ctx, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                // Disable Redis → dùng in-memory cache
                ["ConnectionStrings:Redis"] = "",
                ["ConnectionStrings:RedisCloud"] = "",
                // Google OAuth test credentials
                ["Authentication:Google:ClientId"] = "test-google-client-id",
                ["Authentication:Google:ClientSecret"] = "test-google-secret",
                // JWT - cần key đủ độ dài cho HMAC-SHA256
                ["JWT:SecretKey"] = "test-jwt-secret-key-minimum-256-bits-for-hmacsha256-algorithm!abc",
                ["JWT:Issuer"] = "TestIssuer",
                ["JWT:Audience"] = "TestAudience",
                // Email mock
                ["EmailSettings:SmtpServer"] = "localhost",
                ["EmailSettings:SmtpPort"] = "25",
                ["EmailSettings:Username"] = "test@test.com",
                ["EmailSettings:Password"] = "testpass",
                ["EmailSettings:FromEmail"] = "test@test.com",
                ["EmailSettings:FromName"] = "Test"
            });
        });
    }
}
