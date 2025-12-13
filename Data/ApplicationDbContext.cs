using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using JohnHenryFashionWeb.Models;

namespace JohnHenryFashionWeb.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Override SaveChangesAsync to automatically convert DateTime to UTC
        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            // Get all entities being tracked
            var entries = ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

            foreach (var entry in entries)
            {
                // Loop through all properties of the entity
                foreach (var property in entry.Properties)
                {
                    // Check if property is DateTime or DateTime?
                    if (property.CurrentValue != null)
                    {
                        if (property.Metadata.ClrType == typeof(DateTime))
                        {
                            var dateTime = (DateTime)property.CurrentValue;
                            // Convert to UTC if not already
                            if (dateTime.Kind == DateTimeKind.Unspecified)
                            {
                                property.CurrentValue = DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
                            }
                            else if (dateTime.Kind == DateTimeKind.Local)
                            {
                                property.CurrentValue = dateTime.ToUniversalTime();
                            }
                        }
                        else if (property.Metadata.ClrType == typeof(DateTime?))
                        {
                            var dateTime = (DateTime?)property.CurrentValue;
                            if (dateTime.HasValue)
                            {
                                if (dateTime.Value.Kind == DateTimeKind.Unspecified)
                                {
                                    property.CurrentValue = DateTime.SpecifyKind(dateTime.Value, DateTimeKind.Utc);
                                }
                                else if (dateTime.Value.Kind == DateTimeKind.Local)
                                {
                                    property.CurrentValue = dateTime.Value.ToUniversalTime();
                                }
                            }
                        }
                    }
                }
            }

            return base.SaveChangesAsync(cancellationToken);
        }

        // DbSets cho các bảng chính
        public DbSet<Product> Products { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Brand> Brands { get; set; }
        public DbSet<Store> Stores { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<ShoppingCartItem> ShoppingCartItems { get; set; }
        public DbSet<ProductImage> ProductImages { get; set; }
        public DbSet<ProductReview> ProductReviews { get; set; }
        public DbSet<Coupon> Coupons { get; set; }
        public DbSet<CouponUsage> CouponUsages { get; set; }
        public DbSet<SellerStore> SellerStores { get; set; }
        public DbSet<StoreSettings> StoreSettings { get; set; }
        public DbSet<StoreInventory> StoreInventories { get; set; }
        public DbSet<Wishlist> Wishlists { get; set; }
        public DbSet<Address> Addresses { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<BlogPost> BlogPosts { get; set; }
        public DbSet<BlogCategory> BlogCategories { get; set; }
        public DbSet<ContactMessage> ContactMessages { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<SecurityLog> SecurityLogs { get; set; }
        public DbSet<PasswordHistory> PasswordHistories { get; set; }
        public DbSet<ActiveSession> ActiveSessions { get; set; }
        public DbSet<TwoFactorToken> TwoFactorTokens { get; set; }
        
        // Payment entities
        public DbSet<PaymentAttempt> PaymentAttempts { get; set; }
        public DbSet<PaymentMethod> PaymentMethods { get; set; }
        public DbSet<CheckoutSession> CheckoutSessions { get; set; }
        public DbSet<CheckoutSessionItem> CheckoutSessionItems { get; set; }
        public DbSet<RefundRequest> RefundRequests { get; set; }
        public DbSet<ShippingMethod> ShippingMethods { get; set; }
        public DbSet<OrderStatusHistory> OrderStatusHistories { get; set; }
        public DbSet<OrderRevenue> OrderRevenues { get; set; }
        public DbSet<Promotion> Promotions { get; set; }
        
        // DbSets cho admin/inventory management
        public DbSet<InventoryItem> InventoryItems { get; set; }
        public DbSet<StockMovement> StockMovements { get; set; }
        
        // Analytics and Reporting entities
        public DbSet<UserSession> UserSessions { get; set; }
        public DbSet<PageView> PageViews { get; set; }
        public DbSet<ConversionEvent> ConversionEvents { get; set; }
        public DbSet<AnalyticsData> AnalyticsData { get; set; }
        public DbSet<SalesReport> SalesReports { get; set; }
        public DbSet<ReportTemplate> ReportTemplates { get; set; }
        
        // Audit and Security
        public DbSet<AuditLog> AuditLogs { get; set; }
        
        // Vietnamese Administrative Divisions
        public DbSet<Province> Provinces { get; set; }
        public DbSet<District> Districts { get; set; }
        public DbSet<Ward> Wards { get; set; }
        
        // Payment Management (New)
        public DbSet<PaymentTransaction> PaymentTransactions { get; set; }
        public DbSet<SellerSettlement> SellerSettlements { get; set; }
        public DbSet<WithdrawalRequest> WithdrawalRequests { get; set; }
        public DbSet<PaymentMethodConfig> PaymentMethodConfigs { get; set; }
        
        // Support System (New)
        public DbSet<SupportTicket> SupportTickets { get; set; }
        public DbSet<TicketReply> TicketReplies { get; set; }
        public DbSet<Dispute> Disputes { get; set; }
        public DbSet<FAQ> FAQs { get; set; }
        
        // Marketing (New)
        public DbSet<SystemPromotion> SystemPromotions { get; set; }
        public DbSet<MarketingBanner> MarketingBanners { get; set; }
        public DbSet<EmailCampaign> EmailCampaigns { get; set; }
        public DbSet<PushNotificationCampaign> PushNotificationCampaigns { get; set; }
        public DbSet<FlashSale> FlashSales { get; set; }
        
        // System Configuration (New)
        public DbSet<SystemConfiguration> SystemConfigurations { get; set; }
        public DbSet<ShippingConfiguration> ShippingConfigurations { get; set; }
        public DbSet<TaxConfiguration> TaxConfigurations { get; set; }
        public DbSet<EmailConfiguration> EmailConfigurations { get; set; }
        public DbSet<PaymentGatewayConfiguration> PaymentGatewayConfigurations { get; set; }
        public DbSet<RolePermission> RolePermissions { get; set; }
        public DbSet<PlatformFeeConfiguration> PlatformFeeConfigurations { get; set; }
        
        // Product Approval (New)
        public DbSet<ProductApproval> ProductApprovals { get; set; }
        public DbSet<ProductApprovalHistory> ProductApprovalHistories { get; set; }
        public DbSet<CategoryApprovalRule> CategoryApprovalRules { get; set; }
        public DbSet<ContentModeration> ContentModerations { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Cấu hình cho bảng Products
            modelBuilder.Entity<Product>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Price).HasColumnType("decimal(10,2)");
                entity.Property(e => e.SalePrice).HasColumnType("decimal(10,2)");
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                
                // Indexes
                entity.HasIndex(e => e.Slug).IsUnique();
                entity.HasIndex(e => e.SKU).IsUnique();
                entity.HasIndex(e => e.CategoryId);
                entity.HasIndex(e => e.BrandId);
                
                // Foreign Keys
                entity.HasOne(d => d.Category)
                    .WithMany(p => p.Products)
                    .HasForeignKey(d => d.CategoryId)
                    .OnDelete(DeleteBehavior.Restrict);
                    
                entity.HasOne(d => d.Brand)
                    .WithMany(p => p.Products)
                    .HasForeignKey(d => d.BrandId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Cấu hình cho bảng Categories
            modelBuilder.Entity<Category>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                
                entity.HasIndex(e => e.Slug).IsUnique();
                
                // Self-referencing relationship
                entity.HasOne(d => d.Parent)
                    .WithMany(p => p.Children)
                    .HasForeignKey(d => d.ParentId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Cấu hình cho bảng Orders
            modelBuilder.Entity<Order>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.TotalAmount).HasColumnType("decimal(10,2)");
                entity.Property(e => e.ShippingFee).HasColumnType("decimal(10,2)");
                entity.Property(e => e.Tax).HasColumnType("decimal(10,2)");
                entity.Property(e => e.DiscountAmount).HasColumnType("decimal(10,2)");
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                
                entity.HasIndex(e => e.OrderNumber).IsUnique();
                entity.HasIndex(e => e.UserId);
                
                // Foreign Key
                entity.HasOne(d => d.User)
                    .WithMany(p => p.Orders)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Cấu hình cho bảng OrderItems
            modelBuilder.Entity<OrderItem>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.UnitPrice).HasColumnType("decimal(10,2)");
                entity.Property(e => e.TotalPrice).HasColumnType("decimal(10,2)");
                
                entity.HasIndex(e => e.OrderId);
                entity.HasIndex(e => e.ProductId);
                
                entity.HasOne(d => d.Order)
                    .WithMany(p => p.OrderItems)
                    .HasForeignKey(d => d.OrderId)
                    .OnDelete(DeleteBehavior.Cascade);
                    
                entity.HasOne(d => d.Product)
                    .WithMany(p => p.OrderItems)
                    .HasForeignKey(d => d.ProductId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Cấu hình cho BlogPost
            modelBuilder.Entity<BlogPost>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(255);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                
                entity.HasIndex(e => e.Slug).IsUnique();
                entity.HasIndex(e => e.CategoryId);
                entity.HasIndex(e => e.AuthorId);
                
                entity.HasOne(d => d.Category)
                    .WithMany(p => p.BlogPosts)
                    .HasForeignKey(d => d.CategoryId)
                    .OnDelete(DeleteBehavior.Restrict);
                    
                entity.HasOne(d => d.Author)
                    .WithMany(p => p.BlogPosts)
                    .HasForeignKey(d => d.AuthorId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Cấu hình cho InventoryItem
            modelBuilder.Entity<InventoryItem>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.CostPrice).HasColumnType("decimal(10,2)");
                entity.Property(e => e.LastUpdated).HasDefaultValueSql("CURRENT_TIMESTAMP");
                
                entity.HasIndex(e => e.ProductId).IsUnique();
                
                entity.HasOne(d => d.Product)
                    .WithOne()
                    .HasForeignKey<InventoryItem>(d => d.ProductId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Cấu hình cho StockMovement
            modelBuilder.Entity<StockMovement>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                
                entity.HasIndex(e => e.ProductId);
                entity.HasIndex(e => e.UserId);
                
                entity.HasOne(d => d.Product)
                    .WithMany()
                    .HasForeignKey(d => d.ProductId)
                    .OnDelete(DeleteBehavior.Cascade);
                    
                entity.HasOne(d => d.User)
                    .WithMany()
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Cấu hình cho bảng ContactMessages
            modelBuilder.Entity<ContactMessage>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Phone).HasMaxLength(20);
                entity.Property(e => e.Subject).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Message).IsRequired().HasMaxLength(5000);
                entity.Property(e => e.AdminNotes).HasMaxLength(2000);
                entity.Property(e => e.RepliedBy).HasMaxLength(255);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                
                // Optional user reference
                entity.HasOne(d => d.User)
                    .WithMany()
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.SetNull);
                    
                // Indexes
                entity.HasIndex(e => e.Email);
                entity.HasIndex(e => e.CreatedAt);
                entity.HasIndex(e => e.IsRead);
            });

            // Cấu hình cho bảng Notifications
            modelBuilder.Entity<Notification>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.UserId).IsRequired().HasMaxLength(450);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Message).IsRequired().HasMaxLength(1000);
                entity.Property(e => e.Type).IsRequired().HasMaxLength(50);
                entity.Property(e => e.ActionUrl).HasMaxLength(500);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                
                // Foreign key relationship
                entity.HasOne(d => d.User)
                    .WithMany(u => u.Notifications)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
                    
                // Indexes
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.Type);
                entity.HasIndex(e => e.IsRead);
                entity.HasIndex(e => e.CreatedAt);
                entity.HasIndex(e => new { e.UserId, e.IsRead });
            });

            // Cấu hình cho bảng SecurityLogs
            modelBuilder.Entity<SecurityLog>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.UserId).IsRequired().HasMaxLength(450);
                entity.Property(e => e.EventType).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Description).IsRequired().HasMaxLength(1000);
                entity.Property(e => e.IpAddress).HasMaxLength(45);
                entity.Property(e => e.UserAgent).HasMaxLength(500);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                
                // Foreign key relationship
                entity.HasOne(d => d.User)
                    .WithMany(u => u.SecurityLogs)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
                    
                // Indexes
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.EventType);
                entity.HasIndex(e => e.IpAddress);
                entity.HasIndex(e => e.CreatedAt);
                entity.HasIndex(e => new { e.UserId, e.EventType });
            });

            // Cấu hình cho bảng PasswordHistories
            modelBuilder.Entity<PasswordHistory>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.UserId).IsRequired().HasMaxLength(450);
                entity.Property(e => e.PasswordHash).IsRequired().HasMaxLength(500);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                
                // Foreign key relationship
                entity.HasOne(d => d.User)
                    .WithMany(u => u.PasswordHistories)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
                    
                // Indexes
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.CreatedAt);
            });

            // Cấu hình cho bảng ActiveSessions
            modelBuilder.Entity<ActiveSession>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.SessionId).IsRequired().HasMaxLength(255);
                entity.Property(e => e.UserId).IsRequired().HasMaxLength(450);
                entity.Property(e => e.IpAddress).HasMaxLength(45);
                entity.Property(e => e.UserAgent).HasMaxLength(500);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.LastActivity).HasDefaultValueSql("CURRENT_TIMESTAMP");
                
                // Foreign key relationship
                entity.HasOne(d => d.User)
                    .WithMany(u => u.ActiveSessions)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
                    
                // Indexes
                entity.HasIndex(e => e.SessionId).IsUnique();
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.ExpiresAt);
                entity.HasIndex(e => e.IsActive);
            });

            // Cấu hình cho bảng TwoFactorTokens
            modelBuilder.Entity<TwoFactorToken>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.UserId).IsRequired().HasMaxLength(450);
                entity.Property(e => e.Token).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Purpose).IsRequired().HasMaxLength(50);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                
                // Foreign key relationship
                entity.HasOne(d => d.User)
                    .WithMany(u => u.TwoFactorTokens)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
                    
                // Indexes
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.Token);
                entity.HasIndex(e => e.ExpiresAt);
                entity.HasIndex(e => e.IsUsed);
                entity.HasIndex(e => new { e.UserId, e.Purpose });
            });

            // Cấu hình cho bảng RefundRequests
            modelBuilder.Entity<RefundRequest>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Amount).HasColumnType("decimal(10,2)");
                entity.Property(e => e.Reason).IsRequired().HasMaxLength(1000);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                
                // Foreign key relationships
                entity.HasOne(d => d.Order)
                    .WithMany()
                    .HasForeignKey(d => d.OrderId)
                    .OnDelete(DeleteBehavior.Restrict);
                    
                entity.HasOne(d => d.RequestedByUser)
                    .WithMany()
                    .HasForeignKey(d => d.RequestedBy)
                    .OnDelete(DeleteBehavior.Restrict);
                    
                // Indexes
                entity.HasIndex(e => e.OrderId);
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.RequestedBy);
                entity.HasIndex(e => e.CreatedAt);
            });

            // Seed data
            SeedData(modelBuilder);
        }

        private void SeedData(ModelBuilder modelBuilder)
        {
            // Static datetime for seeding
            var seedDateTime = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            
            // Seed Categories
            modelBuilder.Entity<Category>().HasData(
                new Category
                {
                    Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    Name = "Thời trang nam",
                    Slug = "thoi-trang-nam",
                    Description = "Thời trang dành cho nam giới",
                    IsActive = true,
                    SortOrder = 1,
                    CreatedAt = seedDateTime,
                    UpdatedAt = seedDateTime
                },
                new Category
                {
                    Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    Name = "Thời trang nữ",
                    Slug = "thoi-trang-nu",
                    Description = "Thời trang dành cho nữ giới",
                    IsActive = true,
                    SortOrder = 2,
                    CreatedAt = seedDateTime,
                    UpdatedAt = seedDateTime
                }
            );

            // Seed Brands
            modelBuilder.Entity<Brand>().HasData(
                new Brand
                {
                    Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                    Name = "John Henry",
                    Slug = "john-henry",
                    Description = "Thương hiệu thời trang cao cấp John Henry",
                    IsActive = true,
                    CreatedAt = seedDateTime,
                    UpdatedAt = seedDateTime
                }
            );

            // Seed Blog Categories
            modelBuilder.Entity<BlogCategory>().HasData(
                new BlogCategory
                {
                    Id = Guid.Parse("44444444-4444-4444-4444-444444444444"),
                    Name = "Xu hướng thời trang",
                    Slug = "xu-huong-thoi-trang",
                    Description = "Các xu hướng thời trang mới nhất",
                    IsActive = true,
                    SortOrder = 1,
                    CreatedAt = seedDateTime,
                    UpdatedAt = seedDateTime
                }
            );
            
            // Payment entity configurations
            ConfigurePaymentEntities(modelBuilder);
            
            // New system configurations
            ConfigureNewSystemEntities(modelBuilder);
        }
        
        private void ConfigureNewSystemEntities(ModelBuilder modelBuilder)
        {
            // Payment Transaction configuration
            modelBuilder.Entity<PaymentTransaction>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.OrderId);
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.SellerId);
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.CreatedAt);
                
                entity.HasOne(e => e.Order)
                    .WithMany()
                    .HasForeignKey(e => e.OrderId)
                    .OnDelete(DeleteBehavior.Restrict);
                    
                entity.HasOne(e => e.Customer)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
                    
                entity.HasOne(e => e.Seller)
                    .WithMany()
                    .HasForeignKey(e => e.SellerId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Seller Settlement configuration
            modelBuilder.Entity<SellerSettlement>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.SellerId);
                entity.HasIndex(e => e.SettlementNumber).IsUnique();
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.CreatedAt);
                
                entity.HasOne(e => e.Seller)
                    .WithMany()
                    .HasForeignKey(e => e.SellerId)
                    .OnDelete(DeleteBehavior.Restrict);
                    
                entity.HasOne(e => e.SettlementAdmin)
                    .WithMany()
                    .HasForeignKey(e => e.SettledBy)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Withdrawal Request configuration
            modelBuilder.Entity<WithdrawalRequest>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.SellerId);
                entity.HasIndex(e => e.WithdrawalNumber).IsUnique();
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.RequestedAt);
                
                entity.HasOne(e => e.Seller)
                    .WithMany()
                    .HasForeignKey(e => e.SellerId)
                    .OnDelete(DeleteBehavior.Restrict);
                    
                entity.HasOne(e => e.Processor)
                    .WithMany()
                    .HasForeignKey(e => e.ProcessedBy)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Support Ticket configuration
            modelBuilder.Entity<SupportTicket>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.TicketNumber).IsUnique();
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.Category);
                entity.HasIndex(e => e.Priority);
                entity.HasIndex(e => e.CreatedAt);
                
                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
                    
                entity.HasOne(e => e.AssignedAdmin)
                    .WithMany()
                    .HasForeignKey(e => e.AssignedTo)
                    .OnDelete(DeleteBehavior.Restrict);
                    
                entity.HasOne(e => e.RelatedOrder)
                    .WithMany()
                    .HasForeignKey(e => e.RelatedOrderId)
                    .OnDelete(DeleteBehavior.Restrict);
                    
                entity.HasOne(e => e.RelatedProduct)
                    .WithMany()
                    .HasForeignKey(e => e.RelatedProductId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Ticket Reply configuration
            modelBuilder.Entity<TicketReply>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.TicketId);
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.CreatedAt);
                
                entity.HasOne(e => e.Ticket)
                    .WithMany(t => t.Replies)
                    .HasForeignKey(e => e.TicketId)
                    .OnDelete(DeleteBehavior.Cascade);
                    
                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Dispute configuration
            modelBuilder.Entity<Dispute>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.DisputeNumber).IsUnique();
                entity.HasIndex(e => e.OrderId);
                entity.HasIndex(e => e.CustomerId);
                entity.HasIndex(e => e.SellerId);
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.CreatedAt);
                
                entity.HasOne(e => e.Order)
                    .WithMany()
                    .HasForeignKey(e => e.OrderId)
                    .OnDelete(DeleteBehavior.Restrict);
                    
                entity.HasOne(e => e.Customer)
                    .WithMany()
                    .HasForeignKey(e => e.CustomerId)
                    .OnDelete(DeleteBehavior.Restrict);
                    
                entity.HasOne(e => e.Seller)
                    .WithMany()
                    .HasForeignKey(e => e.SellerId)
                    .OnDelete(DeleteBehavior.Restrict);
                    
                entity.HasOne(e => e.Resolver)
                    .WithMany()
                    .HasForeignKey(e => e.ResolvedBy)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // System Promotion configuration
            modelBuilder.Entity<SystemPromotion>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Code).IsUnique();
                entity.HasIndex(e => e.IsActive);
                entity.HasIndex(e => e.StartDate);
                entity.HasIndex(e => e.EndDate);
                
                entity.HasOne(e => e.Creator)
                    .WithMany()
                    .HasForeignKey(e => e.CreatedBy)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Marketing Banner configuration
            modelBuilder.Entity<MarketingBanner>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Position);
                entity.HasIndex(e => e.IsActive);
                entity.HasIndex(e => e.StartDate);
                entity.HasIndex(e => e.EndDate);
                
                entity.HasOne(e => e.Creator)
                    .WithMany()
                    .HasForeignKey(e => e.CreatedBy)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Product Approval configuration
            modelBuilder.Entity<ProductApproval>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.ProductId);
                entity.HasIndex(e => e.SellerId);
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.SubmittedAt);
                
                entity.HasOne(e => e.Product)
                    .WithMany()
                    .HasForeignKey(e => e.ProductId)
                    .OnDelete(DeleteBehavior.Cascade);
                    
                entity.HasOne(e => e.Seller)
                    .WithMany()
                    .HasForeignKey(e => e.SellerId)
                    .OnDelete(DeleteBehavior.Restrict);
                    
                entity.HasOne(e => e.Reviewer)
                    .WithMany()
                    .HasForeignKey(e => e.ReviewedBy)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Product Approval History configuration
            modelBuilder.Entity<ProductApprovalHistory>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.ProductApprovalId);
                entity.HasIndex(e => e.CreatedAt);
                
                entity.HasOne(e => e.ProductApproval)
                    .WithMany(pa => pa.History)
                    .HasForeignKey(e => e.ProductApprovalId)
                    .OnDelete(DeleteBehavior.Cascade);
                    
                entity.HasOne(e => e.Performer)
                    .WithMany()
                    .HasForeignKey(e => e.PerformedBy)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // System Configuration
            modelBuilder.Entity<SystemConfiguration>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Key).IsUnique();
                entity.HasIndex(e => e.Category);
            });

            // Shipping Configuration
            modelBuilder.Entity<ShippingConfiguration>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.ProviderCode).IsUnique();
                entity.HasIndex(e => e.IsActive);
            });

            // Platform Fee Configuration
            modelBuilder.Entity<PlatformFeeConfiguration>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.SellerTier);
                entity.HasIndex(e => e.CategoryId);
                entity.HasIndex(e => e.IsActive);
                
                entity.HasOne(e => e.Category)
                    .WithMany()
                    .HasForeignKey(e => e.CategoryId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }

        private void ConfigurePaymentEntities(ModelBuilder modelBuilder)
        {
            // PaymentAttempt configuration
            modelBuilder.Entity<PaymentAttempt>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.PaymentId).IsRequired().HasMaxLength(255);
                entity.Property(e => e.OrderId).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Amount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Currency).HasMaxLength(3).HasDefaultValue("VND");
                entity.Property(e => e.PaymentMethod).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
                entity.Property(e => e.TransactionId).HasMaxLength(255);
                entity.Property(e => e.IpAddress).HasMaxLength(45);
                entity.Property(e => e.UserAgent).HasMaxLength(500);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasIndex(e => e.PaymentId).IsUnique();
                entity.HasIndex(e => e.OrderId);
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.CreatedAt);

                entity.HasOne(e => e.User)
                    .WithMany(u => u.PaymentAttempts)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                // OrderId stores Order.OrderNumber as string reference (no FK constraint)
                entity.Property(e => e.OrderId).IsRequired().HasMaxLength(255);
            });

            // PaymentMethod configuration
            modelBuilder.Entity<PaymentMethod>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Code).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.IconUrl).HasMaxLength(255);
                entity.Property(e => e.MinAmount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.MaxAmount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.SupportedCurrencies).HasMaxLength(100);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasIndex(e => e.Code).IsUnique();
                entity.HasIndex(e => e.IsActive);
                entity.HasIndex(e => e.SortOrder);
            });

            // CheckoutSession configuration
            modelBuilder.Entity<CheckoutSession>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Email).HasMaxLength(255);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
                entity.Property(e => e.TotalAmount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.ShippingFee).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Tax).HasColumnType("decimal(18,2)");
                entity.Property(e => e.DiscountAmount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.CouponCode).HasMaxLength(50);
                entity.Property(e => e.ShippingMethod).HasMaxLength(50);
                entity.Property(e => e.PaymentMethod).HasMaxLength(50);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.ExpiresAt);

                entity.HasOne(e => e.User)
                    .WithMany(u => u.CheckoutSessions)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // CheckoutSessionItem configuration
            modelBuilder.Entity<CheckoutSessionItem>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.UnitPrice).HasColumnType("decimal(18,2)");
                entity.Property(e => e.TotalPrice).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Size).HasMaxLength(20);
                entity.Property(e => e.Color).HasMaxLength(50);
                entity.Property(e => e.ProductName).HasMaxLength(255);
                entity.Property(e => e.ProductImage).HasMaxLength(500);

                entity.HasIndex(e => e.CheckoutSessionId);
                entity.HasIndex(e => e.ProductId);

                entity.HasOne(e => e.CheckoutSession)
                    .WithMany(cs => cs.Items)
                    .HasForeignKey(e => e.CheckoutSessionId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Product)
                    .WithMany()
                    .HasForeignKey(e => e.ProductId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // RefundRequest configuration - Moved to OnModelCreating (line 372-396)
            // Configuration is already defined earlier in OnModelCreating method

            // ShippingMethod configuration
            modelBuilder.Entity<ShippingMethod>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Code).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.Cost).HasColumnType("decimal(18,2)");
                entity.Property(e => e.MinOrderAmount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.MaxWeight).HasColumnType("decimal(10,2)");
                entity.Property(e => e.AvailableRegions).HasMaxLength(1000);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasIndex(e => e.Code).IsUnique();
                entity.HasIndex(e => e.IsActive);
                entity.HasIndex(e => e.SortOrder);
            });

            // OrderStatusHistory configuration
            modelBuilder.Entity<OrderStatusHistory>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Notes).HasMaxLength(1000);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasIndex(e => e.OrderId);
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.CreatedAt);

                entity.HasOne(e => e.Order)
                    .WithMany()
                    .HasForeignKey(e => e.OrderId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.ChangedByUser)
                    .WithMany()
                    .HasForeignKey(e => e.ChangedBy)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Promotion configuration
            modelBuilder.Entity<Promotion>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Code).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Type).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Value).HasColumnType("decimal(18,2)");
                entity.Property(e => e.MinOrderAmount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.MaxDiscountAmount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Description).HasMaxLength(1000);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasIndex(e => e.Code).IsUnique();
                entity.HasIndex(e => e.IsActive);
                entity.HasIndex(e => e.StartDate);
                entity.HasIndex(e => e.EndDate);
            });

            // AuditLog configuration
            modelBuilder.Entity<AuditLog>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Action).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Details).HasMaxLength(2000);
                entity.Property(e => e.IpAddress).HasMaxLength(45); // IPv6 support
                entity.Property(e => e.UserAgent).HasMaxLength(500);
                entity.Property(e => e.Timestamp).HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(e => e.TargetUser)
                    .WithMany()
                    .HasForeignKey(e => e.TargetUserId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.TargetUserId);
                entity.HasIndex(e => e.Action);
                entity.HasIndex(e => e.Timestamp);
            });

            // PageView configuration
            modelBuilder.Entity<PageView>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Page).IsRequired().HasMaxLength(500);
                entity.Property(e => e.Referrer).HasMaxLength(500);
                entity.Property(e => e.UserAgent).HasMaxLength(1000);
                entity.Property(e => e.IpAddress).HasMaxLength(45);
                entity.Property(e => e.Source).HasMaxLength(100);
                entity.Property(e => e.Medium).HasMaxLength(100);
                entity.Property(e => e.Campaign).HasMaxLength(100);
                entity.Property(e => e.ExitPage).HasMaxLength(500);

                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.SessionId);
                entity.HasIndex(e => e.ViewedAt);

                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(e => e.Session)
                    .WithMany(s => s.PageViews)
                    .HasForeignKey(e => e.SessionId)
                    .HasPrincipalKey(s => s.SessionId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Cấu hình cho bảng ProductReviews
            modelBuilder.Entity<ProductReview>(entity =>
            {
                entity.HasKey(e => e.Id);
                
                entity.HasOne(e => e.Product)
                    .WithMany(p => p.ProductReviews)
                    .HasForeignKey(e => e.ProductId)
                    .OnDelete(DeleteBehavior.Cascade);
                    
                entity.HasOne(e => e.User)
                    .WithMany(u => u.ProductReviews)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
                    
                entity.Property(e => e.Rating)
                    .IsRequired();
                    
                entity.Property(e => e.Comment)
                    .HasMaxLength(1000);
                    
                // entity.Property(e => e.SellerResponse)
                //     .HasMaxLength(1000);
            });
        }
    }
}
