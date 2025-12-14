using System.ComponentModel.DataAnnotations;
using JohnHenryFashionWeb.Models;

namespace JohnHenryFashionWeb.ViewModels
{
    // Dashboard ViewModels
    public class AdminDashboardViewModel
    {
        public DashboardStats Stats { get; set; } = new();
        public List<RecentOrder> RecentOrders { get; set; } = new();
        public List<TopSellingProduct> TopProducts { get; set; } = new();
        public List<MonthlyRevenue> RevenueChart { get; set; } = new();
    }

    public class SellerDashboardViewModel
    {
        public int MyProductsCount { get; set; }
        public int MyOrdersCount { get; set; }
        public decimal MyRevenue { get; set; }
        public decimal MonthlyRevenue { get; set; }
        public List<Models.RecentOrder> MyRecentOrders { get; set; } = new();
        public List<TopSellingProduct> MyTopProducts { get; set; } = new();
        public int PendingReviewsCount { get; set; }
        public int ActiveCouponsCount { get; set; }
        public int LowInventoryProductsCount { get; set; }
    }

    // Product Management ViewModels
    public class ProductListViewModel
    {
        public IEnumerable<ProductListItemViewModel> Products { get; set; } = new List<ProductListItemViewModel>();
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public int PageSize { get; set; } = 10;
        public string SearchTerm { get; set; } = string.Empty;
        public Guid? CategoryId { get; set; }
        public string Status { get; set; } = string.Empty;
        public List<Category> Categories { get; set; } = new();
        public int TotalProducts { get; set; }
    }

    public class ProductListItemViewModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal? SalePrice { get; set; }
        public int StockQuantity { get; set; }
        public string Status { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string? BrandName { get; set; }
        public string? FeaturedImageUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsFeatured { get; set; }
        public bool IsActive { get; set; }
        public string? Description { get; set; }
        public Category? Category { get; set; }
    }

    public class ProductCreateEditViewModel
    {
        public Guid? Id { get; set; }
        
