using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JohnHenryFashionWeb.Data;
using JohnHenryFashionWeb.Models;
using JohnHenryFashionWeb.ViewModels;
using JohnHenryFashionWeb.Services;

namespace JohnHenryFashionWeb.Controllers.Api
{
    [ApiController]
    [Route("api/admin")]
    [Authorize(AuthenticationSchemes = "Identity.Application,Bearer", Roles = UserRoles.Admin)]
    public class AdminApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IAnalyticsService _analyticsService;
        private readonly IReportingService _reportingService;
        private readonly ILogger<AdminApiController> _logger;

        public AdminApiController(
            ApplicationDbContext context,
            IAnalyticsService analyticsService,
            IReportingService reportingService,
            ILogger<AdminApiController> logger)
        {
            _context = context;
            _analyticsService = analyticsService;
            _reportingService = reportingService;
            _logger = logger;
        }

        #region Dashboard APIs

        [HttpGet("dashboard/stats")]
        public async Task<IActionResult> GetDashboardStats()
        {
            try
            {
                var stats = new
                {
                    TotalProducts = await _context.Products.CountAsync(),
                    TotalOrders = await _context.Orders.CountAsync(),
                    TotalUsers = await _context.Users.CountAsync(),
                    TotalRevenue = await _context.Orders
                        .Where(o => o.Status == "completed")
                        .SumAsync(o => o.TotalAmount),
                    
                    TodayOrders = await _context.Orders
                        .Where(o => o.CreatedAt.Date == DateTime.UtcNow.Date)
                        .CountAsync(),
                    
                    TodayRevenue = await _context.Orders
                        .Where(o => o.CreatedAt.Date == DateTime.UtcNow.Date && o.Status == "completed")
                        .SumAsync(o => o.TotalAmount),
                    
                    PendingOrders = await _context.Orders
                        .Where(o => o.Status == "pending" || o.Status == "processing")
                        .CountAsync(),
                    
                    LowStockProducts = await _context.Products
                        .Where(p => p.StockQuantity <= 10 && p.IsActive)
                        .CountAsync()
                };

                return Ok(new { success = true, data = stats });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting dashboard stats");
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("dashboard/recent-orders")]
        public async Task<IActionResult> GetRecentOrders(int limit = 10)
        {
            try
            {
                var orders = await _context.Orders
                    .Include(o => o.User)
                    .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                    .OrderByDescending(o => o.CreatedAt)
                    .Take(limit)
                    .Select(o => new
                    {
                        o.Id,
                        o.OrderNumber,
                        CustomerName = o.User.FullName,
                        CustomerEmail = o.User.Email,
                        o.TotalAmount,
                        o.Status,
                        o.CreatedAt,
                        ItemCount = o.OrderItems.Count
                    })
                    .ToListAsync();

                return Ok(new { success = true, data = orders });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recent orders");
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        #endregion

        #region Product APIs

        [HttpGet("products")]
        public async Task<IActionResult> GetProducts(
            int page = 1, 
            int pageSize = 10, 
            string? search = null, 
            Guid? categoryId = null, 
            string? status = null)
        {
            try
            {
                var query = _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.Brand)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(p => p.Name.Contains(search) || p.SKU.Contains(search));
                }

                if (categoryId.HasValue)
                {
                    query = query.Where(p => p.CategoryId == categoryId.Value);
                }

                if (!string.IsNullOrEmpty(status))
                {
                    query = query.Where(p => p.Status == status);
                }

                var totalProducts = await query.CountAsync();
                var products = await query
                    .OrderByDescending(p => p.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(p => new
                    {
                        p.Id,
                        p.Name,
                        p.SKU,
                        p.Price,
                        p.SalePrice,
                        p.StockQuantity,
                        p.Status,
                        CategoryName = p.Category.Name,
                        BrandName = p.Brand != null ? p.Brand.Name : null,
                        p.FeaturedImageUrl,
                        p.CreatedAt,
                        p.IsFeatured,
                        p.IsActive
                    })
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    data = products,
                    pagination = new
                    {
                        currentPage = page,
                        pageSize = pageSize,
                        totalItems = totalProducts,
                        totalPages = (int)Math.Ceiling((double)totalProducts / pageSize)
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting products");
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("products/{id}/toggle-status")]
        public async Task<IActionResult> ToggleProductStatus(Guid id)
        {
            try
            {
                var product = await _context.Products.FindAsync(id);
                if (product == null)
                {
                    return NotFound(new { success = false, message = "Sản phẩm không tồn tại" });
                }

                product.IsActive = !product.IsActive;
                product.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Ok(new 
                { 
                    success = true, 
                    message = product.IsActive ? "Đã kích hoạt sản phẩm" : "Đã vô hiệu hóa sản phẩm",
                    isActive = product.IsActive
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling product status for {ProductId}", id);
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        #endregion

        #region Order APIs

        [HttpGet("orders")]
        public async Task<IActionResult> GetOrders(
            int page = 1,
            int pageSize = 20,
            string? search = null,
            string? status = null,
            DateTime? fromDate = null,
            DateTime? toDate = null)
        {
            try
            {
                var query = _context.Orders
                    .Include(o => o.User)
                    .Include(o => o.OrderItems)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(o => o.OrderNumber.Contains(search) ||
                                           (o.User != null && o.User.Email != null && o.User.Email.Contains(search)) ||
                                           (o.User != null && o.User.FirstName != null && o.User.FirstName.Contains(search)) ||
                                           (o.User != null && o.User.LastName != null && o.User.LastName.Contains(search)));
                }

                if (!string.IsNullOrEmpty(status))
                {
                    query = query.Where(o => o.Status == status);
                }

                if (fromDate.HasValue)
                {
                    query = query.Where(o => o.CreatedAt >= fromDate.Value);
                }

                if (toDate.HasValue)
                {
                    query = query.Where(o => o.CreatedAt <= toDate.Value);
                }

                var totalOrders = await query.CountAsync();
                var orders = await query
                    .OrderByDescending(o => o.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(o => new
                    {
                        o.Id,
                        o.OrderNumber,
                        CustomerName = o.User.FullName,
                        CustomerEmail = o.User.Email,
                        o.TotalAmount,
                        o.Status,
                        o.PaymentStatus,
                        o.CreatedAt,
                        ItemCount = o.OrderItems.Count
                    })
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    data = orders,
                    pagination = new
                    {
                        currentPage = page,
                        pageSize = pageSize,
                        totalItems = totalOrders,
                        totalPages = (int)Math.Ceiling((double)totalOrders / pageSize)
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting orders");
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("orders/{id}/status")]
        public async Task<IActionResult> UpdateOrderStatus(Guid id, [FromBody] UpdateOrderStatusRequest request)
        {
            try
            {
                var order = await _context.Orders.FindAsync(id);
                if (order == null)
                {
                    return NotFound(new { success = false, message = "Đơn hàng không tồn tại" });
                }

                order.Status = request.Status;
                order.UpdatedAt = DateTime.UtcNow;

                if (request.Status == "shipped" && !order.ShippedAt.HasValue)
                {
                    order.ShippedAt = DateTime.UtcNow;
                }
                else if (request.Status == "delivered" && !order.DeliveredAt.HasValue)
                {
                    order.DeliveredAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();

                return Ok(new 
                { 
                    success = true, 
                    message = "Đã cập nhật trạng thái đơn hàng",
                    status = order.Status
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order status for {OrderId}", id);
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        #endregion

        #region Analytics APIs

        [HttpGet("analytics/realtime")]
        public async Task<IActionResult> GetRealTimeAnalytics()
        {
            try
            {
                var analytics = await _analyticsService.GetRealTimeAnalyticsAsync();
                return Ok(new { success = true, data = analytics });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting real-time analytics");
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("analytics/sales")]
        public async Task<IActionResult> GetSalesAnalytics(
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null)
        {
            try
            {
                var from = fromDate ?? DateTime.UtcNow.AddDays(-30);
                var to = toDate ?? DateTime.UtcNow;

                var analytics = await _analyticsService.GetSalesAnalyticsAsync(from, to);
                return Ok(new { success = true, data = analytics });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting sales analytics");
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        #endregion

        #region User APIs

        [HttpGet("users")]
        public async Task<IActionResult> GetUsers(
            int page = 1,
            int pageSize = 20,
            string? search = null,
            string? role = null)
        {
            try
            {
                var query = _context.Users.AsQueryable();

                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(u => (u.Email != null && u.Email.Contains(search)) ||
                                           (u.FirstName != null && u.FirstName.Contains(search)) ||
                                           (u.LastName != null && u.LastName.Contains(search)));
                }

                var totalUsers = await query.CountAsync();
                var users = await query
                    .OrderByDescending(u => u.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(u => new
                    {
                        u.Id,
                        u.Email,
                        u.FirstName,
                        u.LastName,
                        FullName = u.FullName,
                        u.PhoneNumber,
                        u.IsActive,
                        u.EmailConfirmed,
                        u.CreatedAt,
                        u.LastLoginDate
                    })
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    data = users,
                    pagination = new
                    {
                        currentPage = page,
                        pageSize = pageSize,
                        totalItems = totalUsers,
                        totalPages = (int)Math.Ceiling((double)totalUsers / pageSize)
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting users");
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("users/{id}/toggle-status")]
        public async Task<IActionResult> ToggleUserStatus(string id)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                {
                    return NotFound(new { success = false, message = "Người dùng không tồn tại" });
                }

                user.IsActive = !user.IsActive;
                await _context.SaveChangesAsync();

                return Ok(new 
                { 
                    success = true, 
                    message = user.IsActive ? "Đã kích hoạt người dùng" : "Đã vô hiệu hóa người dùng",
                    isActive = user.IsActive
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling user status for {UserId}", id);
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        #endregion

        #region System APIs

        [HttpGet("system/health")]
        public async Task<IActionResult> GetSystemHealth()
        {
            try
            {
                var health = new
                {
                    Status = "Healthy",
                    Timestamp = DateTime.UtcNow,
                    Database = await CheckDatabaseHealthAsync(),
                    Memory = new
                    {
                        WorkingSet = Environment.WorkingSet,
                        GcMemory = GC.GetTotalMemory(false)
                    },
                    Uptime = DateTime.UtcNow - System.Diagnostics.Process.GetCurrentProcess().StartTime
                };

                return Ok(new { success = true, data = health });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting system health");
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        #endregion

        #region Banner APIs

        [HttpGet("banners/{id}")]
        public async Task<IActionResult> GetBanner(Guid id)
        {
            try
            {
                var banner = await _context.MarketingBanners.FindAsync(id);
                if (banner == null)
                {
                    return NotFound(new { success = false, message = "Banner không tồn tại" });
                }

                return Ok(new { success = true, data = banner });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting banner {BannerId}", id);
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("banners")]
        [RequestSizeLimit(10_000_000)] // 10MB limit for images
        public async Task<IActionResult> CreateBanner([FromForm] BannerFormRequest request)
        {
            try
            {
                var banner = new MarketingBanner
                {
                    Id = Guid.NewGuid(),
                    Title = request.Title,
                    Description = request.Description,
                    Position = request.Position,
                    TargetPage = request.TargetPage,
                    LinkUrl = request.LinkUrl,
                    OpenInNewTab = request.OpenInNewTab,
                    SortOrder = request.SortOrder,
                    IsActive = request.IsActive,
                    StartDate = request.StartDate ?? DateTime.UtcNow,
                    EndDate = request.EndDate,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                // Handle image upload
                if (request.imageFile != null)
                {
                    var imageUrl = await SaveBannerImageAsync(request.imageFile, banner.Id);
                    banner.ImageUrl = imageUrl;
                }
                else if (!string.IsNullOrEmpty(request.ImageUrl))
                {
                    banner.ImageUrl = request.ImageUrl;
                }
                else
                {
                    return BadRequest(new { success = false, message = "Vui lòng chọn hình ảnh banner" });
                }

                // Handle mobile image upload
                if (request.mobileImageFile != null)
                {
                    var mobileImageUrl = await SaveBannerImageAsync(request.mobileImageFile, banner.Id, "mobile");
                    banner.MobileImageUrl = mobileImageUrl;
                }
                else if (!string.IsNullOrEmpty(request.MobileImageUrl))
                {
                    banner.MobileImageUrl = request.MobileImageUrl;
                }

                _context.MarketingBanners.Add(banner);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Banner created: {BannerId} - {BannerTitle} by {User}", 
                    banner.Id, banner.Title, User.Identity?.Name);

                return Ok(new { 
                    success = true, 
                    message = "Banner đã được tạo thành công",
                    data = banner 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating banner");
                return BadRequest(new { success = false, message = "Có lỗi xảy ra khi thêm tin banner: " + ex.Message });
            }
        }

        [HttpPost("banners/{id}/update")]
        [RequestSizeLimit(10_000_000)]
        public async Task<IActionResult> UpdateBanner(Guid id, [FromForm] BannerFormRequest request)
        {
            try
            {
                var banner = await _context.MarketingBanners.FindAsync(id);
                if (banner == null)
                {
                    return NotFound(new { success = false, message = "Banner không tồn tại" });
                }

                banner.Title = request.Title;
                banner.Description = request.Description;
                banner.Position = request.Position;
                banner.TargetPage = request.TargetPage;
                banner.LinkUrl = request.LinkUrl;
                banner.OpenInNewTab = request.OpenInNewTab;
                banner.SortOrder = request.SortOrder;
                banner.IsActive = request.IsActive;
                banner.StartDate = request.StartDate ?? banner.StartDate;
                banner.EndDate = request.EndDate;
                banner.UpdatedAt = DateTime.UtcNow;

                // Handle image upload
                if (request.imageFile != null)
                {
                    var imageUrl = await SaveBannerImageAsync(request.imageFile, banner.Id);
                    banner.ImageUrl = imageUrl;
                }
                else if (!string.IsNullOrEmpty(request.ImageUrl))
                {
                    banner.ImageUrl = request.ImageUrl;
                }

                // Handle mobile image upload
                if (request.mobileImageFile != null)
                {
                    var mobileImageUrl = await SaveBannerImageAsync(request.mobileImageFile, banner.Id, "mobile");
                    banner.MobileImageUrl = mobileImageUrl;
                }
                else if (!string.IsNullOrEmpty(request.MobileImageUrl))
                {
                    banner.MobileImageUrl = request.MobileImageUrl;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Banner updated: {BannerId} - {BannerTitle} by {User}", 
                    banner.Id, banner.Title, User.Identity?.Name);

                return Ok(new { 
                    success = true, 
                    message = "Banner đã được cập nhật thành công",
                    data = banner 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating banner {BannerId}", id);
                return BadRequest(new { success = false, message = "Có lỗi xảy ra khi cập nhật banner: " + ex.Message });
            }
        }

        [HttpPost("banners/{id}/delete")]
        public async Task<IActionResult> DeleteBanner(Guid id)
        {
            try
            {
                var banner = await _context.MarketingBanners.FindAsync(id);
                if (banner == null)
                {
                    return NotFound(new { success = false, message = "Banner không tồn tại" });
                }

                _context.MarketingBanners.Remove(banner);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Banner deleted: {BannerId} - {BannerTitle} by {User}", 
                    banner.Id, banner.Title, User.Identity?.Name);

                return Ok(new { success = true, message = "Banner đã được xóa thành công" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting banner {BannerId}", id);
                return BadRequest(new { success = false, message = "Có lỗi xảy ra khi xóa banner: " + ex.Message });
            }
        }

        [HttpPost("banners/{id}/toggle")]
        public async Task<IActionResult> ToggleBanner(Guid id)
        {
            try
            {
                var banner = await _context.MarketingBanners.FindAsync(id);
                if (banner == null)
                {
                    return NotFound(new { success = false, message = "Banner không tồn tại" });
                }

                banner.IsActive = !banner.IsActive;
                banner.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                var status = banner.IsActive ? "kích hoạt" : "tạm dừng";
                _logger.LogInformation("Banner toggled: {BannerId} - {BannerTitle} is now {Status} by {User}", 
                    banner.Id, banner.Title, status, User.Identity?.Name);

                return Ok(new { 
                    success = true, 
                    message = $"Banner đã được {status}",
                    data = new { isActive = banner.IsActive }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling banner {BannerId}", id);
                return BadRequest(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        [HttpPost("banners/{id}/activate")]
        public async Task<IActionResult> ActivateBanner(Guid id)
        {
            try
            {
                var banner = await _context.MarketingBanners.FindAsync(id);
                if (banner == null)
                {
                    return NotFound(new { success = false, message = "Banner không tồn tại" });
                }

                banner.IsActive = true;
                banner.StartDate = DateTime.UtcNow;
                banner.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Banner activated: {BannerId} - {BannerTitle} by {User}", 
                    banner.Id, banner.Title, User.Identity?.Name);

                return Ok(new { success = true, message = "Banner đã được kích hoạt" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error activating banner {BannerId}", id);
                return BadRequest(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        [HttpPost("banners/{id}/reorder")]
        public async Task<IActionResult> ReorderBanner(Guid id, [FromBody] ReorderBannerRequest request)
        {
            try
            {
                var banner = await _context.MarketingBanners.FindAsync(id);
                if (banner == null)
                {
                    return NotFound(new { success = false, message = "Banner không tồn tại" });
                }

                // Get all banners in the same position (and same TargetPage if applicable)
                var query = _context.MarketingBanners
                    .Where(b => b.Position == request.Position);
                
                // For collection_hero banners, also filter by TargetPage
                if (!string.IsNullOrEmpty(request.TargetPage))
                {
                    query = query.Where(b => b.TargetPage == request.TargetPage);
                }
                
                var bannersInPosition = await query
                    .OrderBy(b => b.SortOrder)
                    .ToListAsync();

                var oldSortOrder = banner.SortOrder;
                var newSortOrder = request.NewSortOrder;

                // If moving to a higher position (lower number)
                if (newSortOrder < oldSortOrder)
                {
                    // Shift banners down that are between new and old position
                    foreach (var b in bannersInPosition)
                    {
                        if (b.Id != id && b.SortOrder >= newSortOrder && b.SortOrder < oldSortOrder)
                        {
                            b.SortOrder++;
                            b.UpdatedAt = DateTime.UtcNow;
                        }
                    }
                }
                // If moving to a lower position (higher number)
                else if (newSortOrder > oldSortOrder)
                {
                    // Shift banners up that are between old and new position
                    foreach (var b in bannersInPosition)
                    {
                        if (b.Id != id && b.SortOrder > oldSortOrder && b.SortOrder <= newSortOrder)
                        {
                            b.SortOrder--;
                            b.UpdatedAt = DateTime.UtcNow;
                        }
                    }
                }

                // Update the dragged banner's sort order
                banner.SortOrder = newSortOrder;
                banner.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Banner reordered: {BannerId} - {BannerTitle} from position {OldSort} to {NewSort} by {User}", 
                    banner.Id, banner.Title, oldSortOrder, newSortOrder, User.Identity?.Name);

                return Ok(new { success = true, message = "Đã cập nhật thứ tự banner" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reordering banner {BannerId}", id);
                return BadRequest(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        private async Task<string> SaveBannerImageAsync(IFormFile file, Guid bannerId, string suffix = "")
        {
            try
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "banners");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var fileExtension = Path.GetExtension(file.FileName);
                var fileName = suffix != string.Empty 
                    ? $"{bannerId}_{suffix}{fileExtension}"
                    : $"{bannerId}{fileExtension}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(fileStream);
                }

                return $"/uploads/banners/{fileName}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving banner image for {BannerId}", bannerId);
                throw;
            }
        }

        #endregion

        #region Helper Methods

        private async Task<object> CheckDatabaseHealthAsync()
        {
            try
            {
                var canConnect = await _context.Database.CanConnectAsync();
                return new { Status = canConnect ? "Connected" : "Disconnected" };
            }
            catch (Exception ex)
            {
                return new { Status = "Error", Message = ex.Message };
            }
        }

        #endregion
    }

    // Request DTOs
    public class UpdateOrderStatusRequest
    {
        public string Status { get; set; } = string.Empty;
        public string? Notes { get; set; }
    }

    public class BannerFormRequest
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Position { get; set; } = string.Empty;
        public string? TargetPage { get; set; }
        public string? LinkUrl { get; set; }
        public bool OpenInNewTab { get; set; }
        public int SortOrder { get; set; }
        public bool IsActive { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? ImageUrl { get; set; }
        public string? MobileImageUrl { get; set; }
        public IFormFile? imageFile { get; set; }
        public IFormFile? mobileImageFile { get; set; }
    }

    public class ReorderBannerRequest
    {
        public string Position { get; set; } = string.Empty;
        public string? TargetPage { get; set; }
        public int NewSortOrder { get; set; }
        public int OldSortOrder { get; set; }
    }
}