namespace JohnHenryFashionWeb.Extensions;

public static class EnvironmentConfigExtensions
{
    public static void LoadEnvironmentOverrides(this IConfiguration configuration)
    {
        DotNetEnv.Env.Load();

        // Database Configuration
        var dbHost = Environment.GetEnvironmentVariable("DB_HOST");
        var dbPort = Environment.GetEnvironmentVariable("DB_PORT");
        var dbName = Environment.GetEnvironmentVariable("DB_NAME");
        var dbUser = Environment.GetEnvironmentVariable("DB_USER");
        var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD");
        if (!string.IsNullOrWhiteSpace(dbHost) && !string.IsNullOrWhiteSpace(dbName))
        {
            configuration["ConnectionStrings:DefaultConnection"] = $"Host={dbHost};Port={dbPort};Database={dbName};Username={dbUser};Password={dbPassword};SSL Mode=Prefer;Trust Server Certificate=true";
        }

        // Payment Gateways
        OverrideIfSet(configuration, "VNPAY_TMN_CODE", "PaymentGateways:VNPay:TmnCode");
        OverrideIfSet(configuration, "VNPAY_HASH_SECRET", "PaymentGateways:VNPay:HashSecret");
        OverrideIfSet(configuration, "VNPAY_PAYMENT_URL", "PaymentGateways:VNPay:PaymentUrl");
        OverrideIfSet(configuration, "VNPAY_API_URL", "PaymentGateways:VNPay:ApiUrl");
        OverrideIfSet(configuration, "VNPAY_ENABLED", "PaymentGateways:VNPay:IsEnabled");
        OverrideIfSet(configuration, "VNPAY_SANDBOX", "PaymentGateways:VNPay:IsSandbox");

        OverrideIfSet(configuration, "MOMO_PARTNER_CODE", "PaymentGateways:MoMo:PartnerCode");
        OverrideIfSet(configuration, "MOMO_ACCESS_KEY", "PaymentGateways:MoMo:AccessKey");
        OverrideIfSet(configuration, "MOMO_SECRET_KEY", "PaymentGateways:MoMo:SecretKey");
        OverrideIfSet(configuration, "MOMO_API_URL", "PaymentGateways:MoMo:ApiUrl");
        OverrideIfSet(configuration, "MOMO_PUBLIC_KEY", "PaymentGateways:MoMo:PublicKey");
        OverrideIfSet(configuration, "MOMO_PRIVATE_KEY", "PaymentGateways:MoMo:PrivateKey");
        OverrideIfSet(configuration, "MOMO_ENABLED", "PaymentGateways:MoMo:IsEnabled");
        OverrideIfSet(configuration, "MOMO_SANDBOX", "PaymentGateways:MoMo:IsSandbox");

        OverrideIfSet(configuration, "STRIPE_PUBLISHABLE_KEY", "PaymentGateways:Stripe:PublishableKey");
        OverrideIfSet(configuration, "STRIPE_SECRET_KEY", "PaymentGateways:Stripe:SecretKey");
        OverrideIfSet(configuration, "STRIPE_WEBHOOK_SECRET", "PaymentGateways:Stripe:WebhookSecret");
        OverrideIfSet(configuration, "STRIPE_API_URL", "PaymentGateways:Stripe:ApiUrl");
        OverrideIfSet(configuration, "STRIPE_CURRENCY", "PaymentGateways:Stripe:Currency");
        OverrideIfSet(configuration, "STRIPE_ENABLED", "PaymentGateways:Stripe:IsEnabled");
        OverrideIfSet(configuration, "STRIPE_SANDBOX", "PaymentGateways:Stripe:IsSandbox");

        // Google OAuth
        OverrideIfSet(configuration, "GOOGLE_CLIENT_ID", "Authentication:Google:ClientId");
        OverrideIfSet(configuration, "GOOGLE_CLIENT_SECRET", "Authentication:Google:ClientSecret");

        // Email
        OverrideIfSet(configuration, "EMAIL_HOST", "EmailSettings:SmtpServer");
        OverrideIfSet(configuration, "EMAIL_PORT", "EmailSettings:SmtpPort");
        OverrideIfSet(configuration, "EMAIL_USE_SSL", "EmailSettings:UseSsl");
        OverrideIfSet(configuration, "EMAIL_USER", "EmailSettings:Username");
        OverrideIfSet(configuration, "EMAIL_PASSWORD", "EmailSettings:Password");
        OverrideIfSet(configuration, "EMAIL_FROM", "EmailSettings:FromEmail");
        OverrideIfSet(configuration, "EMAIL_FROM_NAME", "EmailSettings:FromName");
        OverrideIfSet(configuration, "EMAIL_ADMIN", "EmailSettings:AdminEmail");
    }

    private static void OverrideIfSet(IConfiguration configuration, string envVar, string configKey)
    {
        var value = Environment.GetEnvironmentVariable(envVar);
        if (!string.IsNullOrWhiteSpace(value))
            configuration[configKey] = value;
    }
}
