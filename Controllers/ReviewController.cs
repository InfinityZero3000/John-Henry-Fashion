using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using JohnHenryFashionWeb.Data;
using JohnHenryFashionWeb.Models;
using JohnHenryFashionWeb.Services;
using System.Security.Claims;

namespace JohnHenryFashionWeb.Controllers
{
    public class ReviewController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IContentModerationService _moderationService;
        private readonly ILogger<ReviewController> _logger;

        public ReviewController(
            ApplicationDbContext context, 
            UserManager<ApplicationUser> userManager,
            IContentModerationService moderationService,
            ILogger<ReviewController> logger)
        {
            _context = context;
            _userManager = userManager;
            _moderationService = moderationService;
            _logger = logger;
        }

        // GET: Review/Product/5
        public async Task<IActionResult> Product(Guid productId, int page = 1)
        {
            const int pageSize = 10;
            
            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Id == productId);

            if (product == null)
            {
                return NotFound();
            }

            var reviewsQuery = _context.ProductReviews
                .Include(r => r.User)
                .Where(r => r.ProductId == productId && r.IsApproved)
                .OrderByDescending(r => r.CreatedAt);

            var totalReviews = await reviewsQuery.CountAsync();
            var reviews = await reviewsQuery
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Calculate rating statistics
            var ratingStats = await _context.ProductReviews
                .Where(r => r.ProductId == productId && r.IsApproved)
                .GroupBy(r => r.Rating)
                .Select(g => new { Rating = g.Key, Count = g.Count() })
                .ToListAsync();

