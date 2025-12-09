using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using JohnHenryFashionWeb.Data;
using JohnHenryFashionWeb.Models;
using JohnHenryFashionWeb.ViewModels;
using System.Security.Claims;

namespace JohnHenryFashionWeb.Controllers
{
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ProductsController> _logger;

        public ProductsController(ApplicationDbContext context, ILogger<ProductsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Products/ProductDetail/{id}
        public async Task<IActionResult> ProductDetail(Guid id)
        {
            try
            {
                var product = await _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.Brand)
                    .FirstOrDefaultAsync(p => p.Id == id && p.IsActive);

                if (product == null)
                {
                    _logger.LogWarning("Product not found with ID: {ProductId}", id);
                    return NotFound();
                }

                // Increment view count
                product.ViewCount++;
                await _context.SaveChangesAsync();

                // Get related products from the same category
                var relatedProducts = await _context.Products
                    .Where(p => p.CategoryId == product.CategoryId && p.Id != product.Id && p.IsActive)
                    .OrderByDescending(p => p.IsFeatured)
                    .ThenByDescending(p => p.CreatedAt)
                    .Take(4)
                    .ToListAsync();

                ViewBag.RelatedProducts = relatedProducts.Select(p => MapToProductViewModel(p)).ToList();

                // Map to ProductViewModel for consistent display
                var productViewModel = MapToProductViewModel(product);

                return View(productViewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading product detail for ID: {ProductId}", id);
                return RedirectToAction("Index", "Home");
            }
        }

                // POST: Products/BuyNow - Direct to checkout with single product
        [HttpPost]
        public async Task<IActionResult> BuyNow(string productId, int quantity = 1, string? size = null, string? color = null)
        {
            try
            {
                _logger.LogInformation("BuyNow called - ProductId: {ProductId}, Quantity: {Quantity}, Size: {Size}, Color: {Color}", 
                    productId, quantity, size, color);

                // Get user ID
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                _logger.LogInformation("User authenticated - UserId: {UserId}", userId ?? "Anonymous");

                // Validate input
                if (string.IsNullOrWhiteSpace(productId))
                {
                    _logger.LogWarning("BuyNow validation failed - ProductId is null or empty");
                    return Json(new { success = false, message = "Mã sản phẩm không hợp lệ" });
                }

                if (quantity <= 0)
                {
                    _logger.LogWarning("BuyNow validation failed - Invalid quantity: {Quantity}", quantity);
                    return Json(new { success = false, message = "Số lượng phải lớn hơn 0" });
                }

                // Try to find product by SKU first
                var product = await _context.Products
                    .FirstOrDefaultAsync(p => p.SKU == productId && p.IsActive);

                // If not found by SKU, try by ID
                if (product == null && Guid.TryParse(productId, out var productGuid))
                {
                    product = await _context.Products
                        .FirstOrDefaultAsync(p => p.Id == productGuid && p.IsActive);
                }

                if (product == null)
                {
                    _logger.LogWarning("Product not found with SKU or ID: {ProductId}", productId);
                    return Json(new { success = false, message = "Không tìm thấy sản phẩm" });
                }

                _logger.LogInformation("Product found - ID: {ProductId}, SKU: {SKU}, Name: {Name}, Stock: {Stock}, Price: {Price}", 
                    product.Id, product.SKU, product.Name, product.StockQuantity, product.Price);

                // Check stock availability - only check actual stock in database
                // Don't count other users' cart items as "reserved" since they haven't paid yet
                if (quantity > product.StockQuantity)
                {
                    _logger.LogWarning("Insufficient stock for BuyNow - Requested: {Quantity}, Stock: {Stock}", 
                        quantity, product.StockQuantity);
                    
                    if (product.StockQuantity <= 0)
                    {
                        return Json(new { 
                            success = false, 
                            message = $"Sản phẩm {product.Name} hiện đã hết hàng" 
                        });
                    }
                    else
                    {
                        return Json(new { 
                            success = false, 
                            message = $"Chỉ còn {product.StockQuantity} sản phẩm trong kho" 
                        });
                    }
                }

                // Get price (use sale price if available)
                var price = product.SalePrice > 0 && product.SalePrice < product.Price 
                    ? product.SalePrice 
                    : product.Price;

                // Create buy now item for session
                var buyNowItem = new
                {
                    ProductId = product.Id,
                    ProductSKU = product.SKU,
                    ProductName = product.Name,
                    ProductImage = product.FeaturedImageUrl,
                    Price = price,
                    Quantity = quantity,
                    Size = size ?? "M",
                    Color = color ?? string.Empty,
                    Subtotal = price * quantity
                };

                // Store in TempData for checkout
                TempData["BuyNowItem"] = System.Text.Json.JsonSerializer.Serialize(buyNowItem);
                TempData["IsBuyNow"] = true;

                _logger.LogInformation("BuyNow item stored in TempData - ProductId: {ProductId}, Quantity: {Quantity}, Total: {Total}", 
                    product.Id, quantity, price * quantity);

                return Json(new { 
                    success = true, 
                    message = "Chuyển đến trang thanh toán",
                    redirectUrl = "/Payment/Checkout?mode=buynow"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in BuyNow - ProductId: {ProductId}, Exception: {Message}, StackTrace: {StackTrace}", 
                    productId, ex.Message, ex.StackTrace);
                return Json(new { success = false, message = "Đã xảy ra lỗi. Vui lòng thử lại." });
            }
        }

        // POST: Products/AddToCart
        [HttpPost]
        public async Task<IActionResult> AddToCart([FromBody] AddToCartRequest request)
        {
            try
            {
                // LOG 1: Input parameters
                _logger.LogInformation("AddToCart called - ProductId: {ProductId}, Quantity: {Quantity}, Size: {Size}, Color: {Color}", 
                    request.ProductId, request.Quantity, request.Size, request.Color);
                
                string productId = request.ProductId;
                int quantity = request.Quantity;
                string? size = request.Size;
                string? color = request.Color;

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("AddToCart failed - User not authenticated");
                    return Json(new { success = false, message = "Bạn cần đăng nhập để thêm sản phẩm vào giỏ hàng" });
                }

                // LOG 2: User authentication successful
                _logger.LogInformation("User authenticated - UserId: {UserId}", userId);

                // Validate input
                if (string.IsNullOrWhiteSpace(productId))
                {
                    _logger.LogError("AddToCart failed - ProductId is null or empty");
                    return Json(new { success = false, message = "Mã sản phẩm không hợp lệ" });
                }

                if (quantity <= 0)
                {
                    _logger.LogError("AddToCart failed - Invalid quantity: {Quantity}", quantity);
                    return Json(new { success = false, message = "Số lượng không hợp lệ" });
                }

                // Find product by SKU - use AsNoTracking to ensure fresh data
                var product = await _context.Products
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.SKU == productId && p.IsActive);

                // LOG 3: Product search result
                if (product == null)
                {
                    _logger.LogWarning("Product not found with SKU: {SKU}. Attempting alternative search...", productId);
                    
                    // Alternative 1: Try to find by Id (in case frontend sent Id instead of SKU)
                    Guid productGuid;
                    if (Guid.TryParse(productId, out productGuid))
                    {
                        product = await _context.Products
                            .FirstOrDefaultAsync(p => p.Id == productGuid && p.IsActive);

                        if (product != null)
                        {
                            _logger.LogInformation("Product found by ID instead of SKU. ID: {ProductId}, SKU: {SKU}, Name: {Name}", 
                                product.Id, product.SKU, product.Name);
                        }
                    }
                    
                    // Still not found? Check if product exists but is inactive
                    if (product == null)
                    {
                        // Pre-parse GUID to avoid TryParse inside expression tree
                        Guid pid;
                        var isGuid = Guid.TryParse(productId, out pid);

                        var inactiveProduct = await _context.Products
                            .FirstOrDefaultAsync(p => p.SKU == productId || (isGuid && p.Id == pid));
                        
                        if (inactiveProduct != null)
                        {
                            _logger.LogWarning("Product exists but is inactive. SKU: {SKU}, IsActive: {IsActive}, Stock: {Stock}", 
                                productId, inactiveProduct.IsActive, inactiveProduct.StockQuantity);
                            return Json(new { success = false, message = "Sản phẩm hiện không khả dụng" });
                        }
                        
                        // Product doesn't exist at all
                        _logger.LogError("Product not found in database. ProductId/SKU: {ProductId}", productId);
                        
                        // Query to check what's in database
                        var existingProducts = await _context.Products
                            .Where(p => p.SKU != null && p.SKU != "")
                            .Take(5)
                            .Select(p => new { p.SKU, p.Name, p.IsActive })
                            .ToListAsync();
                        
                        _logger.LogInformation("Sample products in DB: {Products}", 
                            System.Text.Json.JsonSerializer.Serialize(existingProducts));
                        
                        return Json(new { success = false, message = "Không tìm thấy sản phẩm. Vui lòng thử lại hoặc liên hệ hỗ trợ." });
                    }
                }
                
                // LOG 4: Product found successfully
                _logger.LogInformation("Product found - ID: {ProductId}, SKU: {SKU}, Name: {Name}, Stock: {Stock}, Price: {Price}", 
                    product.Id, product.SKU, product.Name, product.StockQuantity, product.Price);

                // Check if item already exists in cart
                var existingCartItem = await _context.ShoppingCartItems
                    .FirstOrDefaultAsync(c => c.UserId == userId && c.ProductId == product.Id);

                // Current quantity in THIS user's cart
                var currentCartQuantity = existingCartItem?.Quantity ?? 0;
                
                // The new total quantity this user wants in their cart
                var newTotalQuantity = currentCartQuantity + quantity;

                _logger.LogInformation("Stock check - ProductId: {ProductId}, Stock: {Stock}, CurrentUserCartQty: {CurrentCartQty}, RequestedToAdd: {RequestedQty}, NewTotal: {NewTotal}", 
                    product.Id, product.StockQuantity, currentCartQuantity, quantity, newTotalQuantity);

                // Check if the new total quantity exceeds actual stock
                // Don't count other users' cart items as "reserved" since they haven't paid yet
                if (newTotalQuantity > product.StockQuantity)
                {
                    var maxCanAdd = Math.Max(0, product.StockQuantity - currentCartQuantity);
                    _logger.LogWarning("Insufficient stock - Product: {ProductId}, RequestedTotal: {NewTotal}, Stock: {Stock}, CurrentInCart: {CurrentQty}, MaxCanAdd: {MaxCanAdd}", 
                        product.Id, newTotalQuantity, product.StockQuantity, currentCartQuantity, maxCanAdd);
                    
                    if (product.StockQuantity == 0)
                    {
                        return Json(new { 
                            success = false, 
                            message = $"Sản phẩm {product.Name} hiện đã hết hàng" 
                        });
                    }
                    else if (maxCanAdd <= 0)
                    {
                        return Json(new { 
                            success = false, 
                            message = $"Bạn đã có {currentCartQuantity} sản phẩm trong giỏ. Không thể thêm nữa (kho còn: {product.StockQuantity})" 
                        });
                    }
                    else
                    {
                        return Json(new { 
                            success = false, 
                            message = $"Chỉ có thể thêm tối đa {maxCanAdd} sản phẩm nữa (bạn đã có {currentCartQuantity} trong giỏ, kho còn: {product.StockQuantity})" 
                        });
                    }
                }

                if (existingCartItem != null)
                {
                    // Update quantity
                    var newQuantity = existingCartItem.Quantity + quantity;

                    existingCartItem.Quantity = newQuantity;
                    existingCartItem.UpdatedAt = DateTime.UtcNow;
                    
                    _logger.LogInformation("Updated cart item - CartItemId: {CartItemId}, UserId: {UserId}, ProductId: {ProductId}, OldQty: {OldQty}, NewQty: {NewQty}", 
                        existingCartItem.Id, userId, product.Id, existingCartItem.Quantity - quantity, newQuantity);
                }
                else
                {
                    // Create new cart item
                    var cartItem = new ShoppingCartItem
                    {
                        Id = Guid.NewGuid(),
                        UserId = userId,
                        ProductId = product.Id,
                        Quantity = quantity,
                        Price = product.SalePrice ?? product.Price,
                        Size = size,
                        Color = color,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    _context.ShoppingCartItems.Add(cartItem);
                    
                    _logger.LogInformation("Created new cart item - CartItemId: {CartItemId}, UserId: {UserId}, ProductId: {ProductId}, Quantity: {Quantity}, Price: {Price}", 
                        cartItem.Id, userId, product.Id, quantity, cartItem.Price);
                }

                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Cart updated successfully - UserId: {UserId}, ProductId: {ProductId}", userId, product.Id);

                // Calculate cart totals
                var cartItems = await _context.ShoppingCartItems
                    .Where(c => c.UserId == userId)
                    .ToListAsync();

                var cartCount = cartItems.Sum(c => c.Quantity);
                var cartTotal = cartItems.Sum(c => c.Price * c.Quantity);

                _logger.LogInformation("AddToCart SUCCESS - UserId: {UserId}, ProductId: {ProductId}, CartCount: {CartCount}, CartTotal: {CartTotal}", 
                    userId, product.Id, cartCount, cartTotal);

                return Json(new 
                { 
                    success = true, 
                    message = "Đã thêm sản phẩm vào giỏ hàng",
                    cartCount = cartCount,
                    cartTotal = cartTotal,
                    productName = product.Name
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "EXCEPTION in AddToCart - ProductId: {ProductId}, UserId: {UserId}, Quantity: {Quantity}, Message: {Message}, StackTrace: {StackTrace}", 
                    request.ProductId, User.FindFirstValue(ClaimTypes.NameIdentifier), request.Quantity, ex.Message, ex.StackTrace);
                return Json(new { success = false, message = "Có lỗi xảy ra khi thêm sản phẩm vào giỏ hàng: " + ex.Message });
            }
        }

        // Helper method to map Product to ProductViewModel
        private ProductViewModel MapToProductViewModel(Product product)
        {
            return new ProductViewModel
            {
                Id = product.Id,
                Name = product.Name,
                SKU = product.SKU,
                Price = product.Price,
                FeaturedImageUrl = product.FeaturedImageUrl ?? "/images/default-product.jpg",
                Images = product.GalleryImages?.ToList() ?? new List<string>(),
                StockQuantity = product.StockQuantity,
                CategoryName = product.Category?.Name ?? "Chưa phân loại",
                BrandName = product.Brand?.Name,
                Rating = product.Rating,
                ReviewCount = product.ReviewCount,
                IsNew = product.CreatedAt > DateTime.UtcNow.AddDays(-30),
                IsFeatured = product.IsFeatured,
                Description = product.Description ?? string.Empty,
                // Parse comma-separated colors and sizes from Product model
                AvailableColors = !string.IsNullOrEmpty(product.Color) 
                    ? product.Color.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(c => c.Trim())
                        .ToList()
                    : new List<string>(),
                AvailableSizes = !string.IsNullOrEmpty(product.Size)
                    ? product.Size.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => s.Trim())
                        .ToList()
                    : new List<string>()
            };
        }
        
        // Request model for AddToCart
        public class AddToCartRequest
        {
            public string ProductId { get; set; } = string.Empty;
            public int Quantity { get; set; } = 1;
            public string? Size { get; set; }
            public string? Color { get; set; }
        }
    }
}
