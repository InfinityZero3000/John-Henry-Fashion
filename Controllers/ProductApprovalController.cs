using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JohnHenryFashionWeb.Data;
using JohnHenryFashionWeb.Models;

namespace JohnHenryFashionWeb.Controllers
{
    [Authorize(Roles = UserRoles.Admin, AuthenticationSchemes = "Identity.Application")]
    [Route("admin/approvals")]
    public class ProductApprovalController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ProductApprovalController> _logger;

        public ProductApprovalController(ApplicationDbContext context, ILogger<ProductApprovalController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Trang tổng quan phê duyệt - điều hướng chính cho các loại phê duyệt
        /// </summary>
        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            ViewData["CurrentSection"] = "Approvals";
            ViewData["Title"] = "Duyệt sản phẩm";

            // Thống kê tổng quan
            var pendingReviewsCount = await _context.ProductReviews.CountAsync(r => !r.IsApproved);
            var totalReviewsCount = await _context.ProductReviews.CountAsync();
            var approvedReviewsCount = await _context.ProductReviews.CountAsync(r => r.IsApproved);
            
            // Thống kê auto-moderation hôm nay
            var today = DateTime.UtcNow.Date;
            var reviewsToday = await _context.ProductReviews
                .Where(r => r.CreatedAt >= today)
                .ToListAsync();
            
            var autoApprovedToday = reviewsToday.Count(r => r.IsApproved && r.CreatedAt == r.UpdatedAt);
            var manualReviewToday = reviewsToday.Count(r => !r.IsApproved);
            
            ViewBag.AutoApprovedToday = autoApprovedToday;
            ViewBag.ManualReviewToday = manualReviewToday;
            
            // Lấy reviews chờ duyệt mới nhất (10 items)
            var recentPendingReviews = await _context.ProductReviews
                .Include(r => r.Product)
                .Include(r => r.User)
                .Where(r => !r.IsApproved)
                .OrderByDescending(r => r.CreatedAt)
                .Take(10)
                .ToListAsync();

            // Lấy reviews đã duyệt gần đây (5 items)
            var recentApprovedReviews = await _context.ProductReviews
                .Include(r => r.Product)
                .Include(r => r.User)
                .Where(r => r.IsApproved)
                .OrderByDescending(r => r.UpdatedAt)
                .Take(5)
                .ToListAsync();

            ViewBag.PendingReviewsCount = pendingReviewsCount;
            ViewBag.TotalReviewsCount = totalReviewsCount;
            ViewBag.ApprovedReviewsCount = approvedReviewsCount;
            ViewBag.RecentPendingReviews = recentPendingReviews;
            ViewBag.RecentApprovedReviews = recentApprovedReviews;

            return View("~/Views/Admin/Approvals.cshtml");
        }

        #region Product Reviews Approval
        
