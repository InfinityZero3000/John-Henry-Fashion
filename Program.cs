using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using JohnHenryFashionWeb.Data;
using JohnHenryFashionWeb.Models;
using JohnHenryFashionWeb.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Serilog;
using Microsoft.AspNetCore.ResponseCompression;
using System.IO.Compression;
using Microsoft.Net.Http.Headers;

var builder = WebApplication.CreateBuilder(args);

// Load environment variables from .env file
DotNetEnv.Env.Load();

// Override configuration with environment variables
var configuration = builder.Configuration;

// Database Configuration - Override from environment variables if they exist
var dbHost = Environment.GetEnvironmentVariable("DB_HOST");
var dbPort = Environment.GetEnvironmentVariable("DB_PORT");
var dbName = Environment.GetEnvironmentVariable("DB_NAME");
var dbUser = Environment.GetEnvironmentVariable("DB_USER");
var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD");
if (!string.IsNullOrWhiteSpace(dbHost) && !string.IsNullOrWhiteSpace(dbName))
{
    configuration["ConnectionStrings:DefaultConnection"] = $"Host={dbHost};Port={dbPort};Database={dbName};Username={dbUser};Password={dbPassword};SSL Mode=Prefer;Trust Server Certificate=true";
}

// Payment Gateways - Only override if environment variable exists
if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("VNPAY_TMN_CODE")))
    configuration["PaymentGateways:VNPay:TmnCode"] = Environment.GetEnvironmentVariable("VNPAY_TMN_CODE");
if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("VNPAY_HASH_SECRET")))
    configuration["PaymentGateways:VNPay:HashSecret"] = Environment.GetEnvironmentVariable("VNPAY_HASH_SECRET");
if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("VNPAY_PAYMENT_URL")))
    configuration["PaymentGateways:VNPay:PaymentUrl"] = Environment.GetEnvironmentVariable("VNPAY_PAYMENT_URL");
if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("VNPAY_API_URL")))
    configuration["PaymentGateways:VNPay:ApiUrl"] = Environment.GetEnvironmentVariable("VNPAY_API_URL");
if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("VNPAY_ENABLED")))
    configuration["PaymentGateways:VNPay:IsEnabled"] = Environment.GetEnvironmentVariable("VNPAY_ENABLED");
if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("VNPAY_SANDBOX")))
    configuration["PaymentGateways:VNPay:IsSandbox"] = Environment.GetEnvironmentVariable("VNPAY_SANDBOX");

if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("MOMO_PARTNER_CODE")))
    configuration["PaymentGateways:MoMo:PartnerCode"] = Environment.GetEnvironmentVariable("MOMO_PARTNER_CODE");
if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("MOMO_ACCESS_KEY")))
    configuration["PaymentGateways:MoMo:AccessKey"] = Environment.GetEnvironmentVariable("MOMO_ACCESS_KEY");
if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("MOMO_SECRET_KEY")))
    configuration["PaymentGateways:MoMo:SecretKey"] = Environment.GetEnvironmentVariable("MOMO_SECRET_KEY");
if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("MOMO_API_URL")))
    configuration["PaymentGateways:MoMo:ApiUrl"] = Environment.GetEnvironmentVariable("MOMO_API_URL");
if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("MOMO_PUBLIC_KEY")))
    configuration["PaymentGateways:MoMo:PublicKey"] = Environment.GetEnvironmentVariable("MOMO_PUBLIC_KEY");
if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("MOMO_PRIVATE_KEY")))
    configuration["PaymentGateways:MoMo:PrivateKey"] = Environment.GetEnvironmentVariable("MOMO_PRIVATE_KEY");
if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("MOMO_ENABLED")))
    configuration["PaymentGateways:MoMo:IsEnabled"] = Environment.GetEnvironmentVariable("MOMO_ENABLED");
if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("MOMO_SANDBOX")))
    configuration["PaymentGateways:MoMo:IsSandbox"] = Environment.GetEnvironmentVariable("MOMO_SANDBOX");

// Stripe Configuration
if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("STRIPE_PUBLISHABLE_KEY")))
    configuration["PaymentGateways:Stripe:PublishableKey"] = Environment.GetEnvironmentVariable("STRIPE_PUBLISHABLE_KEY");
if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("STRIPE_SECRET_KEY")))
    configuration["PaymentGateways:Stripe:SecretKey"] = Environment.GetEnvironmentVariable("STRIPE_SECRET_KEY");
if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("STRIPE_WEBHOOK_SECRET")))
    configuration["PaymentGateways:Stripe:WebhookSecret"] = Environment.GetEnvironmentVariable("STRIPE_WEBHOOK_SECRET");
if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("STRIPE_API_URL")))
    configuration["PaymentGateways:Stripe:ApiUrl"] = Environment.GetEnvironmentVariable("STRIPE_API_URL");
