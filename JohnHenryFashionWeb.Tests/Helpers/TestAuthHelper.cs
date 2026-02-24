using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using JohnHenryFashionWeb.Models;
using JohnHenryFashionWeb.Data;

namespace JohnHenryFashionWeb.Tests.Helpers;

public static class TestAuthHelper
{
    /// <summary>
    /// Tạo cookie authentication bằng cách đăng nhập qua API
    /// </summary>
    public static async Task<HttpClient> CreateAuthenticatedClient(
        TestWebApplicationFactory factory,
        string role = "Admin")
    {
        // Tạo user test trong DB
        using var scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        // Seed test user
        var email = role == "Admin" ? "testadmin@test.com" : "testuser@test.com";
        var password = "Test@123456";

        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole(role));

        var user = await userManager.FindByEmailAsync(email);
        if (user == null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FirstName = "Test",
                LastName = role,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            await userManager.CreateAsync(user, password);
        }

        if (!await userManager.IsInRoleAsync(user, role))
            await userManager.AddToRoleAsync(user, role);

        // Tạo client và đăng nhập
        var client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        return client;
    }

    public static JsonElement ParseJson(string json)
    {
        return JsonDocument.Parse(json).RootElement;
    }

    public static StringContent JsonContent(object obj)
    {
        return new StringContent(JsonSerializer.Serialize(obj), Encoding.UTF8, "application/json");
    }
}
