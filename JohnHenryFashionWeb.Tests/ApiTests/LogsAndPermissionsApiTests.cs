using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace JohnHenryFashionWeb.Tests.ApiTests;

/// <summary>
/// Tests cho LogsApiController - /api/admin/logs/*
/// Yêu cầu role Admin
/// </summary>
public class LogsApiTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _anonymousClient;

    public LogsApiTests(TestWebApplicationFactory factory)
    {
        _anonymousClient = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task GetLogs_WithoutAuth_ShouldReturn401Or302()
    {
        var response = await _anonymousClient.GetAsync("/api/admin/logs");
        
        Assert.True(
            response.StatusCode is HttpStatusCode.Unauthorized or
            HttpStatusCode.Redirect or HttpStatusCode.Found,
            $"Expected 401/302 but got {response.StatusCode}");
    }

    [Fact]
    public async Task GetLogs_Endpoint_ShouldExist()
    {
        var response = await _anonymousClient.GetAsync("/api/admin/logs");
        Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetLogs_WithLevelFilter_ShouldExist()
    {
        var response = await _anonymousClient.GetAsync("/api/admin/logs?level=ERR&maxCount=100");
        Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetLogs_WithDateRange_ShouldExist()
    {
        var from = DateTime.UtcNow.AddDays(-7).ToString("yyyy-MM-ddTHH:mm:ssZ");
        var to = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
        var response = await _anonymousClient.GetAsync($"/api/admin/logs?fromDate={from}&toDate={to}");
        Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetLogById_Endpoint_ShouldExist()
    {
        var response = await _anonymousClient.GetAsync("/api/admin/logs/test-log-id-123");
        Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);
    }
}

/// <summary>
/// Tests cho PermissionsApiController - /api/admin/permissions/*
/// Yêu cầu role Admin
/// </summary>
public class PermissionsApiTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _anonymousClient;

    public PermissionsApiTests(TestWebApplicationFactory factory)
    {
        _anonymousClient = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task GetRoles_WithoutAuth_ShouldReturn401Or302()
    {
        var response = await _anonymousClient.GetAsync("/api/admin/permissions/roles");
        
        Assert.True(
            response.StatusCode is HttpStatusCode.Unauthorized or
            HttpStatusCode.Redirect or HttpStatusCode.Found,
            $"Expected 401/302 but got {response.StatusCode}");
    }

    [Fact]
    public async Task GetRoles_Endpoint_ShouldExist()
    {
        var response = await _anonymousClient.GetAsync("/api/admin/permissions/roles");
        Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetRolePermissions_Endpoint_ShouldExist()
    {
        var response = await _anonymousClient.GetAsync("/api/admin/permissions/roles/Admin");
        Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task SearchUsers_Endpoint_ShouldExist()
    {
        var response = await _anonymousClient.GetAsync("/api/admin/permissions/users/search?q=test");
        Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task AssignUserRole_Endpoint_ShouldExist()
    {
        var userId = "test-user-id";
        var content = new StringContent(
            JsonSerializer.Serialize(new { RoleName = "Seller" }),
            Encoding.UTF8, "application/json");

        var response = await _anonymousClient.PostAsync(
            $"/api/admin/permissions/users/{userId}/roles", content);
        Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);
    }
}