if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("STRIPE_CURRENCY")))
    configuration["PaymentGateways:Stripe:Currency"] = Environment.GetEnvironmentVariable("STRIPE_CURRENCY");
if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("STRIPE_ENABLED")))
    configuration["PaymentGateways:Stripe:IsEnabled"] = Environment.GetEnvironmentVariable("STRIPE_ENABLED");
if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("STRIPE_SANDBOX")))
    configuration["PaymentGateways:Stripe:IsSandbox"] = Environment.GetEnvironmentVariable("STRIPE_SANDBOX");

// Google OAuth Configuration
if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID")))
    configuration["Authentication:Google:ClientId"] = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID");
if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("GOOGLE_CLIENT_SECRET")))
    configuration["Authentication:Google:ClientSecret"] = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_SECRET");

// Email Configuration
if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("EMAIL_HOST")))
    configuration["EmailSettings:SmtpServer"] = Environment.GetEnvironmentVariable("EMAIL_HOST");
if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("EMAIL_PORT")))
    configuration["EmailSettings:SmtpPort"] = Environment.GetEnvironmentVariable("EMAIL_PORT");
if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("EMAIL_USE_SSL")))
    configuration["EmailSettings:UseSsl"] = Environment.GetEnvironmentVariable("EMAIL_USE_SSL");
if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("EMAIL_USER")))
    configuration["EmailSettings:Username"] = Environment.GetEnvironmentVariable("EMAIL_USER");
if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("EMAIL_PASSWORD")))
    configuration["EmailSettings:Password"] = Environment.GetEnvironmentVariable("EMAIL_PASSWORD");
if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("EMAIL_FROM")))
    configuration["EmailSettings:FromEmail"] = Environment.GetEnvironmentVariable("EMAIL_FROM");
if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("EMAIL_FROM_NAME")))
    configuration["EmailSettings:FromName"] = Environment.GetEnvironmentVariable("EMAIL_FROM_NAME");
if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("EMAIL_ADMIN")))
    configuration["EmailSettings:AdminEmail"] = Environment.GetEnvironmentVariable("EMAIL_ADMIN");

// All other settings will use values from appsettings.json
// Environment variables can still override them but won't set null values

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    // Reduce EF Core SQL noise ("Executed DbCommand" at Information)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", Serilog.Events.LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", Serilog.Events.LogEventLevel.Warning)
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/john-henry-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add Entity Framework and PostgreSQL
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register BCrypt password hasher (replaces default PBKDF2)
builder.Services.AddScoped<IPasswordHasher<ApplicationUser>, JohnHenryFashionWeb.Services.BcryptPasswordHasher<ApplicationUser>>();

// Add Identity with enhanced security settings
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Password settings - Enhanced security
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 8;
    options.Password.RequiredUniqueChars = 3;

    // Lockout settings - Enhanced protection
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 3;
    options.Lockout.AllowedForNewUsers = true;

    // User settings - Security focused
    options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
    options.User.RequireUniqueEmail = true;

    // Sign-in settings - Enhanced security
    options.SignIn.RequireConfirmedEmail = bool.Parse(Environment.GetEnvironmentVariable("REQUIRE_EMAIL_CONFIRMATION") ?? "false");
    options.SignIn.RequireConfirmedAccount = bool.Parse(Environment.GetEnvironmentVariable("REQUIRE_EMAIL_CONFIRMATION") ?? "false");
    options.SignIn.RequireConfirmedPhoneNumber = false;

    // Token settings
    options.Tokens.EmailConfirmationTokenProvider = TokenOptions.DefaultEmailProvider;
    options.Tokens.PasswordResetTokenProvider = TokenOptions.DefaultEmailProvider;
    options.Tokens.ChangeEmailTokenProvider = TokenOptions.DefaultEmailProvider;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders()
.AddTokenProvider<DataProtectorTokenProvider<ApplicationUser>>("Custom");

// Cookie settings - Đơn giản cho development
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.None; // Cho phép HTTP trong development
    options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax;
    options.Cookie.IsEssential = true;
    // Set AccessDeniedPath to redirect to our custom AccessDenied page
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.ExpireTimeSpan = TimeSpan.FromDays(30);
    options.SlidingExpiration = true;
});

// External Cookie cho Google OAuth
builder.Services.ConfigureExternalCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.None;
    options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax;
    options.Cookie.IsEssential = true;
});