        [Required(ErrorMessage = "Tên sản phẩm là bắt buộc")]
        [StringLength(255)]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "SKU là bắt buộc")]
        [StringLength(50)]
        public string SKU { get; set; } = string.Empty;

        [Required(ErrorMessage = "Giá sản phẩm là bắt buộc")]
        [Range(0, double.MaxValue, ErrorMessage = "Giá phải lớn hơn 0")]
        public decimal Price { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Giá khuyến mãi phải lớn hơn 0")]
        public decimal? SalePrice { get; set; }

        [Required(ErrorMessage = "Danh mục là bắt buộc")]
        public Guid CategoryId { get; set; }

        public Guid? BrandId { get; set; }

        [StringLength(1000)]
        public string? ShortDescription { get; set; }

        public string? Description { get; set; }

        [Range(0, int.MaxValue)]
        public int StockQuantity { get; set; } = 0;

        public bool ManageStock { get; set; } = true;
        public bool InStock { get; set; } = true;
        public bool IsFeatured { get; set; } = false;
        public bool IsActive { get; set; } = true;

        [StringLength(100)]
        public string? Size { get; set; }

        [StringLength(100)]
        public string? Color { get; set; }

        [StringLength(100)]
        public string? Material { get; set; }

        public decimal? Weight { get; set; }

        [StringLength(100)]
        public string? Dimensions { get; set; }

        public string Status { get; set; } = "active";

        // SEO Fields
        [StringLength(255)]
        public string? MetaTitle { get; set; }
        
        [StringLength(500)]
        public string? MetaDescription { get; set; }
        
        [StringLength(1000)]
        public string? Tags { get; set; }

        // For file uploads
        public IFormFile? FeaturedImage { get; set; }
        public IList<IFormFile>? GalleryImages { get; set; }
        public string? FeaturedImageUrl { get; set; }
        public string[]? ExistingGalleryImages { get; set; }

        // Dropdown data
        public List<Category> Categories { get; set; } = new();
        public List<Brand> Brands { get; set; } = new();
    }

    // Category Management ViewModels
    public class CategoryListViewModel
    {
        public IEnumerable<CategoryListItemViewModel> Categories { get; set; } = new List<CategoryListItemViewModel>();
        public string SearchTerm { get; set; } = string.Empty;
    }

    public class CategoryListItemViewModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public Guid? ParentId { get; set; }
        public string? ParentName { get; set; }
        public bool IsActive { get; set; }
        public int ProductCount { get; set; }
        public int SortOrder { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CategoryCreateEditViewModel
    {
        public Guid? Id { get; set; }

        [Required(ErrorMessage = "Tên danh mục là bắt buộc")]
        [StringLength(255)]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        public Guid? ParentId { get; set; }

        public bool IsActive { get; set; } = true;

        [Range(0, int.MaxValue)]
        public int SortOrder { get; set; } = 0;

        public IFormFile? Image { get; set; }
        public string? ImageUrl { get; set; }

        public List<Category> ParentCategories { get; set; } = new();
    }

    // Order Management ViewModels
    public class OrderListViewModel
    {
        public IEnumerable<OrderListItemViewModel> Orders { get; set; } = new List<OrderListItemViewModel>();
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public int PageSize { get; set; } = 10;
        public string SearchTerm { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }

    public class OrderListItemViewModel
    {
        public Guid Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public decimal Total { get; set; }
        public string Status { get; set; } = string.Empty;
        public string PaymentStatus { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public int ItemCount { get; set; }
    }

    public class OrderDetailViewModel
    {
        public Guid Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public string ShippingAddress { get; set; } = string.Empty;
        public string BillingAddress { get; set; } = string.Empty;
        public decimal Subtotal { get; set; }
        public decimal ShippingCost { get; set; }
        public decimal Tax { get; set; }
        public decimal Discount { get; set; }
        public decimal Total { get; set; }
        public string Status { get; set; } = string.Empty;
        public string PaymentStatus { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string? Notes { get; set; }
        
        public List<OrderItemViewModel> Items { get; set; } = new();
    }

    public class OrderItemViewModel
    {
        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ProductSKU { get; set; } = string.Empty;
        public string? ProductImage { get; set; }
        public string Size { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Total { get; set; }
        public decimal TotalPrice { get; set; }
    }

    // Inventory Management ViewModels
    public class InventoryListViewModel
    {
        public IEnumerable<InventoryItemViewModel> Items { get; set; } = new List<InventoryItemViewModel>();
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public int PageSize { get; set; } = 10;
        public string SearchTerm { get; set; } = string.Empty;
        public string Filter { get; set; } = string.Empty; // all, low_stock, out_of_stock
        
        // Statistics for all products (not just current page)
        public int TotalProducts { get; set; }
        public int TotalStockQuantity { get; set; }
        public int LowStockProducts { get; set; }
        public int OutOfStockProducts { get; set; }
        public int InStockProducts { get; set; }
    }

    public class InventoryItemViewModel
    {
        public Guid Id { get; set; }
        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public int StockQuantity { get; set; }
        public int CurrentStock { get; set; }
        public int MinStockLevel { get; set; }
        public int MinStock { get; set; }
        public int MaxStock { get; set; }
        public int SoldQuantity { get; set; }
        public decimal Price { get; set; }
        public decimal CostPrice { get; set; }
        public string? ImageUrl { get; set; }
        public string? Location { get; set; }
        public DateTime LastUpdated { get; set; }
        public bool IsLowStock => CurrentStock <= MinStock;
        public bool IsOutOfStock => CurrentStock <= 0;
    }

    public class StockAdjustmentViewModel
    {
        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public int CurrentStock { get; set; }

        [Required(ErrorMessage = "Số lượng điều chỉnh là bắt buộc")]
        public int Quantity { get; set; }

        [Required(ErrorMessage = "Loại điều chỉnh là bắt buộc")]
        public string Type { get; set; } = string.Empty; // IN, OUT, ADJUSTMENT

        [StringLength(500)]
        public string? Reason { get; set; }

        [StringLength(100)]
        public string? Reference { get; set; }
    }

    // User Management ViewModels
    public class UserListViewModel
    {
        public IEnumerable<UserListItemViewModel> Users { get; set; } = new List<UserListItemViewModel>();
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public int PageSize { get; set; } = 10;
        public string SearchTerm { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    public class UserListItemViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string FullName => $"{FirstName} {LastName}".Trim();
        public string? PhoneNumber { get; set; }
        public List<string> Roles { get; set; } = new();
        public string Role => Roles.FirstOrDefault() ?? "Customer";
        public bool IsActive { get; set; }
        public bool IsApproved { get; set; }
        public string Status => IsActive ? "Active" : "Inactive";
        public string? Avatar { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLogin { get; set; }
        public string? CompanyName { get; set; }
        
        // Enhanced properties for User Management
        public DateTime? LastLoginDate { get; set; }
        public int OrderCount { get; set; }
        public decimal TotalSpent { get; set; }
        public string PrimaryRole => Roles.FirstOrDefault() ?? "Customer";
        public string StatusText => IsActive ? "Hoạt động" : "Đã khóa";
        public string StatusClass => IsActive ? "success" : "danger";
        
        // Lockout properties
        public bool IsLockedOut { get; set; }
        public DateTime? LockoutEnd { get; set; }
    }

    // Inventory Management ViewModels
    public class InventoryManagementViewModel
    {
        public int TotalProducts { get; set; }
        public int InStockProducts { get; set; }
        public int LowStockProducts { get; set; }
        public int OutOfStockProducts { get; set; }
        public List<InventoryItemViewModel> InventoryItems { get; set; } = new();
        public int TotalItems { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
    }

    // Order Management ViewModels
    public class OrderManagementViewModel
    {
        public int PendingOrders { get; set; }
        public int ShippingOrders { get; set; }
        public int CompletedOrders { get; set; }
        public int CancelledOrders { get; set; }
        public int TotalOrders { get; set; }
        public List<OrderSummaryViewModel> Orders { get; set; } = new();
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        
        // Enhanced properties for new Orders view
        public int PageSize { get; set; } = 20;
        public string SearchTerm { get; set; } = string.Empty;
        public string StatusFilter { get; set; } = string.Empty;
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string SortBy { get; set; } = "newest";
        
        // Statistics
        public int PendingOrdersCount => PendingOrders;
        public int ProcessingOrdersCount { get; set; }
        public int ShippedOrdersCount => ShippingOrders;
        public decimal TotalRevenue { get; set; }
    }

    public class OrderSummaryViewModel
    {
        public Guid Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string PaymentStatus { get; set; } = string.Empty;
    }

    public class OrderDetailsViewModel
    {
        public Guid Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string PaymentStatus { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public string ShippingAddress { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public decimal SubTotal { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal ShippingFee { get; set; }
        public decimal TotalAmount { get; set; }
        public string? CouponCode { get; set; }
        public List<OrderItemViewModel> OrderItems { get; set; } = new();
        public List<OrderHistoryViewModel> OrderHistory { get; set; } = new();
    }

    public class OrderHistoryViewModel
    {
        public string Status { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
    }

    // Product Management ViewModels
    public class CreateProductViewModel
    {
        [Required]
        public string Name { get; set; } = string.Empty;
        public string? SKU { get; set; }
        public string? Description { get; set; }
        [Required]
        public decimal Price { get; set; }
        public decimal? OriginalPrice { get; set; }
        [Required]
        public int StockQuantity { get; set; }
        [Required]
        public Guid CategoryId { get; set; }
        public bool IsActive { get; set; } = true;
        public string? AvailableSizes { get; set; }
        public string? AvailableColors { get; set; }
    }

    public class EditProductViewModel
    {
        public Guid Id { get; set; }
        [Required]
        public string Name { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public string? Description { get; set; }
        [Required]
        public decimal Price { get; set; }
        public decimal? OriginalPrice { get; set; }
        [Required]
        public int StockQuantity { get; set; }
        [Required]
        public Guid CategoryId { get; set; }
        public bool IsActive { get; set; }
        public string? AvailableSizes { get; set; }
        public string? AvailableColors { get; set; }
        public string? ImageUrl { get; set; }
        public int ViewCount { get; set; }
        public int SoldQuantity { get; set; }
        public decimal? AverageRating { get; set; }
        public Category? Category { get; set; }
    }

    // Reports Supporting Classes
    public class TopSellingProductReport
    {
        public string ProductName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public int QuantitySold { get; set; }
        public decimal Revenue { get; set; }
        public string? ImageUrl { get; set; }
    }

    public class LowStockProductReport
    {
        public string ProductName { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public int StockQuantity { get; set; }
    }

    // User Management ViewModels - Enhanced
    public class UserManagementViewModel
    {
        public IEnumerable<UserListItemViewModel> Users { get; set; } = new List<UserListItemViewModel>();
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public int PageSize { get; set; } = 20;
        public string SearchTerm { get; set; } = string.Empty;
        public string RoleFilter { get; set; } = string.Empty;
        public string StatusFilter { get; set; } = string.Empty;
        public string DateRange { get; set; } = string.Empty;
        public string SortBy { get; set; } = "newest";
        
        // Statistics
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int NewUsersThisMonth { get; set; }
        public int SellersCount { get; set; }
        
        // Enhanced properties for new functionality
        public UserFilterViewModel Filter { get; set; } = new();
        public int TotalCount { get; set; }
        public List<string> AvailableRoles { get; set; } = new();
        public Dictionary<string, int> Statistics { get; set; } = new();
    }

    public class InventoryViewModel
    {
        public int TotalProducts { get; set; }
        public decimal TotalStockValue { get; set; }
        public int LowStockCount { get; set; }
        public int OutOfStockCount { get; set; }
        
        public string SearchTerm { get; set; } = string.Empty;
        public string SelectedCategory { get; set; } = string.Empty;
        public string StockStatus { get; set; } = string.Empty;
        public string SortBy { get; set; } = "Name";
        
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public int TotalCount { get; set; }
        
        public Dictionary<string, string> Categories { get; set; } = new();
        public List<InventoryItem> InventoryItems { get; set; } = new();
        public List<StockMovement> RecentMovements { get; set; } = new();
    }

    public class InventoryItem
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public int Quantity { get; set; }
        public int MinStockLevel { get; set; }
        public decimal CostPrice { get; set; }
        public DateTime LastUpdated { get; set; }
    }

    public class StockMovement
    {
        public string ProductName { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // In, Out, Adjustment
        public int Quantity { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
    }

    // Blog Management ViewModels
    public class AdminBlogViewModel
    {
        public List<BlogPost> Posts { get; set; } = new();
        public List<BlogCategory> Categories { get; set; } = new();
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public int TotalPosts { get; set; }
        public string? StatusFilter { get; set; }
        public string? CategoryFilter { get; set; }
        public string? SearchTerm { get; set; }
    }

    // Seller Management ViewModels
    public class SellerSalesViewModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public decimal TotalRevenue { get; set; }
        public int TotalOrders { get; set; }
        public List<DailySales> SalesData { get; set; } = new();
    }

    public class DailySales
    {
        public DateTime Date { get; set; }
        public decimal Revenue { get; set; }
        public int OrderCount { get; set; }
    }

    public class SellerAnalyticsViewModel
    {
        public List<TopSellingProduct> TopProducts { get; set; } = new();
        public List<CategoryPerformance> CategoryPerformance { get; set; } = new();
        public List<DailyRevenue> DailyRevenues { get; set; } = new();
        public int TotalProductViews { get; set; }
        public int ConversionRate { get; set; }
    }

    public class DailyRevenue
    {
        public DateTime Date { get; set; }
        public decimal Revenue { get; set; }
        public int OrderCount { get; set; }
    }

    public class CategoryPerformance
    {
        public string CategoryName { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
        public int ProductCount { get; set; }
        public int OrderCount { get; set; }
    }

    public class SellerProfileViewModel
    {
        [Required(ErrorMessage = "Tên công ty là bắt buộc")]
        [Display(Name = "Tên công ty")]
        public string? CompanyName { get; set; }

        [Display(Name = "Giấy phép kinh doanh")]
        public string? BusinessLicense { get; set; }

        [Display(Name = "Mã số thuế")]
        public string? TaxCode { get; set; }

        [Display(Name = "Địa chỉ")]
        public string? Address { get; set; }

        [Display(Name = "Số điện thoại")]
        public string? Phone { get; set; }

        [Required(ErrorMessage = "Email là bắt buộc")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Họ là bắt buộc")]
        [Display(Name = "Họ")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tên là bắt buộc")]
        [Display(Name = "Tên")]
        public string LastName { get; set; } = string.Empty;

        [Display(Name = "Trạng thái duyệt")]
        public bool IsApproved { get; set; }

        [Display(Name = "Ngày duyệt")]
        public DateTime? ApprovedAt { get; set; }

        [Display(Name = "Ghi chú")]
        public string? Notes { get; set; }

        [Display(Name = "Ngày tạo")]
        public DateTime CreatedAt { get; set; }

        [Display(Name = "Cập nhật lần cuối")]
        public DateTime? UpdatedAt { get; set; }

        [Display(Name = "Trạng thái tài khoản")]
        public bool IsActive { get; set; } = true;
    }

    public class SellerSettingsViewModel
    {
        // Store Settings
        [Display(Name = "Kích hoạt cửa hàng")]
        public bool IsStoreActive { get; set; } = true;

        [Display(Name = "Mô tả cửa hàng")]
        [StringLength(500, ErrorMessage = "Mô tả không được vượt quá 500 ký tự")]
        public string? StoreDescription { get; set; }

        // Business Hours
        [Display(Name = "Giờ mở cửa")]
        public TimeSpan? BusinessHoursStart { get; set; }

        [Display(Name = "Giờ đóng cửa")]
        public TimeSpan? BusinessHoursEnd { get; set; }

        // Days of week
        [Display(Name = "Nghỉ thứ Hai")]
        public bool ClosedOnMonday { get; set; } = false;

        [Display(Name = "Nghỉ thứ Ba")]
        public bool ClosedOnTuesday { get; set; } = false;

        [Display(Name = "Nghỉ thứ Tư")]
        public bool ClosedOnWednesday { get; set; } = false;

        [Display(Name = "Nghỉ thứ Năm")]
        public bool ClosedOnThursday { get; set; } = false;

        [Display(Name = "Nghỉ thứ Sáu")]
        public bool ClosedOnFriday { get; set; } = false;

        [Display(Name = "Nghỉ thứ Bảy")]
        public bool ClosedOnSaturday { get; set; } = false;

        [Display(Name = "Nghỉ chủ nhật")]
        public bool ClosedOnSunday { get; set; } = false;

        // Notification Settings
        [Display(Name = "Cài đặt thông báo")]
        public EmailNotificationSettings EmailNotifications { get; set; } = new();

        [Display(Name = "Tần suất báo cáo")]
        public string ReportFrequency { get; set; } = "Weekly";

        // Inventory Settings
        [Display(Name = "Ngưỡng cảnh báo hết hàng")]
        [Range(0, int.MaxValue, ErrorMessage = "Ngưỡng phải lớn hơn hoặc bằng 0")]
        public int LowStockThreshold { get; set; } = 10;

        [Display(Name = "Tự động ẩn sản phẩm hết hàng")]
        public bool AutoHideOutOfStock { get; set; } = false;

        // Statistics
        public int TotalProducts { get; set; }
        public int TotalOrders { get; set; }
    }

    public class EmailNotificationSettings
    {
        public bool NewOrders { get; set; } = true;
        public bool LowStock { get; set; } = true;
        public bool ProductReviews { get; set; } = true;
        public bool SystemUpdates { get; set; } = true;
    }

    // Enhanced User Management ViewModels
    public class UserFilterViewModel
    {
        public string SearchTerm { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public bool? IsActive { get; set; }
        public DateTime? CreatedFrom { get; set; }
        public DateTime? CreatedTo { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string SortBy { get; set; } = "CreatedAt";
        public string SortDirection { get; set; } = "desc";
    }

    public class UserEditViewModel
    {
        public string Id { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Họ là bắt buộc")]
        [StringLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tên là bắt buộc")]
        [StringLength(50)]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email là bắt buộc")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        public string? PhoneNumber { get; set; }

        public bool IsActive { get; set; } = true;

        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }

        public string? Gender { get; set; }

        [StringLength(500)]
        public string? Address { get; set; }

        public List<string> SelectedRoles { get; set; } = new();
        public List<string> AvailableRoles { get; set; } = new();
        public IFormFile? AvatarFile { get; set; }
        public string? CurrentAvatarUrl { get; set; }
    }

    public class UserDetailViewModel
    {
        public ApplicationUser User { get; set; } = new();
        public List<string> Roles { get; set; } = new();
        public List<Order> RecentOrders { get; set; } = new();
        public int OrderCount { get; set; }
        public decimal TotalSpent { get; set; }
        public DateTime JoinDate { get; set; }
        public DateTime? LastActivity { get; set; }
        public Dictionary<string, object> AdditionalInfo { get; set; } = new();
        
        // Lockout information
        public bool IsLockedOut { get; set; }
        public DateTime? LockoutEnd { get; set; }
        public int FailedLoginAttempts { get; set; }
    }

    public class UserCreateViewModel
    {
        [Required(ErrorMessage = "Họ là bắt buộc")]
        [StringLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tên là bắt buộc")]
        [StringLength(50)]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email là bắt buộc")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Mật khẩu xác nhận không khớp")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        public string? PhoneNumber { get; set; }

        public bool IsActive { get; set; } = true;

        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }

        public string? Gender { get; set; }

        [StringLength(500)]
        public string? Address { get; set; }

        public List<string> SelectedRoles { get; set; } = new();
        public List<string> AvailableRoles { get; set; } = new();
        public IFormFile? AvatarFile { get; set; }
    }

    public class UserStatsViewModel
    {
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int InactiveUsers { get; set; }
        public int NewUsersThisMonth { get; set; }
        public double GrowthRate { get; set; }
    }

    public class SellerReviewsViewModel
    {
        public List<ProductReview> Reviews { get; set; } = new();
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public int PageSize { get; set; } = 10;
        public int TotalCount { get; set; }
        
        // Filters
        public string Search { get; set; } = string.Empty;
        public int? Rating { get; set; }
        public string Status { get; set; } = string.Empty;
        
        // Statistics
        public ReviewStatistics Statistics { get; set; } = new();
    }

    public class ReviewStatistics
    {
        public int TotalReviews { get; set; }
        public int PendingReviews { get; set; }
        public int ApprovedReviews { get; set; }
        public int RejectedReviews { get; set; }
        public double AverageRating { get; set; }
        public Dictionary<int, int> RatingDistribution { get; set; } = new();
    }

    public class CouponManagementViewModel
    {
        public List<CouponItem> Coupons { get; set; } = new();
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public int PageSize { get; set; } = 10;
        public int TotalCount { get; set; }
        public string Search { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public CouponStatistics Statistics { get; set; } = new();
    }

    public class CouponStatistics
    {
        public int TotalCoupons { get; set; }
        public int ActiveCoupons { get; set; }
        public int ExpiredCoupons { get; set; }
        public int UsedCoupons { get; set; }
        public decimal TotalDiscountGiven { get; set; }
    }

    public class CouponItem
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public decimal Value { get; set; }
        public decimal? MinOrderAmount { get; set; }
        public int? UsageLimit { get; set; }
        public int UsageCount { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool IsActive { get; set; }
        public string Status => GetStatus();

        private string GetStatus()
        {
            if (!IsActive) return "inactive";
            if (EndDate.HasValue && EndDate.Value < DateTime.Now) return "expired";
            if (UsageLimit.HasValue && UsageCount >= UsageLimit.Value) return "used_up";
            return "active";
        }
    }

    public class CouponCreateEditViewModel
    {
        public Guid? Id { get; set; }
        
        [Required(ErrorMessage = "Mã coupon là bắt buộc")]
        [StringLength(20, ErrorMessage = "Mã coupon không được vượt quá 20 ký tự")]
        public string Code { get; set; } = string.Empty;

        [StringLength(100, ErrorMessage = "Mô tả không được vượt quá 100 ký tự")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Loại giảm giá là bắt buộc")]
        public string DiscountType { get; set; } = "percentage";

        [Required(ErrorMessage = "Giá trị giảm giá là bắt buộc")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Giá trị giảm giá phải lớn hơn 0")]
        public decimal DiscountValue { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Giá trị đơn hàng tối thiểu phải lớn hơn hoặc bằng 0")]
        public decimal? MinOrderAmount { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Giới hạn sử dụng phải lớn hơn 0")]
        public int? UsageLimit { get; set; }

        [Required(ErrorMessage = "Ngày hết hạn là bắt buộc")]
        public DateTime ExpiryDate { get; set; } = DateTime.Now.AddDays(30);

        public bool IsActive { get; set; } = true;
    }

    public class SellerNotificationsViewModel
    {
        public List<Notification> Notifications { get; set; } = new();
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public int PageSize { get; set; } = 10;
        public int TotalCount { get; set; }
        public string Type { get; set; } = string.Empty;
        public bool? IsRead { get; set; }
        public int UnreadCount { get; set; }
    }

    // Store Management ViewModels
    public class StoreManagementViewModel
    {
        public Store? Store { get; set; }
        public List<StoreInventoryItem> Inventory { get; set; } = new();
        public List<StoreSettingItem> Settings { get; set; } = new();
        public StoreStatistics Statistics { get; set; } = new();
        public List<SellerStore> StoreStaff { get; set; } = new();
    }

    public class StoreInventoryItem
    {
        public Guid Id { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ProductImageUrl { get; set; } = string.Empty;
        public string ProductSku { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public int MinimumStock { get; set; }
        public int MaximumStock { get; set; }
        public string? Location { get; set; }
        public DateTime LastUpdated { get; set; }
    }

    public class StoreSettingItem
    {
        public Guid Id { get; set; }
        public string SettingKey { get; set; } = string.Empty;
        public string SettingValue { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string UpdatedByName { get; set; } = string.Empty;
    }

    public class StoreStatistics
    {
        public int TotalProducts { get; set; }
        public int LowStockProducts { get; set; }
        public int OutOfStockProducts { get; set; }
        public decimal TotalInventoryValue { get; set; }
        public int StaffCount { get; set; }
        public decimal MonthlyRevenue { get; set; }
        public int MonthlyOrders { get; set; }
    }

    public class StoreSettingsViewModel
    {
        public Guid StoreId { get; set; }
        public string StoreName { get; set; } = string.Empty;
        public string StoreAddress { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Website { get; set; }
        public string? WorkingHours { get; set; }
        public string? Description { get; set; }
        public string? SocialMedia { get; set; }
        public bool IsActive { get; set; } = true;
        public Dictionary<string, string> AdditionalSettings { get; set; } = new();
    }

    // Additional Seller ViewModels
    public class SellerCommissionsViewModel
    {
        public List<MonthlyCommissionData> MonthlyData { get; set; } = new();
        public decimal TotalCommission { get; set; }
        public decimal CurrentMonthCommission { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        
        // Additional properties required by controller
        public decimal TotalSales { get; set; }
        public int TotalOrders { get; set; }
        public decimal CommissionRate { get; set; }
        public List<RecentOrder> RecentOrders { get; set; } = new();
    }

    public class MonthlyCommissionData
    {
        public int Month { get; set; }
        public int Year { get; set; }
        public decimal Commission { get; set; }
        public int OrderCount { get; set; }
        
        // Additional property for sales data
        public decimal Sales { get; set; }
    }

    public class RecentOrder
    {
        public Guid OrderId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public decimal Total { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class SellerCustomersViewModel
    {
        public List<CustomerInfo> Customers { get; set; } = new();
        public int TotalCustomers { get; set; }
        public int NewCustomersThisMonth { get; set; }
        public int ReturningCustomers { get; set; }
        
        // Additional properties for pagination and search
        public List<CustomerInfo> TopCustomers { get; set; } = new();
        public List<CustomerInfo> NewCustomers { get; set; } = new();
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public int PageSize { get; set; } = 10;
        public string Search { get; set; } = string.Empty;
        public int TotalCount { get; set; }
    }

    public class CustomerInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int OrderCount { get; set; }
        public decimal TotalSpent { get; set; }
        public DateTime LastOrderDate { get; set; }
        
        // Additional properties required by controller
        public string UserId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public DateTime FirstOrderDate { get; set; }
        public int TotalOrders { get; set; }
        public decimal AverageOrderValue { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class SellerReportsViewModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public decimal TotalRevenue { get; set; }
        public int TotalOrders { get; set; }
        public decimal AverageOrderValue { get; set; }
        public List<DailySalesData> DailySales { get; set; } = new();
        
        // Additional properties required by controller
        public decimal MonthlyRevenue { get; set; }
        public int MonthlyOrders { get; set; }
        public int TotalProducts { get; set; }
        public int ActiveProducts { get; set; }
        public int TotalCustomers { get; set; }
        public int NewCustomers { get; set; }
        public decimal ConversionRate { get; set; }
        public List<string> SalesChartLabels { get; set; } = new();
        public List<decimal> SalesChartData { get; set; } = new();
        public List<string> OrdersChartLabels { get; set; } = new();
        public List<int> OrdersChartData { get; set; } = new();
    }

    public class DailySalesData
    {
        public DateTime Date { get; set; }
        public decimal Revenue { get; set; }
        public int Orders { get; set; }
    }

    public class SellerProductPerformanceViewModel
    {
        public List<ProductPerformanceItem> Products { get; set; } = new();
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        
        // Additional properties required by controller
        public List<ProductPerformanceItem> TopProducts { get; set; } = new();
        public List<ProductPerformanceItem> LowPerformingProducts { get; set; } = new();
    }

    public class ProductPerformanceItem
    {
        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ProductImageUrl { get; set; } = string.Empty;
        public int ViewCount { get; set; }
        public int SalesCount { get; set; }
        public decimal Revenue { get; set; }
        public decimal ConversionRate { get; set; }
        
        // Additional properties required by controller
        public string ImageUrl { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int TotalSold { get; set; }
        public int ReviewCount { get; set; }
        public double AverageRating { get; set; }
    }
}
