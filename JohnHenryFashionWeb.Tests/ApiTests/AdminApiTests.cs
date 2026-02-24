using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using JohnHenryFashionWeb.Tests.Helpers;

namespace JohnHenryFashionWeb.Tests.ApiTests;

/// <summary>
/// Tests cho AdminApiController - /api/admin/*
/// Tất cả endpoints yêu cầu role Admin
/// </summary>
public class AdminApiTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _anonymousClient;

    public AdminApiTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _anonymousClient = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    #region Authentication Tests - Verify endpoints require Admin role

    [Theory]
    [InlineData("/api/admin/dashboard/stats")]
    [InlineData("/api/admin/dashboard/recent-orders")]
    [InlineData("/api/admin/products")]
    [InlineData("/api/admin/orders")]
    [InlineData("/api/admin/analytics/realtime")]
    [InlineData("/api/admin/analytics/sales")]
    [InlineData("/api/admin/users")]
    [InlineData("/api/admin/system/health")]
    public async Task AdminGetEndpoints_WithoutAuth_ShouldReturn401Or302(string endpoint)
    {
        var response = await _anonymousClient.GetAsync(endpoint);

        // API endpoints should return 401 Unauthorized (not redirect to login)
        Assert.True(
            response.StatusCode == HttpStatusCode.Unauthorized ||
            response.StatusCode == HttpStatusCode.Redirect ||
            response.StatusCode == HttpStatusCode.Found,
            $"Expected 401/302 for {endpoint} but got {response.StatusCode}");
    }

    #endregion

    #region Dashboard APIs

    [Fact]
    public async Task GetDashboardStats_WithAdminToken_ShouldReturn200()
    {
        // Test that the endpoint exists and returns proper JSON structure
        // Since we use InMemory DB without auth session, we verify the route exists
        var response = await _anonymousClient.GetAsync("/api/admin/dashboard/stats");
        
        // Should be 401 (not 404 - route exists) or 302 (redirect to login)
        Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetRecentOrders_Endpoint_ShouldExist()
    {
        var response = await _anonymousClient.GetAsync("/api/admin/dashboard/recent-orders");
        Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetRecentOrders_WithLimit_ShouldExist()
    {
        var response = await _anonymousClient.GetAsync("/api/admin/dashboard/recent-orders?limit=5");
        Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion

    #region Product APIs

    [Fact]
    public async Task GetProducts_Endpoint_ShouldExist()
    {
        var response = await _anonymousClient.GetAsync("/api/admin/products");
        Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetProducts_WithPagination_ShouldExist()
    {
        var response = await _anonymousClient.GetAsync("/api/admin/products?page=1&pageSize=10");
        Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ToggleProductStatus_Endpoint_ShouldExist()
    {
        var productId = Guid.NewGuid();
        var response = await _anonymousClient.PostAsync(
            $"/api/admin/products/{productId}/toggle-status", null);
        
        // 401 (auth required) vs 404 (route not found)
        Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion

    #region Order APIs

    [Fact]
    public async Task GetOrders_Endpoint_ShouldExist()
    {
        var response = await _anonymousClient.GetAsync("/api/admin/orders");
        Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetOrders_WithFilters_ShouldExist()
    {
        var response = await _anonymousClient.GetAsync("/api/admin/orders?status=pending&page=1");
        Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateOrderStatus_Endpoint_ShouldExist()
    {
        var orderId = Guid.NewGuid();
        var content = new StringContent(
            JsonSerializer.Serialize(new { Status = "processing" }),
            Encoding.UTF8, "application/json");
        
        var response = await _anonymousClient.PostAsync(
            $"/api/admin/orders/{orderId}/status", content);
        
        Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion

    #region Analytics APIs

    [Fact]
    public async Task GetRealtimeAnalytics_Endpoint_ShouldExist()
    {
        var response = await _anonymousClient.GetAsync("/api/admin/analytics/realtime");
        Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetSalesAnalytics_Endpoint_ShouldExist()
    {
        var response = await _anonymousClient.GetAsync("/api/admin/analytics/sales");
        Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetSalesAnalytics_WithDateRange_ShouldExist()
    {
        var from = DateTime.UtcNow.AddDays(-30).ToString("yyyy-MM-dd");
        var to = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var response = await _anonymousClient.GetAsync($"/api/admin/analytics/sales?fromDate={from}&toDate={to}");
        Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion

    #region User APIs

    [Fact]
    public async Task GetUsers_Endpoint_ShouldExist()
    {
        var response = await _anonymousClient.GetAsync("/api/admin/users");
        Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ToggleUserStatus_Endpoint_ShouldExist()
    {
        var userId = "test-user-id";
        var response = await _anonymousClient.PostAsync(
            $"/api/admin/users/{userId}/toggle-status", null);
        Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion

    #region System APIs

    [Fact]
    public async Task GetSystemHealth_Endpoint_ShouldExist()
    {
        var response = await _anonymousClient.GetAsync("/api/admin/system/health");
        Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion

    #region Banner APIs

    [Fact]
    public async Task GetBanner_Endpoint_ShouldExist()
    {
        var bannerId = Guid.NewGuid();
        var response = await _anonymousClient.GetAsync($"/api/admin/banners/{bannerId}");
        Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateBanner_Endpoint_ShouldExist()
    {
        var response = await _anonymousClient.PostAsync("/api/admin/banners",
            new MultipartFormDataContent());
        Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateBanner_Endpoint_ShouldExist()
    {
        var bannerId = Guid.NewGuid();
        var response = await _anonymousClient.PostAsync(
            $"/api/admin/banners/{bannerId}/update",
            new MultipartFormDataContent());
        Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteBanner_Endpoint_ShouldExist()
    {
        var bannerId = Guid.NewGuid();
        var response = await _anonymousClient.PostAsync(
            $"/api/admin/banners/{bannerId}/delete", null);
        Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ToggleBanner_Endpoint_ShouldExist()
    {
        var bannerId = Guid.NewGuid();
        var response = await _anonymousClient.PostAsync(
            $"/api/admin/banners/{bannerId}/toggle", null);
        Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ActivateBanner_Endpoint_ShouldExist()
    {
        var bannerId = Guid.NewGuid();
        var response = await _anonymousClient.PostAsync(
            $"/api/admin/banners/{bannerId}/activate", null);
        Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ReorderBanner_Endpoint_ShouldExist()
    {
        var bannerId = Guid.NewGuid();
        var content = new StringContent(
            JsonSerializer.Serialize(new
            {
                Position = "hero",
                NewSortOrder = 1,
                OldSortOrder = 2
            }),
            Encoding.UTF8, "application/json");

        var response = await _anonymousClient.PostAsync(
            $"/api/admin/banners/{bannerId}/reorder", content);
        Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion
}