// Add Authentication with Identity as default and JWT for API
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = IdentityConstants.ApplicationScheme;
    options.DefaultSignInScheme = IdentityConstants.ApplicationScheme;
    options.DefaultChallengeScheme = IdentityConstants.ApplicationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["JWT:Issuer"],
        ValidAudience = builder.Configuration["JWT:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JWT:SecretKey"]!))
    };
})
.AddGoogle(googleOptions =>
{
    // Cấu hình cơ bản
    googleOptions.ClientId = builder.Configuration["Authentication:Google:ClientId"]!;
    googleOptions.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]!;
    googleOptions.CallbackPath = "/signin-google";
    
    // QUAN TRỌNG: Chỉ định SignInScheme để Google sử dụng External cookie thay vì Application cookie
    googleOptions.SignInScheme = IdentityConstants.ExternalScheme;
    
    // Scope cần thiết
    googleOptions.Scope.Add("email");
    googleOptions.Scope.Add("profile");
    googleOptions.SaveTokens = true;
    
    // Cookie settings đơn giản
    googleOptions.CorrelationCookie.SecurePolicy = CookieSecurePolicy.None;
    googleOptions.CorrelationCookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax;
    googleOptions.CorrelationCookie.HttpOnly = true;
    googleOptions.CorrelationCookie.IsEssential = true;
});

// Add Authorization policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AdminPolicies.RequireAdminRole,
        policy => policy.RequireRole(UserRoles.Admin));
    
    options.AddPolicy(AdminPolicies.RequireSellerRole,
        policy => policy.RequireRole(UserRoles.Seller));
    
    options.AddPolicy(AdminPolicies.RequireAdminOrSellerRole,
        policy => policy.RequireRole(UserRoles.Admin, UserRoles.Seller));
});

// AutoMapper will be configured later

// Register custom services
builder.Services.AddScoped<JohnHenryFashionWeb.Services.ICacheService, JohnHenryFashionWeb.Services.CacheService>();
builder.Services.AddScoped<JohnHenryFashionWeb.Services.IImageOptimizationService, JohnHenryFashionWeb.Services.ImageOptimizationService>();
builder.Services.AddScoped<JohnHenryFashionWeb.Services.ISeoService, JohnHenryFashionWeb.Services.SeoService>();
builder.Services.AddScoped<JohnHenryFashionWeb.Services.SeoService>();
builder.Services.AddScoped<JohnHenryFashionWeb.Services.IPerformanceMonitorService, JohnHenryFashionWeb.Services.PerformanceMonitorService>();
builder.Services.AddScoped<JohnHenryFashionWeb.Services.IOptimizedDataService, JohnHenryFashionWeb.Services.OptimizedDataService>();
builder.Services.AddScoped<JohnHenryFashionWeb.Services.IEmailService, JohnHenryFashionWeb.Services.EmailService>();
builder.Services.AddScoped<JohnHenryFashionWeb.Services.INotificationService, JohnHenryFashionWeb.Services.NotificationService>();
builder.Services.AddScoped<JohnHenryFashionWeb.Services.ISecurityService, JohnHenryFashionWeb.Services.SecurityService>();
builder.Services.AddScoped<JohnHenryFashionWeb.Services.ISystemConfigService, JohnHenryFashionWeb.Services.SystemConfigService>();
builder.Services.AddScoped<JohnHenryFashionWeb.Services.IAnalyticsService, JohnHenryFashionWeb.Services.AnalyticsService>();
builder.Services.AddScoped<JohnHenryFashionWeb.Services.IReportingService, JohnHenryFashionWeb.Services.ReportingService>();
builder.Services.AddScoped<JohnHenryFashionWeb.Services.IAuthService, JohnHenryFashionWeb.Services.AuthService>();
builder.Services.AddScoped<JohnHenryFashionWeb.Services.IPaymentService, JohnHenryFashionWeb.Services.PaymentService>();
builder.Services.AddScoped<JohnHenryFashionWeb.Services.IUserManagementService, JohnHenryFashionWeb.Services.UserManagementService>();
builder.Services.AddScoped<JohnHenryFashionWeb.Services.IPermissionService, JohnHenryFashionWeb.Services.PermissionService>();
builder.Services.AddScoped<JohnHenryFashionWeb.Services.PermissionSeedService>();
builder.Services.AddScoped<JohnHenryFashionWeb.Services.IAuditLogService, JohnHenryFashionWeb.Services.AuditLogService>();
builder.Services.AddScoped<JohnHenryFashionWeb.Services.ILogService, JohnHenryFashionWeb.Services.LogService>();
builder.Services.AddScoped<JohnHenryFashionWeb.Services.SeedDataService>();
builder.Services.AddScoped<JohnHenryFashionWeb.Services.IVietnameseAddressService, JohnHenryFashionWeb.Services.VietnameseAddressService>();
builder.Services.AddScoped<JohnHenryFashionWeb.Services.IContentModerationService, JohnHenryFashionWeb.Services.ContentModerationService>();

// Add Background Services
builder.Services.AddHostedService<JohnHenryFashionWeb.Services.NotificationCleanupService>();

// Add Payment Validator Helper
builder.Services.AddScoped<JohnHenryFashionWeb.Helpers.PaymentValidator>();

// Add Application Insights
builder.Services.AddApplicationInsightsTelemetry();

// Add Memory Caching and Distributed Caching
builder.Services.AddMemoryCache();
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "JohnHenryFashion";
});

// Add Session support
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax;
});

