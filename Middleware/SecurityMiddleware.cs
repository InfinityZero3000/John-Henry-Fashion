using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Caching.Memory;
using System.Net;

namespace JohnHenryFashionWeb.Middleware;

// Rate Limiting Middleware
public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IMemoryCache _cache;
    private readonly ILogger<RateLimitingMiddleware> _logger;
    private readonly int _maxRequestsPerMinute = 300;  // Tăng lên 300 requests/min

    public RateLimitingMiddleware(RequestDelegate next, IMemoryCache cache, ILogger<RateLimitingMiddleware> logger)
    {
        _next = next;
        _cache = cache;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip rate limiting for health check endpoints
        if (context.Request.Path.StartsWithSegments("/health") || 
            context.Request.Path.StartsWithSegments("/api/health"))
        {
            await _next(context);
            return;
        }

        var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var cacheKey = $"rate_limit_{clientIp}";
        
        List<DateTime> requests;
        if (_cache.TryGetValue(cacheKey, out List<DateTime>? cachedRequests) && cachedRequests != null)
        {
            requests = cachedRequests;
            // Remove requests older than 1 minute
            requests.RemoveAll(r => r < DateTime.UtcNow.AddMinutes(-1));
            
            if (requests.Count >= _maxRequestsPerMinute)
            {
                _logger.LogWarning("Rate limit exceeded for IP: {ClientIp}, Path: {Path}", 
                    clientIp, context.Request.Path);
                context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync("{\"error\":\"Rate limit exceeded. Please try again in a minute.\"}");
                return;
            }
            
            requests.Add(DateTime.UtcNow);
        }
        else
        {
            requests = new List<DateTime> { DateTime.UtcNow };
        }

        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1),
            SlidingExpiration = TimeSpan.FromSeconds(30)
        };

        _cache.Set(cacheKey, requests, cacheOptions);
        await _next(context);
    }
}

// CSRF Protection Attribute
public class ValidateCSRFTokenAttribute : Attribute, IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var request = context.HttpContext.Request;
        
        // Skip validation for GET requests
        if (request.Method == HttpMethods.Get)
            return;

        // Check for CSRF token in headers or form data
        var token = request.Headers["X-CSRF-TOKEN"].FirstOrDefault() ??
                   request.Form["__RequestVerificationToken"].FirstOrDefault();

        if (string.IsNullOrEmpty(token))
        {
            context.Result = new BadRequestObjectResult("CSRF token is required");
            return;
        }

        // Additional CSRF validation logic can be added here
    }
}

// Login Attempt Tracking Middleware
public class LoginAttemptTrackingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IMemoryCache _cache;
    private readonly ILogger<LoginAttemptTrackingMiddleware> _logger;

    public LoginAttemptTrackingMiddleware(RequestDelegate next, IMemoryCache cache, ILogger<LoginAttemptTrackingMiddleware> logger)
    {
        _next = next;
        _cache = cache;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Track login attempts for /Account/Login POST requests
        if (context.Request.Path.StartsWithSegments("/Account/Login") && 
            context.Request.Method == HttpMethods.Post)
        {
            var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var cacheKey = $"login_attempts_{clientIp}";
            
            // Get existing attempts
            var attempts = _cache.Get<List<DateTime>>(cacheKey) ?? new List<DateTime>();
            
            // Remove attempts older than 15 minutes
            attempts.RemoveAll(a => a < DateTime.UtcNow.AddMinutes(-15));
            
            // Check if too many attempts
            if (attempts.Count >= 10)
            {
                _logger.LogWarning("Too many login attempts from IP: {ClientIp}", clientIp);
                context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
                await context.Response.WriteAsync("Too many login attempts. Please try again later.");
                return;
            }
            
            // Record this attempt
            attempts.Add(DateTime.UtcNow);
            
            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15)
            };
            
            _cache.Set(cacheKey, attempts, cacheOptions);
        }

        await _next(context);
    }
}

// IP Whitelist/Blacklist Middleware
public class IPFilterMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConfiguration _configuration;
    private readonly ILogger<IPFilterMiddleware> _logger;

    public IPFilterMiddleware(RequestDelegate next, IConfiguration configuration, ILogger<IPFilterMiddleware> logger)
    {
        _next = next;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var clientIp = context.Connection.RemoteIpAddress?.ToString();
        
        if (string.IsNullOrEmpty(clientIp))
        {
            await _next(context);
            return;
        }

        // Check blacklisted IPs
        var blacklistedIPs = _configuration.GetSection("Security:BlacklistedIPs").Get<string[]>() ?? Array.Empty<string>();
        
        if (blacklistedIPs.Contains(clientIp))
        {
            _logger.LogWarning("Blocked request from blacklisted IP: {ClientIp}", clientIp);
            context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
            await context.Response.WriteAsync("Access denied");
            return;
        }

        // Check if admin endpoints require whitelisted IPs
        if (context.Request.Path.StartsWithSegments("/Admin"))
        {
            var whitelistedIPs = _configuration.GetSection("Security:AdminWhitelistedIPs").Get<string[]>() ?? Array.Empty<string>();
            
            if (whitelistedIPs.Length > 0 && !whitelistedIPs.Contains(clientIp))
            {
                _logger.LogWarning("Blocked admin access from non-whitelisted IP: {ClientIp}", clientIp);
                context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                await context.Response.WriteAsync("Admin access denied");
                return;
            }
        }

        await _next(context);
    }
}

// Session Security Middleware
public class SessionSecurityMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SessionSecurityMiddleware> _logger;

    public SessionSecurityMiddleware(RequestDelegate next, ILogger<SessionSecurityMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Check for session hijacking attempts
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var userAgent = context.Request.Headers.UserAgent.ToString();
            var storedUserAgent = context.Session.GetString("UserAgent");
            
            if (!string.IsNullOrEmpty(storedUserAgent) && storedUserAgent != userAgent)
            {
                _logger.LogWarning("Potential session hijacking detected for user: {UserId}", context.User.Identity.Name);
                
                // Clear session and force re-authentication
                context.Session.Clear();
                context.Response.Redirect("/Account/Login");
                return;
            }
            
            // Store user agent on first request
            if (string.IsNullOrEmpty(storedUserAgent))
            {
                context.Session.SetString("UserAgent", userAgent);
            }
            
            // Update last activity timestamp
            context.Session.SetString("LastActivity", DateTime.UtcNow.ToString());
        }

        await _next(context);
    }
}

// Middleware Extensions
public static class SecurityMiddlewareExtensions
{
    public static IApplicationBuilder UseRateLimiting(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<RateLimitingMiddleware>();
    }

    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<SecurityHeadersMiddleware>();
    }

    public static IApplicationBuilder UseLoginAttemptTracking(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<LoginAttemptTrackingMiddleware>();
    }

    public static IApplicationBuilder UseIPFilter(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<IPFilterMiddleware>();
    }

    public static IApplicationBuilder UseSessionSecurity(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<SessionSecurityMiddleware>();
    }
}
