using JohnHenryFashionWeb.Data;
using JohnHenryFashionWeb.Models;
using Microsoft.EntityFrameworkCore;

namespace JohnHenryFashionWeb.Services
{
    public class SeedDataService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SeedDataService> _logger;

        public SeedDataService(ApplicationDbContext context, ILogger<SeedDataService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task SeedAdminSystemDataAsync()
        {
            try
            {
                _logger.LogInformation("Starting Admin System Data Seeding...");

                // Check if data already exists
                if (await _context.SystemConfigurations.AnyAsync())
                {
                    _logger.LogInformation("Admin system data already seeded. Skipping...");
                    return;
                }

                // 1. Seed System Configurations
                await SeedSystemConfigurations();

                // 2. Seed FAQs
                await SeedFAQs();

                await _context.SaveChangesAsync();
                _logger.LogInformation("Admin System Data Seeding completed successfully!");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error seeding admin system data");
                throw;
            }
        }

        private async Task SeedSystemConfigurations()
        {
            var configs = new[]
            {
                new SystemConfiguration
                {
                    Key = "SiteName",
                    Value = "John Henry Fashion",
                    Type = "string",
                    Category = "general",
                    Description = "Website name displayed in header",
                    IsEncrypted = false
                },
                new SystemConfiguration
                {
                    Key = "SiteDescription",
                    Value = "Premium Fashion Marketplace",
                    Type = "string",
                    Category = "general",
                    Description = "Site meta description",
                    IsEncrypted = false
                },
                new SystemConfiguration
                {
                    Key = "MaintenanceMode",
                    Value = "false",
                    Type = "boolean",
                    Category = "general",
                    Description = "Enable/disable maintenance mode",
                    IsEncrypted = false
                },
                new SystemConfiguration
                {
                    Key = "AllowNewSellerRegistration",
                    Value = "true",
                    Type = "boolean",
                    Category = "general",
                    Description = "Allow new sellers to register",
                    IsEncrypted = false
                },
                new SystemConfiguration
                {
                    Key = "RequireProductApproval",
                    Value = "true",
                    Type = "boolean",
                    Category = "general",
                    Description = "Require admin approval for new products",
                    IsEncrypted = false
                },
                new SystemConfiguration
                {
                    Key = "DefaultCommissionRate",
                    Value = "10",
                    Type = "number",
                    Category = "payment",
                    Description = "Default commission rate in percentage",
                    IsEncrypted = false
                },
                new SystemConfiguration
                {
                    Key = "MinimumOrderAmount",
                    Value = "50000",
                    Type = "number",
                    Category = "general",
                    Description = "Minimum order amount in VND",
                    IsEncrypted = false
                },
                new SystemConfiguration
                {
                    Key = "FreeShippingThreshold",
                    Value = "500000",
                    Type = "number",
                    Category = "shipping",
                    Description = "Free shipping threshold in VND",
                    IsEncrypted = false
                },
                new SystemConfiguration
                {
                    Key = "MaxImageSize",
                    Value = "5242880",
                    Type = "number",
                    Category = "general",
                    Description = "Maximum image upload size in bytes (5MB)",
                    IsEncrypted = false
                },
                new SystemConfiguration
                {
                    Key = "AllowedImageTypes",
                    Value = "jpg,jpeg,png,webp",
                    Type = "string",
                    Category = "general",
                    Description = "Allowed image file extensions",
                    IsEncrypted = false
                },
                new SystemConfiguration
                {
                    Key = "CurrencyCode",
                    Value = "VND",
                    Type = "string",
                    Category = "payment",
                    Description = "Currency code",
                    IsEncrypted = false
                },
                new SystemConfiguration
                {
                    Key = "CurrencySymbol",
                    Value = "₫",
                    Type = "string",
                    Category = "payment",
                    Description = "Currency symbol",
                    IsEncrypted = false
                },
                new SystemConfiguration
                {
                    Key = "ContactEmail",
                    Value = "support@johnhenry.vn",
                    Type = "string",
                    Category = "general",
                    Description = "Contact email address",
                    IsEncrypted = false
                },
                new SystemConfiguration
                {
                    Key = "ContactPhone",
                    Value = "+84 123 456 789",
                    Type = "string",
                    Category = "general",
                    Description = "Contact phone number",
                    IsEncrypted = false
                },
                new SystemConfiguration
                {
                    Key = "FacebookUrl",
                    Value = "https://facebook.com/johnhenryfashion",
                    Type = "string",
                    Category = "general",
                    Description = "Facebook page URL",
                    IsEncrypted = false
                },
                new SystemConfiguration
                {
                    Key = "InstagramUrl",
                    Value = "https://instagram.com/johnhenryfashion",
                    Type = "string",
                    Category = "general",
                    Description = "Instagram page URL",
                    IsEncrypted = false
                },
                // Security Settings
                new SystemConfiguration
                {
                    Key = "max_login_attempts",
                    Value = "5",
                    Type = "number",
                    Category = "security",
                    Description = "Maximum login attempts before account lockout",
                    IsEncrypted = false
                },
                new SystemConfiguration
                {
                    Key = "session_timeout_minutes",
                    Value = "30",
                    Type = "number",
                    Category = "security",
                    Description = "Session timeout in minutes",
                    IsEncrypted = false
                },
                new SystemConfiguration
                {
                    Key = "password_expiry_days",
                    Value = "90",
                    Type = "number",
                    Category = "security",
                    Description = "Password expiration period in days",
                    IsEncrypted = false
                },
                new SystemConfiguration
                {
                    Key = "require_2fa",
                    Value = "false",
                    Type = "boolean",
                    Category = "security",
                    Description = "Require two-factor authentication for all users",
                    IsEncrypted = false
                },
                new SystemConfiguration
                {
                    Key = "lockout_duration_minutes",
                    Value = "15",
                    Type = "number",
                    Category = "security",
                    Description = "Account lockout duration after max failed attempts",
                    IsEncrypted = false
                }
            };

            await _context.SystemConfigurations.AddRangeAsync(configs);
            _logger.LogInformation($"Seeded {configs.Length} system configurations");
        }

        private async Task SeedFAQs()
        {
            var faqs = new[]
            {
                new FAQ
                {
                    Category = "Order",
                    Question = "Làm thế nào để đặt hàng?",
                    Answer = "Để đặt hàng, bạn chỉ cần chọn sản phẩm, thêm vào giỏ hàng, sau đó điền thông tin giao hàng và chọn phương thức thanh toán.",
                    SortOrder = 1,
                    IsActive = true,
                    ViewCount = 0
                },
                new FAQ
                {
                    Category = "Shipping",
                    Question = "Thời gian giao hàng là bao lâu?",
                    Answer = "Thời gian giao hàng thông thường là 3-5 ngày làm việc tùy thuộc vào địa chỉ của bạn. Đơn hàng nội thành có thể giao trong 1-2 ngày.",
                    SortOrder = 2,
                    IsActive = true,
                    ViewCount = 0
                },
                new FAQ
                {
                    Category = "Payment",
                    Question = "Có những phương thức thanh toán nào?",
                    Answer = "Chúng tôi hỗ trợ thanh toán COD, chuyển khoản ngân hàng, VNPay, MoMo, và ZaloPay.",
                    SortOrder = 3,
                    IsActive = true,
                    ViewCount = 0
                },
                new FAQ
                {
                    Category = "Return",
                    Question = "Chính sách đổi trả như thế nào?",
                    Answer = "Bạn có thể đổi trả sản phẩm trong vòng 7 ngày kể từ ngày nhận hàng nếu sản phẩm còn nguyên vẹn, chưa qua sử dụng và còn tem mác.",
                    SortOrder = 4,
                    IsActive = true,
                    ViewCount = 0
                },
                new FAQ
                {
                    Category = "Account",
                    Question = "Làm thế nào để đăng ký tài khoản Seller?",
                    Answer = "Bạn có thể đăng ký tài khoản Seller bằng cách điền form đăng ký trên trang web, cung cấp thông tin doanh nghiệp và chờ admin phê duyệt.",
                    SortOrder = 5,
                    IsActive = true,
                    ViewCount = 0
                }
            };

            await _context.FAQs.AddRangeAsync(faqs);
            _logger.LogInformation($"Seeded {faqs.Length} FAQs");
        }
    }
}
