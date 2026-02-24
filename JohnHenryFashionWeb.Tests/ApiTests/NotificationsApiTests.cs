using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace JohnHenryFashionWeb.Tests.ApiTests;

/// <summary>
/// Tests cho NotificationsController - /api/Notifications/*
/// Tất cả endpoints yêu cầu đăng nhập
/// </summary>
public class NotificationsApiTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _anonymousClient;

    public NotificationsApiTests(TestWebApplicationFactory factory)
    {
        _anonymousClient = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task GetNotifications_WithoutAuth_ShouldReturn401()
    {
        var response = await _anonymousClient.GetAsync("/api/Notifications");
        
        Assert.True(
            response.StatusCode == HttpStatusCode.Unauthorized ||
            response.StatusCode == HttpStatusCode.Redirect ||
            response.StatusCode == HttpStatusCode.Found,
            $"Expected 401/302 but got {response.StatusCode}");
    }

    [Fact]
    public async Task GetNotifications_Endpoint_ShouldExist()
    {
        var response = await _anonymousClient.GetAsync("/api/Notifications");
        Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetUnreadCount_Endpoint_ShouldExist()
    {
        var response = await _anonymousClient.GetAsync("/api/Notifications/unread-count");
        Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetNotifications_UnreadOnly_Endpoint_ShouldExist()
    {
        var response = await _anonymousClient.GetAsync("/api/Notifications?unreadOnly=true");
        Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task MarkNotificationRead_Endpoint_ShouldExist()
    {
        var notificationId = Guid.NewGuid();
        var response = await _anonymousClient.PostAsync(
            $"/api/Notifications/{notificationId}/mark-read", null);
        Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task MarkAllRead_Endpoint_ShouldExist()
    {
        var response = await _anonymousClient.PostAsync("/api/Notifications/mark-all-read", null);
        Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteNotification_Endpoint_ShouldExist()
    {
        var notificationId = Guid.NewGuid();
        var response = await _anonymousClient.DeleteAsync($"/api/Notifications/{notificationId}");
        Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);
    }
}
