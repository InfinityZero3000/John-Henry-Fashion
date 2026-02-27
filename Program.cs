using Microsoft.Net.Http.Headers;
using Serilog;
using JohnHenryFashionWeb.Extensions;
using JohnHenryFashionWeb.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Load environment variable overrides
builder.Configuration.LoadEnvironmentOverrides();

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", Serilog.Events.LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", Serilog.Events.LogEventLevel.Warning)
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/john-henry-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Determine environment
var isTestingEnvironment = builder.Environment.EnvironmentName == "Testing";
var cookieSecurePolicy = builder.Environment.IsDevelopment()
    ? CookieSecurePolicy.SameAsRequest
    : CookieSecurePolicy.Always;

// Register services
builder.Services.AddDatabase(builder.Configuration, isTestingEnvironment);
builder.Services.AddIdentityConfiguration(cookieSecurePolicy);
builder.Services.AddAuthenticationConfiguration(builder.Configuration, cookieSecurePolicy);
builder.Services.AddApplicationServices();
builder.Services.AddCachingConfiguration(builder.Configuration, isTestingEnvironment);
builder.Services.AddCompressionConfiguration();
builder.Services.AddHealthChecksConfiguration(builder.Configuration, isTestingEnvironment);

// Configure Email Settings
builder.Services.Configure<JohnHenryFashionWeb.Services.EmailSettings>(
    builder.Configuration.GetSection("EmailSettings"));

// Application Insights
builder.Services.AddApplicationInsightsTelemetry();

// Configure routing
builder.Services.Configure<RouteOptions>(options =>
{
    options.LowercaseUrls = true;
    options.LowercaseQueryStrings = false;
});

// Add Controllers with Views and API support
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new Microsoft.AspNetCore.Mvc.ResponseCacheAttribute
    {
        VaryByHeader = "User-Agent",
        Duration = 300
    });
})
    .AddNewtonsoftJson();

// Antiforgery
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
});

// HttpClient
builder.Services.AddHttpClient();
builder.Services.AddHttpClient<JohnHenryFashionWeb.Services.IVietnameseAddressService, JohnHenryFashionWeb.Services.VietnameseAddressService>();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "John Henry Fashion API",
        Version = "v1",
        Description = "API for John Henry Fashion E-commerce Platform"
    });
    c.DocInclusionPredicate((docName, apiDesc) =>
    {
        var controllerName = apiDesc.ActionDescriptor.RouteValues["controller"];
        return controllerName?.StartsWith("Api") == true ||
               apiDesc.ActionDescriptor.EndpointMetadata.Any(m => m.GetType().Name == "ApiControllerAttribute");
    });
});

var app = builder.Build();

// Seed data
await app.SeedAdminSystemDataAsync();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();

    // Add security headers for production
    app.Use((context, next) =>
    {
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        context.Response.Headers["X-Frame-Options"] = "DENY";
        context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
        context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        context.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
        return next();
    });
}

// Middleware pipeline
app.UseResponseCompression();
app.UseMiddleware<JohnHenryFashionWeb.Middleware.PerformanceMiddleware>();
app.UseRateLimiting();
app.UseLoginAttemptTracking();
app.UseIPFilter();
app.UseSessionSecurity();
app.UseSecurityHeaders();
app.UseResponseCaching();
app.UseHttpsRedirection();

// Static files with caching
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        const int durationInSeconds = 60 * 60 * 24 * 365;
        ctx.Context.Response.Headers[HeaderNames.CacheControl] =
            "public,max-age=" + durationInSeconds;
    }
});

app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();
app.MapStaticAssets();

// Routes
app.MapControllerRoute(
    name: "blogCategory",
    pattern: "blog/category/{slug}",
    defaults: new { controller = "Blog", action = "Category" });

app.MapControllerRoute(
    name: "blogPost",
    pattern: "blog/{slug}",
    defaults: new { controller = "Blog", action = "Details" });

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapControllerRoute(
    name: "api",
    pattern: "api/{controller}/{action=Index}/{id?}");

// Health Check
app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var result = System.Text.Json.JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                duration = e.Value.Duration.ToString()
            }),
            totalDuration = report.TotalDuration.ToString()
        });
        await context.Response.WriteAsync(result);
    }
});

// Database seeding
await app.SeedDatabaseAsync(builder.Configuration);

// Check for import-products command
if (args.Contains("--import-products"))
{
    Console.WriteLine("\n╔════════════════════════════════════════════════╗");
    Console.WriteLine("║  JOHN HENRY - IMPORT PRODUCTS FROM CSV        ║");
    Console.WriteLine("╚════════════════════════════════════════════════╝\n");

    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<JohnHenryFashionWeb.Data.ApplicationDbContext>();
        var csvPath = File.Exists("database/seed/all_products_export.csv")
            ? "database/seed/all_products_export.csv"
            : "database/seed/johnhenry_products.csv";

        await JohnHenryFashionWeb.Scripts.ImportProductsFromCsv.RunAsync(context, csvPath);
    }

    Console.WriteLine("\n✅ Import process completed! Press any key to exit...");
    Console.ReadKey();
    return;
}

try
{
    Log.Information("Starting John Henry Fashion Web Application");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
// Required for WebApplicationFactory in integration tests
public partial class Program { }