        /// <summary>
        /// Danh sách các review chờ phê duyệt
        /// </summary>
        [HttpGet("reviews")]
        public async Task<IActionResult> Reviews(string? status = null, int page = 1, int pageSize = 20)
        {
            var query = _context.ProductReviews
                .Include(r => r.Product)
                .Include(r => r.User)
                .AsQueryable();

            // Filter by approval status
            if (status == "pending")
            {
                query = query.Where(r => !r.IsApproved);
            }
            else if (status == "approved")
            {
                query = query.Where(r => r.IsApproved);
            }

            // Statistics
            var totalReviews = await _context.ProductReviews.CountAsync();
            var pendingReviews = await _context.ProductReviews.CountAsync(r => !r.IsApproved);
            var approvedReviews = await _context.ProductReviews.CountAsync(r => r.IsApproved);

            ViewBag.TotalReviews = totalReviews;
            ViewBag.PendingReviews = pendingReviews;
            ViewBag.ApprovedReviews = approvedReviews;
            ViewBag.StatusFilter = status;

            // Pagination
            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            
            var reviews = await query
                .OrderByDescending(r => r.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.PageSize = pageSize;

            return View(reviews);
        }

        /// <summary>
        /// Chi tiết review để xem xét phê duyệt
        /// </summary>
        [HttpGet("review/{id}")]
        public async Task<IActionResult> ReviewDetails(Guid id)
        {
            var review = await _context.ProductReviews
                .Include(r => r.Product)
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (review == null)
            {
                return NotFound();
            }

            // Get other reviews from same user
            var userReviews = await _context.ProductReviews
                .Where(r => r.UserId == review.UserId && r.Id != id)
                .OrderByDescending(r => r.CreatedAt)
                .Take(5)
                .ToListAsync();

            ViewBag.UserReviews = userReviews;

            return View(review);
        }

        /// <summary>
        /// Phê duyệt review
        /// </summary>
        [HttpPost("review/{id}/approve")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveReview(Guid id)
        {
            var review = await _context.ProductReviews.FindAsync(id);
            if (review == null)
            {
                return NotFound();
            }

            review.IsApproved = true;
            review.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation($"Review {id} approved by {User.Identity?.Name}");
            TempData["SuccessMessage"] = "Review đã được phê duyệt";

            return RedirectToAction(nameof(Reviews), new { status = "pending" });
        }

        /// <summary>
        /// Từ chối và xóa review
        /// </summary>
        [HttpPost("review/{id}/reject")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectReview(Guid id, string? reason)
        {
            var review = await _context.ProductReviews.FindAsync(id);
            if (review == null)
            {
                return NotFound();
            }

            _context.ProductReviews.Remove(review);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Review {id} rejected and deleted by {User.Identity?.Name}. Reason: {reason}");
            TempData["SuccessMessage"] = "Review đã bị từ chối và xóa";

            return RedirectToAction(nameof(Reviews), new { status = "pending" });
        }

        /// <summary>
        /// Phê duyệt hàng loạt
        /// </summary>
        [HttpPost("reviews/bulk-approve")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkApproveReviews(string reviewIds)
        {
            if (string.IsNullOrEmpty(reviewIds))
            {
                TempData["ErrorMessage"] = "Không có review nào được chọn";
                return RedirectToAction(nameof(Reviews));
            }

            var ids = reviewIds.Split(',')
                .Where(s => Guid.TryParse(s, out _))
                .Select(s => Guid.Parse(s))
                .ToList();

            var reviews = await _context.ProductReviews
                .Where(r => ids.Contains(r.Id))
                .ToListAsync();

            foreach (var review in reviews)
            {
                review.IsApproved = true;
                review.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation($"Bulk approved {reviews.Count} reviews by {User.Identity?.Name}");
            TempData["SuccessMessage"] = $"Đã phê duyệt {reviews.Count} reviews";

            return RedirectToAction(nameof(Reviews), new { status = "pending" });
        }

        /// <summary>
        /// Xóa hàng loạt
        /// </summary>
        [HttpPost("reviews/bulk-reject")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkRejectReviews(string reviewIds)
        {
            if (string.IsNullOrEmpty(reviewIds))
            {
                TempData["ErrorMessage"] = "Không có review nào được chọn";
                return RedirectToAction(nameof(Reviews));
            }

            var ids = reviewIds.Split(',')
                .Where(s => Guid.TryParse(s, out _))
                .Select(s => Guid.Parse(s))
                .ToList();

            var reviews = await _context.ProductReviews
                .Where(r => ids.Contains(r.Id))
                .ToListAsync();

            _context.ProductReviews.RemoveRange(reviews);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Bulk rejected {reviews.Count} reviews by {User.Identity?.Name}");
            TempData["SuccessMessage"] = $"Đã từ chối và xóa {reviews.Count} reviews";

            return RedirectToAction(nameof(Reviews), new { status = "pending" });
        }

        #endregion

        #region Product Approval (for future seller system)
        
        /// <summary>
        /// Danh sách sản phẩm chờ phê duyệt (dành cho seller system trong tương lai)
        /// </summary>
        [HttpGet("products")]
        public async Task<IActionResult> Products(string? status = null, int page = 1, int pageSize = 20)
        {
            var query = _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .AsQueryable();

            // Filter by status
            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(p => p.Status == status);
            }

            // Statistics
            var totalProducts = await _context.Products.CountAsync();
            var activeProducts = await _context.Products.CountAsync(p => p.Status == "active");
            var inactiveProducts = await _context.Products.CountAsync(p => p.Status == "inactive");

            ViewBag.TotalProducts = totalProducts;
            ViewBag.ActiveProducts = activeProducts;
            ViewBag.InactiveProducts = inactiveProducts;
            ViewBag.StatusFilter = status;

            // Pagination
            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            
            var products = await query
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.PageSize = pageSize;

            return View(products);
        }

        /// <summary>
        /// Chi tiết sản phẩm để phê duyệt
        /// </summary>
        [HttpGet("product/{id}")]
        public async Task<IActionResult> ProductDetails(Guid id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        /// <summary>
        /// Kích hoạt sản phẩm
        /// </summary>
        [HttpPost("product/{id}/activate")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ActivateProduct(Guid id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            product.Status = "active";
            product.IsActive = true;
            product.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation($"Product {id} activated by {User.Identity?.Name}");
            TempData["SuccessMessage"] = "Sản phẩm đã được kích hoạt";

            return RedirectToAction(nameof(Products));
        }

        /// <summary>
        /// Vô hiệu hóa sản phẩm
        /// </summary>
        [HttpPost("product/{id}/deactivate")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeactivateProduct(Guid id, string? reason)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            product.Status = "inactive";
            product.IsActive = false;
            product.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation($"Product {id} deactivated by {User.Identity?.Name}. Reason: {reason}");
            TempData["SuccessMessage"] = "Sản phẩm đã bị vô hiệu hóa";

            return RedirectToAction(nameof(Products));
        }

        #endregion
    }
}
