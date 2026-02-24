using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace JohnHenryFashionWeb.Tests.ApiTests;

/// <summary>
/// Tests cho AddressApiController - GET /api/AddressApi/provinces|districts|wards
/// </summary>
public class AddressApiTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AddressApiTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetProvinces_ShouldReturn200()
    {
        // Arrange & Act
        var response = await _client.GetAsync("/api/AddressApi/provinces");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content).RootElement;
        Assert.Equal(JsonValueKind.Array, json.ValueKind);
    }

    [Fact]
    public async Task GetDistricts_WithValidProvinceId_ShouldReturn200()
    {
        // Arrange & Act
        var response = await _client.GetAsync("/api/AddressApi/districts/1");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content).RootElement;
        Assert.Equal(JsonValueKind.Array, json.ValueKind);
    }

    [Fact]
    public async Task GetWards_WithValidDistrictId_ShouldReturn200()
    {
        // Arrange & Act
        var response = await _client.GetAsync("/api/AddressApi/wards/1");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content).RootElement;
        Assert.Equal(JsonValueKind.Array, json.ValueKind);
    }
}

/// <summary>
/// Tests cho VietnameseAddressController - GET /api/VietnameseAddress/provinces|districts|wards
/// </summary>
public class VietnameseAddressApiTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public VietnameseAddressApiTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetProvinces_ShouldReturn200()
    {
        var response = await _client.GetAsync("/api/VietnameseAddress/provinces");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetDistricts_WithProvinceCode_ShouldReturn200()
    {
        var response = await _client.GetAsync("/api/VietnameseAddress/districts/01");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetWards_WithDistrictCode_ShouldReturn200()
    {
        var response = await _client.GetAsync("/api/VietnameseAddress/wards/001");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