// Configure Email Settings
builder.Services.Configure<JohnHenryFashionWeb.Services.EmailSettings>(
    builder.Configuration.GetSection("EmailSettings"));

// Add Response Caching
builder.Services.AddResponseCaching();

// Add Response Compression
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[]
    {
        "image/svg+xml",
        "application/json",
        "text/json"
    });
});

builder.Services.Configure<BrotliCompressionProviderOptions>(options =>
{
    options.Level = CompressionLevel.Fastest;
});

builder.Services.Configure<GzipCompressionProviderOptions>(options =>
{
    options.Level = CompressionLevel.SmallestSize;
});

// Configure routing options to be case-insensitive
builder.Services.Configure<RouteOptions>(options =>
{
    options.LowercaseUrls = true;
    options.LowercaseQueryStrings = false; // Keep query strings as-is
});

// Add Controllers with Views and API support
builder.Services.AddControllersWithViews(options =>
{
    // Add response caching filter globally for performance
    options.Filters.Add(new Microsoft.AspNetCore.Mvc.ResponseCacheAttribute
    {
        VaryByHeader = "User-Agent",
        Duration = 300 // 5 minutes default cache
    });
})
    .AddNewtonsoftJson();

// Configure Antiforgery to accept token from header
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
});

// Add HttpClient for Payment Service
builder.Services.AddHttpClient();
builder.Services.AddHttpClient<JohnHenryFashionWeb.Services.IVietnameseAddressService, JohnHenryFashionWeb.Services.VietnameseAddressService>();

// Add API Explorer for Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo 
    { 
        Title = "John Henry Fashion API", 
        Version = "v1",
        Description = "API for John Henry Fashion E-commerce Platform"
    });
    
    // Only include API controllers (those in Api namespace or with [ApiController])
    c.DocInclusionPredicate((docName, apiDesc) =>
    {
        // Include only actions from controllers in Api folder or with ApiController attribute
        var controllerName = apiDesc.ActionDescriptor.RouteValues["controller"];
        return controllerName?.StartsWith("Api") == true || 
               apiDesc.ActionDescriptor.EndpointMetadata.Any(m => m.GetType().Name == "ApiControllerAttribute");
    });
});

var app = builder.Build();

// Seed Admin System Data
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("Starting Admin System Data Seeding...");
        
        var seedService = services.GetRequiredService<JohnHenryFashionWeb.Services.SeedDataService>();
        await seedService.SeedAdminSystemDataAsync();

        var permissionSeedService = services.GetRequiredService<JohnHenryFashionWeb.Services.PermissionSeedService>();
        await permissionSeedService.SeedDefaultRolePermissionsAsync(seededBy: "system");
        
        logger.LogInformation("Admin System Data Seeding completed successfully!");
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding admin system data.");
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseDeveloperExceptionPage();
}
else
{
    // Enable Swagger in Production for testing/documentation (remove in real production)
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "John Henry Fashion API V1");
    });
    
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

// Enable response compression
app.UseResponseCompression();

// Add custom middleware
app.UseMiddleware<JohnHenryFashionWeb.Middleware.PerformanceMiddleware>();

// Enable response caching
app.UseResponseCaching();

app.UseHttpsRedirection();

// Configure static files with caching
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        // Cache static files for 1 year
        const int durationInSeconds = 60 * 60 * 24 * 365;
        ctx.Context.Response.Headers[HeaderNames.CacheControl] =
            "public,max-age=" + durationInSeconds;
    }
});

// Note: Removed lowercase URL redirect middleware because:
// - Image files are stored in UPPERCASE (BE25FH45-HL.jpg)
// - Middleware would redirect to lowercase, causing 404 errors
// - Database FeaturedImageUrl paths already match actual file cases

app.UseRouting();

// Enable session before authentication
app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

// Blog specific routes
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

// API Routes
app.MapControllerRoute(
    name: "api",
    pattern: "api/{controller}/{action=Index}/{id?}");

// Ensure database is created and roles seeded
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    
    try
    {
        // First, ensure database is created
        await context.Database.EnsureCreatedAsync();
        Log.Information("Database ensured created successfully");
        
        // Now seed data after model is fully created
        await SeedRoles(roleManager);
        Log.Information("Roles seeded successfully");
        
        await SeedAdminUser(userManager);
        Log.Information("Admin user seeded successfully");
        
        await SeedBlogPosts(context, userManager);
        Log.Information("Sample blog posts seeded successfully");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "An error occurred while ensuring database created or seeding data");
    }
}

// Helper methods for seeding
static async Task SeedRoles(RoleManager<IdentityRole> roleManager)
{
    var roles = new[] { UserRoles.Admin, UserRoles.Seller, UserRoles.Customer };
    
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }
}

