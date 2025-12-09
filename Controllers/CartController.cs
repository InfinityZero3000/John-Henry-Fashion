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
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public CartController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Cart
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var cartItems = await _context.ShoppingCartItems
                .Include(c => c.Product)
                .ThenInclude(p => p.Category)
                .Where(c => c.UserId == userId)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            ViewBag.CartTotal = cartItems.Sum(c => c.Price * c.Quantity);
            ViewBag.CartCount = cartItems.Sum(c => c.Quantity);

            // Get suggested products (random products, excluding items already in cart)
            var cartProductIds = cartItems.Select(c => c.ProductId).ToList();
            var suggestedProducts = await _context.Products
                .Where(p => p.IsActive && p.StockQuantity > 0 && !cartProductIds.Contains(p.Id))
                .OrderBy(p => Guid.NewGuid())
                .Take(4)
                .ToListAsync();
            
            ViewBag.SuggestedProducts = suggestedProducts;

            return View(cartItems);
        }

        // POST: Cart/UpdateQuantity
        [HttpPost]
        public async Task<IActionResult> UpdateQuantity([FromBody] UpdateQuantityRequest request)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                var cartItem = await _context.ShoppingCartItems
                    .Include(c => c.Product)
                    .FirstOrDefaultAsync(c => c.Id == request.CartItemId && c.UserId == userId);

                if (cartItem == null)
                {
                    return Json(new { success = false, message = "Cart item not found" });
                }

                if (request.Quantity <= 0)
                {
                    return Json(new { success = false, message = "Quantity must be greater than 0" });
                }

                // Reload product from database to get latest stock (avoid stale data)
                var product = await _context.Products
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Id == cartItem.ProductId);

                if (product == null)
                {
                    return Json(new { success = false, message = "Product not found" });
                }

                // Check stock availability - only check actual stock in database
                // Don't count other users' cart items as "reserved" since they haven't paid yet
                if (product.StockQuantity < request.Quantity)
                {
                    return Json(new { 
                        success = false, 
                        message = product.StockQuantity == 0 
                            ? $"Sản phẩm hiện đã hết hàng" 
                            : $"Chỉ còn {product.StockQuantity} sản phẩm trong kho" 
                    });
                }

                cartItem.Quantity = request.Quantity;
                cartItem.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // Calculate new totals
                var cartItems = await _context.ShoppingCartItems
                    .Where(c => c.UserId == userId)
                    .ToListAsync();

                var cartTotal = cartItems.Sum(c => c.Price * c.Quantity);
                var cartCount = cartItems.Sum(c => c.Quantity);
                var itemTotal = cartItem.Price * cartItem.Quantity;

                return Json(new { 
                    success = true, 
                    message = "Quantity updated",
                    cartTotal = cartTotal,
                    cartCount = cartCount,
                    itemTotal = itemTotal
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating cart quantity: {ex.Message}");
                return Json(new { success = false, message = "An error occurred while updating quantity" });
            }
        }

        // Request model for UpdateQuantity
        public class UpdateQuantityRequest
        {
            public Guid CartItemId { get; set; }
            public int Quantity { get; set; }
        }

        // POST: Cart/RemoveItem
        [HttpPost]
        public async Task<IActionResult> RemoveItem([FromBody] RemoveItemRequest request)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                var cartItem = await _context.ShoppingCartItems
                    .FirstOrDefaultAsync(c => c.Id == request.CartItemId && c.UserId == userId);

                if (cartItem == null)
                {
                    return Json(new { success = false, message = "Cart item not found" });
                }

                _context.ShoppingCartItems.Remove(cartItem);
                await _context.SaveChangesAsync();

                // Calculate new totals
                var cartItems = await _context.ShoppingCartItems
                    .Where(c => c.UserId == userId)
                    .ToListAsync();

                var cartTotal = cartItems.Sum(c => c.Price * c.Quantity);
                var cartCount = cartItems.Sum(c => c.Quantity);

                return Json(new { 
                    success = true, 
                    message = "Item removed from cart",
                    cartTotal = cartTotal,
                    cartCount = cartCount
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error removing cart item: {ex.Message}");
                return Json(new { success = false, message = "An error occurred while removing item" });
            }
        }

        // Request model for RemoveItem
        public class RemoveItemRequest
        {
            public Guid CartItemId { get; set; }
        }

        // POST: Cart/ClearCart
        [HttpPost]
        public async Task<IActionResult> ClearCart()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                var cartItems = await _context.ShoppingCartItems
                    .Where(c => c.UserId == userId)
                    .ToListAsync();

                _context.ShoppingCartItems.RemoveRange(cartItems);
                await _context.SaveChangesAsync();

                return Json(new { 
                    success = true, 
                    message = "Cart cleared successfully",
                    cartTotal = 0,
                    cartCount = 0
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error clearing cart: {ex.Message}");
                return Json(new { success = false, message = "An error occurred while clearing cart" });
            }
        }

        // POST: Cart/SaveSelectedItems
        [HttpPost]
        public IActionResult SaveSelectedItems([FromBody] List<Guid> selectedItemIds)
        {
            try
            {
                if (selectedItemIds == null || !selectedItemIds.Any())
                {
                    return Json(new { success = false, message = "Không có sản phẩm nào được chọn" });
                }

                HttpContext.Session.SetString("SelectedCartItems", 
                    System.Text.Json.JsonSerializer.Serialize(selectedItemIds));
                
                return Json(new { success = true, message = "Đã lưu danh sách sản phẩm" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving selected items: {ex.Message}");
                return Json(new { success = false, message = "Lỗi lưu danh sách sản phẩm" });
            }
        }

        // GET: Cart/GetCartCount
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetCartCount()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { cartCount = 0 });
            }

            var cartCount = await _context.ShoppingCartItems
                .Where(c => c.UserId == userId)
                .SumAsync(c => c.Quantity);

            return Json(new { cartCount = cartCount });
        }

        // GET: Cart/GetSidebarData
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetSidebarData()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return PartialView("_CartSidebar", new List<ShoppingCartItem>());
            }

            var cartItems = await _context.ShoppingCartItems
                .Include(c => c.Product)
                .ThenInclude(p => p.Category)
                .Where(c => c.UserId == userId)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            ViewBag.CartTotal = cartItems.Sum(c => c.Price * c.Quantity);
            ViewBag.CartCount = cartItems.Sum(c => c.Quantity);

            return PartialView("_CartSidebar", cartItems);
        }
    }
}
