using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace JohnHenryFashionWeb.TempModels;

public partial class JohnhenryDbContext : DbContext
{
    public JohnhenryDbContext()
    {
    }

    public JohnhenryDbContext(DbContextOptions<JohnhenryDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<ActiveSession> ActiveSessions { get; set; }

    public virtual DbSet<Address> Addresses { get; set; }

    public virtual DbSet<AnalyticsDatum> AnalyticsData { get; set; }

    public virtual DbSet<AspNetRole> AspNetRoles { get; set; }

    public virtual DbSet<AspNetRoleClaim> AspNetRoleClaims { get; set; }

    public virtual DbSet<AspNetUser> AspNetUsers { get; set; }

    public virtual DbSet<AspNetUserClaim> AspNetUserClaims { get; set; }

    public virtual DbSet<AspNetUserLogin> AspNetUserLogins { get; set; }

    public virtual DbSet<AspNetUserToken> AspNetUserTokens { get; set; }

    public virtual DbSet<AuditLog> AuditLogs { get; set; }

    public virtual DbSet<BlogCategory> BlogCategories { get; set; }

    public virtual DbSet<BlogPost> BlogPosts { get; set; }

    public virtual DbSet<Brand> Brands { get; set; }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<CategoryApprovalRule> CategoryApprovalRules { get; set; }

    public virtual DbSet<CheckoutSession> CheckoutSessions { get; set; }

    public virtual DbSet<CheckoutSessionItem> CheckoutSessionItems { get; set; }

    public virtual DbSet<ContactMessage> ContactMessages { get; set; }

    public virtual DbSet<ContentModeration> ContentModerations { get; set; }

    public virtual DbSet<ConversionEvent> ConversionEvents { get; set; }

    public virtual DbSet<Coupon> Coupons { get; set; }

    public virtual DbSet<Dispute> Disputes { get; set; }

    public virtual DbSet<District> Districts { get; set; }

    public virtual DbSet<EmailCampaign> EmailCampaigns { get; set; }

    public virtual DbSet<EmailConfiguration> EmailConfigurations { get; set; }

    public virtual DbSet<Faq> Faqs { get; set; }

    public virtual DbSet<FlashSale> FlashSales { get; set; }

    public virtual DbSet<InventoryItem> InventoryItems { get; set; }

    public virtual DbSet<MarketingBanner> MarketingBanners { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<Order> Orders { get; set; }

    public virtual DbSet<OrderItem> OrderItems { get; set; }

    public virtual DbSet<OrderRevenue> OrderRevenues { get; set; }

    public virtual DbSet<OrderStatusHistory> OrderStatusHistories { get; set; }

    public virtual DbSet<PageView> PageViews { get; set; }

    public virtual DbSet<PasswordHistory> PasswordHistories { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<PaymentAttempt> PaymentAttempts { get; set; }

    public virtual DbSet<PaymentGatewayConfiguration> PaymentGatewayConfigurations { get; set; }

    public virtual DbSet<PaymentMethod> PaymentMethods { get; set; }

    public virtual DbSet<PaymentMethodConfig> PaymentMethodConfigs { get; set; }

    public virtual DbSet<PaymentTransaction> PaymentTransactions { get; set; }

    public virtual DbSet<PlatformFeeConfiguration> PlatformFeeConfigurations { get; set; }

    public virtual DbSet<Product> Products { get; set; }

    public virtual DbSet<ProductApproval> ProductApprovals { get; set; }

    public virtual DbSet<ProductApprovalHistory> ProductApprovalHistories { get; set; }

    public virtual DbSet<ProductImage> ProductImages { get; set; }

    public virtual DbSet<ProductReview> ProductReviews { get; set; }

    public virtual DbSet<Promotion> Promotions { get; set; }

    public virtual DbSet<Province> Provinces { get; set; }

    public virtual DbSet<PushNotificationCampaign> PushNotificationCampaigns { get; set; }

    public virtual DbSet<RefundRequest> RefundRequests { get; set; }

    public virtual DbSet<ReportTemplate> ReportTemplates { get; set; }

    public virtual DbSet<RolePermission> RolePermissions { get; set; }

    public virtual DbSet<SalesReport> SalesReports { get; set; }

    public virtual DbSet<SecurityLog> SecurityLogs { get; set; }

    public virtual DbSet<SellerSettlement> SellerSettlements { get; set; }

    public virtual DbSet<SellerStore> SellerStores { get; set; }

    public virtual DbSet<ShippingConfiguration> ShippingConfigurations { get; set; }

    public virtual DbSet<ShippingMethod> ShippingMethods { get; set; }

    public virtual DbSet<ShoppingCartItem> ShoppingCartItems { get; set; }

    public virtual DbSet<StockMovement> StockMovements { get; set; }

    public virtual DbSet<Store> Stores { get; set; }

    public virtual DbSet<SupportTicket> SupportTickets { get; set; }

    public virtual DbSet<SystemConfiguration> SystemConfigurations { get; set; }

    public virtual DbSet<SystemPromotion> SystemPromotions { get; set; }

    public virtual DbSet<TaxConfiguration> TaxConfigurations { get; set; }

    public virtual DbSet<TicketReply> TicketReplies { get; set; }

    public virtual DbSet<TwoFactorToken> TwoFactorTokens { get; set; }

    public virtual DbSet<UserSession> UserSessions { get; set; }

    public virtual DbSet<VwAdminRevenueReport> VwAdminRevenueReports { get; set; }

    public virtual DbSet<VwOrdersPaymentStatus> VwOrdersPaymentStatuses { get; set; }

    public virtual DbSet<VwPendingConfirmation> VwPendingConfirmations { get; set; }

    public virtual DbSet<Ward> Wards { get; set; }

    public virtual DbSet<Wishlist> Wishlists { get; set; }

    public virtual DbSet<WithdrawalRequest> WithdrawalRequests { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseNpgsql("Name=ConnectionStrings:DefaultConnection");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ActiveSession>(entity =>
        {
            entity.HasIndex(e => e.ExpiresAt, "IX_ActiveSessions_ExpiresAt");

            entity.HasIndex(e => e.IsActive, "IX_ActiveSessions_IsActive");

            entity.HasIndex(e => e.SessionId, "IX_ActiveSessions_SessionId").IsUnique();

            entity.HasIndex(e => e.UserId, "IX_ActiveSessions_UserId");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.IpAddress).HasMaxLength(45);
            entity.Property(e => e.LastActivity).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.SessionId).HasMaxLength(255);
            entity.Property(e => e.UserAgent).HasMaxLength(500);
            entity.Property(e => e.UserId).HasMaxLength(450);

            entity.HasOne(d => d.User).WithMany(p => p.ActiveSessions).HasForeignKey(d => d.UserId);
        });

        modelBuilder.Entity<Address>(entity =>
        {
            entity.HasIndex(e => e.UserId, "IX_Addresses_UserId");

            entity.Property(e => e.Id).ValueGeneratedNever();

            entity.HasOne(d => d.User).WithMany(p => p.Addresses).HasForeignKey(d => d.UserId);
        });

        modelBuilder.Entity<AnalyticsDatum>(entity =>
        {
            entity.HasIndex(e => e.UserId, "IX_AnalyticsData_UserId");

            entity.Property(e => e.Id).ValueGeneratedNever();

            entity.HasOne(d => d.User).WithMany(p => p.AnalyticsData).HasForeignKey(d => d.UserId);
        });

        modelBuilder.Entity<AspNetRole>(entity =>
        {
            entity.HasIndex(e => e.NormalizedName, "RoleNameIndex").IsUnique();

            entity.Property(e => e.Name).HasMaxLength(256);
            entity.Property(e => e.NormalizedName).HasMaxLength(256);
        });

        modelBuilder.Entity<AspNetRoleClaim>(entity =>
        {
            entity.HasIndex(e => e.RoleId, "IX_AspNetRoleClaims_RoleId");

            entity.HasOne(d => d.Role).WithMany(p => p.AspNetRoleClaims).HasForeignKey(d => d.RoleId);
        });

        modelBuilder.Entity<AspNetUser>(entity =>
        {
            entity.HasIndex(e => e.NormalizedEmail, "EmailIndex");

            entity.HasIndex(e => e.NormalizedUserName, "UserNameIndex").IsUnique();

            entity.Property(e => e.Email).HasMaxLength(256);
            entity.Property(e => e.NormalizedEmail).HasMaxLength(256);
            entity.Property(e => e.NormalizedUserName).HasMaxLength(256);
            entity.Property(e => e.UserName).HasMaxLength(256);

            entity.HasMany(d => d.Roles).WithMany(p => p.Users)
                .UsingEntity<Dictionary<string, object>>(
                    "AspNetUserRole",
                    r => r.HasOne<AspNetRole>().WithMany().HasForeignKey("RoleId"),
                    l => l.HasOne<AspNetUser>().WithMany().HasForeignKey("UserId"),
                    j =>
                    {
                        j.HasKey("UserId", "RoleId");
                        j.ToTable("AspNetUserRoles");
                        j.HasIndex(new[] { "RoleId" }, "IX_AspNetUserRoles_RoleId");
                    });
        });

        modelBuilder.Entity<AspNetUserClaim>(entity =>
        {
            entity.HasIndex(e => e.UserId, "IX_AspNetUserClaims_UserId");

            entity.HasOne(d => d.User).WithMany(p => p.AspNetUserClaims).HasForeignKey(d => d.UserId);
        });

        modelBuilder.Entity<AspNetUserLogin>(entity =>
        {
            entity.HasKey(e => new { e.LoginProvider, e.ProviderKey });

            entity.HasIndex(e => e.UserId, "IX_AspNetUserLogins_UserId");

            entity.HasOne(d => d.User).WithMany(p => p.AspNetUserLogins).HasForeignKey(d => d.UserId);
        });

        modelBuilder.Entity<AspNetUserToken>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.LoginProvider, e.Name });