            ViewBag.Product = product;
            ViewBag.TotalReviews = totalReviews;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalReviews / pageSize);
            ViewBag.RatingStats = ratingStats;
            ViewBag.AverageRating = ratingStats.Any() ? 
                ratingStats.Sum(r => r.Rating * r.Count) / (double)ratingStats.Sum(r => r.Count) : 0;

            // Check if user can leave a review
            ViewBag.CanReview = false;
            if (User.Identity?.IsAuthenticated == true)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                
                // Check if user has purchased this product
                var hasPurchased = await _context.OrderItems
                    .Include(oi => oi.Order)
                    .AnyAsync(oi => oi.ProductId == productId && 
                                   oi.Order.UserId == userId && 
                                   oi.Order.Status == "delivered");

                // Check if user has already reviewed this product
                var hasReviewed = await _context.ProductReviews
                    .AnyAsync(r => r.ProductId == productId && r.UserId == userId);

                ViewBag.CanReview = hasPurchased && !hasReviewed;
            }

            return View(reviews);
        }

        // POST: Review/Add
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(Guid productId, int rating, string? title, string? comment)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                // Validate product exists
                var product = await _context.Products.FindAsync(productId);
                if (product == null)
                {
                    return Json(new { success = false, message = "Product not found" });
                }

                // Check if user has purchased this product
                var hasPurchased = await _context.OrderItems
                    .Include(oi => oi.Order)
                    .AnyAsync(oi => oi.ProductId == productId && 
                                   oi.Order.UserId == userId && 
                                   oi.Order.Status == "delivered");

                if (!hasPurchased)
                {
                    return Json(new { success = false, message = "Bạn chỉ có thể đánh giá sản phẩm đã mua" });
                }

                // Check if user has already reviewed this product
                var existingReview = await _context.ProductReviews
                    .FirstOrDefaultAsync(r => r.ProductId == productId && r.UserId == userId);

                if (existingReview != null)
                {
                    return Json(new { success = false, message = "Bạn đã đánh giá sản phẩm này rồi" });
                }

                // Validate rating
                if (rating < 1 || rating > 5)
                {
                    return Json(new { success = false, message = "Rating phải từ 1 đến 5 sao" });
                }

                // Auto-moderate content - kiểm tra từ ngữ xúc phạm
                var shouldAutoApprove = await _moderationService.ShouldAutoApproveReviewAsync(title, comment);
                
                // Lấy chi tiết moderation để log
                string moderationReason = "Clean content";
                if (!shouldAutoApprove)
                {
                    // Kiểm tra chi tiết lý do
                    var titleResult = !string.IsNullOrWhiteSpace(title) 
                        ? await _moderationService.ModerateContentAsync(title) 
                        : null;
                    var commentResult = !string.IsNullOrWhiteSpace(comment) 
                        ? await _moderationService.ModerateContentAsync(comment) 
                        : null;

                    if (titleResult != null && !titleResult.ShouldAutoApprove)
                    {
                        moderationReason = $"Title: {titleResult.Reason}";
                    }
                    else if (commentResult != null && !commentResult.ShouldAutoApprove)
                    {
                        moderationReason = $"Comment: {commentResult.Reason}";
                    }

                    _logger.LogWarning($"Review from user {userId} for product {productId} flagged for manual review. Reason: {moderationReason}");
                }
                else
                {
                    _logger.LogInformation($"Review from user {userId} for product {productId} auto-approved (no offensive content detected)");
                }

                // Create review
                var review = new ProductReview
                {
                    Id = Guid.NewGuid(),
                    ProductId = productId,
                    UserId = userId,
                    Rating = rating,
                    Title = title?.Trim(),
                    Comment = comment?.Trim(),
                    IsApproved = shouldAutoApprove, // Tự động duyệt nếu không có từ ngữ xúc phạm
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.ProductReviews.Add(review);

                // Update product rating (chỉ cập nhật nếu review được duyệt)
                if (shouldAutoApprove)
                {
                    await UpdateProductRating(productId);
                }

                await _context.SaveChangesAsync();

                var message = shouldAutoApprove 
                    ? "Đánh giá của bạn đã được gửi và hiển thị thành công!"
                    : "Đánh giá của bạn đã được gửi và đang chờ kiểm duyệt. Cảm ơn bạn đã đánh giá!";

                return Json(new { 
                    success = true, 
                    message = message,
                    isApproved = shouldAutoApprove,
                    review = new {
                        id = review.Id,
                        rating = review.Rating,
                        title = review.Title,
                        comment = review.Comment,
                        userFullName = $"{User.Identity?.Name}",
                        createdAt = review.CreatedAt.ToString("dd/MM/yyyy")
                    }
                });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra khi gửi đánh giá" });
            }
        }

        // POST: Review/Edit
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid reviewId, int rating, string? title, string? comment)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var review = await _context.ProductReviews
                    .FirstOrDefaultAsync(r => r.Id == reviewId && r.UserId == userId);

                if (review == null)
                {
                    return Json(new { success = false, message = "Review not found" });
                }

                // Validate rating
                if (rating < 1 || rating > 5)
                {
                    return Json(new { success = false, message = "Rating phải từ 1 đến 5 sao" });
                }

                // Update review
                review.Rating = rating;
                review.Title = title?.Trim();
                review.Comment = comment?.Trim();
                review.UpdatedAt = DateTime.UtcNow;
                review.IsApproved = true; // Reset approval status

                // Update product rating
                await UpdateProductRating(review.ProductId);

                await _context.SaveChangesAsync();

                return Json(new { 
                    success = true, 
                    message = "Đánh giá đã được cập nhật thành công!" 
                });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra khi cập nhật đánh giá" });
            }
        }

        // POST: Review/Delete
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid reviewId)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var review = await _context.ProductReviews
                    .FirstOrDefaultAsync(r => r.Id == reviewId && r.UserId == userId);

                if (review == null)
                {
                    return Json(new { success = false, message = "Review not found" });
                }

                var productId = review.ProductId;
                _context.ProductReviews.Remove(review);

                // Update product rating
                await UpdateProductRating(productId);

                await _context.SaveChangesAsync();

                return Json(new { 
                    success = true, 
                    message = "Đánh giá đã được xóa thành công!" 
                });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra khi xóa đánh giá" });
            }
        }

        // POST: Review/ToggleHelpful
        [HttpPost]
        [Authorize]
        public IActionResult ToggleHelpful(Guid reviewId)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                
                // For now, just return success
                // In a full implementation, you'd track helpful votes in a separate table
                
                return Json(new { 
                    success = true, 
                    message = "Cảm ơn phản hồi của bạn!" 
                });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra" });
            }
        }

        // GET: Review/MyReviews
        [Authorize]
        public async Task<IActionResult> MyReviews(int page = 1)
        {
            const int pageSize = 10;
            
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            var reviewsQuery = _context.ProductReviews
                .Include(r => r.Product)
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.CreatedAt);

            var totalReviews = await reviewsQuery.CountAsync();
            var reviews = await reviewsQuery
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalReviews / pageSize);

            return View(reviews);
        }

        private async Task UpdateProductRating(Guid productId)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null) return;

            var reviews = await _context.ProductReviews
                .Where(r => r.ProductId == productId && r.IsApproved)
                .ToListAsync();

            if (reviews.Any())
            {
                product.Rating = (decimal)reviews.Average(r => r.Rating);
                product.ReviewCount = reviews.Count;
            }
            else
            {
                product.Rating = null;
                product.ReviewCount = 0;
            }
        }
    }
}
