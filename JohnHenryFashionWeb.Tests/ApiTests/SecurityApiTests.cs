using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace JohnHenryFashionWeb.Tests.ApiTests;

/// <summary>
/// Tests cho SecurityController - /api/Security/*
/// Tất cả endpoints yêu cầu đăng nhập
/// </summary>
public class SecurityApiTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _anonymousClient;

    public SecurityApiTests(TestWebApplicationFactory factory)
    {
        _anonymousClient = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task CheckSecurity_WithoutAuth_ShouldReturn401Or302()
    {
        var response = await _anonymousClient.GetAsync("/api/Security/check");
        
        Assert.True(
            response.StatusCode == HttpStatusCode.Unauthorized ||
            response.StatusCode == HttpStatusCode.Redirect ||
            response.StatusCode == HttpStatusCode.Found,
            $"Expected 401/302 but got {response.StatusCode}");
    }

    [Theory]
    [InlineData("GET", "/api/Security/check")]
    [InlineData("GET", "/api/Security/logs")]
    [InlineData("GET", "/api/Security/active-sessions")]
    public async Task GetEndpoints_ShouldExist(string method, string url)
    {
        HttpResponseMessage response;
        if (method == "GET")
            response = await _anonymousClient.GetAsync(url);
        else
            response = await _anonymousClient.GetAsync(url);

        Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_Endpoint_ShouldExist()
    {
        var content = new StringContent(
            JsonSerializer.Serialize(new
            {
                CurrentPassword = "old@Pass123",
                NewPassword = "new@Pass123",
                ConfirmNewPassword = "new@Pass123"
            }),
            Encoding.UTF8, "application/json");

        var response = await _anonymousClient.PostAsync("/api/Security/change-password", content);
        Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task TwoFactor_Endpoint_ShouldExist()
    {
        var content = new StringContent(
            JsonSerializer.Serialize(new { Enable = true }),
            Encoding.UTF8, "application/json");

        var response = await _anonymousClient.PostAsync("/api/Security/two-factor", content);
        Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task SendTwoFactorToken_Endpoint_ShouldExist()
    {
        var response = await _anonymousClient.PostAsync("/api/Security/send-2fa-token", null);
        Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task VerifyTwoFactorToken_Endpoint_ShouldExist()
    {
        var content = new StringContent(
            JsonSerializer.Serialize(new { Token = "123456" }),
            Encoding.UTF8, "application/json");

        var response = await _anonymousClient.PostAsync("/api/Security/verify-2fa-token", content);
        Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetActiveSessions_Endpoint_ShouldExist()
    {
        var response = await _anonymousClient.GetAsync("/api/Security/active-sessions");
        Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteSession_Endpoint_ShouldExist()
    {
        var sessionId = "test-session-id";
        var response = await _anonymousClient.DeleteAsync($"/api/Security/sessions/{sessionId}");
        Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteAllSessions_Endpoint_ShouldExist()
    {
        var response = await _anonymousClient.DeleteAsync("/api/Security/sessions");
        Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);
    }
}
