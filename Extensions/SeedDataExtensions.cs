using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using JohnHenryFashionWeb.Data;
using JohnHenryFashionWeb.Models;
using Serilog;

namespace JohnHenryFashionWeb.Extensions;

public static class SeedDataExtensions
{
    public static async Task SeedAdminSystemDataAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;
        try
        {
            var logger = services.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("Starting Admin System Data Seeding...");

            var seedService = services.GetRequiredService<Services.SeedDataService>();
            await seedService.SeedAdminSystemDataAsync();

            var permissionSeedService = services.GetRequiredService<Services.PermissionSeedService>();
            await permissionSeedService.SeedDefaultRolePermissionsAsync(seededBy: "system");

            logger.LogInformation("Admin System Data Seeding completed successfully!");
        }
        catch (Exception ex)
        {
            var logger = services.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "An error occurred while seeding admin system data.");
        }
    }

    public static async Task SeedDatabaseAsync(this WebApplication app, IConfiguration configuration)
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        try
        {
            await context.Database.EnsureCreatedAsync();
            Log.Information("Database ensured created successfully");

            await SeedRoles(roleManager);
            Log.Information("Roles seeded successfully");

            var adminEmail = configuration["AdminSettings:DefaultAdminEmail"] ?? "thefirestar312@gmail.com";
            await SeedAdminUser(userManager, adminEmail);
            Log.Information("Admin user seeded successfully");

            await SeedBlogPosts(context, userManager, adminEmail);
            Log.Information("Sample blog posts seeded successfully");

            await SeedProducts(context);
            Log.Information("Products seeded successfully");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while ensuring database created or seeding data");
        }
    }

    private static async Task SeedRoles(RoleManager<IdentityRole> roleManager)
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

    private static async Task SeedAdminUser(UserManager<ApplicationUser> userManager, string adminEmail)
    {
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

    private static async Task SeedBlogPosts(ApplicationDbContext context, UserManager<ApplicationUser> userManager, string adminEmail)
    {
        if (await context.BlogPosts.AnyAsync())
            return;

        var adminUser = await userManager.FindByEmailAsync(adminEmail);
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

        var blogPosts = CreateSampleBlogPosts(fashionCategory.Id, adminUser.Id);
        context.BlogPosts.AddRange(blogPosts);
        await context.SaveChangesAsync();
    }

    private static List<BlogPost> CreateSampleBlogPosts(Guid categoryId, string authorId)
    {
        return new List<BlogPost>
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
<p>Không gì lý tưởng hơn một chiếc áo len mềm mại trong những ngày se lạnh.</p>
<h3>Jacket & Blazer - Mạnh Mẽ Và Bản Lĩnh</h3>
<p>Layer ngoài quan trọng nhất trong mùa Thu Đông.</p>
<p>Hãy để BST Thu Đông 2025 từ JOHN HENRY đồng hành cùng bạn!</p>",
                FeaturedImageUrl = "/images/blog/banner_02160e22.jpg",
                Status = "published",
                IsFeatured = true,
                ViewCount = 0,
                Tags = new[] { "Thu Đông 2025", "Xu Hướng", "Áo Len", "Jacket" },
                MetaTitle = "Xu hướng thời trang Thu Đông 2025 - JOHN HENRY",
                MetaDescription = "Khám phá những xu hướng thời trang mới nhất cho mùa Thu Đông năm nay.",
                CategoryId = categoryId,
                AuthorId = authorId,
                PublishedAt = new DateTime(2025, 10, 15, 0, 0, 0, DateTimeKind.Utc),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new BlogPost
            {
                Id = Guid.NewGuid(),
                Title = "Bí quyết phối đồ nam hiện đại",
                Slug = "bi-quyet-phoi-do-nam-hien-dai",
                Excerpt = "Hướng dẫn chi tiết cách phối đồ nam tính và lịch lãm...",
                Content = @"<h2>HƯỚNG DẪN PHỐI ĐỒ CHUYÊN NGHIỆP</h2>
<p>Phối đồ không chỉ là việc mặc những gì có sẵn trong tủ, mà là nghệ thuật kết hợp các items để tạo nên phong cách riêng biệt.</p>",
                FeaturedImageUrl = "/images/blog/banner_23da5ec2.jpg",
                Status = "published",
                IsFeatured = true,
                ViewCount = 0,
                Tags = new[] { "Phối Đồ", "Nam Tính", "Style Tips" },
                MetaTitle = "Bí quyết phối đồ nam hiện đại - JOHN HENRY",
                MetaDescription = "Hướng dẫn chi tiết cách phối đồ nam tính và lịch lãm.",
                CategoryId = categoryId,
                AuthorId = authorId,
                PublishedAt = new DateTime(2025, 10, 12, 0, 0, 0, DateTimeKind.Utc),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new BlogPost
            {
                Id = Guid.NewGuid(),
                Title = "Thời trang công sở cho phái đẹp",
                Slug = "thoi-trang-cong-so-cho-phai-dep",
                Excerpt = "Những gợi ý trang phục công sở thanh lịch và chuyên nghiệp...",
                Content = @"<h2>PHONG CÁCH CÔNG SỞ HIỆN ĐẠI</h2>
<p>Thời trang công sở ngày nay không còn đơn điệu và cứng nhắc như trước.</p>",
                FeaturedImageUrl = "/images/blog/banner_ecb4d0c5.jpg",
                Status = "published",
                IsFeatured = true,
                ViewCount = 0,
                Tags = new[] { "Công Sở", "Nữ", "Freelancer" },
                MetaTitle = "Thời trang công sở cho phái đẹp - Freelancer",
                MetaDescription = "Những gợi ý trang phục công sở thanh lịch và chuyên nghiệp.",
                CategoryId = categoryId,
                AuthorId = authorId,
                PublishedAt = new DateTime(2025, 10, 10, 0, 0, 0, DateTimeKind.Utc),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new BlogPost
            {
                Id = Guid.NewGuid(),
                Title = "VISCOSE – Chất liệu nâng tầm trải nghiệm đồ len",
                Slug = "viscose-chat-lieu-nang-tam-trai-nghiem-do-len",
                Excerpt = "Khám phá chất liệu viscose - 'lụa nhân tạo' mang lại sự mềm mại...",
                Content = @"<h2>VISCOSE - VẬT LIỆU ĐỘT PHÁ CHO ÁO LEN</h2>
<p>Bạn đang tìm một chiếc áo len vừa thoải mái, vừa bền đẹp? Bí quyết nằm ở sợi viscose.</p>",
                FeaturedImageUrl = "/images/blog/banner_e9ada939.jpg",
                Status = "published",
                IsFeatured = false,
                ViewCount = 0,
                Tags = new[] { "Viscose", "Chất Liệu", "Áo Len" },
                MetaTitle = "VISCOSE – Chất liệu nâng tầm trải nghiệm đồ len",
                MetaDescription = "Khám phá chất liệu viscose mang lại sự mềm mại cho áo len.",
                CategoryId = categoryId,
                AuthorId = authorId,
                PublishedAt = new DateTime(2025, 10, 8, 0, 0, 0, DateTimeKind.Utc),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new BlogPost
            {
                Id = Guid.NewGuid(),
                Title = "Modern Office Look - Chỉn chu và phóng khoáng",
                Slug = "modern-office-look-chin-chu-va-phong-khoang",
                Excerpt = "Phong cách công sở hiện đại kết hợp giữa chỉn chu và thoải mái...",
                Content = @"<h2>PHONG CÁCH CÔNG SỞ HIỆN ĐẠI</h2>
<p>Giữa nhịp sống năng động của thành thị, phong cách công sở ngày nay không còn bó buộc.</p>",
                FeaturedImageUrl = "/images/blog/banner_2f547192.jpg",
                Status = "published",
                IsFeatured = false,
                ViewCount = 0,
                Tags = new[] { "Công Sở", "Modern", "Smart Casual" },
                MetaTitle = "Modern Office Look - Chỉn chu và phóng khoáng",
                MetaDescription = "Phong cách công sở hiện đại từ JOHN HENRY.",
                CategoryId = categoryId,
                AuthorId = authorId,
                PublishedAt = new DateTime(2025, 10, 17, 0, 0, 0, DateTimeKind.Utc),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new BlogPost
            {
                Id = Guid.NewGuid(),
                Title = "Áo len mới mùa mới - Ấm áp và thanh lịch",
                Slug = "ao-len-moi-mua-moi-am-ap-va-thanh-lich",
                Excerpt = "Làm mới phong cách với những chiếc áo len đơn giản nhưng tinh tế...",
                Content = @"<h2>ÁO LEN - ITEM KHÔNG THỂ THIẾU MÙA ĐÔNG</h2>
<p>Mùa Thu – Đông là thời điểm lý tưởng để làm mới phong cách với những chiếc áo len.</p>",
                FeaturedImageUrl = "/images/blog/banner_508e8b56.jpg",
                Status = "published",
                IsFeatured = false,
                ViewCount = 0,
                Tags = new[] { "Áo Len", "Sweater", "Mùa Đông" },
                MetaTitle = "Áo len mới mùa mới - Ấm áp và thanh lịch",
                MetaDescription = "Làm mới phong cách với áo len từ JOHN HENRY.",
                CategoryId = categoryId,
                AuthorId = authorId,
                PublishedAt = new DateTime(2025, 10, 1, 0, 0, 0, DateTimeKind.Utc),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };
    }

    private static async Task SeedProducts(ApplicationDbContext context)
    {
        if (await context.Products.AnyAsync())
            return;

        var csvCandidates = new[]
        {
            "database/seed/all_products_export.csv",
            "database/seed/johnhenry_products.csv",
            "database/johnhenry_products.csv",
        };

        string? csvPath = null;
        foreach (var candidate in csvCandidates)
        {
            if (File.Exists(candidate)) { csvPath = candidate; break; }
        }

        if (csvPath == null)
        {
            Log.Warning("No product CSV found – skipping product seeding.");
            return;
        }

        Log.Information("Seeding products from {CsvPath}", csvPath);
        await Scripts.ImportProductsFromCsv.RunAsync(context, csvPath);
    }
}