            entity.HasOne(d => d.User).WithMany(p => p.AspNetUserTokens).HasForeignKey(d => d.UserId);
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasIndex(e => e.Action, "IX_AuditLogs_Action");

            entity.HasIndex(e => e.TargetUserId, "IX_AuditLogs_TargetUserId");

            entity.HasIndex(e => e.Timestamp, "IX_AuditLogs_Timestamp");

            entity.HasIndex(e => e.UserId, "IX_AuditLogs_UserId");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Action).HasMaxLength(100);
            entity.Property(e => e.Details).HasMaxLength(2000);
            entity.Property(e => e.IpAddress).HasMaxLength(45);
            entity.Property(e => e.Timestamp).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UserAgent).HasMaxLength(500);

            entity.HasOne(d => d.TargetUser).WithMany(p => p.AuditLogTargetUsers)
                .HasForeignKey(d => d.TargetUserId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(d => d.User).WithMany(p => p.AuditLogUsers)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<BlogCategory>(entity =>
        {
            entity.Property(e => e.Id).ValueGeneratedNever();
        });

        modelBuilder.Entity<BlogPost>(entity =>
        {
            entity.HasIndex(e => e.AuthorId, "IX_BlogPosts_AuthorId");

            entity.HasIndex(e => e.CategoryId, "IX_BlogPosts_CategoryId");

            entity.HasIndex(e => e.Slug, "IX_BlogPosts_Slug").IsUnique();

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.Title).HasMaxLength(255);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(d => d.Author).WithMany(p => p.BlogPosts)
                .HasForeignKey(d => d.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.Category).WithMany(p => p.BlogPosts)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Brand>(entity =>
        {
            entity.Property(e => e.Id).ValueGeneratedNever();
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasIndex(e => e.ParentId, "IX_Categories_ParentId");

            entity.HasIndex(e => e.Slug, "IX_Categories_Slug").IsUnique();

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(d => d.Parent).WithMany(p => p.InverseParent)
                .HasForeignKey(d => d.ParentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CategoryApprovalRule>(entity =>
        {
            entity.HasIndex(e => e.CategoryId, "IX_CategoryApprovalRules_CategoryId");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.ApprovalTier).HasMaxLength(50);

            entity.HasOne(d => d.Category).WithMany(p => p.CategoryApprovalRules).HasForeignKey(d => d.CategoryId);
        });

        modelBuilder.Entity<CheckoutSession>(entity =>
        {
            entity.HasIndex(e => e.ExpiresAt, "IX_CheckoutSessions_ExpiresAt");

            entity.HasIndex(e => e.Status, "IX_CheckoutSessions_Status");

            entity.HasIndex(e => e.UserId, "IX_CheckoutSessions_UserId");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.CouponCode).HasMaxLength(50);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.DiscountAmount).HasPrecision(18, 2);
            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.PaymentMethod).HasMaxLength(50);
            entity.Property(e => e.ShippingFee).HasPrecision(18, 2);
            entity.Property(e => e.ShippingMethod).HasMaxLength(50);
            entity.Property(e => e.Status).HasMaxLength(50);
            entity.Property(e => e.Tax).HasPrecision(18, 2);
            entity.Property(e => e.TotalAmount).HasPrecision(18, 2);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(d => d.User).WithMany(p => p.CheckoutSessions)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<CheckoutSessionItem>(entity =>
        {
            entity.HasIndex(e => e.CheckoutSessionId, "IX_CheckoutSessionItems_CheckoutSessionId");

            entity.HasIndex(e => e.ProductId, "IX_CheckoutSessionItems_ProductId");

            entity.Property(e => e.Color).HasMaxLength(50);
            entity.Property(e => e.ProductImage).HasMaxLength(500);
            entity.Property(e => e.ProductName).HasMaxLength(255);
            entity.Property(e => e.Size).HasMaxLength(20);
            entity.Property(e => e.TotalPrice).HasPrecision(18, 2);
            entity.Property(e => e.UnitPrice).HasPrecision(18, 2);

            entity.HasOne(d => d.CheckoutSession).WithMany(p => p.CheckoutSessionItems).HasForeignKey(d => d.CheckoutSessionId);

            entity.HasOne(d => d.Product).WithMany(p => p.CheckoutSessionItems).HasForeignKey(d => d.ProductId);
        });

        modelBuilder.Entity<ContactMessage>(entity =>
        {
            entity.HasIndex(e => e.CreatedAt, "IX_ContactMessages_CreatedAt");

            entity.HasIndex(e => e.Email, "IX_ContactMessages_Email");

            entity.HasIndex(e => e.IsRead, "IX_ContactMessages_IsRead");

            entity.HasIndex(e => e.UserId, "IX_ContactMessages_UserId");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.AdminNotes).HasMaxLength(2000);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.Message).HasMaxLength(5000);
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.Property(e => e.RepliedBy).HasMaxLength(255);
            entity.Property(e => e.Subject).HasMaxLength(255);

            entity.HasOne(d => d.User).WithMany(p => p.ContactMessages)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<ContentModeration>(entity =>
        {
            entity.HasIndex(e => e.ModeratedBy, "IX_ContentModerations_ModeratedBy");

            entity.HasIndex(e => e.SubmittedBy, "IX_ContentModerations_SubmittedBy");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.AutoModerationScore).HasPrecision(3, 2);
            entity.Property(e => e.ContentType).HasMaxLength(50);
            entity.Property(e => e.FlaggedReason).HasMaxLength(100);
            entity.Property(e => e.Status).HasMaxLength(50);

            entity.HasOne(d => d.ModeratedByNavigation).WithMany(p => p.ContentModerationModeratedByNavigations).HasForeignKey(d => d.ModeratedBy);

            entity.HasOne(d => d.SubmittedByNavigation).WithMany(p => p.ContentModerationSubmittedByNavigations).HasForeignKey(d => d.SubmittedBy);
        });

        modelBuilder.Entity<ConversionEvent>(entity =>
        {
            entity.HasIndex(e => e.OrderId, "IX_ConversionEvents_OrderId");

            entity.HasIndex(e => e.UserId, "IX_ConversionEvents_UserId");

            entity.Property(e => e.Id).ValueGeneratedNever();

            entity.HasOne(d => d.Order).WithMany(p => p.ConversionEvents).HasForeignKey(d => d.OrderId);

            entity.HasOne(d => d.User).WithMany(p => p.ConversionEvents).HasForeignKey(d => d.UserId);
        });

        modelBuilder.Entity<Coupon>(entity =>
        {
            entity.HasIndex(e => e.SellerId, "IX_Coupons_SellerId");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.SellerId).HasMaxLength(450);

            entity.HasOne(d => d.Seller).WithMany(p => p.Coupons).HasForeignKey(d => d.SellerId);
        });

        modelBuilder.Entity<Dispute>(entity =>
        {
            entity.HasIndex(e => e.CreatedAt, "IX_Disputes_CreatedAt");

            entity.HasIndex(e => e.CustomerId, "IX_Disputes_CustomerId");

            entity.HasIndex(e => e.DisputeNumber, "IX_Disputes_DisputeNumber").IsUnique();

            entity.HasIndex(e => e.OrderId, "IX_Disputes_OrderId");

            entity.HasIndex(e => e.ResolvedBy, "IX_Disputes_ResolvedBy");

            entity.HasIndex(e => e.SellerId, "IX_Disputes_SellerId");

            entity.HasIndex(e => e.Status, "IX_Disputes_Status");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.DisputeNumber).HasMaxLength(50);
            entity.Property(e => e.DisputedAmount).HasPrecision(18, 2);
            entity.Property(e => e.Reason).HasMaxLength(100);
            entity.Property(e => e.RefundAmount).HasPrecision(18, 2);
            entity.Property(e => e.ResolutionType).HasMaxLength(50);
            entity.Property(e => e.Status).HasMaxLength(50);

            entity.HasOne(d => d.Customer).WithMany(p => p.DisputeCustomers)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.Order).WithMany(p => p.Disputes)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.ResolvedByNavigation).WithMany(p => p.DisputeResolvedByNavigations)
                .HasForeignKey(d => d.ResolvedBy)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.Seller).WithMany(p => p.DisputeSellers)
                .HasForeignKey(d => d.SellerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<District>(entity =>
        {
            entity.HasIndex(e => e.ProvinceId, "IX_Districts_ProvinceId");

            entity.HasOne(d => d.Province).WithMany(p => p.Districts).HasForeignKey(d => d.ProvinceId);
        });

        modelBuilder.Entity<EmailCampaign>(entity =>
        {
            entity.HasIndex(e => e.CreatedBy, "IX_EmailCampaigns_CreatedBy");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Name).HasMaxLength(200);
            entity.Property(e => e.Status).HasMaxLength(50);
            entity.Property(e => e.Subject).HasMaxLength(500);
            entity.Property(e => e.TargetAudience).HasMaxLength(50);

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.EmailCampaigns).HasForeignKey(d => d.CreatedBy);
        });

        modelBuilder.Entity<EmailConfiguration>(entity =>
        {
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.FromEmail).HasMaxLength(255);
            entity.Property(e => e.FromName).HasMaxLength(200);
            entity.Property(e => e.Password).HasMaxLength(500);
            entity.Property(e => e.Provider).HasMaxLength(100);
            entity.Property(e => e.SmtpHost).HasMaxLength(255);
            entity.Property(e => e.Username).HasMaxLength(255);
        });

        modelBuilder.Entity<Faq>(entity =>
        {
            entity.ToTable("FAQs");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Category).HasMaxLength(50);
            entity.Property(e => e.Question).HasMaxLength(500);
        });

        modelBuilder.Entity<FlashSale>(entity =>
        {
            entity.HasIndex(e => e.CreatedBy, "IX_FlashSales_CreatedBy");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.BannerImageUrl).HasMaxLength(500);
            entity.Property(e => e.DiscountPercentage).HasPrecision(5, 2);
            entity.Property(e => e.Name).HasMaxLength(200);

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.FlashSales).HasForeignKey(d => d.CreatedBy);
        });

        modelBuilder.Entity<InventoryItem>(entity =>
        {
            entity.HasIndex(e => e.ProductId, "IX_InventoryItems_ProductId").IsUnique();

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.CostPrice).HasPrecision(10, 2);
            entity.Property(e => e.LastUpdated).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.Sku).HasColumnName("SKU");

            entity.HasOne(d => d.Product).WithOne(p => p.InventoryItem).HasForeignKey<InventoryItem>(d => d.ProductId);
        });

        modelBuilder.Entity<MarketingBanner>(entity =>
        {
            entity.HasIndex(e => e.CreatedBy, "IX_MarketingBanners_CreatedBy");

            entity.HasIndex(e => e.EndDate, "IX_MarketingBanners_EndDate");

            entity.HasIndex(e => e.IsActive, "IX_MarketingBanners_IsActive");

            entity.HasIndex(e => e.Position, "IX_MarketingBanners_Position");

            entity.HasIndex(e => e.StartDate, "IX_MarketingBanners_StartDate");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.ImageUrl).HasMaxLength(500);
            entity.Property(e => e.LinkUrl).HasMaxLength(500);
            entity.Property(e => e.MobileImageUrl).HasMaxLength(500);
            entity.Property(e => e.Position).HasMaxLength(50);
            entity.Property(e => e.TargetPage).HasMaxLength(100);
            entity.Property(e => e.Title).HasMaxLength(200);

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.MarketingBanners)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasIndex(e => e.CreatedAt, "IX_Notifications_CreatedAt");

            entity.HasIndex(e => e.IsRead, "IX_Notifications_IsRead");

            entity.HasIndex(e => e.Type, "IX_Notifications_Type");

            entity.HasIndex(e => e.UserId, "IX_Notifications_UserId");

            entity.HasIndex(e => new { e.UserId, e.IsRead }, "IX_Notifications_UserId_IsRead");

            entity.Property(e => e.ActionUrl).HasMaxLength(500);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.Message).HasMaxLength(1000);
            entity.Property(e => e.Title).HasMaxLength(255);
            entity.Property(e => e.Type).HasMaxLength(50);
            entity.Property(e => e.UserId).HasMaxLength(450);

            entity.HasOne(d => d.User).WithMany(p => p.Notifications).HasForeignKey(d => d.UserId);
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasIndex(e => e.IsRevenueCalculated, "IX_Orders_IsRevenueCalculated");

            entity.HasIndex(e => e.IsSellerConfirmed, "IX_Orders_IsSellerConfirmed");

            entity.HasIndex(e => e.IsUserConfirmedDelivery, "IX_Orders_IsUserConfirmedDelivery");

            entity.HasIndex(e => e.OrderNumber, "IX_Orders_OrderNumber").IsUnique();

            entity.HasIndex(e => e.SellerId, "IX_Orders_SellerId");

            entity.HasIndex(e => e.UserId, "IX_Orders_UserId");

            entity.HasIndex(e => new { e.Status, e.CreatedAt }, "idx_orders_status_created");

            entity.HasIndex(e => new { e.UserId, e.Status }, "idx_orders_user_status");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.DiscountAmount).HasPrecision(10, 2);
            entity.Property(e => e.IsRevenueCalculated)
                .HasDefaultValue(false)
                .HasComment("Đã tính revenue cho admin chưa");
            entity.Property(e => e.IsSellerConfirmed)
                .HasDefaultValue(false)
                .HasComment("Seller đã xác nhận đơn hàng chưa");
            entity.Property(e => e.IsUserConfirmedDelivery)
                .HasDefaultValue(false)
                .HasComment("User đã xác nhận nhận hàng chưa");
            entity.Property(e => e.RevenueCalculatedAt).HasColumnType("timestamp without time zone");
            entity.Property(e => e.SellerConfirmedAt).HasColumnType("timestamp without time zone");
            entity.Property(e => e.SellerConfirmedBy).HasMaxLength(450);
            entity.Property(e => e.SellerId).HasMaxLength(450);
            entity.Property(e => e.ShippingFee).HasPrecision(10, 2);
            entity.Property(e => e.Tax).HasPrecision(10, 2);
            entity.Property(e => e.TotalAmount).HasPrecision(10, 2);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UserConfirmedDeliveryAt).HasColumnType("timestamp without time zone");

            entity.HasOne(d => d.User).WithMany(p => p.Orders).HasForeignKey(d => d.UserId);
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasIndex(e => e.OrderId, "IX_OrderItems_OrderId");

            entity.HasIndex(e => e.ProductId, "IX_OrderItems_ProductId");

            entity.HasIndex(e => new { e.OrderId, e.ProductId }, "idx_order_items_order_product");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.ProductSku).HasColumnName("ProductSKU");
            entity.Property(e => e.TotalPrice).HasPrecision(10, 2);
            entity.Property(e => e.UnitPrice).HasPrecision(10, 2);

            entity.HasOne(d => d.Order).WithMany(p => p.OrderItems).HasForeignKey(d => d.OrderId);

            entity.HasOne(d => d.Product).WithMany(p => p.OrderItems)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<OrderRevenue>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("OrderRevenues_pkey");

            entity.ToTable(tb => tb.HasComment("Bảng lưu revenue chỉ khi user đã xác nhận nhận hàng"));

            entity.HasIndex(e => e.CalculatedAt, "IX_OrderRevenues_CalculatedAt");

            entity.HasIndex(e => e.OrderId, "IX_OrderRevenues_OrderId");

            entity.HasIndex(e => e.SellerId, "IX_OrderRevenues_SellerId");

            entity.HasIndex(e => e.UserId, "IX_OrderRevenues_UserId");

            entity.HasIndex(e => e.OrderId, "UQ_OrderRevenues_OrderId").IsUnique();

            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.CalculatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone");
            entity.Property(e => e.CommissionAmount).HasPrecision(18, 2);
            entity.Property(e => e.CommissionRate)
                .HasPrecision(5, 2)
                .HasDefaultValueSql("10.00");
            entity.Property(e => e.DiscountAmount)
                .HasPrecision(18, 2)
                .HasDefaultValueSql("0");
            entity.Property(e => e.NetRevenue).HasPrecision(18, 2);
            entity.Property(e => e.OrderNumber).HasMaxLength(50);
            entity.Property(e => e.SellerEarning).HasPrecision(18, 2);
            entity.Property(e => e.SellerId).HasMaxLength(450);
            entity.Property(e => e.ShippingFee)
                .HasPrecision(18, 2)
                .HasDefaultValueSql("0");
            entity.Property(e => e.Tax)
                .HasPrecision(18, 2)
                .HasDefaultValueSql("0");
            entity.Property(e => e.TotalAmount).HasPrecision(18, 2);
            entity.Property(e => e.UserId).HasMaxLength(450);

            entity.HasOne(d => d.Order).WithOne(p => p.OrderRevenue)
                .HasForeignKey<OrderRevenue>(d => d.OrderId)
                .HasConstraintName("OrderRevenues_OrderId_fkey");
        });

        modelBuilder.Entity<OrderStatusHistory>(entity =>
        {
            entity.HasIndex(e => e.ChangedBy, "IX_OrderStatusHistories_ChangedBy");

            entity.HasIndex(e => e.CreatedAt, "IX_OrderStatusHistories_CreatedAt");

            entity.HasIndex(e => e.OrderId, "IX_OrderStatusHistories_OrderId");

            entity.HasIndex(e => e.Status, "IX_OrderStatusHistories_Status");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.Notes).HasMaxLength(1000);
            entity.Property(e => e.Status).HasMaxLength(50);

            entity.HasOne(d => d.ChangedByNavigation).WithMany(p => p.OrderStatusHistories)
                .HasForeignKey(d => d.ChangedBy)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(d => d.Order).WithMany(p => p.OrderStatusHistories).HasForeignKey(d => d.OrderId);
        });

        modelBuilder.Entity<PageView>(entity =>
        {
            entity.HasIndex(e => e.SessionId1, "IX_PageViews_SessionId1");

            entity.HasIndex(e => e.UserId, "IX_PageViews_UserId");

            entity.Property(e => e.Id).ValueGeneratedNever();

            entity.HasOne(d => d.SessionId1Navigation).WithMany(p => p.PageViews).HasForeignKey(d => d.SessionId1);

            entity.HasOne(d => d.User).WithMany(p => p.PageViews).HasForeignKey(d => d.UserId);
        });

        modelBuilder.Entity<PasswordHistory>(entity =>
        {
            entity.HasIndex(e => e.CreatedAt, "IX_PasswordHistories_CreatedAt");

            entity.HasIndex(e => e.UserId, "IX_PasswordHistories_UserId");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.PasswordHash).HasMaxLength(500);
            entity.Property(e => e.UserId).HasMaxLength(450);

            entity.HasOne(d => d.User).WithMany(p => p.PasswordHistories).HasForeignKey(d => d.UserId);
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasIndex(e => e.OrderId, "IX_Payments_OrderId");

            entity.Property(e => e.Id).ValueGeneratedNever();

            entity.HasOne(d => d.Order).WithMany(p => p.Payments).HasForeignKey(d => d.OrderId);
        });

        modelBuilder.Entity<PaymentAttempt>(entity =>
        {
            entity.HasIndex(e => e.CreatedAt, "IX_PaymentAttempts_CreatedAt");

            entity.HasIndex(e => e.OrderId, "IX_PaymentAttempts_OrderId");

            entity.HasIndex(e => e.OrderId1, "IX_PaymentAttempts_OrderId1");

            entity.HasIndex(e => e.PaymentId, "IX_PaymentAttempts_PaymentId").IsUnique();

            entity.HasIndex(e => e.Status, "IX_PaymentAttempts_Status");

            entity.HasIndex(e => e.UserId, "IX_PaymentAttempts_UserId");

            entity.Property(e => e.Amount).HasPrecision(18, 2);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.Currency)
                .HasMaxLength(3)
                .HasDefaultValueSql("'VND'::character varying");
            entity.Property(e => e.IpAddress).HasMaxLength(45);
            entity.Property(e => e.OrderId).HasMaxLength(255);
            entity.Property(e => e.PaymentId).HasMaxLength(255);
            entity.Property(e => e.PaymentMethod).HasMaxLength(50);
            entity.Property(e => e.Status).HasMaxLength(50);
            entity.Property(e => e.TransactionId).HasMaxLength(255);
            entity.Property(e => e.UserAgent).HasMaxLength(500);

            entity.HasOne(d => d.OrderId1Navigation).WithMany(p => p.PaymentAttempts).HasForeignKey(d => d.OrderId1);

            entity.HasOne(d => d.User).WithMany(p => p.PaymentAttempts).HasForeignKey(d => d.UserId);
        });

        modelBuilder.Entity<PaymentGatewayConfiguration>(entity =>
        {
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.ApiKey).HasMaxLength(255);
            entity.Property(e => e.ApiSecret).HasMaxLength(500);
            entity.Property(e => e.ApiUrl).HasMaxLength(500);
            entity.Property(e => e.GatewayCode).HasMaxLength(50);
            entity.Property(e => e.GatewayName).HasMaxLength(100);
            entity.Property(e => e.LogoUrl).HasMaxLength(255);
            entity.Property(e => e.MerchantId).HasMaxLength(255);
            entity.Property(e => e.TransactionFeeFixed).HasPrecision(18, 2);
            entity.Property(e => e.TransactionFeePercent).HasPrecision(5, 2);
        });

        modelBuilder.Entity<PaymentMethod>(entity =>
        {
            entity.HasIndex(e => e.Code, "IX_PaymentMethods_Code").IsUnique();

            entity.HasIndex(e => e.IsActive, "IX_PaymentMethods_IsActive");

            entity.HasIndex(e => e.SortOrder, "IX_PaymentMethods_SortOrder");

            entity.Property(e => e.Code).HasMaxLength(50);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.IconUrl).HasMaxLength(255);
            entity.Property(e => e.MaxAmount).HasPrecision(18, 2);
            entity.Property(e => e.MinAmount).HasPrecision(18, 2);
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.SupportedCurrencies).HasMaxLength(100);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        modelBuilder.Entity<PaymentMethodConfig>(entity =>
        {
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Code).HasMaxLength(50);
            entity.Property(e => e.LogoUrl).HasMaxLength(255);
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.TransactionFeeFixed).HasPrecision(18, 2);
            entity.Property(e => e.TransactionFeePercent).HasPrecision(5, 2);
        });

        modelBuilder.Entity<PaymentTransaction>(entity =>
        {
            entity.HasIndex(e => e.CreatedAt, "IX_PaymentTransactions_CreatedAt");

            entity.HasIndex(e => e.OrderId, "IX_PaymentTransactions_OrderId");

            entity.HasIndex(e => e.SellerId, "IX_PaymentTransactions_SellerId");

            entity.HasIndex(e => e.Status, "IX_PaymentTransactions_Status");

            entity.HasIndex(e => e.UserId, "IX_PaymentTransactions_UserId");

            entity.HasIndex(e => new { e.SellerId, e.Status }, "idx_payment_transactions_seller_status");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Amount).HasPrecision(18, 2);
            entity.Property(e => e.PaymentGateway).HasMaxLength(100);
            entity.Property(e => e.PaymentMethod).HasMaxLength(50);
            entity.Property(e => e.PlatformFee).HasPrecision(18, 2);
            entity.Property(e => e.SellerAmount).HasPrecision(18, 2);
            entity.Property(e => e.Status).HasMaxLength(50);
            entity.Property(e => e.TransactionReference).HasMaxLength(255);

            entity.HasOne(d => d.Order).WithMany(p => p.PaymentTransactions)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.Seller).WithMany(p => p.PaymentTransactionSellers)
                .HasForeignKey(d => d.SellerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.User).WithMany(p => p.PaymentTransactionUsers)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PlatformFeeConfiguration>(entity =>
        {
            entity.HasIndex(e => e.CategoryId, "IX_PlatformFeeConfigurations_CategoryId");

            entity.HasIndex(e => e.IsActive, "IX_PlatformFeeConfigurations_IsActive");

            entity.HasIndex(e => e.SellerTier, "IX_PlatformFeeConfigurations_SellerTier");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.FeePercent).HasPrecision(5, 2);
            entity.Property(e => e.MaxFee).HasPrecision(18, 2);
            entity.Property(e => e.MinFee).HasPrecision(18, 2);
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.SellerTier).HasMaxLength(50);

            entity.HasOne(d => d.Category).WithMany(p => p.PlatformFeeConfigurations)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasIndex(e => e.BrandId, "IX_Products_BrandId");

            entity.HasIndex(e => e.CategoryId, "IX_Products_CategoryId");

            entity.HasIndex(e => e.Sku, "IX_Products_SKU").IsUnique();

            entity.HasIndex(e => e.SellerId, "IX_Products_SellerId");

            entity.HasIndex(e => e.Slug, "IX_Products_Slug").IsUnique();

            entity.HasIndex(e => new { e.BrandId, e.IsActive }, "idx_products_brand_active");

            entity.HasIndex(e => new { e.CategoryId, e.IsActive }, "idx_products_category_active");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.ApprovalStatus).HasDefaultValueSql("''::text");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.Name).HasMaxLength(255);
            entity.Property(e => e.Price).HasPrecision(10, 2);
            entity.Property(e => e.SalePrice).HasPrecision(10, 2);
            entity.Property(e => e.SellerId).HasMaxLength(450);
            entity.Property(e => e.Sku).HasColumnName("SKU");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(d => d.Brand).WithMany(p => p.Products)
                .HasForeignKey(d => d.BrandId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.Category).WithMany(p => p.Products)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.Seller).WithMany(p => p.Products).HasForeignKey(d => d.SellerId);
        });

        modelBuilder.Entity<ProductApproval>(entity =>
        {
            entity.HasIndex(e => e.ProductId, "IX_ProductApprovals_ProductId");

            entity.HasIndex(e => e.ReviewedBy, "IX_ProductApprovals_ReviewedBy");

            entity.HasIndex(e => e.SellerId, "IX_ProductApprovals_SellerId");

            entity.HasIndex(e => e.Status, "IX_ProductApprovals_Status");

            entity.HasIndex(e => e.SubmittedAt, "IX_ProductApprovals_SubmittedAt");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Priority).HasMaxLength(50);
            entity.Property(e => e.Status).HasMaxLength(50);

            entity.HasOne(d => d.Product).WithMany(p => p.ProductApprovals).HasForeignKey(d => d.ProductId);

            entity.HasOne(d => d.ReviewedByNavigation).WithMany(p => p.ProductApprovalReviewedByNavigations)
                .HasForeignKey(d => d.ReviewedBy)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.Seller).WithMany(p => p.ProductApprovalSellers)
                .HasForeignKey(d => d.SellerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ProductApprovalHistory>(entity =>
        {
            entity.HasIndex(e => e.CreatedAt, "IX_ProductApprovalHistories_CreatedAt");

            entity.HasIndex(e => e.PerformedBy, "IX_ProductApprovalHistories_PerformedBy");

            entity.HasIndex(e => e.ProductApprovalId, "IX_ProductApprovalHistories_ProductApprovalId");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Action).HasMaxLength(50);
            entity.Property(e => e.NewStatus).HasMaxLength(50);
            entity.Property(e => e.PerformedByType).HasMaxLength(50);
            entity.Property(e => e.PreviousStatus).HasMaxLength(50);

            entity.HasOne(d => d.PerformedByNavigation).WithMany(p => p.ProductApprovalHistories)
                .HasForeignKey(d => d.PerformedBy)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.ProductApproval).WithMany(p => p.ProductApprovalHistories).HasForeignKey(d => d.ProductApprovalId);
        });

        modelBuilder.Entity<ProductImage>(entity =>
        {
            entity.HasIndex(e => e.ProductId, "IX_ProductImages_ProductId");

            entity.Property(e => e.Id).ValueGeneratedNever();

            entity.HasOne(d => d.Product).WithMany(p => p.ProductImages).HasForeignKey(d => d.ProductId);
        });

        modelBuilder.Entity<ProductReview>(entity =>
        {
            entity.HasIndex(e => e.ProductId, "IX_ProductReviews_ProductId");

            entity.HasIndex(e => e.UserId, "IX_ProductReviews_UserId");

            entity.HasIndex(e => new { e.ProductId, e.IsApproved }, "idx_product_reviews_product_approved");

            entity.Property(e => e.Id).ValueGeneratedNever();

            entity.HasOne(d => d.Product).WithMany(p => p.ProductReviews).HasForeignKey(d => d.ProductId);

            entity.HasOne(d => d.User).WithMany(p => p.ProductReviews).HasForeignKey(d => d.UserId);
        });

        modelBuilder.Entity<Promotion>(entity =>
        {
            entity.HasIndex(e => e.Code, "IX_Promotions_Code").IsUnique();

            entity.HasIndex(e => e.EndDate, "IX_Promotions_EndDate");

            entity.HasIndex(e => e.IsActive, "IX_Promotions_IsActive");

            entity.HasIndex(e => e.StartDate, "IX_Promotions_StartDate");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Code).HasMaxLength(50);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.MaxDiscountAmount).HasPrecision(18, 2);
            entity.Property(e => e.MinOrderAmount).HasPrecision(18, 2);
            entity.Property(e => e.Name).HasMaxLength(255);
            entity.Property(e => e.Type).HasMaxLength(50);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.Value).HasPrecision(18, 2);
        });

        modelBuilder.Entity<PushNotificationCampaign>(entity =>
        {
            entity.HasIndex(e => e.CreatedBy, "IX_PushNotificationCampaigns_CreatedBy");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.ActionUrl).HasMaxLength(500);
            entity.Property(e => e.ImageUrl).HasMaxLength(500);
            entity.Property(e => e.Message).HasMaxLength(500);
            entity.Property(e => e.Status).HasMaxLength(50);
            entity.Property(e => e.TargetAudience).HasMaxLength(50);
            entity.Property(e => e.Title).HasMaxLength(200);

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.PushNotificationCampaigns).HasForeignKey(d => d.CreatedBy);
        });

        modelBuilder.Entity<RefundRequest>(entity =>
        {
            entity.HasIndex(e => e.OrderId, "IX_RefundRequests_OrderId");

            entity.HasIndex(e => e.OrderId1, "IX_RefundRequests_OrderId1");

            entity.HasIndex(e => e.PaymentId, "IX_RefundRequests_PaymentId");

            entity.HasIndex(e => e.RequestedBy, "IX_RefundRequests_RequestedBy");

            entity.HasIndex(e => e.Status, "IX_RefundRequests_Status");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.AdminNotes).HasMaxLength(1000);
            entity.Property(e => e.Amount).HasPrecision(18, 2);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.OrderId).HasMaxLength(255);
            entity.Property(e => e.PaymentId).HasMaxLength(255);
            entity.Property(e => e.Reason).HasMaxLength(1000);
            entity.Property(e => e.RefundTransactionId).HasMaxLength(255);
            entity.Property(e => e.Status).HasMaxLength(50);

            entity.HasOne(d => d.OrderId1Navigation).WithMany(p => p.RefundRequests).HasForeignKey(d => d.OrderId1);

            entity.HasOne(d => d.RequestedByNavigation).WithMany(p => p.RefundRequests).HasForeignKey(d => d.RequestedBy);
        });

        modelBuilder.Entity<ReportTemplate>(entity =>
        {
            entity.HasIndex(e => e.CreatedByUserId, "IX_ReportTemplates_CreatedByUserId");

            entity.Property(e => e.Id).ValueGeneratedNever();

            entity.HasOne(d => d.CreatedByUser).WithMany(p => p.ReportTemplates).HasForeignKey(d => d.CreatedByUserId);
        });

        modelBuilder.Entity<RolePermission>(entity =>
        {
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Module).HasMaxLength(100);
            entity.Property(e => e.Permission).HasMaxLength(100);
            entity.Property(e => e.RoleId).HasMaxLength(100);
        });

        modelBuilder.Entity<SalesReport>(entity =>
        {
            entity.HasIndex(e => e.GeneratedByUserId, "IX_SalesReports_GeneratedByUserId");

            entity.Property(e => e.Id).ValueGeneratedNever();

            entity.HasOne(d => d.GeneratedByUser).WithMany(p => p.SalesReports).HasForeignKey(d => d.GeneratedByUserId);
        });

        modelBuilder.Entity<SecurityLog>(entity =>
        {
            entity.HasIndex(e => e.CreatedAt, "IX_SecurityLogs_CreatedAt");

            entity.HasIndex(e => e.EventType, "IX_SecurityLogs_EventType");

            entity.HasIndex(e => e.IpAddress, "IX_SecurityLogs_IpAddress");

            entity.HasIndex(e => e.UserId, "IX_SecurityLogs_UserId");

            entity.HasIndex(e => new { e.UserId, e.EventType }, "IX_SecurityLogs_UserId_EventType");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.EventType).HasMaxLength(100);
            entity.Property(e => e.IpAddress).HasMaxLength(45);
            entity.Property(e => e.UserAgent).HasMaxLength(500);
            entity.Property(e => e.UserId).HasMaxLength(450);

            entity.HasOne(d => d.User).WithMany(p => p.SecurityLogs).HasForeignKey(d => d.UserId);
        });

        modelBuilder.Entity<SellerSettlement>(entity =>
        {
            entity.HasIndex(e => e.CreatedAt, "IX_SellerSettlements_CreatedAt");

            entity.HasIndex(e => e.SellerId, "IX_SellerSettlements_SellerId");

            entity.HasIndex(e => e.SettledBy, "IX_SellerSettlements_SettledBy");

            entity.HasIndex(e => e.SettlementNumber, "IX_SellerSettlements_SettlementNumber").IsUnique();

            entity.HasIndex(e => e.Status, "IX_SellerSettlements_Status");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.FinalBalance).HasPrecision(18, 2);
            entity.Property(e => e.NetAmount).HasPrecision(18, 2);
            entity.Property(e => e.PlatformFee).HasPrecision(18, 2);
            entity.Property(e => e.PreviousBalance).HasPrecision(18, 2);
            entity.Property(e => e.SettlementNumber).HasMaxLength(50);
            entity.Property(e => e.Status).HasMaxLength(50);
            entity.Property(e => e.TotalRevenue).HasPrecision(18, 2);

            entity.HasOne(d => d.Seller).WithMany(p => p.SellerSettlementSellers)
                .HasForeignKey(d => d.SellerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.SettledByNavigation).WithMany(p => p.SellerSettlementSettledByNavigations)
                .HasForeignKey(d => d.SettledBy)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SellerStore>(entity =>
        {
            entity.HasIndex(e => e.SellerId, "IX_SellerStores_SellerId");

            entity.HasIndex(e => e.StoreId, "IX_SellerStores_StoreId");

            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.AssignedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Role)
                .HasMaxLength(50)
                .HasDefaultValueSql("'Manager'::character varying");

            entity.HasOne(d => d.Seller).WithMany(p => p.SellerStores).HasForeignKey(d => d.SellerId);

            entity.HasOne(d => d.Store).WithMany(p => p.SellerStores).HasForeignKey(d => d.StoreId);
        });

        modelBuilder.Entity<ShippingConfiguration>(entity =>
        {
            entity.HasIndex(e => e.IsActive, "IX_ShippingConfigurations_IsActive");

            entity.HasIndex(e => e.ProviderCode, "IX_ShippingConfigurations_ProviderCode").IsUnique();

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.ApiKey).HasMaxLength(255);
            entity.Property(e => e.ApiSecret).HasMaxLength(255);
            entity.Property(e => e.ApiUrl).HasMaxLength(500);
            entity.Property(e => e.BaseRate).HasPrecision(18, 2);
            entity.Property(e => e.FreeShippingThreshold).HasPrecision(18, 2);
            entity.Property(e => e.LogoUrl).HasMaxLength(255);
            entity.Property(e => e.PerKgRate).HasPrecision(18, 2);
            entity.Property(e => e.ProviderCode).HasMaxLength(50);
            entity.Property(e => e.ProviderName).HasMaxLength(100);
        });

        modelBuilder.Entity<ShippingMethod>(entity =>
        {
            entity.HasIndex(e => e.Code, "IX_ShippingMethods_Code").IsUnique();

            entity.HasIndex(e => e.IsActive, "IX_ShippingMethods_IsActive");

            entity.HasIndex(e => e.SortOrder, "IX_ShippingMethods_SortOrder");

            entity.Property(e => e.AvailableRegions).HasMaxLength(1000);
            entity.Property(e => e.Code).HasMaxLength(50);
            entity.Property(e => e.Cost).HasPrecision(18, 2);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.MaxWeight).HasPrecision(10, 2);
            entity.Property(e => e.MinOrderAmount).HasPrecision(18, 2);
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        modelBuilder.Entity<ShoppingCartItem>(entity =>
        {
            entity.HasIndex(e => e.ProductId, "IX_ShoppingCartItems_ProductId");

            entity.HasIndex(e => e.UserId, "IX_ShoppingCartItems_UserId");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.ExpiresAt).HasDefaultValueSql("(now() + '7 days'::interval)");

            entity.HasOne(d => d.Product).WithMany(p => p.ShoppingCartItems).HasForeignKey(d => d.ProductId);

            entity.HasOne(d => d.User).WithMany(p => p.ShoppingCartItems).HasForeignKey(d => d.UserId);
        });

        modelBuilder.Entity<StockMovement>(entity =>
        {
            entity.HasIndex(e => e.InventoryItemId, "IX_StockMovements_InventoryItemId");

            entity.HasIndex(e => e.ProductId, "IX_StockMovements_ProductId");

            entity.HasIndex(e => e.UserId, "IX_StockMovements_UserId");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(d => d.InventoryItem).WithMany(p => p.StockMovements).HasForeignKey(d => d.InventoryItemId);

            entity.HasOne(d => d.Product).WithMany(p => p.StockMovements).HasForeignKey(d => d.ProductId);

            entity.HasOne(d => d.User).WithMany(p => p.StockMovements)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Store>(entity =>
        {
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Address).HasMaxLength(500);
            entity.Property(e => e.Brand).HasMaxLength(50);
            entity.Property(e => e.City).HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.ImageUrl).HasMaxLength(255);
            entity.Property(e => e.Name).HasMaxLength(255);
            entity.Property(e => e.Phone).HasMaxLength(50);
            entity.Property(e => e.Province).HasMaxLength(100);
            entity.Property(e => e.SocialMedia).HasMaxLength(100);
            entity.Property(e => e.StoreType).HasMaxLength(50);
            entity.Property(e => e.Website).HasMaxLength(255);
            entity.Property(e => e.WorkingHours).HasMaxLength(100);
        });

        modelBuilder.Entity<SupportTicket>(entity =>
        {
            entity.HasIndex(e => e.AssignedTo, "IX_SupportTickets_AssignedTo");

            entity.HasIndex(e => e.Category, "IX_SupportTickets_Category");

            entity.HasIndex(e => e.CreatedAt, "IX_SupportTickets_CreatedAt");

            entity.HasIndex(e => e.Priority, "IX_SupportTickets_Priority");

            entity.HasIndex(e => e.RelatedOrderId, "IX_SupportTickets_RelatedOrderId");

            entity.HasIndex(e => e.RelatedProductId, "IX_SupportTickets_RelatedProductId");

            entity.HasIndex(e => e.Status, "IX_SupportTickets_Status");

            entity.HasIndex(e => e.TicketNumber, "IX_SupportTickets_TicketNumber").IsUnique();

            entity.HasIndex(e => e.UserId, "IX_SupportTickets_UserId");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Category).HasMaxLength(50);
            entity.Property(e => e.Priority).HasMaxLength(20);
            entity.Property(e => e.Status).HasMaxLength(20);
            entity.Property(e => e.Subject).HasMaxLength(500);
            entity.Property(e => e.TicketNumber).HasMaxLength(50);
            entity.Property(e => e.UserType).HasMaxLength(20);

            entity.HasOne(d => d.AssignedToNavigation).WithMany(p => p.SupportTicketAssignedToNavigations)
                .HasForeignKey(d => d.AssignedTo)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.RelatedOrder).WithMany(p => p.SupportTickets)
                .HasForeignKey(d => d.RelatedOrderId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.RelatedProduct).WithMany(p => p.SupportTickets)
                .HasForeignKey(d => d.RelatedProductId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.User).WithMany(p => p.SupportTicketUsers)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SystemConfiguration>(entity =>
        {
            entity.HasIndex(e => e.Category, "IX_SystemConfigurations_Category");

            entity.HasIndex(e => e.Key, "IX_SystemConfigurations_Key").IsUnique();

            entity.HasIndex(e => e.UpdatedBy, "IX_SystemConfigurations_UpdatedBy");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Category).HasMaxLength(50);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Key).HasMaxLength(100);
            entity.Property(e => e.Type).HasMaxLength(50);

            entity.HasOne(d => d.UpdatedByNavigation).WithMany(p => p.SystemConfigurations).HasForeignKey(d => d.UpdatedBy);
        });

        modelBuilder.Entity<SystemPromotion>(entity =>
        {
            entity.HasIndex(e => e.Code, "IX_SystemPromotions_Code").IsUnique();

            entity.HasIndex(e => e.CreatedBy, "IX_SystemPromotions_CreatedBy");

            entity.HasIndex(e => e.EndDate, "IX_SystemPromotions_EndDate");

            entity.HasIndex(e => e.IsActive, "IX_SystemPromotions_IsActive");

            entity.HasIndex(e => e.StartDate, "IX_SystemPromotions_StartDate");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Code).HasMaxLength(50);
            entity.Property(e => e.MaxDiscountAmount).HasPrecision(18, 2);
            entity.Property(e => e.MinOrderAmount).HasPrecision(18, 2);
            entity.Property(e => e.Name).HasMaxLength(200);
            entity.Property(e => e.Type).HasMaxLength(50);
            entity.Property(e => e.Value).HasPrecision(18, 2);

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.SystemPromotions)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<TaxConfiguration>(entity =>
        {
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.Province).HasMaxLength(100);
            entity.Property(e => e.Rate).HasPrecision(5, 2);
            entity.Property(e => e.Region).HasMaxLength(100);
            entity.Property(e => e.TaxType).HasMaxLength(50);
        });

        modelBuilder.Entity<TicketReply>(entity =>
        {
            entity.HasIndex(e => e.CreatedAt, "IX_TicketReplies_CreatedAt");

            entity.HasIndex(e => e.TicketId, "IX_TicketReplies_TicketId");

            entity.HasIndex(e => e.UserId, "IX_TicketReplies_UserId");

            entity.Property(e => e.Id).ValueGeneratedNever();

            entity.HasOne(d => d.Ticket).WithMany(p => p.TicketReplies).HasForeignKey(d => d.TicketId);

            entity.HasOne(d => d.User).WithMany(p => p.TicketReplies)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<TwoFactorToken>(entity =>
        {
            entity.HasIndex(e => e.ExpiresAt, "IX_TwoFactorTokens_ExpiresAt");

            entity.HasIndex(e => e.IsUsed, "IX_TwoFactorTokens_IsUsed");

            entity.HasIndex(e => e.Token, "IX_TwoFactorTokens_Token");

            entity.HasIndex(e => e.UserId, "IX_TwoFactorTokens_UserId");

            entity.HasIndex(e => new { e.UserId, e.Purpose }, "IX_TwoFactorTokens_UserId_Purpose");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.Purpose).HasMaxLength(50);
            entity.Property(e => e.Token).HasMaxLength(100);
            entity.Property(e => e.UserId).HasMaxLength(450);

            entity.HasOne(d => d.User).WithMany(p => p.TwoFactorTokens).HasForeignKey(d => d.UserId);
        });

        modelBuilder.Entity<UserSession>(entity =>
        {
            entity.HasIndex(e => e.UserId, "IX_UserSessions_UserId");

            entity.Property(e => e.Id).ValueGeneratedNever();

            entity.HasOne(d => d.User).WithMany(p => p.UserSessions).HasForeignKey(d => d.UserId);
        });

        modelBuilder.Entity<VwAdminRevenueReport>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_admin_revenue_report");

            entity.Property(e => e.CommissionRate).HasPrecision(5, 2);
            entity.Property(e => e.CustomerEmail)
                .HasMaxLength(256)
                .HasColumnName("customer_email");
            entity.Property(e => e.DaysToRevenue).HasColumnName("days_to_revenue");
            entity.Property(e => e.DeliveredDate).HasColumnName("delivered_date");
            entity.Property(e => e.DiscountAmount).HasPrecision(18, 2);
            entity.Property(e => e.NetRevenue).HasPrecision(18, 2);
            entity.Property(e => e.OrderDate).HasColumnName("order_date");
            entity.Property(e => e.OrderNumber).HasMaxLength(50);
            entity.Property(e => e.PlatformRevenue)
                .HasPrecision(18, 2)
                .HasColumnName("platform_revenue");
            entity.Property(e => e.RevenueDate)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("revenue_date");
            entity.Property(e => e.RevenueId).HasColumnName("revenue_id");
            entity.Property(e => e.SellerEarning).HasPrecision(18, 2);
            entity.Property(e => e.SellerEmail)
                .HasMaxLength(256)
                .HasColumnName("seller_email");
            entity.Property(e => e.TotalAmount).HasPrecision(18, 2);
            entity.Property(e => e.UserConfirmedDate)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("user_confirmed_date");
        });

        modelBuilder.Entity<VwOrdersPaymentStatus>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_orders_payment_status");

            entity.Property(e => e.CanComplete).HasColumnName("can_complete");
            entity.Property(e => e.CanShip).HasColumnName("can_ship");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.OrderNumber).HasColumnName("order_number");
            entity.Property(e => e.OrderStatus).HasColumnName("order_status");
            entity.Property(e => e.PaymentMethod).HasColumnName("payment_method");
            entity.Property(e => e.PaymentStatus).HasColumnName("payment_status");
            entity.Property(e => e.TotalAmount)
                .HasPrecision(10, 2)
                .HasColumnName("total_amount");
            entity.Property(e => e.UserId).HasColumnName("user_id");
        });

        modelBuilder.Entity<VwPendingConfirmation>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_pending_confirmations");

            entity.Property(e => e.CustomerEmail)
                .HasMaxLength(256)
                .HasColumnName("customer_email");
            entity.Property(e => e.DaysPending).HasColumnName("days_pending");
            entity.Property(e => e.PendingAction).HasColumnName("pending_action");
            entity.Property(e => e.SellerConfirmedAt).HasColumnType("timestamp without time zone");
            entity.Property(e => e.TotalAmount).HasPrecision(10, 2);
            entity.Property(e => e.UserConfirmedDeliveryAt).HasColumnType("timestamp without time zone");
        });

        modelBuilder.Entity<Ward>(entity =>
        {
            entity.HasIndex(e => e.DistrictId, "IX_Wards_DistrictId");

            entity.HasOne(d => d.District).WithMany(p => p.Wards).HasForeignKey(d => d.DistrictId);
        });

        modelBuilder.Entity<Wishlist>(entity =>
        {
            entity.HasIndex(e => e.ProductId, "IX_Wishlists_ProductId");

            entity.HasIndex(e => e.UserId, "IX_Wishlists_UserId");

            entity.Property(e => e.Id).ValueGeneratedNever();

            entity.HasOne(d => d.Product).WithMany(p => p.Wishlists).HasForeignKey(d => d.ProductId);

            entity.HasOne(d => d.User).WithMany(p => p.Wishlists).HasForeignKey(d => d.UserId);
        });

        modelBuilder.Entity<WithdrawalRequest>(entity =>
        {
            entity.HasIndex(e => e.ProcessedBy, "IX_WithdrawalRequests_ProcessedBy");

            entity.HasIndex(e => e.RequestedAt, "IX_WithdrawalRequests_RequestedAt");

            entity.HasIndex(e => e.SellerId, "IX_WithdrawalRequests_SellerId");

            entity.HasIndex(e => e.Status, "IX_WithdrawalRequests_Status");

            entity.HasIndex(e => e.WithdrawalNumber, "IX_WithdrawalRequests_WithdrawalNumber").IsUnique();

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.AccountName).HasMaxLength(200);
            entity.Property(e => e.AccountNumber).HasMaxLength(50);
            entity.Property(e => e.Amount).HasPrecision(18, 2);
            entity.Property(e => e.BankName).HasMaxLength(100);
            entity.Property(e => e.Branch).HasMaxLength(100);
            entity.Property(e => e.Status).HasMaxLength(50);
            entity.Property(e => e.TransactionReference).HasMaxLength(255);
            entity.Property(e => e.WithdrawalNumber).HasMaxLength(50);

            entity.HasOne(d => d.ProcessedByNavigation).WithMany(p => p.WithdrawalRequestProcessedByNavigations)
                .HasForeignKey(d => d.ProcessedBy)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.Seller).WithMany(p => p.WithdrawalRequestSellers)
                .HasForeignKey(d => d.SellerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
