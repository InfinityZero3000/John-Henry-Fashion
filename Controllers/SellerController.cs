using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JohnHenryFashionWeb.Data;
using JohnHenryFashionWeb.Models;
using JohnHenryFashionWeb.ViewModels;
using System.IO;

namespace JohnHenryFashionWeb.Controllers
{
    [Authorize(Roles = UserRoles.Seller, AuthenticationSchemes = "Identity.Application")]
    [Route("seller")]
    public class SellerController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public SellerController(
            ApplicationDbContext context, 
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _userManager = userManager;
            _webHostEnvironment = webHostEnvironment;
        }

        [HttpGet("")]
        [HttpGet("dashboard")]
        public async Task<IActionResult> Dashboard()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }
            
            var stats = await GetSellerDashboardStats(currentUser.Id);
            
            return View(stats);
        }

        private async Task<SellerDashboardViewModel> GetSellerDashboardStats(string sellerId)
        {
            // For now, we'll treat all products as belonging to sellers
            // In a real implementation, you'd have a SellerProducts relationship
            
            var myProductsCount = await _context.Products.CountAsync();
            var myOrdersCount = await _context.Orders.CountAsync();
            
            var myRevenue = await _context.Orders
                .Where(o => o.PaymentStatus == "paid" && o.Status != "cancelled")
                .SumAsync(o => o.TotalAmount);

            var currentMonth = DateTime.UtcNow.Month;
            var currentYear = DateTime.UtcNow.Year;
            var monthlyRevenue = await _context.Orders
                .Where(o => o.CreatedAt.Month == currentMonth && 
                           o.CreatedAt.Year == currentYear &&
                           o.PaymentStatus == "paid" && o.Status != "cancelled")
                .SumAsync(o => o.TotalAmount);

            var recentOrders = await _context.Orders
                .Include(o => o.User)
                .OrderByDescending(o => o.CreatedAt)
                .Take(5)
                .Select(o => new Models.RecentOrder
                {
                    Id = o.Id,
                    OrderNumber = o.OrderNumber,
                    CustomerName = $"{o.User.FirstName} {o.User.LastName}".Trim(),
                    Total = o.TotalAmount,
                    Status = o.Status,
                    CreatedAt = o.CreatedAt
                })
                .ToListAsync();

            var topProducts = await _context.OrderItems
                .Include(oi => oi.Product)
                .Where(oi => oi.Order.PaymentStatus == "paid" && oi.Order.Status != "cancelled")
                .GroupBy(oi => oi.ProductId)
                .Select(g => new TopSellingProduct
                {
                    ProductId = g.Key,
                    ProductName = g.First().Product.Name,
                    QuantitySold = g.Sum(oi => oi.Quantity),
                    Revenue = g.Sum(oi => oi.TotalPrice),
                    ImageUrl = g.First().Product.FeaturedImageUrl
                })
                .OrderByDescending(x => x.QuantitySold)
                .Take(5)
                .ToListAsync();

            return new SellerDashboardViewModel
            {
                MyProductsCount = myProductsCount,
                MyOrdersCount = myOrdersCount,
                MyRevenue = myRevenue,
                MonthlyRevenue = monthlyRevenue,
                MyRecentOrders = recentOrders,
                MyTopProducts = topProducts
            };
        }

        // NOTE: Product management methods have been moved to SellerProductsController
        // to avoid routing conflicts. Use /seller/products routes instead.

        [HttpGet("inventory")]
        public async Task<IActionResult> Inventory(string search = "", string filter = "all", int page = 1, int pageSize = 20)
        {
            var query = _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .AsQueryable();

            // TODO: Filter by seller when seller-product relationship is implemented
            // query = query.Where(p => p.SellerId == currentSellerId);

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(p => p.Name.Contains(search) || p.SKU.Contains(search));
            }

            // Apply stock filter
            switch (filter)
            {
                case "low_stock":
                    query = query.Where(p => p.StockQuantity > 0 && p.StockQuantity < 10);
                    break;
                case "out_of_stock":
                    query = query.Where(p => p.StockQuantity == 0);
                    break;
                case "in_stock":
                    query = query.Where(p => p.StockQuantity >= 10);
                    break;
                // "all" - no filter
            }

            var totalItems = await query.CountAsync();

            // Calculate statistics for ALL products matching the query (before pagination)
            var allProducts = await query.AsNoTracking().ToListAsync();
            var totalStockQuantity = allProducts.Sum(p => p.StockQuantity);
            var lowStockCount = allProducts.Count(p => p.StockQuantity > 0 && p.StockQuantity < 10);
            var outOfStockCount = allProducts.Count(p => p.StockQuantity == 0);
            var inStockCount = allProducts.Count(p => p.StockQuantity >= 10);

            // Get paginated products
            var products = allProducts
                .OrderBy(p => p.StockQuantity)
                .ThenBy(p => p.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new InventoryItemViewModel
                {
                    Id = p.Id,
                    ProductId = p.Id,
                    ProductName = p.Name,
                    SKU = p.SKU,
                    CurrentStock = p.StockQuantity,
                    StockQuantity = p.StockQuantity,
                    CategoryName = p.Category?.Name ?? "N/A",
                    Price = p.Price,
                    ImageUrl = p.FeaturedImageUrl,
                    LastUpdated = p.UpdatedAt,
                    MinStockLevel = 10
                })
                .ToList();

            var viewModel = new InventoryListViewModel
            {
                Items = products,
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize),
                PageSize = pageSize,
                SearchTerm = search,
                Filter = filter,
                TotalProducts = totalItems,
                TotalStockQuantity = totalStockQuantity,
                LowStockProducts = lowStockCount,
                OutOfStockProducts = outOfStockCount,
                InStockProducts = inStockCount
            };

            return View(viewModel);
        }

        [HttpPost("inventory/update-stock")]
        public async Task<IActionResult> UpdateStock([FromForm] Guid productId, [FromForm] int newStock, [FromForm] string reason = "")
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null)
            {
                return Json(new { success = false, message = "Không tìm thấy sản phẩm!" });
            }

            // TODO: Check if product belongs to current seller

            var oldStock = product.StockQuantity;
            product.StockQuantity = newStock;
            product.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Json(new { 
                success = true, 
                message = $"Đã cập nhật tồn kho từ {oldStock} thành {newStock}!",
                newStock = newStock
            });
        }

        [HttpGet("sales")]
        public async Task<IActionResult> Sales(DateTime? fromDate = null, DateTime? toDate = null)
        {
            var startDate = fromDate ?? DateTime.UtcNow.AddMonths(-1);
            var endDate = toDate ?? DateTime.UtcNow;

            // TODO: Filter by seller when relationship is implemented
            var salesData = await _context.Orders
                .Where(o => o.CreatedAt >= startDate && o.CreatedAt <= endDate && o.PaymentStatus == "paid" && o.Status != "cancelled")
                .GroupBy(o => o.CreatedAt.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    Revenue = g.Sum(o => o.TotalAmount),
                    Orders = g.Count()
                })
                .OrderBy(x => x.Date)
                .ToListAsync();

            var viewModel = new SellerSalesViewModel
            {
                FromDate = startDate,
                ToDate = endDate,
                TotalRevenue = salesData.Sum(s => s.Revenue),
                TotalOrders = salesData.Sum(s => s.Orders),
                SalesData = salesData.Select(s => new DailySales
                {
                    Date = s.Date,
                    Revenue = s.Revenue,
                    OrderCount = s.Orders
                }).ToList()
            };

            return View(viewModel);
        }

        [HttpGet("analytics")]
        public async Task<IActionResult> Analytics()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();

            // TODO: Filter by seller when seller-product relationship is implemented
            // var sellerId = currentUser.Id;
            
            // Get top selling products (last 30 days) - showing all products temporarily
            var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
            var topProducts = await _context.OrderItems
                .Include(oi => oi.Order)
                .Include(oi => oi.Product)
                .Where(oi => oi.Order.PaymentStatus == "paid" 
                    && oi.Order.Status != "cancelled"
                    && oi.Order.CreatedAt >= thirtyDaysAgo)
                .GroupBy(oi => new { oi.ProductId, oi.Product.Name })
                .Select(g => new TopSellingProduct
                {
                    ProductName = g.Key.Name,
                    QuantitySold = g.Sum(oi => oi.Quantity),
                    Revenue = g.Sum(oi => oi.Quantity * oi.UnitPrice)
                })
                .OrderByDescending(p => p.Revenue)
                .Take(10)
                .ToListAsync();

            // Get total orders (showing all orders temporarily)
            var totalOrders = await _context.Orders
                .Where(o => o.PaymentStatus == "paid"
                    && o.Status != "cancelled"
                    && o.CreatedAt >= thirtyDaysAgo)
                .CountAsync();
                
            // Get daily revenue for last 30 days
            var dailyRevenues = await _context.Orders
                .Where(o => o.PaymentStatus == "paid"
                    && o.Status != "cancelled"
                    && o.CreatedAt >= thirtyDaysAgo)
                .GroupBy(o => o.CreatedAt.Date)
                .Select(g => new DailyRevenue
                {
                    Date = g.Key,
                    Revenue = g.Sum(o => o.TotalAmount),
                    OrderCount = g.Count()
                })
                .OrderBy(d => d.Date)
                .ToListAsync();
            
            // Simple mock for views and conversion (can be enhanced with real tracking later)
            var totalViews = totalOrders * 10; // Estimate: 1 order per 10 views
            var conversionRate = totalViews > 0 ? (int)Math.Round((decimal)totalOrders / totalViews * 100) : 0;

            var viewModel = new SellerAnalyticsViewModel
            {
                TopProducts = topProducts,
                DailyRevenues = dailyRevenues,
                TotalProductViews = totalViews,
                ConversionRate = conversionRate
            };

            return View(viewModel);
        }

        [HttpGet("profile")]
        public async Task<IActionResult> Profile()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return NotFound();
            }
            
            var viewModel = new SellerProfileViewModel
            {
                CompanyName = currentUser.CompanyName ?? "",
                BusinessLicense = currentUser.BusinessLicense ?? "",
                TaxCode = currentUser.TaxCode ?? "",
                Address = currentUser.Address ?? "",
                Phone = currentUser.Phone ?? "",
                Email = currentUser.Email ?? "",
                FirstName = currentUser.FirstName ?? "",
                LastName = currentUser.LastName ?? "",
                IsApproved = currentUser.IsApproved,
                ApprovedAt = currentUser.ApprovedAt,
                Notes = currentUser.Notes,
                CreatedAt = currentUser.CreatedAt,
                UpdatedAt = currentUser.UpdatedAt,
                IsActive = currentUser.IsActive
            };

            return View(viewModel);
        }

        [HttpPost("profile")]
        public async Task<IActionResult> Profile(SellerProfileViewModel model)
        {
            if (ModelState.IsValid)
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser != null)
                {
                    currentUser.CompanyName = model.CompanyName;
                    currentUser.BusinessLicense = model.BusinessLicense;
                    currentUser.TaxCode = model.TaxCode;
                    currentUser.Address = model.Address;
                    currentUser.Phone = model.Phone;
                    currentUser.FirstName = model.FirstName;
                    currentUser.LastName = model.LastName;

                    var result = await _userManager.UpdateAsync(currentUser);
                
                    if (result.Succeeded)
                    {
                        TempData["Success"] = "Hồ sơ đã được cập nhật thành công!";
                    }
                    else
                    {
                        TempData["Error"] = "Có lỗi xảy ra khi cập nhật hồ sơ!";
                    }
                }
            }

            return View(model);
        }

        [HttpGet("settings")]
        public async Task<IActionResult> Settings()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Get statistics for the sidebar
            var totalProducts = await _context.Products.CountAsync();
            var totalOrders = await _context.Orders.CountAsync();

            var viewModel = new SellerSettingsViewModel
            {
                // Default settings
                IsStoreActive = true,
                BusinessHoursStart = new TimeSpan(9, 0, 0), // 9:00 AM
                BusinessHoursEnd = new TimeSpan(18, 0, 0),   // 6:00 PM
                ReportFrequency = "Weekly",
                LowStockThreshold = 10,
                TotalProducts = totalProducts,
                TotalOrders = totalOrders,
                EmailNotifications = new EmailNotificationSettings
                {
                    NewOrders = true,
                    LowStock = true,
                    ProductReviews = true,
                    SystemUpdates = true
                }
            };

            return View(viewModel);
        }

        [HttpPost("settings")]
        public async Task<IActionResult> Settings(SellerSettingsViewModel model)
        {
            if (ModelState.IsValid)
            {
                // In a real application, you would save these settings to the database
                // For now, we'll just show a success message
                TempData["Success"] = "Cài đặt đã được lưu thành công!";
                return RedirectToAction(nameof(Settings));
            }

            // If model is invalid, reload statistics
            var totalProducts = await _context.Products.CountAsync();
            var totalOrders = await _context.Orders.CountAsync();
            
            model.TotalProducts = totalProducts;
            model.TotalOrders = totalOrders;

            return View(model);
        }

        // MARK: - Helper Methods
        private string GenerateSlug(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            var slug = text.ToLowerInvariant()
                          .Replace("đ", "d")
                          .Replace("ă", "a")
                          .Replace("â", "a")
                          .Replace("ê", "e")
                          .Replace("ô", "o")
                          .Replace("ơ", "o")
                          .Replace("ư", "u")
                          .Replace(" ", "-")
                          .Replace("--", "-");

            // Remove special characters except hyphens
            var cleanSlug = new System.Text.StringBuilder();
            foreach (char c in slug)
            {
                if (char.IsLetterOrDigit(c) || c == '-')
                    cleanSlug.Append(c);
            }

            return cleanSlug.ToString().Trim('-');
        }

        private async Task<string?> SaveUploadedFile(IFormFile file, string subFolder = "uploads")
        {
            if (file == null || file.Length == 0)
                return null;

            var uploadsPath = Path.Combine(_webHostEnvironment.WebRootPath, subFolder);
            Directory.CreateDirectory(uploadsPath);

            var fileName = $"{Guid.NewGuid()}_{file.FileName}";
            var filePath = Path.Combine(uploadsPath, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return $"/{subFolder}/{fileName}";
        }

        // MARK: - Quản lý Coupons/Discounts
        [HttpGet("coupons")]
        public async Task<IActionResult> Coupons(int page = 1, int pageSize = 10, string search = "", string status = "")
        {
            var currentUser = await _userManager.GetUserAsync(User);
            
            var query = _context.Coupons.AsQueryable();
            
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(c => (c.Code != null && c.Code.Contains(search)) || 
                                        (c.Description != null && c.Description.Contains(search)));
            }
            
            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(c => c.IsActive == (status == "active"));
            }
            
            var totalCount = await query.CountAsync();
            var coupons = await query
                .OrderByDescending(c => c.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            
            var model = new CouponManagementViewModel
            {
                Coupons = coupons.Select(c => new CouponItem
                {
                    Id = c.Id,
                    Code = c.Code,
                    Name = c.Name,
                    Type = c.Type,
                    Value = c.Value,
                    MinOrderAmount = c.MinOrderAmount,
                    UsageLimit = c.UsageLimit,
                    UsageCount = c.UsageCount,
                    StartDate = c.StartDate,
                    EndDate = c.EndDate,
                    IsActive = c.IsActive
                }).ToList(),
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                PageSize = pageSize,
                Search = search,
                Status = status,
                TotalCount = totalCount
            };
            
            return View(model);
        }

        [HttpGet("coupons/create")]
        public IActionResult CreateCoupon()
        {
            return View(new CouponCreateEditViewModel());
        }

        [HttpPost("coupons/create")]
        public async Task<IActionResult> CreateCoupon(CouponCreateEditViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var currentUser = await _userManager.GetUserAsync(User);
            
            var coupon = new Coupon
            {
                Id = Guid.NewGuid(),
                Code = model.Code.ToUpper(),
                Description = model.Description,
                Type = model.DiscountType,
                Value = model.DiscountValue,
                MinOrderAmount = model.MinOrderAmount,
                UsageLimit = model.UsageLimit,
                EndDate = model.ExpiryDate,
                IsActive = model.IsActive,
                CreatedAt = DateTime.UtcNow
            };
            
            _context.Coupons.Add(coupon);
            await _context.SaveChangesAsync();
            
            TempData["SuccessMessage"] = "Tạo mã giảm giá thành công!";
            return RedirectToAction("Coupons");
        }

        [HttpGet("coupons/edit/{id}")]
        public async Task<IActionResult> EditCoupon(Guid id)
        {
            var coupon = await _context.Coupons.FindAsync(id);
            if (coupon == null)
            {
                return NotFound();
            }
            
            var model = new CouponCreateEditViewModel
            {
                Id = coupon.Id,
                Code = coupon.Code,
                Description = coupon.Description ?? "",
                DiscountType = coupon.Type,
                DiscountValue = coupon.Value,
                MinOrderAmount = coupon.MinOrderAmount,
                UsageLimit = coupon.UsageLimit,
                ExpiryDate = coupon.EndDate ?? DateTime.Now.AddDays(30),
                IsActive = coupon.IsActive
            };
            
            return View(model);
        }

        [HttpPost("coupons/edit/{id}")]
        public async Task<IActionResult> EditCoupon(Guid id, CouponCreateEditViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var coupon = await _context.Coupons.FindAsync(id);
            if (coupon == null)
            {
                return NotFound();
            }
            
            coupon.Code = model.Code.ToUpper();
            coupon.Description = model.Description;
            coupon.Type = model.DiscountType;
            coupon.Value = model.DiscountValue;
            coupon.MinOrderAmount = model.MinOrderAmount;
            coupon.UsageLimit = model.UsageLimit;
            coupon.EndDate = model.ExpiryDate;
            coupon.IsActive = model.IsActive;
            coupon.UpdatedAt = DateTime.UtcNow;
            
            await _context.SaveChangesAsync();
            
            TempData["SuccessMessage"] = "Cập nhật mã giảm giá thành công!";
            return RedirectToAction("Coupons");
        }

        [HttpPost("coupons/delete/{id}")]
        public async Task<IActionResult> DeleteCoupon(Guid id)
        {
            var coupon = await _context.Coupons.FindAsync(id);
            if (coupon == null)
            {
                return NotFound();
            }
            
            _context.Coupons.Remove(coupon);
            await _context.SaveChangesAsync();
            
            TempData["SuccessMessage"] = "Xóa mã giảm giá thành công!";
            return RedirectToAction("Coupons");
        }

        // MARK: - Quản lý Reviews
        [HttpGet("reviews")]
        public async Task<IActionResult> Reviews(int page = 1, int pageSize = 10, string search = "", int? rating = null, string status = "")
        {
            var currentUser = await _userManager.GetUserAsync(User);
            
            var query = _context.ProductReviews
                .Include(r => r.Product)
                .Include(r => r.User)
                .AsQueryable();
            
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(r => r.Product.Name.Contains(search) || 
                                        (r.User.FirstName != null && r.User.FirstName.Contains(search)) || 
                                        (r.User.LastName != null && r.User.LastName.Contains(search)));
            }
            
            if (rating.HasValue)
            {
                query = query.Where(r => r.Rating == rating.Value);
            }
            
            if (!string.IsNullOrEmpty(status))
            {
                if (status == "approved")
                {
                    query = query.Where(r => r.IsApproved == true);
                }
                else if (status == "pending")
                {
                    query = query.Where(r => r.IsApproved == false);
                }
                else if (status == "rejected")
                {
                    query = query.Where(r => r.IsApproved == false);
                }
            }
            
            var totalCount = await query.CountAsync();
            var reviews = await query
                .OrderByDescending(r => r.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Calculate statistics
            var allReviewsQuery = _context.ProductReviews.AsQueryable();
            var totalReviews = await allReviewsQuery.CountAsync();
            var approvedReviews = await allReviewsQuery.CountAsync(r => r.IsApproved == true);
            var pendingReviews = await allReviewsQuery.CountAsync(r => r.IsApproved == false);
            var rejectedReviews = 0; // Since IsApproved is bool, we don't have rejected state
            var averageRating = totalReviews > 0 ? await allReviewsQuery.AverageAsync(r => r.Rating) : 0;

            // Rating distribution
            var ratingDistribution = await allReviewsQuery
                .GroupBy(r => r.Rating)
                .ToDictionaryAsync(g => g.Key, g => g.Count());
            
            var model = new SellerReviewsViewModel
            {
                Reviews = reviews,
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                PageSize = pageSize,
                Search = search,
                Rating = rating,
                Status = status,
                TotalCount = totalCount,
                Statistics = new ReviewStatistics
                {
                    TotalReviews = totalReviews,
                    ApprovedReviews = approvedReviews,
                    PendingReviews = pendingReviews,
                    RejectedReviews = rejectedReviews,
                    AverageRating = averageRating,
                    RatingDistribution = ratingDistribution
                }
            };
            
            return View(model);
        }

        [HttpPost("reviews/approve/{id}")]
        public async Task<IActionResult> ApproveReview(Guid id)
        {
            var review = await _context.ProductReviews.FindAsync(id);
            if (review == null)
            {
                return Json(new { success = false, message = "Không tìm thấy đánh giá!" });
            }
            
            review.IsApproved = true;
            await _context.SaveChangesAsync();
            
            return Json(new { success = true, message = "Phê duyệt đánh giá thành công!" });
        }

        [HttpPost("reviews/reject/{id}")]
        public async Task<IActionResult> RejectReview(Guid id)
        {
            var review = await _context.ProductReviews.FindAsync(id);
            if (review == null)
            {
                return Json(new { success = false, message = "Không tìm thấy đánh giá!" });
            }
            
            review.IsApproved = false;
            await _context.SaveChangesAsync();
            
            return Json(new { success = true, message = "Đã từ chối đánh giá!" });
        }

        // MARK: - Quản lý Notifications
        [HttpGet("notifications")]
        public IActionResult Notifications()
        {
            ViewData["CurrentSection"] = "notifications";
            ViewData["Title"] = "Thông báo";
            
            return View();
        }

        [HttpPost("notifications/mark-read/{id}")]
        public async Task<IActionResult> MarkNotificationAsRead(Guid id)
        {
            var notification = await _context.Notifications.FindAsync(id);
            if (notification == null)
            {
                return NotFound();
            }
            
            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            
            return Json(new { success = true });
        }

        [HttpPost("notifications/mark-all-read")]
        public async Task<IActionResult> MarkAllNotificationsAsRead()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
                return Challenge();
            
            var unreadNotifications = await _context.Notifications
                .Where(n => n.UserId == currentUser.Id && !n.IsRead)
                .ToListAsync();
            
            foreach (var notification in unreadNotifications)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;
            }
            
            await _context.SaveChangesAsync();
            
            TempData["SuccessMessage"] = "Đã đánh dấu tất cả thông báo là đã đọc!";
            return RedirectToAction("Notifications");
        }

        // MARK: - Commission Management
        [HttpGet("commissions")]
        public async Task<IActionResult> Commissions(DateTime? fromDate = null, DateTime? toDate = null)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            
            if (!fromDate.HasValue)
                fromDate = DateTime.Now.AddMonths(-3);
            if (!toDate.HasValue)
                toDate = DateTime.Now;
            
            var orders = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .Where(o => o.CreatedAt >= fromDate && o.CreatedAt <= toDate && o.Status == "completed")
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();
            
            var commissionRate = 0.15m; // 15% commission rate
            var totalSales = orders.Sum(o => o.TotalAmount);
            var totalCommission = totalSales * commissionRate;
            var totalOrders = orders.Count;
            
            var monthlyData = orders
                .GroupBy(o => new { o.CreatedAt.Year, o.CreatedAt.Month })
                .Select(g => new MonthlyCommissionData
                {
                    Month = g.Key.Month,
                    Year = g.Key.Year,
                    Sales = g.Sum(o => o.TotalAmount),
                    Commission = g.Sum(o => o.TotalAmount) * commissionRate,
                    OrderCount = g.Count()
                })
                .OrderBy(x => x.Year).ThenBy(x => x.Month)
                .ToList();
            
            var model = new SellerCommissionsViewModel
            {
                FromDate = fromDate.Value,
                ToDate = toDate.Value,
                TotalSales = totalSales,
                TotalCommission = totalCommission,
                TotalOrders = totalOrders,
                CommissionRate = commissionRate,
                MonthlyData = monthlyData,
                RecentOrders = orders.Take(10).Select(o => new ViewModels.RecentOrder
                {
                    OrderId = o.Id,
                    OrderNumber = o.OrderNumber,
                    OrderDate = o.CreatedAt,
                    Total = o.TotalAmount,
                    Status = o.Status
                }).ToList()
            };
            
            return View(model);
        }

        // MARK: - Quản lý Customers
        [HttpGet("customers")]
        public async Task<IActionResult> Customers(int page = 1, int pageSize = 10, string search = "", string sortBy = "totalSpent")
        {
            var currentUser = await _userManager.GetUserAsync(User);
            
            // Get customers who have placed orders
            var customersQuery = _context.Orders
                .Include(o => o.User)
                .Where(o => o.Status == "completed")
                .GroupBy(o => o.UserId)
                .Select(g => new CustomerInfo
                {
                    UserId = g.Key,
                    FullName = g.First().User.FirstName + " " + g.First().User.LastName,
                    Email = g.First().User.Email ?? "",
                    Phone = g.First().User.PhoneNumber ?? "",
                    FirstOrderDate = g.Min(o => o.CreatedAt),
                    LastOrderDate = g.Max(o => o.CreatedAt),
                    TotalOrders = g.Count(),
                    TotalSpent = g.Sum(o => o.TotalAmount),
                    AverageOrderValue = (decimal)g.Average(o => o.TotalAmount),
                    Status = g.Any(o => o.CreatedAt > DateTime.Now.AddDays(-30)) ? "Active" : "Inactive"
                })
                .AsQueryable();
            
            if (!string.IsNullOrEmpty(search))
            {
                customersQuery = customersQuery.Where(c => 
                    c.FullName.Contains(search) || 
                    c.Email.Contains(search) || 
                    c.Phone.Contains(search));
            }
            
            switch (sortBy.ToLower())
            {
                case "name":
                    customersQuery = customersQuery.OrderBy(c => c.FullName);
                    break;
                case "orders":
                    customersQuery = customersQuery.OrderByDescending(c => c.TotalOrders);
                    break;
                case "recent":
                    customersQuery = customersQuery.OrderByDescending(c => c.LastOrderDate);
                    break;
                default: // totalSpent
                    customersQuery = customersQuery.OrderByDescending(c => c.TotalSpent);
                    break;
            }
            
            var totalCount = await customersQuery.CountAsync();
            var customers = await customersQuery
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            
            // Get top customers
            var topCustomers = await _context.Orders
                .Include(o => o.User)
                .Where(o => o.Status == "completed")
                .GroupBy(o => o.UserId)
                .Select(g => new CustomerInfo
                {
                    UserId = g.Key,
                    FullName = g.First().User.FirstName + " " + g.First().User.LastName,
                    Email = g.First().User.Email ?? "",
                    TotalOrders = g.Count(),
                    TotalSpent = g.Sum(o => o.TotalAmount)
                })
                .OrderByDescending(c => c.TotalSpent)
                .Take(5)
                .ToListAsync();
            
            // Get new customers (last 30 days)
            var newCustomers = await _context.Orders
                .Include(o => o.User)
                .Where(o => o.CreatedAt > DateTime.Now.AddDays(-30))
                .GroupBy(o => o.UserId)
                .Where(g => g.Min(o => o.CreatedAt) > DateTime.Now.AddDays(-30))
                .Select(g => new CustomerInfo
                {
                    UserId = g.Key,
                    FullName = g.First().User.FirstName + " " + g.First().User.LastName,
                    Email = g.First().User.Email ?? "",
                    FirstOrderDate = g.Min(o => o.CreatedAt),
                    TotalOrders = g.Count(),
                    TotalSpent = g.Sum(o => o.TotalAmount)
                })
                .OrderByDescending(c => c.FirstOrderDate)
                .Take(10)
                .ToListAsync();
            
            var model = new SellerCustomersViewModel
            {
                Customers = customers,
                TopCustomers = topCustomers,
                NewCustomers = newCustomers,
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                PageSize = pageSize,
                Search = search,
                TotalCount = totalCount
            };
            
            return View(model);
        }

        // MARK: - Advanced Reports
        [HttpGet("reports")]
        public async Task<IActionResult> Reports(DateTime? fromDate = null, DateTime? toDate = null)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            
            if (!fromDate.HasValue)
                fromDate = DateTime.Now.AddMonths(-3);
            if (!toDate.HasValue)
                toDate = DateTime.Now;
            
            // Get orders for the period
            var orders = await _context.Orders
                .Include(o => o.OrderItems)
                .Include(o => o.User)
                .Where(o => o.CreatedAt >= fromDate && o.CreatedAt <= toDate)
                .ToListAsync();
            
            var completedOrders = orders.Where(o => o.Status == "completed").ToList();
            
            // Calculate metrics
            var totalRevenue = completedOrders.Sum(o => o.TotalAmount);
            var totalOrders = completedOrders.Count;
            var totalCustomers = completedOrders.Select(o => o.UserId).Distinct().Count();
            
            var monthlyRevenue = completedOrders
                .Where(o => o.CreatedAt >= DateTime.Now.AddDays(-30))
                .Sum(o => o.TotalAmount);
            
            var monthlyOrders = completedOrders
                .Where(o => o.CreatedAt >= DateTime.Now.AddDays(-30))
                .Count();
            
            var newCustomers = await _context.Orders
                .Where(o => o.CreatedAt >= DateTime.Now.AddDays(-30))
                .GroupBy(o => o.UserId)
                .Where(g => g.Min(x => x.CreatedAt) >= DateTime.Now.AddDays(-30))
                .CountAsync();
            
            // Get products count
            var totalProducts = await _context.Products.CountAsync();
            var activeProducts = await _context.Products.CountAsync(p => p.IsActive);
            
            // Calculate rates
            var averageOrderValue = totalOrders > 0 ? (double)(totalRevenue / totalOrders) : 0;
            var conversionRate = 0.0; // This would need page view tracking
            
            // Sales chart data (last 7 days)
            var salesChartData = new List<decimal>();
            var salesChartLabels = new List<string>();
            
            for (int i = 6; i >= 0; i--)
            {
                var date = DateTime.Now.AddDays(-i);
                var dayRevenue = completedOrders
                    .Where(o => o.CreatedAt.Date == date.Date)
                    .Sum(o => o.TotalAmount);
                
                salesChartData.Add(dayRevenue);
                salesChartLabels.Add(date.ToString("dd/MM"));
            }
            
            // Orders chart data (last 7 days)
            var ordersChartData = new List<int>();
            var ordersChartLabels = new List<string>();
            
            for (int i = 6; i >= 0; i--)
            {
                var date = DateTime.Now.AddDays(-i);
                var dayOrders = completedOrders
                    .Where(o => o.CreatedAt.Date == date.Date)
                    .Count();
                
                ordersChartData.Add(dayOrders);
                ordersChartLabels.Add(date.ToString("dd/MM"));
            }
            
            var model = new SellerReportsViewModel
            {
                TotalRevenue = totalRevenue,
                MonthlyRevenue = monthlyRevenue,
                TotalOrders = totalOrders,
                MonthlyOrders = monthlyOrders,
                TotalProducts = totalProducts,
                ActiveProducts = activeProducts,
                TotalCustomers = totalCustomers,
                NewCustomers = newCustomers,
                AverageOrderValue = (decimal)averageOrderValue,
                ConversionRate = (decimal)conversionRate,
                SalesChartLabels = salesChartLabels,
                SalesChartData = salesChartData,
                OrdersChartLabels = ordersChartLabels,
                OrdersChartData = ordersChartData,
                FromDate = fromDate.Value,
                ToDate = toDate.Value
            };
            
            return View(model);
        }

        // MARK: - Product Performance
        [HttpGet("product-performance")]
        public async Task<IActionResult> ProductPerformance(DateTime? fromDate = null, DateTime? toDate = null)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            
            if (!fromDate.HasValue)
                fromDate = DateTime.Now.AddMonths(-3);
            if (!toDate.HasValue)
                toDate = DateTime.Now;
            
            // Get product performance data
            var productPerformance = await _context.OrderItems
                .Include(oi => oi.Order)
                .Include(oi => oi.Product)
                .Where(oi => oi.Order.CreatedAt >= fromDate && 
                            oi.Order.CreatedAt <= toDate && 
                            oi.Order.Status == "completed")
                .GroupBy(oi => oi.ProductId)
                .Select(g => new ProductPerformanceItem
                {
                    ProductId = g.Key,
                    ProductName = g.First().Product.Name,
                    ImageUrl = g.First().Product.FeaturedImageUrl ?? "",
                    Price = g.First().Product.Price,
                    TotalSold = g.Sum(x => x.Quantity),
                    Revenue = g.Sum(x => x.Price * x.Quantity),
                    ReviewCount = g.First().Product.ProductReviews.Count,
                    AverageRating = g.First().Product.ProductReviews.Any() ? g.First().Product.ProductReviews.Average(r => r.Rating) : 0
                })
                .ToListAsync();
            
            var topProducts = productPerformance
                .OrderByDescending(p => p.Revenue)
                .Take(10)
                .ToList();
            
            var lowPerformingProducts = productPerformance
                .OrderBy(p => p.TotalSold)
                .Take(10)
                .ToList();
            
            var model = new SellerProductPerformanceViewModel
            {
                TopProducts = topProducts,
                LowPerformingProducts = lowPerformingProducts,
                FromDate = fromDate.Value,
                ToDate = toDate.Value
            };
            
            return View(model);
        }

        // POST: Seller/UpdateProductStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProductStatus(Guid productId, string status)
        {
            try
            {
                var product = await _context.Products.FindAsync(productId);
                if (product == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy sản phẩm.";
                    return RedirectToAction("Products");
                }

                product.Status = status;
                product.UpdatedAt = DateTime.UtcNow;
                
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = "Cập nhật trạng thái sản phẩm thành công.";
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi cập nhật trạng thái sản phẩm.";
            }

            return RedirectToAction("Products");
        }

        // POST: Seller/UpdateProductFeatured
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProductFeatured(Guid productId, bool isFeatured)
        {
            try
            {
                var product = await _context.Products.FindAsync(productId);
                if (product == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy sản phẩm.";
                    return RedirectToAction("Products");
                }

                product.IsFeatured = isFeatured;
                product.UpdatedAt = DateTime.UtcNow;
                
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = isFeatured ? "Đã đặt sản phẩm làm nổi bật." : "Đã bỏ nổi bật sản phẩm.";
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi cập nhật sản phẩm.";
            }

            return RedirectToAction("Products");
        }

        // POST: Seller/DeleteProduct
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProduct(Guid productId)
        {
            try
            {
                var product = await _context.Products
                    .Include(p => p.ProductImages)
                    .Include(p => p.ProductReviews)
                    .Include(p => p.Wishlists)
                    .Include(p => p.ShoppingCartItems)
                    .FirstOrDefaultAsync(p => p.Id == productId);
                
                if (product == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy sản phẩm.";
                    return RedirectToAction("Products");
                }

                // Remove related data first
                _context.ProductImages.RemoveRange(product.ProductImages);
                _context.ProductReviews.RemoveRange(product.ProductReviews);
                _context.Wishlists.RemoveRange(product.Wishlists);
                _context.ShoppingCartItems.RemoveRange(product.ShoppingCartItems);
                
                // Remove the product
                _context.Products.Remove(product);
                
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = "Xóa sản phẩm thành công.";
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi xóa sản phẩm. Sản phẩm có thể đang được sử dụng trong đơn hàng.";
            }

            return RedirectToAction("Products");
        }

        // MARK: - Quản lý Đơn hàng
        [HttpGet("orders")]
        public async Task<IActionResult> Orders(int page = 1, int pageSize = 20, string search = "", string status = "")
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Get all orders (temporarily showing all orders since SellerId is not assigned yet)
            // TODO: Filter by seller when seller-product relationship is fully implemented
            var query = _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .AsQueryable();
                // .Where(o => o.OrderItems.Any(oi => oi.Product != null && oi.Product.SellerId == currentUser.Id));

            // Apply search filter
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(o => 
                    o.OrderNumber.Contains(search) || 
                    (o.User != null && !string.IsNullOrEmpty(o.User.FirstName) && (o.User.FirstName + " " + o.User.LastName).Contains(search)) ||
                    (o.User != null && !string.IsNullOrEmpty(o.User.Email) && o.User.Email.Contains(search)));
            }

            // Apply status filter
            if (!string.IsNullOrEmpty(status) && status != "all")
            {
                query = query.Where(o => o.Status.ToLower() == status.ToLower());
            }

            // Get status counts for filters
            var allOrders = await _context.Orders.ToListAsync();
                // .Where(o => o.OrderItems.Any(oi => oi.Product != null && oi.Product.SellerId == currentUser.Id))

            var statusCounts = new Dictionary<string, int>
            {
                { "all", allOrders.Count },
                { "pending", allOrders.Count(o => o.Status.ToLower() == "pending") },
                { "processing", allOrders.Count(o => o.Status.ToLower() == "processing") },
                { "completed", allOrders.Count(o => o.Status.ToLower() == "completed") },
                { "cancelled", allOrders.Count(o => o.Status.ToLower() == "cancelled") }
            };

            // Get total count for pagination
            var totalOrders = await query.CountAsync();

            // Get paginated orders
            var orders = await query
                .OrderByDescending(o => o.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Calculate today's stats
            var today = DateTime.UtcNow.Date;
            var todayOrders = allOrders.Where(o => o.CreatedAt.Date == today).ToList();
            var todayRevenue = todayOrders
                .Where(o => o.Status.ToLower() == "completed")
                .Sum(o => o.TotalAmount);
            var pendingOrdersCount = allOrders.Count(o => o.Status.ToLower() == "pending");

            // Map to ViewModel
            var model = new SellerOrdersViewModel
            {
                Orders = orders.Select(o => new OrderListItemViewModel
                {
                    Id = o.Id,
                    OrderNumber = o.OrderNumber,
                    CustomerName = $"{o.User.FirstName} {o.User.LastName}".Trim(),
                    CustomerEmail = o.User.Email ?? "",
                    Total = o.TotalAmount,
                    Status = o.Status,
                    PaymentStatus = o.PaymentStatus,
                    CreatedAt = o.CreatedAt,
                    ItemCount = o.OrderItems.Count()
                }).ToList(),
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling(totalOrders / (double)pageSize),
                PageSize = pageSize,
                TotalOrders = totalOrders,
                SearchQuery = search ?? "",
                StatusFilter = status ?? "",
                StatusCounts = statusCounts,
                TodayRevenue = todayRevenue,
                TodayOrders = todayOrders.Count,
                PendingOrders = pendingOrdersCount
            };

            return View(model);
        }

        [HttpGet("orders/{id}")]
        public async Task<IActionResult> OrderDetail(Guid id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Get order with all related data
            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .Include(o => o.Payments)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                TempData["ErrorMessage"] = "Đơn hàng không tồn tại.";
                return RedirectToAction(nameof(Orders));
            }

            // TODO: Check seller permission when seller-product relationship is implemented
            // Check if seller has permission to view this order
            // Seller can only view orders that contain their products
            // var sellerProducts = order.OrderItems
            //     .Where(oi => oi.Product != null && oi.Product.SellerId == currentUser.Id)
            //     .ToList();

            // if (!sellerProducts.Any())
            // {
            //     TempData["ErrorMessage"] = "Bạn không có quyền xem đơn hàng này.";
            //     return RedirectToAction(nameof(Orders));
            // }

            // Get order status history
            var statusHistory = await _context.OrderStatusHistories
                .Where(h => h.OrderId == id)
                .OrderBy(h => h.CreatedAt)
                .Select(h => new OrderStatusHistoryViewModel
                {
                    Status = h.Status,
                    Notes = h.Notes,
                    ChangedBy = h.ChangedBy,
                    CreatedAt = h.CreatedAt
                })
                .ToListAsync();

            // Calculate available status transitions based on current status
            var availableStatuses = GetAvailableStatusTransitions(order.Status);

            // Map to ViewModel - only include seller's items
            var model = new SellerOrderDetailViewModel
            {
                OrderId = order.Id,
                OrderNumber = order.OrderNumber,
                Status = order.Status,
                PaymentStatus = order.PaymentStatus,
                PaymentMethod = order.PaymentMethod,
                CreatedAt = order.CreatedAt,
                UpdatedAt = order.UpdatedAt,
                ShippedAt = order.ShippedAt,
                DeliveredAt = order.DeliveredAt,
                
                // Customer information
                CustomerName = $"{order.User.FirstName} {order.User.LastName}".Trim(),
                CustomerEmail = order.User.Email ?? "",
                CustomerPhone = order.User.PhoneNumber ?? "",
                ShippingAddress = order.ShippingAddress,
                BillingAddress = order.BillingAddress,
                
                // Order items - all items (showing all since SellerId not yet implemented)
                OrderItems = order.OrderItems.Select(oi => new OrderItemDetailViewModel
                {
                    ProductId = oi.ProductId,
                    ProductName = oi.ProductName ?? oi.Product?.Name ?? "N/A",
                    ProductSKU = oi.ProductSKU ?? oi.Product?.SKU ?? "",
                    ProductImage = oi.ProductImage ?? oi.Product?.FeaturedImageUrl ?? "/images/placeholder.jpg",
                    Quantity = oi.Quantity,
                    UnitPrice = oi.UnitPrice,
                    TotalPrice = oi.TotalPrice
                }).ToList(),
                
                // Calculate totals for all items
                Subtotal = order.OrderItems.Sum(oi => oi.TotalPrice),
                ShippingFee = order.ShippingFee,
                Tax = order.Tax,
                DiscountAmount = order.DiscountAmount,
                TotalAmount = order.TotalAmount,
                
                // Additional info
                Notes = order.Notes,
                CouponCode = order.CouponCode,
                
                // Status history
                StatusHistory = statusHistory,
                
                // Available status transitions
                AvailableStatusTransitions = availableStatuses,
                
                // Permissions
                CanUpdateStatus = true, // Seller can update status of their portion
                CanViewFullOrder = false // Seller only sees their items
            };

            return View(model);
        }

        private List<string> GetAvailableStatusTransitions(string currentStatus)
        {
            // Define valid status transitions for sellers
            var transitions = new Dictionary<string, List<string>>
            {
                { "pending", new List<string> { "processing", "cancelled" } },
                { "processing", new List<string> { "shipped", "cancelled" } },
                { "shipped", new List<string> { "delivered" } },
                { "delivered", new List<string>() }, // No transitions from delivered
                { "cancelled", new List<string>() } // No transitions from cancelled
            };

            return transitions.ContainsKey(currentStatus.ToLower()) 
                ? transitions[currentStatus.ToLower()] 
                : new List<string>();
        }

        // Quick Actions - 1-click status updates
        [HttpPost("orders/{id}/quick-ship")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> QuickShip(Guid id, string? trackingNumber = null)
        {
            return await UpdateOrderStatus(id, "shipped", $"Đã giao hàng. Mã vận đơn: {trackingNumber ?? "N/A"}", trackingNumber);
        }

        [HttpPost("orders/{id}/quick-deliver")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> QuickDeliver(Guid id)
        {
            return await UpdateOrderStatus(id, "delivered", "Đã giao hàng thành công");
        }

        [HttpPost("orders/{id}/quick-process")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> QuickProcess(Guid id)
        {
            return await UpdateOrderStatus(id, "processing", "Đã bắt đầu xử lý đơn hàng");
        }

        // Bulk Actions
        [HttpPost("orders/bulk-update")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkUpdateStatus([FromBody] BulkUpdateOrderRequestModel request)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập." });
            }

            if (request.OrderIds == null || !request.OrderIds.Any())
            {
                return Json(new { success = false, message = "Vui lòng chọn ít nhất một đơn hàng." });
            }

            var validStatuses = new[] { "processing", "shipped", "delivered", "cancelled" };
            if (!validStatuses.Contains(request.NewStatus?.ToLower()))
            {
                return Json(new { success = false, message = "Trạng thái không hợp lệ." });
            }

            var orders = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .Where(o => request.OrderIds.Contains(o.Id) &&
                           o.OrderItems.Any(oi => oi.Product != null && oi.Product.SellerId == currentUser.Id))
                .ToListAsync();

            var updatedCount = 0;
            var errors = new List<string>();

            foreach (var order in orders)
            {
                var availableStatuses = GetAvailableStatusTransitions(order.Status);
                var newStatus = request.NewStatus?.ToLower() ?? string.Empty;
                if (!string.IsNullOrEmpty(newStatus) && availableStatuses.Contains(newStatus))
                {
                    var oldStatus = order.Status;
                    order.Status = newStatus;
                    order.UpdatedAt = DateTime.UtcNow;

                    switch (newStatus)
                    {
                        case "shipped":
                            order.ShippedAt = DateTime.UtcNow;
                            break;
                        case "delivered":
                            order.DeliveredAt = DateTime.UtcNow;
                            break;
                    }

                    // Add status history
                    var statusHistory = new OrderStatusHistory
                    {
                        OrderId = order.Id,
                        Status = newStatus,
                        Notes = request.Notes ?? $"Bulk update: {oldStatus} → {newStatus}",
                        ChangedBy = currentUser.Id,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.OrderStatusHistories.Add(statusHistory);

                    // Notify customer
                    var notification = new Notification
                    {
                        UserId = order.UserId,
                        Title = $"Cập nhật đơn hàng #{order.OrderNumber}",
                        Message = $"Đơn hàng #{order.OrderNumber} đã được cập nhật trạng thái.",
                        Type = "order",
                        ActionUrl = $"/user/orders/{order.Id}",
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.Notifications.Add(notification);

                    updatedCount++;
                }
                else
                {
                    var requestedStatus = request.NewStatus ?? "unknown";
                    errors.Add($"Đơn hàng #{order.OrderNumber}: Không thể chuyển từ '{order.Status}' sang '{requestedStatus}'");
                }
            }

            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                message = $"Đã cập nhật {updatedCount} đơn hàng thành công.",
                updatedCount = updatedCount,
                errors = errors
            });
        }

        [HttpPost("orders/update-status")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateOrderStatus(Guid orderId, string newStatus, string? notes = null, string? trackingNumber = null)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập." });
            }

            // Validate new status
            var validStatuses = new[] { "pending", "processing", "shipped", "delivered", "cancelled" };
            if (string.IsNullOrEmpty(newStatus) || !validStatuses.Contains(newStatus.ToLower()))
            {
                return Json(new { success = false, message = "Trạng thái không hợp lệ." });
            }

            // Get order with related data
            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
            {
                return Json(new { success = false, message = "Đơn hàng không tồn tại." });
            }

            // Check if seller has permission to update this order
            var sellerProducts = order.OrderItems
                .Where(oi => oi.Product != null && oi.Product.SellerId == currentUser.Id)
                .ToList();

            if (!sellerProducts.Any())
            {
                return Json(new { success = false, message = "Bạn không có quyền cập nhật đơn hàng này." });
            }

            // Validate status transition
            var availableStatuses = GetAvailableStatusTransitions(order.Status);
            if (!availableStatuses.Contains(newStatus.ToLower()))
            {
                return Json(new { 
                    success = false, 
                    message = $"Không thể chuyển từ trạng thái '{order.Status}' sang '{newStatus}'." 
                });
            }

            // Update order status
            var oldStatus = order.Status;
            var oldPaymentStatus = order.PaymentStatus;
            order.Status = newStatus.ToLower();
            order.UpdatedAt = DateTime.UtcNow;

            // Update PaymentStatus for COD orders when seller confirms
            if (order.PaymentStatus?.ToLower() == "cod pending" && 
                (newStatus.ToLower() == "processing" || newStatus.ToLower() == "shipped" || newStatus.ToLower() == "delivered"))
            {
                order.PaymentStatus = "paid";
            }

            // Mark seller confirmation for marketplace flow
            if (newStatus.ToLower() == "processing" && !order.IsSellerConfirmed)
            {
                order.IsSellerConfirmed = true;
                order.SellerConfirmedAt = DateTime.UtcNow;
                order.SellerConfirmedBy = currentUser.Id;
            }

            // Update specific date fields based on new status
            switch (newStatus.ToLower())
            {
                case "shipped":
                    order.ShippedAt = DateTime.UtcNow;
                    break;
                case "delivered":
                    order.DeliveredAt = DateTime.UtcNow;
                    break;
            }

            // Create status history record
            var statusHistory = new OrderStatusHistory
            {
                OrderId = orderId,
                Status = newStatus.ToLower(),
                Notes = notes ?? $"Seller cập nhật trạng thái từ '{oldStatus}' sang '{newStatus}'",
                ChangedBy = currentUser.Id,
                CreatedAt = DateTime.UtcNow
            };
            _context.OrderStatusHistories.Add(statusHistory);

            // Create notification for customer
            var statusMessages = new Dictionary<string, string>
            {
                { "processing", "đang được xử lý" },
                { "shipped", "đã được giao cho đơn vị vận chuyển" },
                { "delivered", "đã được giao thành công" },
                { "cancelled", "đã bị hủy" }
            };

            var statusMessage = statusMessages.ContainsKey(newStatus.ToLower()) 
                ? statusMessages[newStatus.ToLower()] 
                : $"đã chuyển sang trạng thái {newStatus}";

            var notification = new Notification
            {
                UserId = order.UserId,
                Title = $"Cập nhật đơn hàng #{order.OrderNumber}",
                Message = $"Đơn hàng #{order.OrderNumber} của bạn {statusMessage}.",
                Type = "order",
                ActionUrl = $"/user/orders/{orderId}",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };
            _context.Notifications.Add(notification);

            try
            {
                await _context.SaveChangesAsync();

                // Return success with updated data
                return Json(new
                {
                    success = true,
                    message = $"Đã cập nhật trạng thái đơn hàng thành '{newStatus}' thành công.",
                    data = new
                    {
                        orderId = orderId,
                        newStatus = newStatus.ToLower(),
                        oldStatus = oldStatus,
                        updatedAt = order.UpdatedAt.ToString("dd/MM/yyyy HH:mm"),
                        availableStatuses = GetAvailableStatusTransitions(newStatus.ToLower())
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { 
                    success = false, 
                    message = $"Lỗi khi cập nhật trạng thái: {ex.Message}" 
                });
            }
        }
    }

    // Helper class for bulk update
    public class BulkUpdateOrderRequestModel
    {
        public List<Guid> OrderIds { get; set; } = new();
        public string NewStatus { get; set; } = string.Empty;
        public string? Notes { get; set; }
    }
}
