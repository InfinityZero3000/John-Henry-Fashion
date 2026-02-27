using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.ResponseCompression;
using System.Text;
using System.IO.Compression;
using JohnHenryFashionWeb.Data;
using JohnHenryFashionWeb.Models;
using Serilog;

namespace JohnHenryFashionWeb.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration, bool isTesting)
    {
        if (isTesting)
        {
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase("TestDatabase_" + Guid.NewGuid()));
            Log.Information("Using InMemory database for Testing environment");
        }
        else
        {
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));
        }

        return services;
    }

    public static IServiceCollection AddIdentityConfiguration(this IServiceCollection services, CookieSecurePolicy cookieSecurePolicy)
    {
        // Register BCrypt password hasher
        services.AddScoped<IPasswordHasher<ApplicationUser>, JohnHenryFashionWeb.Services.BcryptPasswordHasher<ApplicationUser>>();

        // Add Identity with enhanced security settings
        services.AddIdentity<ApplicationUser, IdentityRole>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequireUppercase = true;
            options.Password.RequiredLength = 8;
            options.Password.RequiredUniqueChars = 3;

            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            options.Lockout.MaxFailedAccessAttempts = 3;
            options.Lockout.AllowedForNewUsers = true;

            options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
            options.User.RequireUniqueEmail = true;

            options.SignIn.RequireConfirmedEmail = bool.Parse(Environment.GetEnvironmentVariable("REQUIRE_EMAIL_CONFIRMATION") ?? "false");
            options.SignIn.RequireConfirmedAccount = bool.Parse(Environment.GetEnvironmentVariable("REQUIRE_EMAIL_CONFIRMATION") ?? "false");
            options.SignIn.RequireConfirmedPhoneNumber = false;

            options.Tokens.EmailConfirmationTokenProvider = TokenOptions.DefaultEmailProvider;
            options.Tokens.PasswordResetTokenProvider = TokenOptions.DefaultEmailProvider;
            options.Tokens.ChangeEmailTokenProvider = TokenOptions.DefaultEmailProvider;
        })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders()
        .AddTokenProvider<DataProtectorTokenProvider<ApplicationUser>>("Custom");

        // Application Cookie
        services.ConfigureApplicationCookie(options =>
        {
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = cookieSecurePolicy;
            options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax;
            options.Cookie.IsEssential = true;
            options.AccessDeniedPath = "/Account/AccessDenied";
            options.LoginPath = "/Account/Login";
            options.LogoutPath = "/Account/Logout";
            options.ExpireTimeSpan = TimeSpan.FromDays(30);
            options.SlidingExpiration = true;
        });

        // External Cookie for Google OAuth
        services.ConfigureExternalCookie(options =>
        {
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = cookieSecurePolicy;
            options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax;
            options.Cookie.IsEssential = true;
        });

        return services;
    }

    public static IServiceCollection AddAuthenticationConfiguration(this IServiceCollection services, IConfiguration configuration, CookieSecurePolicy cookieSecurePolicy)
    {
        services.AddAuthentication(options =>
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
                ValidIssuer = configuration["JWT:Issuer"],
                ValidAudience = configuration["JWT:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["JWT:SecretKey"]!))
            };
        })
        .AddGoogle(googleOptions =>
        {
            googleOptions.ClientId = configuration["Authentication:Google:ClientId"]!;
            googleOptions.ClientSecret = configuration["Authentication:Google:ClientSecret"]!;
            googleOptions.CallbackPath = "/signin-google";
            googleOptions.SignInScheme = IdentityConstants.ExternalScheme;
            googleOptions.Scope.Add("email");
            googleOptions.Scope.Add("profile");
            googleOptions.SaveTokens = true;
            googleOptions.CorrelationCookie.SecurePolicy = cookieSecurePolicy;
            googleOptions.CorrelationCookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax;
            googleOptions.CorrelationCookie.HttpOnly = true;
            googleOptions.CorrelationCookie.IsEssential = true;
        });

        services.AddAuthorization(options =>
        {
            options.AddPolicy(AdminPolicies.RequireAdminRole,
                policy => policy.RequireRole(UserRoles.Admin));
            options.AddPolicy(AdminPolicies.RequireSellerRole,
                policy => policy.RequireRole(UserRoles.Seller));
            options.AddPolicy(AdminPolicies.RequireAdminOrSellerRole,
                policy => policy.RequireRole(UserRoles.Admin, UserRoles.Seller));
        });

        return services;
    }

    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<Services.ICacheService, Services.CacheService>();
        services.AddScoped<Services.IImageOptimizationService, Services.ImageOptimizationService>();
        services.AddScoped<Services.ISeoService, Services.SeoService>();
        services.AddScoped<Services.SeoService>();
        services.AddScoped<Services.IPerformanceMonitorService, Services.PerformanceMonitorService>();
        services.AddScoped<Services.IOptimizedDataService, Services.OptimizedDataService>();
        services.AddScoped<Services.IEmailService, Services.EmailService>();
        services.AddScoped<Services.INotificationService, Services.NotificationService>();
        services.AddScoped<Services.ISecurityService, Services.SecurityService>();
        services.AddScoped<Services.ISystemConfigService, Services.SystemConfigService>();
        services.AddScoped<Services.IAnalyticsService, Services.AnalyticsService>();
        services.AddScoped<Services.IReportingService, Services.ReportingService>();
        services.AddScoped<Services.IAuthService, Services.AuthService>();
        services.AddScoped<Services.IPaymentService, Services.PaymentService>();
        services.AddScoped<Services.IUserManagementService, Services.UserManagementService>();
        services.AddScoped<Services.IPermissionService, Services.PermissionService>();
        services.AddScoped<Services.PermissionSeedService>();
        services.AddScoped<Services.IAuditLogService, Services.AuditLogService>();
        services.AddScoped<Services.ILogService, Services.LogService>();
        services.AddScoped<Services.SeedDataService>();
        services.AddScoped<Services.IVietnameseAddressService, Services.VietnameseAddressService>();
        services.AddScoped<Services.IContentModerationService, Services.ContentModerationService>();
        services.AddScoped<Services.ICloudinaryService, Services.CloudinaryService>();

        services.AddHostedService<Services.NotificationCleanupService>();
        services.AddScoped<Helpers.PaymentValidator>();

        return services;
    }

    public static IServiceCollection AddCachingConfiguration(this IServiceCollection services, IConfiguration configuration, bool isTesting)
    {
        services.AddMemoryCache();

        var redisConnection = isTesting ? null :
            (configuration.GetConnectionString("RedisCloud")
            ?? configuration.GetConnectionString("Redis")
            ?? Environment.GetEnvironmentVariable("REDIS_CONNECTION"));

        if (!isTesting && !string.IsNullOrWhiteSpace(redisConnection) && !redisConnection.Equals("localhost:6379", StringComparison.OrdinalIgnoreCase))
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnection;
                options.InstanceName = "JohnHenry:";
            });
            Log.Information("Using Redis distributed cache: {RedisHost}",
                redisConnection.Substring(0, Math.Min(40, redisConnection.Length)) + "...");
        }
        else
        {
            services.AddDistributedMemoryCache();
            Log.Information(isTesting
                ? "Testing env: dùng in-memory cache (bypass Redis)"
                : "Redis không được cấu hình hoặc đang dùng localhost — fallback sang in-memory cache");
        }

        services.AddSession(options =>
        {
            options.IdleTimeout = TimeSpan.FromMinutes(30);
            options.Cookie.HttpOnly = true;
            options.Cookie.IsEssential = true;
            options.Cookie.SecurePolicy = isTesting ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
            options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax;
        });

        services.AddResponseCaching();

        return services;
    }

    public static IServiceCollection AddCompressionConfiguration(this IServiceCollection services)
    {
        services.AddResponseCompression(options =>
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

        services.Configure<BrotliCompressionProviderOptions>(options =>
        {
            options.Level = CompressionLevel.Fastest;
        });

        services.Configure<GzipCompressionProviderOptions>(options =>
        {
            options.Level = CompressionLevel.SmallestSize;
        });

        return services;
    }

    public static IServiceCollection AddHealthChecksConfiguration(this IServiceCollection services, IConfiguration configuration, bool isTesting)
    {
        var healthChecksBuilder = services.AddHealthChecks();

        if (!isTesting)
        {
            healthChecksBuilder.AddNpgSql(
                configuration.GetConnectionString("DefaultConnection")!,
                name: "database",
                timeout: TimeSpan.FromSeconds(5),
                tags: new[] { "db", "sql", "postgres" });

            var redisConnStr = configuration.GetConnectionString("RedisCloud")
                ?? configuration.GetConnectionString("Redis");
            if (!string.IsNullOrWhiteSpace(redisConnStr) && !redisConnStr.Equals("localhost:6379", StringComparison.OrdinalIgnoreCase))
            {
                healthChecksBuilder.AddRedis(
                    redisConnStr,
                    name: "redis",
                    timeout: TimeSpan.FromSeconds(5),
                    tags: new[] { "cache", "redis" });
                Log.Information("Redis health check enabled: {RedisHost}",
                    redisConnStr.Substring(0, Math.Min(40, redisConnStr.Length)) + "...");
            }
            else
            {
                Log.Warning("Redis connection string không tìm thấy hoặc là localhost — bỏ qua Redis health check");
            }
        }

        healthChecksBuilder.AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy(), tags: new[] { "api" });

        return services;
    }
}
