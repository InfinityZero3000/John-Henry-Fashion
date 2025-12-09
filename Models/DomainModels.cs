using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JohnHenryFashionWeb.Models
{
    public partial class ApplicationUser : IdentityUser
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public string? Avatar { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastLoginDate { get; set; }

        // Computed properties
        public string FullName => $"{FirstName} {LastName}".Trim();

        // Navigation properties
        public ICollection<Order> Orders { get; set; } = new List<Order>();
        public ICollection<ProductReview> ProductReviews { get; set; } = new List<ProductReview>();
        public ICollection<Wishlist> Wishlists { get; set; } = new List<Wishlist>();
        public ICollection<ShoppingCartItem> ShoppingCartItems { get; set; } = new List<ShoppingCartItem>();
        public ICollection<Address> Addresses { get; set; } = new List<Address>();
        public ICollection<BlogPost> BlogPosts { get; set; } = new List<BlogPost>();
        public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
        public ICollection<SecurityLog> SecurityLogs { get; set; } = new List<SecurityLog>();
        public ICollection<PasswordHistory> PasswordHistories { get; set; } = new List<PasswordHistory>();
        public ICollection<ActiveSession> ActiveSessions { get; set; } = new List<ActiveSession>();
        public ICollection<TwoFactorToken> TwoFactorTokens { get; set; } = new List<TwoFactorToken>();
        public ICollection<PaymentAttempt> PaymentAttempts { get; set; } = new List<PaymentAttempt>();
        public ICollection<CheckoutSession> CheckoutSessions { get; set; } = new List<CheckoutSession>();
        public ICollection<RefundRequest> RefundRequests { get; set; } = new List<RefundRequest>();
    }

    public class Category
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public Guid? ParentId { get; set; }
        public bool IsActive { get; set; } = true;
        public int SortOrder { get; set; } = 0;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public Category? Parent { get; set; }
        public ICollection<Category> Children { get; set; } = new List<Category>();
        public ICollection<Product> Products { get; set; } = new List<Product>();
    }

    public class Brand
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? LogoUrl { get; set; }
        public string? Website { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public ICollection<Product> Products { get; set; } = new List<Product>();
    }

    public partial class Product
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? ShortDescription { get; set; }
        public string SKU { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal? SalePrice { get; set; }
        public int StockQuantity { get; set; } = 0;
        public bool ManageStock { get; set; } = true;
        public bool InStock { get; set; } = true;
        public string? FeaturedImageUrl { get; set; }
        public string[]? GalleryImages { get; set; }
        public string? Size { get; set; }
        public string? Color { get; set; }
        public string? Material { get; set; }
        public decimal? Weight { get; set; }
        public string? Dimensions { get; set; }
        public bool IsFeatured { get; set; } = false;
        public bool IsActive { get; set; } = true;
        public string Status { get; set; } = "active"; // active, inactive, out_of_stock
        public int ViewCount { get; set; } = 0;
        public decimal? Rating { get; set; }
        public int ReviewCount { get; set; } = 0;
        public Guid CategoryId { get; set; }
        public Guid? BrandId { get; set; }
        
        [MaxLength(450)]
        public string? SellerId { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public Category Category { get; set; } = null!;
        public Brand? Brand { get; set; }
        
        [ForeignKey("SellerId")]
        public ApplicationUser? Seller { get; set; }
        public ICollection<ProductImage> ProductImages { get; set; } = new List<ProductImage>();
        public ICollection<ProductReview> ProductReviews { get; set; } = new List<ProductReview>();
        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
        public ICollection<ShoppingCartItem> ShoppingCartItems { get; set; } = new List<ShoppingCartItem>();
        public ICollection<Wishlist> Wishlists { get; set; } = new List<Wishlist>();

        // Computed properties
        public string MainImageUrl => FeaturedImageUrl ?? "/images/default-product.jpg";
    }

    public class ProductImage
    {
        public Guid Id { get; set; }
        public Guid ProductId { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public string? AltText { get; set; }
        public bool IsPrimary { get; set; } = false;
        public int SortOrder { get; set; } = 0;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public Product Product { get; set; } = null!;
    }

    public class Order
    {
        public Guid Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string Status { get; set; } = "pending"; // pending, processing, shipped, delivered, cancelled
        public decimal TotalAmount { get; set; }
        public decimal ShippingFee { get; set; } = 0;
        public decimal Tax { get; set; } = 0;
        public decimal DiscountAmount { get; set; } = 0;
        public string? CouponCode { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string PaymentStatus { get; set; } = "pending"; // pending, paid, failed, refunded
        public string? Notes { get; set; }
        public string ShippingAddress { get; set; } = string.Empty;
        public string BillingAddress { get; set; } = string.Empty;
        public DateTime? ShippedAt { get; set; }
        public DateTime? DeliveredAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Computed properties
        public string? ShippingCity => ShippingAddress; // Simplified mapping

        // Navigation properties
        public ApplicationUser User { get; set; } = null!;
        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
        public ICollection<Payment> Payments { get; set; } = new List<Payment>();

        // Computed properties
        public decimal Total => TotalAmount;
    }

    public class OrderItem
    {
        public Guid Id { get; set; }
        public Guid OrderId { get; set; }
        public Guid ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
        public string? ProductName { get; set; }
        public string? ProductSKU { get; set; }
        public string? ProductImage { get; set; }

        // Navigation properties
        public Order Order { get; set; } = null!;
        public Product Product { get; set; } = null!;

        // Computed properties
        public decimal Price => UnitPrice;
    }

    public class ShoppingCartItem
    {
        public Guid Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public Guid ProductId { get; set; }
        public int Quantity { get; set; }
        public string? Size { get; set; }
        public string? Color { get; set; }
        public decimal Price { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddDays(7);

        // Navigation properties
        public ApplicationUser User { get; set; } = null!;
        public Product Product { get; set; } = null!;
    }

    public class ProductReview
    {
        public Guid Id { get; set; }
        public Guid ProductId { get; set; }
        public string UserId { get; set; } = string.Empty;
        [NotMapped]
        public string? ApplicationUserId { get; set; } // EF shadow property - not mapped
        public int Rating { get; set; } // 1-5
        public string? Title { get; set; }
        public string? Comment { get; set; }
        public bool IsApproved { get; set; } = false;
        // public string? SellerResponse { get; set; }
        // public DateTime? SellerResponseDate { get; set; }
        // public string? SellerResponseBy { get; set; } // Seller ID who responded
        // public bool IsHelpful { get; set; } = false;
        // public int HelpfulCount { get; set; } = 0;
        // public bool IsVerifiedPurchase { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public Product Product { get; set; } = null!;
        public ApplicationUser User { get; set; } = null!;
        // public ApplicationUser? SellerResponseUser { get; set; }
    }

    public class Coupon
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Type { get; set; } = "percentage"; // percentage, fixed_amount
        public decimal Value { get; set; }
        public decimal? MinOrderAmount { get; set; }
        [NotMapped]
        public decimal? MaxDiscountAmount { get; set; }
        public int? UsageLimit { get; set; }
        public int UsageCount { get; set; } = 0;
        [NotMapped]
        public int? UsageLimitPerUser { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool IsActive { get; set; } = true;
        
        [MaxLength(450)]
        public string? SellerId { get; set; }  // NULL = system-wide coupon
        
        [NotMapped]
        public string? TargetAudience { get; set; } = "all"; // all, new_customers, returning_customers
        [NotMapped]
        public string? ProductCategories { get; set; } // JSON array of category IDs
        [NotMapped]
        public string? ProductIds { get; set; } // JSON array of specific product IDs
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("SellerId")]
        public ApplicationUser? Seller { get; set; }
        public ICollection<CouponUsage> CouponUsages { get; set; } = new List<CouponUsage>();
    }

    public class CouponUsage
    {
        public Guid Id { get; set; }
        public Guid CouponId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public Guid OrderId { get; set; }
        public decimal DiscountAmount { get; set; }
        public DateTime UsedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public Coupon Coupon { get; set; } = null!;
        public ApplicationUser User { get; set; } = null!;
        public Order Order { get; set; } = null!;
    }

    public class Wishlist
    {
        public Guid Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public Guid ProductId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public ApplicationUser User { get; set; } = null!;
        public Product Product { get; set; } = null!;
    }

    public class Address
    {
        public Guid Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string Type { get; set; } = "shipping"; // shipping, billing
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? Company { get; set; }
        public string Address1 { get; set; } = string.Empty;
        public string? Address2 { get; set; }
        public string City { get; set; } = string.Empty;
        public string? State { get; set; }  // Quận/Huyện, Phường/Xã - optional
        public string? PostalCode { get; set; }  // Mã bưu điện - optional
        public string Country { get; set; } = "Vietnam";
        public string? Phone { get; set; }
        public bool IsDefault { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public ApplicationUser User { get; set; } = null!;

        // Computed properties
        public string FullName => $"{FirstName} {LastName}".Trim();
        public string PhoneNumber => Phone ?? "";
        public string Ward => ""; // Thêm thuộc tính nếu cần
        public string District => State ?? ""; // Hoặc mapping phù hợp
        public string FullAddress => Address1 + (string.IsNullOrEmpty(Address2) ? "" : ", " + Address2);
    }

    public class Payment
    {
        public Guid Id { get; set; }
        public Guid OrderId { get; set; }
        public string PaymentMethod { get; set; } = string.Empty; // credit_card, bank_transfer, cod, momo, vnpay
        public string Status { get; set; } = "pending"; // pending, completed, failed, refunded
        public decimal Amount { get; set; }
        public string? TransactionId { get; set; }
        public string? GatewayResponse { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public Order Order { get; set; } = null!;
    }

    public class BlogCategory
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
        public int SortOrder { get; set; } = 0;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public ICollection<BlogPost> BlogPosts { get; set; } = new List<BlogPost>();
    }

    public class BlogPost
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? Excerpt { get; set; }
        public string Content { get; set; } = string.Empty;
        public string? FeaturedImageUrl { get; set; }
        public string Status { get; set; } = "draft"; // draft, published, archived
        public bool IsFeatured { get; set; } = false;
        public int ViewCount { get; set; } = 0;
        public string[]? Tags { get; set; }
        public string? MetaTitle { get; set; }
        public string? MetaDescription { get; set; }
        public Guid? CategoryId { get; set; }
        public string AuthorId { get; set; } = string.Empty;
        public DateTime? PublishedAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public BlogCategory? Category { get; set; }
        public ApplicationUser Author { get; set; } = null!;
    }

    public class ContactMessage
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string Subject { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public bool IsRead { get; set; } = false;
        public bool IsReplied { get; set; } = false;
        public string? AdminNotes { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? RepliedAt { get; set; }
        public string? RepliedBy { get; set; }

        // Optional user reference if logged in user contacts
        public string? UserId { get; set; }
        public ApplicationUser? User { get; set; }
    }

    public class Notification
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // order, product, system, welcome, admin_order
        public string? ActionUrl { get; set; }
        public bool IsRead { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ReadAt { get; set; }

        // Navigation property
        public ApplicationUser User { get; set; } = null!;
    }

    public class SecurityLog
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string EventType { get; set; } = string.Empty; // LoginSuccess, LoginFailed, PasswordChange, etc.
        public string Description { get; set; } = string.Empty;
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        public ApplicationUser User { get; set; } = null!;
    }

    public class PasswordHistory
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        public ApplicationUser User { get; set; } = null!;
    }

    public class ActiveSession
    {
        public int Id { get; set; }
        public string SessionId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public string? DeviceType { get; set; }
        public string? Location { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastActivity { get; set; } = DateTime.UtcNow;
        public DateTime ExpiresAt { get; set; }
        public bool IsActive { get; set; } = true;

        // Navigation property
        public ApplicationUser User { get; set; } = null!;
    }

    public class TwoFactorToken
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public string Purpose { get; set; } = string.Empty; // Login, PasswordReset, etc.
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime ExpiresAt { get; set; }
        public bool IsUsed { get; set; } = false;

        // Navigation property
        public ApplicationUser User { get; set; } = null!;
    }

    // Payment & Checkout Models
    public class PaymentAttempt
    {
        public int Id { get; set; }
        public string PaymentId { get; set; } = string.Empty;
        public string OrderId { get; set; } = string.Empty;  // Stores Order.OrderNumber (string)
        public string UserId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "VND";
        public string PaymentMethod { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty; // pending, completed, failed, cancelled
        public string? TransactionId { get; set; }
        public string? ErrorMessage { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }

        // Navigation properties
        public ApplicationUser User { get; set; } = null!;
        // Note: Order navigation removed to avoid EF Core creating duplicate FK column
    }

    public class PaymentMethod
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? IconUrl { get; set; }
        public bool IsActive { get; set; } = true;
        public bool RequiresRedirect { get; set; } = false;
        public decimal? MinAmount { get; set; }
        public decimal? MaxAmount { get; set; }
        public string? SupportedCurrencies { get; set; }
        public int SortOrder { get; set; } = 0;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Computed properties
        public string Gateway => Code;
        public string DisplayName => Name;
        public decimal ServiceFee => 0; // Có thể thêm logic tính phí
    }

    public class CheckoutSession
    {
        public Guid Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string Status { get; set; } = "active"; // active, completed, expired, cancelled
        public decimal TotalAmount { get; set; }
        public decimal ShippingFee { get; set; } = 0;
        public decimal Tax { get; set; } = 0;
        public decimal DiscountAmount { get; set; } = 0;
        public string? CouponCode { get; set; }
        public string? ShippingMethod { get; set; }
        public string? PaymentMethod { get; set; }
        public string? ShippingAddress { get; set; }
        public string? BillingAddress { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime ExpiresAt { get; set; }

        // Navigation properties
        public ApplicationUser? User { get; set; }
        public ICollection<CheckoutSessionItem> Items { get; set; } = new List<CheckoutSessionItem>();
    }

    public class CheckoutSessionItem
    {
        public int Id { get; set; }
        public Guid CheckoutSessionId { get; set; }
        public Guid ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
        public string? Size { get; set; }
        public string? Color { get; set; }
        public string? ProductName { get; set; }
        public string? ProductImage { get; set; }

        // Navigation properties
        public CheckoutSession CheckoutSession { get; set; } = null!;
        public Product Product { get; set; } = null!;
    }

    public class RefundRequest
    {
        public Guid Id { get; set; }
        public Guid OrderId { get; set; }
        public string PaymentId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string Status { get; set; } = "pending"; // pending, approved, rejected, completed
        public string? AdminNotes { get; set; }
        public string? RejectionReason { get; set; }
        public string? RefundTransactionId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ProcessedAt { get; set; }
        public string RequestedBy { get; set; } = string.Empty;
        public string? ProcessedBy { get; set; }

        // Navigation properties
        public Order Order { get; set; } = null!;
        public ApplicationUser RequestedByUser { get; set; } = null!;
    }

    public class ShippingMethod
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Cost { get; set; }
        public int EstimatedDays { get; set; }
        public bool IsActive { get; set; } = true;
        public decimal? MinOrderAmount { get; set; }
        public decimal? MaxWeight { get; set; }
        public string? AvailableRegions { get; set; }
        public int SortOrder { get; set; } = 0;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Computed properties
        public string DeliveryTime => $"{EstimatedDays} ngày";
    }

    public class OrderStatusHistory
    {
        public int Id { get; set; }
        public Guid OrderId { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public string? ChangedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public Order Order { get; set; } = null!;
        public ApplicationUser? ChangedByUser { get; set; }
    }

    public class Promotion
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string Type { get; set; } = "percentage"; // percentage, fixed_amount, free_shipping
        public decimal Value { get; set; }
        public decimal? MinOrderAmount { get; set; }
        public decimal? MaxDiscountAmount { get; set; }
        public int? UsageLimit { get; set; }
        public int UsageCount { get; set; } = 0;
        public bool IsActive { get; set; } = true;
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? Description { get; set; }
        public string? ApplicableProductIds { get; set; } // JSON array of product IDs
        public string? ApplicableCategoryIds { get; set; } // JSON array of category IDs
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    // Analytics and Reporting Models
    public class UserSession
    {
        public Guid Id { get; set; }
        public string SessionId { get; set; } = "";
        public string? UserId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public int? Duration { get; set; } // in minutes
        public string UserAgent { get; set; } = "";
        public string IpAddress { get; set; } = "";
        public bool IsActive { get; set; }
        public string? DeviceType { get; set; }
        public string? Browser { get; set; }
        public string? OperatingSystem { get; set; }
        public string? Country { get; set; }
        public string? City { get; set; }
        public DateTime CreatedAt { get; set; }

        // Navigation properties
        public ApplicationUser? User { get; set; }
        public List<PageView> PageViews { get; set; } = new();
    }

    public class PageView
    {
        public Guid Id { get; set; }
        public string? UserId { get; set; }
        public string SessionId { get; set; } = "";
        public string Page { get; set; } = "";
        public string? Referrer { get; set; }
        public string UserAgent { get; set; } = "";
        public string IpAddress { get; set; } = "";
        public DateTime ViewedAt { get; set; }
        public int? TimeOnPage { get; set; } // in seconds
        public string? ExitPage { get; set; }
        public string? Source { get; set; }
        public string? Medium { get; set; }
        public string? Campaign { get; set; }

        // Navigation properties
        public ApplicationUser? User { get; set; }
        public UserSession? Session { get; set; }
    }

    public class ConversionEvent
    {
        public Guid Id { get; set; }
        public string? UserId { get; set; }
        public string SessionId { get; set; } = "";
        public string ConversionType { get; set; } = ""; // purchase, signup, newsletter, etc.
        public decimal Value { get; set; }
        public string? PaymentMethod { get; set; }
        public string? Source { get; set; }
        public string? Medium { get; set; }
        public string? Campaign { get; set; }
        public Guid? OrderId { get; set; }
        public string? ProductIds { get; set; } // JSON array
        public DateTime ConvertedAt { get; set; }
        public string? AdditionalData { get; set; } // JSON

        // Navigation properties
        public ApplicationUser? User { get; set; }
        public Order? Order { get; set; }
    }

    public class AnalyticsData
    {
        public Guid Id { get; set; }
        public string EventType { get; set; } = "";
        public string? EntityId { get; set; }
        public string? UserId { get; set; }
        public string SessionId { get; set; } = "";
        public string? Source { get; set; }
        public string? Medium { get; set; }
        public string? Campaign { get; set; }
        public string? Data { get; set; } // JSON
        public DateTime CreatedAt { get; set; }

        // Navigation properties
        public ApplicationUser? User { get; set; }
    }

    public class SalesReport
    {
        public Guid Id { get; set; }
        public string ReportType { get; set; } = "";
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal TotalRevenue { get; set; }
        public int TotalOrders { get; set; }
        public int TotalProducts { get; set; }
        public decimal AverageOrderValue { get; set; }
        public string? ReportData { get; set; } // JSON
        public DateTime GeneratedAt { get; set; }
        public string? GeneratedBy { get; set; }
        public string Status { get; set; } = ""; // generating, completed, failed

        // Navigation properties
        public ApplicationUser? GeneratedByUser { get; set; }

        // Computed properties for backward compatibility
        public static string Title => "Sales Report";
        public static string Description => "Sales performance report";
        public DateTime CreatedAt => GeneratedAt;
        public string? Data => ReportData;
    }

    public class ReportTemplate
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string ReportType { get; set; } = "";
        public string Format { get; set; } = "excel"; // excel, pdf, csv, json
        public string Frequency { get; set; } = "monthly"; // daily, weekly, monthly, quarterly, yearly
        public string? Parameters { get; set; } // JSON
        public string? Configuration { get; set; } // JSON
        public bool IsActive { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? LastRunDate { get; set; }
        public DateTime? NextRunDate { get; set; }

        // Navigation properties
        public ApplicationUser? CreatedByUser { get; set; }
    }

    public class AuditLog
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string? UserId { get; set; }
        public string? TargetUserId { get; set; }
        public string Action { get; set; } = string.Empty;
        public string? Details { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string? AdditionalData { get; set; } // JSON for extra context

        // Navigation properties
        public ApplicationUser? User { get; set; }
        public ApplicationUser? TargetUser { get; set; }
    }

    // Vietnamese Administrative Divisions
    public class Province
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? CodeName { get; set; }

        // Navigation
        public ICollection<District> Districts { get; set; } = new List<District>();
    }

    public class District
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? CodeName { get; set; }
        public int ProvinceId { get; set; }

        // Navigation
        public Province Province { get; set; } = null!;
        public ICollection<Ward> Wards { get; set; } = new List<Ward>();
    }

    public class Ward
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? CodeName { get; set; }
        public int DistrictId { get; set; }

        // Navigation
        public District District { get; set; } = null!;
    }
}
