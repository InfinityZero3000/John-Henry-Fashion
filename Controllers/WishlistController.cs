using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using JohnHenryFashionWeb.Data;
using JohnHenryFashionWeb.Models;
using System.Security.Claims;

namespace JohnHenryFashionWeb.Controllers
{
    [Authorize]
    [Route("userdashboard/wishlist")]
    public class WishlistController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<WishlistController> _logger;

        public WishlistController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, ILogger<WishlistController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        // GET: userdashboard/wishlist
        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var wishlistItems = await _context.Wishlists
                .Include(w => w.Product)
                .ThenInclude(p => p.Category)
                .Where(w => w.UserId == userId)
                .OrderByDescending(w => w.CreatedAt)
                .ToListAsync();

            ViewBag.WishlistCount = wishlistItems.Count;

            // Get user info for sidebar
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                ViewBag.UserFullName = $"{user.FirstName} {user.LastName}".Trim();
                ViewBag.UserAvatar = user.Avatar;
            }

            return View(wishlistItems);
        }

        // POST: Wishlist/Add
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add([FromForm] string productId)
        {
            try
            {
                _logger.LogInformation("=== WISHLIST ADD START ===");
                _logger.LogInformation($"ProductId: {productId}");
                _logger.LogInformation($"User: {User?.Identity?.Name}");
                _logger.LogInformation($"IsAuthenticated: {User?.Identity?.IsAuthenticated}");
                _logger.LogInformation($"Request Content-Type: {Request.ContentType}");
                _logger.LogInformation($"Request Method: {Request.Method}");
                
                // Log all headers
                foreach (var header in Request.Headers)
                {
                    _logger.LogInformation($"Header: {header.Key} = {header.Value}");
                }
                
                var userId = User?.FindFirstValue(ClaimTypes.NameIdentifier);
                _logger.LogInformation($"UserId from claims: {userId}");
                
                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                if (string.IsNullOrEmpty(productId))
                {
                    return Json(new { success = false, message = "Product ID is required" });
                }

                // Try to find product by SKU first, then by Guid
                Product? product = await _context.Products
                    .FirstOrDefaultAsync(p => p.SKU == productId);

                if (product == null && Guid.TryParse(productId, out var productGuid))
                {
                    product = await _context.Products.FindAsync(productGuid);
                }

                if (product == null)
                {
                    return Json(new { success = false, message = "Product not found" });
                }

                // Check if already in wishlist
                var existingItem = await _context.Wishlists
                    .FirstOrDefaultAsync(w => w.UserId == userId && w.ProductId == product.Id);

                if (existingItem != null)
                {
                    return Json(new { success = false, message = "Sản phẩm đã có trong danh sách yêu thích" });
                }

                // Add to wishlist
                var wishlistItem = new Wishlist
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    ProductId = product.Id,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Wishlists.Add(wishlistItem);
                await _context.SaveChangesAsync();

                // Get updated count
                var wishlistCount = await _context.Wishlists
                    .CountAsync(w => w.UserId == userId);

                return Json(new { 
                    success = true, 
                    message = "Đã thêm sản phẩm vào danh sách yêu thích",
                    wishlistCount = wishlistCount
                });
            }
            catch (Exception ex)
            {
                // Log the error for debugging
                Console.WriteLine($"Error adding to wishlist: {ex.Message}");
                return Json(new { success = false, message = "Có lỗi xảy ra khi thêm vào danh sách yêu thích" });
            }
        }

        // POST: Wishlist/Remove
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove([FromForm] string productId)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { success = false, message = "Vui lòng đăng nhập" });
                }

                if (string.IsNullOrEmpty(productId))
                {
                    return Json(new { success = false, message = "Thiếu thông tin sản phẩm" });
                }

                // Parse product ID as Guid
                if (!Guid.TryParse(productId, out var productGuid))
                {
                    return Json(new { success = false, message = "ID sản phẩm không hợp lệ" });
                }

                // Find wishlist item directly by ProductId
                var wishlistItem = await _context.Wishlists
                    .FirstOrDefaultAsync(w => w.UserId == userId && w.ProductId == productGuid);

                if (wishlistItem == null)
                {
                    return Json(new { success = false, message = "Sản phẩm không có trong danh sách yêu thích" });
                }

                _context.Wishlists.Remove(wishlistItem);
                await _context.SaveChangesAsync();

                // Get updated count
                var wishlistCount = await _context.Wishlists
                    .CountAsync(w => w.UserId == userId);

                return Json(new { 
                    success = true, 
                    message = "Đã xóa sản phẩm khỏi danh sách yêu thích",
                    wishlistCount = wishlistCount
                });
            }
            catch (Exception ex)
            {
                // Log the error for debugging
                Console.WriteLine($"Error removing from wishlist: {ex.Message}");
                return Json(new { success = false, message = "Có lỗi xảy ra khi xóa khỏi danh sách yêu thích" });
            }
        }

        // POST: Wishlist/Clear
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Clear()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                var wishlistItems = await _context.Wishlists
                    .Where(w => w.UserId == userId)
                    .ToListAsync();

                _context.Wishlists.RemoveRange(wishlistItems);
                await _context.SaveChangesAsync();

                return Json(new { 
                    success = true, 
                    message = "Wishlist cleared",
                    wishlistCount = 0
                });
            }
            catch (Exception ex)
            {
                // Log the error for debugging
                Console.WriteLine($"Error clearing wishlist: {ex.Message}");
                return Json(new { success = false, message = "An error occurred while clearing wishlist" });
            }
        }

        // POST: Wishlist/AddToCart
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToCart([FromBody] string productId)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { success = false, message = "Vui lòng đăng nhập" });
                }

                // Parse product ID as Guid
                if (!Guid.TryParse(productId, out var productGuid))
                {
                    return Json(new { success = false, message = "ID sản phẩm không hợp lệ" });
                }

                // Check if product exists and is available
                var product = await _context.Products.FindAsync(productGuid);
                if (product == null || !product.IsActive)
                {
                    return Json(new { success = false, message = "Sản phẩm không khả dụng" });
                }

                // Check stock availability
                if (product.StockQuantity < 1)
                {
                    return Json(new { success = false, message = "Sản phẩm đã hết hàng" });
                }

                // Check if item already exists in cart
                var existingCartItem = await _context.ShoppingCartItems
                    .FirstOrDefaultAsync(c => c.UserId == userId && c.ProductId == productGuid);

                if (existingCartItem != null)
                {
                    // Update quantity
                    existingCartItem.Quantity += 1;
                    existingCartItem.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    // Add new cart item
                    var cartItem = new ShoppingCartItem
                    {
                        Id = Guid.NewGuid(),
                        UserId = userId,
                        ProductId = productGuid,
                        Quantity = 1,
                        Price = product.SalePrice ?? product.Price,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    _context.ShoppingCartItems.Add(cartItem);
                }

                await _context.SaveChangesAsync();

                // Get cart count for response
                var cartCount = await _context.ShoppingCartItems
                    .Where(c => c.UserId == userId)
                    .SumAsync(c => c.Quantity);

                return Json(new { 
                    success = true, 
                    message = "Đã thêm sản phẩm vào giỏ hàng",
                    cartCount = cartCount
                });
            }
            catch (Exception ex)
            {
                // Log the error for debugging
                Console.WriteLine($"Error adding to cart from wishlist: {ex.Message}");
                return Json(new { success = false, message = "Có lỗi xảy ra khi thêm vào giỏ hàng" });
            }
        }

        // GET: Wishlist/GetCount
        [HttpGet("count")]
        public async Task<IActionResult> GetCount()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { wishlistCount = 0 });
            }

            var wishlistCount = await _context.Wishlists
                .CountAsync(w => w.UserId == userId);

            return Json(new { wishlistCount = wishlistCount });
        }

        // GET: Wishlist/IsInWishlist
        [HttpGet("check/{productId}")]
        [AllowAnonymous]
        public async Task<IActionResult> IsInWishlist(string productId)
        {
            if (string.IsNullOrEmpty(productId))
            {
                return Json(new { isInWishlist = false });
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { isInWishlist = false });
            }

            // Try to find product by SKU first, then by Id
            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.SKU == productId);

            if (product == null)
            {
                // Try to parse as Guid and search by Id
                if (Guid.TryParse(productId, out var guid))
                {
                    product = await _context.Products
                        .FirstOrDefaultAsync(p => p.Id == guid);
                }
            }

            if (product == null)
            {
                return Json(new { isInWishlist = false });
            }

            var isInWishlist = await _context.Wishlists
                .AnyAsync(w => w.UserId == userId && w.ProductId == product.Id);

            return Json(new { isInWishlist = isInWishlist });
        }
    }
}
