using Microsoft.Extensions.Primitives;
using JohnHenryFashionWeb.Services;
using System.Security.Claims;

namespace JohnHenryFashionWeb.Middleware
{
    public class PerformanceMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<PerformanceMiddleware> _logger;

        public PerformanceMiddleware(RequestDelegate next, ILogger<PerformanceMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            // Track memory at start
            var memoryBefore = GC.GetTotalMemory(false);
            
            // Add performance headers
            context.Response.OnStarting(() =>
            {
                stopwatch.Stop();
                var responseTime = stopwatch.ElapsedMilliseconds;
                var memoryAfter = GC.GetTotalMemory(false);
                var memoryUsed = memoryAfter - memoryBefore;
                
                context.Response.Headers["X-Response-Time"] = responseTime.ToString() + "ms";
                context.Response.Headers["X-Memory-Used"] = (memoryUsed / 1024).ToString() + "KB";
                context.Response.Headers["X-Powered-By"] = "John Henry Fashion";
                
                // Log performance metrics
                if (responseTime > 1000)
                {
                    _logger.LogWarning("Slow request: {Method} {Path} took {ResponseTime}ms, Memory: {MemoryUsed}KB", 
                        context.Request.Method, context.Request.Path, responseTime, memoryUsed / 1024);
                }
                
                return Task.CompletedTask;
            });

            try
            {
                await _next(context);
                
                stopwatch.Stop();
                
                // Log to performance monitor service if available
                var performanceService = context.RequestServices.GetService<IPerformanceMonitorService>();
                if (performanceService != null)
                {
                    var userId = context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
                    await performanceService.LogPageLoadAsync(
                        context.Request.Path, 
                        stopwatch.Elapsed, 
                        userId);
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Request failed: {Method} {Path} after {ResponseTime}ms", 
                    context.Request.Method, context.Request.Path, stopwatch.ElapsedMilliseconds);
                throw;
            }
        }
    }

    public class SecurityHeadersMiddleware
    {
        private readonly RequestDelegate _next;

        public SecurityHeadersMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Add security headers
            context.Response.Headers["X-Content-Type-Options"] = "nosniff";
            context.Response.Headers["X-Frame-Options"] = "SAMEORIGIN";
            context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
            context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            context.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
            
            // Add Content Security Policy for enhanced security
            var csp = "default-src 'self'; " +
                     "script-src 'self' 'unsafe-inline' 'unsafe-eval' https://cdnjs.cloudflare.com https://unpkg.com; " +
                     "style-src 'self' 'unsafe-inline' https://cdnjs.cloudflare.com https://fonts.googleapis.com; " +
                     "img-src 'self' data: https:; " +
                     "font-src 'self' https://cdnjs.cloudflare.com https://fonts.gstatic.com; " +
                     "connect-src 'self' https://unpkg.com https://lottiefiles.com;";
            
            context.Response.Headers["Content-Security-Policy"] = csp;

            await _next(context);
        }
    }

    public class ImageOptimizationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ImageOptimizationMiddleware> _logger;

        public ImageOptimizationMiddleware(RequestDelegate next, ILogger<ImageOptimizationMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Check if request is for an image
            if (context.Request.Path.HasValue && IsImageRequest(context.Request.Path.Value))
            {
                // Add cache headers for images
                context.Response.Headers["Cache-Control"] = "public, max-age=31536000"; // 1 year
                context.Response.Headers["Expires"] = DateTime.UtcNow.AddYears(1).ToString("R");
                
                // Support WebP if client supports it
                var acceptHeader = context.Request.Headers["Accept"].ToString();
                if (acceptHeader.Contains("image/webp"))
                {
                    // Try to serve WebP version if available
                    var webpPath = context.Request.Path.Value.Replace(Path.GetExtension(context.Request.Path.Value), ".webp");
                    var webpFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", webpPath.TrimStart('/'));
                    
                    if (File.Exists(webpFilePath))
                    {
                        context.Request.Path = webpPath;
                        context.Response.Headers["Vary"] = "Accept";
                    }
                }
            }

            await _next(context);
        }

        private static bool IsImageRequest(string path)
        {
            var extensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".svg" };
            return extensions.Any(ext => path.EndsWith(ext, StringComparison.OrdinalIgnoreCase));
        }
    }
}
