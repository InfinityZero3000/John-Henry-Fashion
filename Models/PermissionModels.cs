using System.ComponentModel.DataAnnotations;

namespace JohnHenryFashionWeb.Models
{
    /// <summary>
    /// Permission constants for the system
    /// </summary>
    public static class Permissions
    {
        // Product Management
        public const string ProductsView = "products.view";
        public const string ProductsCreate = "products.create";
        public const string ProductsEdit = "products.edit";
        public const string ProductsDelete = "products.delete";
        public const string ProductsManageStock = "products.manage_stock";
        
        // Order Management
        public const string OrdersView = "orders.view";
        public const string OrdersApprove = "orders.approve";
        public const string OrdersShip = "orders.ship";
        public const string OrdersComplete = "orders.complete";
        public const string OrdersCancel = "orders.cancel";
        
        // Analytics
        public const string AnalyticsView = "analytics.view";
        public const string AnalyticsExport = "analytics.export";
        
        // Settings
        public const string SettingsManageStore = "settings.manage_store";
        
        // Admin permissions
        public const string AdminManageUsers = "admin.manage_users";
        public const string AdminManagePermissions = "admin.manage_permissions";
        public const string AdminManageSystem = "admin.manage_system";

        /// <summary>
        /// Get all available permissions grouped by module
        /// </summary>
        public static Dictionary<string, List<PermissionInfo>> GetAllPermissions()
        {
            return new Dictionary<string, List<PermissionInfo>>
            {
                ["Sản phẩm"] = new List<PermissionInfo>
                {
                    new(ProductsView, "Xem sản phẩm", "Xem danh sách và chi tiết sản phẩm"),
                    new(ProductsCreate, "Thêm sản phẩm", "Tạo sản phẩm mới"),
                    new(ProductsEdit, "Sửa sản phẩm", "Chỉnh sửa thông tin sản phẩm"),
                    new(ProductsDelete, "Xóa sản phẩm", "Xóa sản phẩm khỏi hệ thống"),
                    new(ProductsManageStock, "Quản lý tồn kho", "Cập nhật số lượng tồn kho")
                },
                ["Đơn hàng"] = new List<PermissionInfo>
                {
                    new(OrdersView, "Xem đơn hàng", "Xem danh sách và chi tiết đơn hàng"),
                    new(OrdersApprove, "Duyệt đơn hàng", "Xác nhận và duyệt đơn hàng mới"),
                    new(OrdersShip, "Gửi hàng", "Cập nhật trạng thái giao hàng"),
                    new(OrdersComplete, "Hoàn thành đơn", "Đánh dấu đơn hàng hoàn tất"),
                    new(OrdersCancel, "Hủy đơn", "Hủy đơn hàng")
                },
                ["Phân tích"] = new List<PermissionInfo>
                {
                    new(AnalyticsView, "Xem báo cáo", "Xem thống kê và báo cáo"),
                    new(AnalyticsExport, "Xuất báo cáo", "Xuất dữ liệu báo cáo")
                },
                ["Cài đặt"] = new List<PermissionInfo>
                {
                    new(SettingsManageStore, "Quản lý cửa hàng", "Cài đặt thông tin cửa hàng")
                },
                ["Quản trị"] = new List<PermissionInfo>
                {
                    new(AdminManageUsers, "Quản lý người dùng", "Thêm, sửa, xóa người dùng"),
                    new(AdminManagePermissions, "Quản lý quyền", "Gán và thu hồi quyền"),
                    new(AdminManageSystem, "Quản lý hệ thống", "Cài đặt hệ thống")
                }
            };
        }

        /// <summary>
        /// Default permissions for Seller role
        /// </summary>
        public static List<string> GetDefaultSellerPermissions()
        {
            return new List<string>
            {
                ProductsView,
                ProductsCreate,
                ProductsEdit,
                ProductsManageStock,
                OrdersView,
                OrdersApprove,
                OrdersShip,
                OrdersComplete,
                AnalyticsView
            };
        }
    }

    /// <summary>
    /// Permission information for display
    /// </summary>
    public record PermissionInfo(string Code, string Name, string Description);

    /// <summary>
    /// User permission assignment (alternative to RolePermission for user-specific permissions)
    /// </summary>
    public class UserPermission
    {
        public Guid Id { get; set; }
        
        [Required]
        public string UserId { get; set; } = string.Empty;
        
        [Required]
        [StringLength(100)]
        public string Permission { get; set; } = string.Empty;
        
        public bool IsGranted { get; set; } = true;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public string? GrantedBy { get; set; }  // Admin user ID
        
        public string? Notes { get; set; }
    }
}