static async Task SeedAdminUser(UserManager<ApplicationUser> userManager)
{
    var adminEmail = "thefirestar312@gmail.com";
    var adminUser = await userManager.FindByEmailAsync(adminEmail);
    
    if (adminUser == null)
    {
        adminUser = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            FirstName = "Admin",
            LastName = "John Henry",
            EmailConfirmed = true,
            IsActive = true,
            IsApproved = true,
            ApprovedAt = DateTime.UtcNow
        };
        
        var result = await userManager.CreateAsync(adminUser, "Admin123!");
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(adminUser, UserRoles.Admin);
        }
    }

    // Create default seller user
    var sellerEmail = "seller@johnhenry.com";
    var sellerUser = await userManager.FindByEmailAsync(sellerEmail);
    
    if (sellerUser == null)
    {
        sellerUser = new ApplicationUser
        {
            UserName = sellerEmail,
            Email = sellerEmail,
            FirstName = "Seller",
            LastName = "Demo",
            EmailConfirmed = true,
            IsActive = true,
            IsApproved = true,
            ApprovedAt = DateTime.UtcNow
        };
        
        var result = await userManager.CreateAsync(sellerUser, "Seller123!");
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(sellerUser, UserRoles.Seller);
        }
    }
}

static async Task SeedBlogPosts(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
{
    // Check if blog posts already exist
    if (await context.BlogPosts.AnyAsync())
    {
        return; // Already seeded
    }

    // Get admin user to set as author
    var adminUser = await userManager.FindByEmailAsync("thefirestar312@gmail.com");
    if (adminUser == null) return;

    // Remove any old categories with wrong slugs
    var oldCategories = await context.BlogCategories
        .Where(c => c.Slug == "xu-huong-thoi-trang" || c.Slug == "thoi-trang")
        .ToListAsync();
    if (oldCategories.Any())
    {
        context.BlogCategories.RemoveRange(oldCategories);
        await context.SaveChangesAsync();
    }

    // Create blog category if not exists
    var fashionCategory = await context.BlogCategories.FirstOrDefaultAsync(c => c.Slug == "xu-huong");
    if (fashionCategory == null)
    {
        fashionCategory = new BlogCategory
        {
            Id = Guid.NewGuid(),
            Name = "Xu Hướng",
            Slug = "xu-huong",
            Description = "Xu hướng thời trang mới nhất",
            IsActive = true,
            SortOrder = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.BlogCategories.Add(fashionCategory);
        await context.SaveChangesAsync();
    }

    var blogPosts = new List<BlogPost>
    {
        new BlogPost
        {
            Id = Guid.NewGuid(),
            Title = "Xu hướng thời trang Thu Đông 2025",
            Slug = "xu-huong-thoi-trang-thu-dong-2025",
            Excerpt = "Khám phá những xu hướng thời trang mới nhất cho mùa Thu Đông năm nay với những màu sắc và phong cách độc đáo...",
            Content = @"<h2>PHONG CÁCH LỊCH LÃM & HIỆN ĐẠI</h2>
<p>Thời tiết đang dần chuyển mình từ những ngày Thu dịu nhẹ sang không khí se lạnh của mùa Đông. Đây cũng là lúc phong cách của bạn cần được làm mới – nhiều layer hơn, sắc sảo hơn và mạnh mẽ hơn.</p>

<h3>Áo Len & Cardigan - Ấm Áp Và Tinh Tế</h3>
<p>Không gì lý tưởng hơn một chiếc áo len mềm mại trong những ngày se lạnh. Chất liệu len cao cấp kết hợp với thiết kế hiện đại giúp bạn vừa giữ ấm, vừa toát lên vẻ thanh lịch trong mọi hoàn cảnh.</p>

<h3>Jacket & Blazer - Mạnh Mẽ Và Bản Lĩnh</h3>
<p>Layer ngoài quan trọng nhất trong mùa Thu Đông. Từ blazer công sở đến jacket dạ phố, mỗi thiết kế đều mang đến sự chỉn chu và nam tính riêng biệt.</p>

<h3>Màu Sắc Chủ Đạo</h3>
<p>Gam màu trung tính như xám, nâu, be và đen vẫn là lựa chọn an toàn và sang trọng. Tuy nhiên, những điểm nhấn màu navy, xanh rêu hay burgundy sẽ giúp outfit trở nên nổi bật và cá tính hơn.</p>

<p>Hãy để BST Thu Đông 2025 từ JOHN HENRY đồng hành cùng bạn trong mùa đông này!</p>",
            FeaturedImageUrl = "/images/blog/banner_02160e22.jpg",
            Status = "published",
            IsFeatured = true,
            ViewCount = 0,
            Tags = new[] { "Thu Đông 2025", "Xu Hướng", "Áo Len", "Jacket" },
            MetaTitle = "Xu hướng thời trang Thu Đông 2025 - JOHN HENRY",
            MetaDescription = "Khám phá những xu hướng thời trang mới nhất cho mùa Thu Đông năm nay với những màu sắc và phong cách độc đáo từ JOHN HENRY.",
            CategoryId = fashionCategory.Id,
            AuthorId = adminUser.Id,
            PublishedAt = new DateTime(2025, 10, 15, 0, 0, 0, DateTimeKind.Utc),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        },
        new BlogPost
        {
            Id = Guid.NewGuid(),
            Title = "Bí quyết phối đồ nam hiện đại",
            Slug = "bi-quyet-phoi-do-nam-hien-dai",
            Excerpt = "Hướng dẫn chi tiết cách phối đồ nam tính và lịch lãm cho các chàng trai hiện đại trong mọi hoàn cảnh...",
            Content = @"<h2>HƯỚNG DẪN PHỐI ĐỒ CHUYÊN NGHIỆP</h2>
<p>Phối đồ không chỉ là việc mặc những gì có sẵn trong tủ, mà là nghệ thuật kết hợp các items để tạo nên phong cách riêng biệt. Dưới đây là những bí quyết giúp bạn luôn tự tin và cuốn hút.</p>

<h3>1. Chọn Áo Sơ Mi Phù Hợp</h3>
<p>Áo sơ mi là nền tảng của tủ đồ công sở. Form ôm vừa vặn, chất liệu cotton cao cấp và màu sắc trung tính sẽ giúp bạn dễ dàng mix-match với nhiều trang phục khác.</p>

<h3>2. Quần Tây & Jeans - Linh Hoạt Đa Phong Cách</h3>
<p>Quần tây cho môi trường chính thức, jeans cho sự thoải mái hàng ngày. Quan trọng là chọn form dáng phù hợp với vóc dáng và biết cách phối với giày dép hợp lý.</p>

<h3>3. Layer Thông Minh</h3>
<p>Trong những ngày se lạnh, việc phối nhiều layer không chỉ giúp giữ ấm mà còn tạo chiều sâu cho outfit. Áo thun + sơ mi + cardigan/blazer là công thức an toàn và thanh lịch.</p>

<h3>4. Phụ Kiện Hoàn Thiện</h3>
<p>Đồng hồ, thắt lưng, mắt kính và ví da chất lượng là những điểm nhấn nhỏ nhưng tạo nên sự khác biệt lớn trong tổng thể phong cách.</p>

<p>Hãy thử áp dụng ngay để trở thành phiên bản phong cách nhất của chính mình!</p>",
            FeaturedImageUrl = "/images/blog/banner_23da5ec2.jpg",
            Status = "published",
            IsFeatured = true,
            ViewCount = 0,
            Tags = new[] { "Phối Đồ", "Nam Tính", "Sơ Mi", "Style Tips" },
            MetaTitle = "Bí quyết phối đồ nam hiện đại - JOHN HENRY",
            MetaDescription = "Hướng dẫn chi tiết cách phối đồ nam tính và lịch lãm cho các chàng trai hiện đại trong mọi hoàn cảnh.",
            CategoryId = fashionCategory.Id,
            AuthorId = adminUser.Id,
            PublishedAt = new DateTime(2025, 10, 12, 0, 0, 0, DateTimeKind.Utc),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        },
        new BlogPost
        {
            Id = Guid.NewGuid(),
            Title = "Thời trang công sở cho phái đẹp",
            Slug = "thoi-trang-cong-so-cho-phai-dep",
            Excerpt = "Những gợi ý trang phục công sở thanh lịch và chuyên nghiệp dành cho các cô nàng hiện đại...",
            Content = @"<h2>PHONG CÁCH CÔNG SỞ HIỆN ĐẠI</h2>
<p>Thời trang công sở ngày nay không còn đơn điệu và cứng nhắc như trước. Nàng công sở hiện đại có thể vừa chuyên nghiệp, vừa thời trang với những gợi ý sau đây từ Freelancer.</p>

<h3>Váy Công Sở - Thanh Lịch Và Nữ Tính</h3>
<p>Váy midi hoặc chân váy bút chì là lựa chọn hoàn hảo. Phom dáng tôn dáng, chất liệu không nhăn và màu sắc trung tính giúp nàng luôn gọn gàng suốt ngày dài.</p>

<h3>Áo Sơ Mi & Blouse - Tinh Tế Từng Chi Tiết</h3>
<p>Từ sơ mi trắng cổ điển đến blouse với chi tiết bèo nhún tinh tế, mỗi item đều có thể tạo nên nhiều phong cách khác nhau tùy cách phối.</p>

<h3>Blazer & Cardigan - Layer Chuyên Nghiệp</h3>
<p>Layer ngoài không chỉ giữ ấm mà còn tăng thêm vẻ chuyên nghiệp. Một chiếc blazer cắt may chuẩn sẽ giúp nàng trông cao hơn, thanh lịch hơn.</p>

<h3>Phụ Kiện Tối Giản</h3>
<p>Túi xách công sở cỡ vừa, giày cao gót 5-7cm và trang sức nhẹ nhàng sẽ là điểm hoàn thiện cho outfit công sở hoàn hảo.</p>

<p>Hãy để Freelancer đồng hành cùng nàng trên hành trình tỏa sáng nơi công sở!</p>",
            FeaturedImageUrl = "/images/blog/banner_ecb4d0c5.jpg",
            Status = "published",
            IsFeatured = true,
            ViewCount = 0,
            Tags = new[] { "Công Sở", "Nữ", "Freelancer", "Thanh Lịch" },
            MetaTitle = "Thời trang công sở cho phái đẹp - Freelancer",
            MetaDescription = "Những gợi ý trang phục công sở thanh lịch và chuyên nghiệp dành cho các cô nàng hiện đại từ Freelancer.",
            CategoryId = fashionCategory.Id,
            AuthorId = adminUser.Id,
            PublishedAt = new DateTime(2025, 10, 10, 0, 0, 0, DateTimeKind.Utc),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        },
        new BlogPost
        {
            Id = Guid.NewGuid(),
            Title = "VISCOSE – Chất liệu nâng tầm trải nghiệm đồ len",
            Slug = "viscose-chat-lieu-nang-tam-trai-nghiem-do-len",
            Excerpt = "Khám phá chất liệu viscose - 'lụa nhân tạo' mang lại sự mềm mại, thoáng khí và bền đẹp cho áo len...",
            Content = @"<h2>VISCOSE - VẬT LIỆU ĐỘT PHÁ CHO ÁO LEN</h2>
<p>Bạn đang tìm một chiếc áo len vừa thoải mái, vừa bền đẹp? Bí quyết nằm ở sợi viscose – chất liệu được yêu thích trong ngành thời trang nhờ loạt ưu điểm vượt trội.</p>

<h3>MỀM MẠI & THOÁNG KHÍ</h3>
<p>Viscose được biết đến như 'lụa nhân tạo' với độ mềm mại tự nhiên. Khi kết hợp với len, nó mang lại cảm giác êm ái trên da, đồng thời cho phép không khí lưu thông tốt hơn.</p>

<h3>HÚT ẨM TỐT</h3>
<p>Khác với len thuần túy có thể gây bí bách, viscose có khả năng hấp thụ độ ẩm tuyệt vời, giúp bạn luôn khô ráo và thoải mái cả ngày dài.</p>

<h3>GIỮ FORM & HẠN CHẾ XÙ LÔNG</h3>
<p>Một trong những vấn đề lớn nhất của áo len là xù lông và giãn form. Viscose giúp áo giữ phom dáng gọn gàng hơn và giảm thiểu tình trạng xù lông đáng kể.</p>

<h3>ĐA DẠNG ỨNG DỤNG</h3>
<p>Từ cardigan nhẹ nhàng, áo len cổ tròn cơ bản đến sweater dày dặn cho mùa đông, viscose đều có thể ứng dụng linh hoạt và mang lại hiệu quả tốt nhất.</p>

<p>Hãy trải nghiệm sự khác biệt với dòng áo len viscose từ JOHN HENRY!</p>",
            FeaturedImageUrl = "/images/blog/banner_e9ada939.jpg",
            Status = "published",
            IsFeatured = false,
            ViewCount = 0,
            Tags = new[] { "Viscose", "Chất Liệu", "Áo Len", "Thu Đông" },
            MetaTitle = "VISCOSE – Chất liệu nâng tầm trải nghiệm đồ len",
            MetaDescription = "Khám phá chất liệu viscose - 'lụa nhân tạo' mang lại sự mềm mại, thoáng khí và bền đẹp cho áo len từ JOHN HENRY.",
            CategoryId = fashionCategory.Id,
            AuthorId = adminUser.Id,
            PublishedAt = new DateTime(2025, 10, 8, 0, 0, 0, DateTimeKind.Utc),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        },
        new BlogPost
        {
            Id = Guid.NewGuid(),
            Title = "Modern Office Look - Chỉn chu và phóng khoáng",
            Slug = "modern-office-look-chin-chu-va-phong-khoang",
            Excerpt = "Phong cách công sở hiện đại kết hợp giữa chỉn chu và thoải mái, giữa thanh lịch và tự do...",
            Content = @"<h2>PHONG CÁCH CÔNG SỞ HIỆN ĐẠI</h2>
<p>Giữa nhịp sống năng động của thành thị, phong cách công sở ngày nay không còn bó buộc trong những bộ suit truyền thống. Người đàn ông hiện đại tìm kiếm sự cân bằng giữa chỉn chu và thoải mái.</p>

<h3>JACKET CITY – KẾT HỢP TINH TẾ</h3>
<p>Jacket hiện đại không quá cứng nhắc như blazer truyền thống nhưng vẫn giữ được nét thanh lịch. Chất liệu co giãn nhẹ, form dáng vừa vặn giúp bạn tự tin di chuyển trong mọi hoàn cảnh.</p>

<h3>SƠ MI SMART CASUAL</h3>
<p>Không nhất thiết phải luôn cài khuy đến cùng. Sơ mi smart casual với chất liệu wrinkle-free giúp bạn gọn gàng cả ngày mà không cần ủi lại.</p>

<h3>QUẦN TÂY MODERN FIT</h3>
<p>Quần tây hiện đại với form dáng vừa phải - không quá ôm cũng không quá rộng. Chất liệu co giãn 4 chiều mang lại sự thoải mái tối đa.</p>

<h3>PHỤ KIỆN TINH TẾ</h3>
<p>Giày Derby hoặc Loafer da thật, thắt lưng da mảnh, đồng hồ đơn giản - những phụ kiện nhỏ này hoàn thiện phong cách một cách tinh tế nhất.</p>

<p>Modern Office Look từ JOHN HENRY - Tự tin bước vào mọi không gian công sở!</p>",
            FeaturedImageUrl = "/images/blog/banner_2f547192.jpg",
            Status = "published",
            IsFeatured = false,
            ViewCount = 0,
            Tags = new[] { "Công Sở", "Modern", "Jacket", "Smart Casual" },
            MetaTitle = "Modern Office Look - Chỉn chu và phóng khoáng",
            MetaDescription = "Phong cách công sở hiện đại kết hợp giữa chỉn chu và thoải mái, giữa thanh lịch và tự do từ JOHN HENRY.",
            CategoryId = fashionCategory.Id,
            AuthorId = adminUser.Id,
            PublishedAt = new DateTime(2025, 10, 17, 0, 0, 0, DateTimeKind.Utc),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        },
        new BlogPost
        {
            Id = Guid.NewGuid(),
            Title = "Áo len mới mùa mới - Ấm áp và thanh lịch",
            Slug = "ao-len-moi-mua-moi-am-ap-va-thanh-lich",
            Excerpt = "Làm mới phong cách với những chiếc áo len đơn giản nhưng tinh tế cho mùa Thu Đông...",
            Content = @"<h2>ÁO LEN - ITEM KHÔNG THỂ THIẾU MÙA ĐÔNG</h2>
<p>Mùa Thu – Đông là thời điểm lý tưởng để làm mới phong cách với những chiếc áo len đơn giản nhưng tinh tế. NEW SWEATER, NEW SEASON từ JOHN HENRY mang đến lựa chọn hoàn hảo cho phái mạnh.</p>

<h3>THIẾT KẾ TỐI GIẢN</h3>
<p>Áo len basic với thiết kế cổ tròn hoặc cổ tim, không có họa tiết rườm rà. Đơn giản nhưng tinh tế, dễ mặc và dễ phối hợp với nhiều trang phục khác.</p>

<h3>GÂM MÀU NHẸ NHÀNG</h3>
<p>Các tông màu trung tính như be, xám, navy và nâu là lựa chọn an toàn và sang trọng. Dễ dàng mix-match với quần jeans, khaki hoặc quần tây.</p>

<h3>CHẤT LIỆU MỀM MẠI</h3>
<p>Len pha viscose mang lại độ mềm mại tự nhiên, không gây ngứa và giữ ấm hiệu quả. Khả năng chống nhăn tốt giúp áo luôn gọn gàng.</p>

<h3>ĐA DẠNG CÁCH PHỐI</h3>
<p>• Layer trong: Mặc bên trong jacket hoặc blazer<br>
• Standalone: Mặc riêng với quần jeans<br>
• Smart casual: Phối với sơ mi bên trong<br>
• Weekend: Kết hợp với jogger cho vẻ năng động</p>

<p>Ấm áp và phong cách cùng bộ sưu tập áo len mới từ JOHN HENRY!</p>",
            FeaturedImageUrl = "/images/blog/banner_508e8b56.jpg",
            Status = "published",
            IsFeatured = false,
            ViewCount = 0,
            Tags = new[] { "Áo Len", "Sweater", "Mùa Đông", "Basic" },
            MetaTitle = "Áo len mới mùa mới - Ấm áp và thanh lịch",
            MetaDescription = "Làm mới phong cách với những chiếc áo len đơn giản nhưng tinh tế cho mùa Thu Đông từ JOHN HENRY.",
            CategoryId = fashionCategory.Id,
            AuthorId = adminUser.Id,
            PublishedAt = new DateTime(2025, 10, 1, 0, 0, 0, DateTimeKind.Utc),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        }
    };

    context.BlogPosts.AddRange(blogPosts);
    await context.SaveChangesAsync();
}

// Check for import-products command
if (args.Contains("--import-products"))
{
    Console.WriteLine("\n╔════════════════════════════════════════════════╗");
    Console.WriteLine("║  JOHN HENRY - IMPORT PRODUCTS FROM CSV        ║");
    Console.WriteLine("╚════════════════════════════════════════════════╝\n");
    
    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var csvPath = "database/johnhenry_products.csv";
        
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
