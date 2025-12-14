using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using JohnHenryFashionWeb.Data;
using JohnHenryFashionWeb.Models;
using JohnHenryFashionWeb.Services;
using System.Security.Claims;
using System.Text.Json;

namespace JohnHenryFashionWeb.Controllers
{
    [Authorize]
    public class PaymentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IPaymentService _paymentService;
        private readonly INotificationService _notificationService;
        private readonly ILogger<PaymentController> _logger;

        public PaymentController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IPaymentService paymentService,
            INotificationService notificationService,
            ILogger<PaymentController> logger)
        {
            _context = context;
            _userManager = userManager;
            _paymentService = paymentService;
            _notificationService = notificationService;
            _logger = logger;
        }

        // GET: Payment/Checkout
        public async Task<IActionResult> Checkout(string? couponCode = null, string? mode = null)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            List<ShoppingCartItem> cartItems;

            // Handle BuyNow mode
            if (mode == "buynow")
            {
                // Check if BuyNow data exists in TempData
                var buyNowItemJson = TempData.Peek("BuyNowItem") as string;
                if (!string.IsNullOrEmpty(buyNowItemJson))
                {
                    try
                    {
                        using var doc = System.Text.Json.JsonDocument.Parse(buyNowItemJson);
                        var buyNowItem = doc.RootElement;
                        
                        var productIdString = buyNowItem.GetProperty("ProductId").GetString();
                        if (Guid.TryParse(productIdString, out var productId))
                        {
                            // Get product and create a temporary cart item for display
                            var product = await _context.Products
                                .FirstOrDefaultAsync(p => p.Id == productId);
                            
                            if (product != null)
                            {
                                var quantity = buyNowItem.TryGetProperty("Quantity", out var qtyEl) ? qtyEl.GetInt32() : 1;
                                var size = buyNowItem.TryGetProperty("Size", out var sizeEl) ? sizeEl.GetString() : null;
                                var color = buyNowItem.TryGetProperty("Color", out var colorEl) ? colorEl.GetString() : null;
                                
                                // Create a temporary cart item (not saved to DB)
                                cartItems = new List<ShoppingCartItem>
                                {
                                    new ShoppingCartItem
                                    {
                                        Id = Guid.NewGuid(),
                                        UserId = userId,
                                        ProductId = productId,
                                        Product = product,
                                        Quantity = quantity,
                                        Size = size,
                                        Color = color,
                                        Price = product.SalePrice ?? product.Price,
                                        CreatedAt = DateTime.UtcNow
                                    }
                                };
                                
                                // Store BuyNow mode in ViewBag
                                ViewBag.IsBuyNow = true;
                                ViewBag.BuyNowData = buyNowItemJson;
                            }
                            else
                            {
                                TempData["Error"] = "Sản phẩm không tồn tại.";
                                return RedirectToAction("Index", "Home");
                            }
                        }
                        else
                        {
                            TempData["Error"] = "Mã sản phẩm không hợp lệ.";
                            return RedirectToAction("Index", "Home");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error parsing BuyNow item");
                        TempData["Error"] = "Có lỗi xảy ra khi xử lý đơn hàng.";
                        return RedirectToAction("Index", "Home");
                    }
                }
                else
                {
                    TempData["Error"] = "Không tìm thấy thông tin sản phẩm.";
                    return RedirectToAction("Index", "Home");
                }
            }
            else
            {
                // Normal checkout from cart - get selected items from session
                var selectedJson = HttpContext.Session.GetString("SelectedCartItems");
                List<Guid> selectedIds;
                
                if (!string.IsNullOrEmpty(selectedJson))
                {
                    try
                    {
                        selectedIds = System.Text.Json.JsonSerializer.Deserialize<List<Guid>>(selectedJson) ?? new List<Guid>();
                        _logger.LogInformation("Checkout: Found {Count} selected items in session", selectedIds.Count);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error deserializing SelectedCartItems");
                        selectedIds = new List<Guid>();
                    }
                }
                else
                {
                    // If no selection in session, take all cart items as fallback
                    selectedIds = await _context.ShoppingCartItems
                        .Where(c => c.UserId == userId)
                        .Select(c => c.Id)
                        .ToListAsync();
                    
                    if (selectedIds.Any())
                    {
                        _logger.LogInformation("Checkout: No selection in session, using all {Count} cart items", selectedIds.Count);
                    }
                }

                // Get only selected items from cart
                cartItems = await _context.ShoppingCartItems
                    .Include(c => c.Product)
                    .Where(c => c.UserId == userId && selectedIds.Contains(c.Id))
                    .ToListAsync();
            }

            if (!cartItems.Any())
            {
                TempData["Error"] = "Giỏ hàng của bạn đang trống.";
                return RedirectToAction("Index", "Cart");
            }

            // Calculate totals
            var subtotal = cartItems.Sum(c => c.Price * c.Quantity);
            var discountAmount = 0m;
            Coupon? appliedCoupon = null;

            // Apply coupon if provided
            if (!string.IsNullOrWhiteSpace(couponCode))
            {
                appliedCoupon = await _context.Coupons
                    .FirstOrDefaultAsync(c => c.Code.ToUpper() == couponCode.ToUpper() && 
                                             c.IsActive &&
                                             (c.StartDate == null || c.StartDate <= DateTime.UtcNow) &&
                                             (c.EndDate == null || c.EndDate >= DateTime.UtcNow));

                if (appliedCoupon != null)
                {
                    // Check usage limits
                    if (appliedCoupon.UsageLimit.HasValue && appliedCoupon.UsageCount >= appliedCoupon.UsageLimit.Value)
                    {
                        TempData["ErrorMessage"] = "Mã giảm giá đã được sử dụng hết";
                    }
                    else if (appliedCoupon.MinOrderAmount.HasValue && subtotal < appliedCoupon.MinOrderAmount.Value)
                    {
                        TempData["ErrorMessage"] = $"Đơn hàng tối thiểu {appliedCoupon.MinOrderAmount.Value:C} để sử dụng mã này";
                    }
                    else
                    {
                        // Calculate discount
                        if (appliedCoupon.Type == "percentage")
                        {
                            discountAmount = subtotal * (appliedCoupon.Value / 100);
                        }
                        else
                        {
                            discountAmount = appliedCoupon.Value;
                        }

                        // Ensure discount doesn't exceed subtotal
                        if (discountAmount > subtotal)
                        {
                            discountAmount = subtotal;
                        }

                        TempData["SuccessMessage"] = $"Áp dụng mã giảm giá {appliedCoupon.Code} thành công!";
                    }
                }
                else
                {
                    TempData["ErrorMessage"] = "Mã giảm giá không hợp lệ hoặc đã hết hạn";
                }
            }

            // Calculate fees and total with discount
            var shippingFee = CalculateShippingFee(subtotal);
            var tax = CalculateTax(subtotal);
            var total = subtotal + shippingFee + tax - discountAmount;

            ViewBag.CartItems = cartItems;
            ViewBag.Subtotal = subtotal;
            ViewBag.ShippingFee = shippingFee;
            ViewBag.Tax = tax;
            ViewBag.Total = total;
            ViewBag.DiscountAmount = discountAmount;
            ViewBag.CouponCode = appliedCoupon?.Code;
            ViewBag.CouponDescription = appliedCoupon?.Description;

            // Get user addresses
            var addresses = await _context.Addresses
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.IsDefault)
                .ToListAsync();

            ViewBag.Addresses = addresses;

            // Get available shipping methods
            var shippingMethods = await _context.ShippingMethods
                .Where(sm => sm.IsActive)
                .OrderBy(sm => sm.SortOrder)
                .ToListAsync();
            ViewBag.ShippingMethods = shippingMethods;

            return View();
        }

        // POST: Payment/ProcessPayment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessPayment(string paymentMethod, string? shippingMethod, Guid? addressId, string? notes, string? couponCode, string? mode = null)
        {
            _logger.LogInformation("ProcessPayment called - PaymentMethod: {PaymentMethod}, ShippingMethod: {ShippingMethod}, AddressId: {AddressId}, Mode: {Mode}", 
                paymentMethod, shippingMethod, addressId, mode);
            
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập để tiếp tục" });
            }

            try
            {
                // Validate required fields
                if (string.IsNullOrEmpty(paymentMethod))
                {
                    return Json(new { success = false, message = "Vui lòng chọn phương thức thanh toán" });
                }
                
                if (!addressId.HasValue || addressId.Value == Guid.Empty)
                {
                    return Json(new { success = false, message = "Vui lòng chọn địa chỉ giao hàng" });
                }
                
                List<ShoppingCartItem> cartItems;
                List<Guid> selectedIds = new List<Guid>();
                bool isBuyNow = mode == "buynow";
                
                // Check if this is a BuyNow order - use Peek to preserve TempData
                var buyNowItemJson = TempData.Peek("BuyNowItem") as string;
                if (isBuyNow && !string.IsNullOrEmpty(buyNowItemJson))
                {
                    isBuyNow = true;
                    try
                    {
                        using var doc = System.Text.Json.JsonDocument.Parse(buyNowItemJson);
                        var buyNowItem = doc.RootElement;
                        
                        var productIdString = buyNowItem.GetProperty("ProductId").GetString();
                        if (Guid.TryParse(productIdString, out var productId))
                        {
                            // CRITICAL: Use AsNoTracking() to get fresh stock data from database
                            // This prevents using stale cached data from EF context
                            var product = await _context.Products
                                .AsNoTracking()
                                .FirstOrDefaultAsync(p => p.Id == productId);
                            
                            if (product == null)
                            {
                                return Json(new { success = false, message = "Sản phẩm không tồn tại" });
                            }
                            
                            var quantity = buyNowItem.TryGetProperty("Quantity", out var qtyEl) ? qtyEl.GetInt32() : 1;
                            var size = buyNowItem.TryGetProperty("Size", out var sizeEl) ? sizeEl.GetString() : null;
                            var color = buyNowItem.TryGetProperty("Color", out var colorEl) ? colorEl.GetString() : null;
                            var price = buyNowItem.TryGetProperty("Price", out var priceEl) ? priceEl.GetDecimal() : (product.SalePrice ?? product.Price);
                            
                            // IMPORTANT: For BuyNow, validate stock IMMEDIATELY before creating cart item
                            // Don't wait until later - check right now with fresh data
                            if (product.StockQuantity < quantity)
                            {
                                var errorMsg = product.StockQuantity == 0
                                    ? $"Sản phẩm {product.Name} hiện đã hết hàng"
                                    : $"Sản phẩm {product.Name} chỉ còn {product.StockQuantity} sản phẩm, không đủ cho số lượng bạn đặt ({quantity})";
                                
                                _logger.LogWarning("BuyNow stock validation failed - ProductId: {ProductId}, Stock: {Stock}, RequestedQty: {Quantity}", 
                                    productId, product.StockQuantity, quantity);
                                
                                return Json(new { success = false, message = errorMsg });
                            }
                            
                            // Create temporary cart item for BuyNow
                            cartItems = new List<ShoppingCartItem>
                            {
                                new ShoppingCartItem
                                {
                                    Id = Guid.NewGuid(),
                                    UserId = userId,
                                    ProductId = productId,
                                    Product = product,
                                    Quantity = quantity,
                                    Size = size,
                                    Color = color,
                                    Price = price,
                                    CreatedAt = DateTime.UtcNow
                                }
                            };
                            
                            _logger.LogInformation("BuyNow cart item created - ProductId: {ProductId}, Quantity: {Quantity}, Stock: {Stock}", 
                                productId, quantity, product.StockQuantity);
                        }
                        else
                        {
                            return Json(new { success = false, message = "Mã sản phẩm không hợp lệ" });
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error parsing BuyNow item in ProcessPayment");
                        return Json(new { success = false, message = "Có lỗi xảy ra khi xử lý đơn hàng" });
                    }
                }
                else
                {
                    // Normal checkout - get selected items from session
                    var selectedJson = HttpContext.Session.GetString("SelectedCartItems");
                    
                    if (!string.IsNullOrEmpty(selectedJson))
                    {
                        try
                        {
                            selectedIds = System.Text.Json.JsonSerializer.Deserialize<List<Guid>>(selectedJson) ?? new List<Guid>();
                            _logger.LogInformation("ProcessPayment: Found {Count} selected items in session", selectedIds.Count);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error deserializing SelectedCartItems in ProcessPayment");
                            selectedIds = new List<Guid>();
                        }
                    }
                    else
                    {
                        // Fallback: use all cart items if no selection
                        selectedIds = await _context.ShoppingCartItems
                            .Where(c => c.UserId == userId)
                            .Select(c => c.Id)
                            .ToListAsync();
                        
                        _logger.LogWarning("ProcessPayment: No selection in session, using all {Count} cart items", selectedIds.Count);
                    }

                    // Get only selected items from cart
                    cartItems = await _context.ShoppingCartItems
                        .Include(c => c.Product)
                        .Where(c => c.UserId == userId && selectedIds.Contains(c.Id))
                        .ToListAsync();
                }

                if (!cartItems.Any())
                {
                    return Json(new { success = false, message = "Giỏ hàng trống" });
                }

                // Validate stock availability - MUST reload from database to get fresh data
                // Entity Framework tracking cache can return stale data
                // IMPORTANT: Skip this check for BuyNow since we already validated stock above
                if (!isBuyNow)
                {
                    _logger.LogInformation("ProcessPayment: Validating stock for {Count} cart items (Normal checkout mode)", cartItems.Count);
                    
                    foreach (var item in cartItems)
                    {
                        // Reload product from database with AsNoTracking to bypass EF cache
                        var freshProduct = await _context.Products
                            .AsNoTracking()
                            .Where(p => p.Id == item.ProductId)
                            .Select(p => new { p.StockQuantity, p.Name })
                            .FirstOrDefaultAsync();
                        
                        if (freshProduct == null)
                        {
                            return Json(new { 
                                success = false, 
                                message = $"Sản phẩm {item.Product?.Name ?? "không xác định"} không tồn tại trong hệ thống" 
                            });
                        }
                        
                        var availableStock = freshProduct.StockQuantity;
                        
                        _logger.LogInformation("Stock check - Product: {ProductId}, Name: {Name}, Stock: {Stock}, RequestedQty: {Qty}", 
                            item.ProductId, freshProduct.Name, availableStock, item.Quantity);
                        
                        // Only show error if stock is insufficient
                        if (availableStock < item.Quantity)
                        {
                            // Only show detailed stock message if there IS some stock but not enough
                            // If completely out of stock, show simple "hết hàng" message
                            _logger.LogWarning("Insufficient stock - Product: {Name}, Stock: {Stock}, Requested: {Qty}", 
                                freshProduct.Name, availableStock, item.Quantity);
                            
                            return Json(new { 
                                success = false, 
                                message = availableStock == 0
                                    ? $"Sản phẩm {freshProduct.Name} hiện đã hết hàng"
                                    : $"Sản phẩm {freshProduct.Name} chỉ còn {availableStock} sản phẩm, không đủ cho số lượng bạn đặt ({item.Quantity})" 
                            });
                        }
                        // If stock is sufficient, continue without any message (no need to notify user)
                    }
                }
                else
                {
                    _logger.LogInformation("ProcessPayment: Skipping stock validation for BuyNow mode (already validated)");
                }

                // Get and validate shipping address
                var address = await _context.Addresses
                    .FirstOrDefaultAsync(a => a.Id == addressId.Value && a.UserId == userId);
                    
                if (address == null)
                {
                    return Json(new { success = false, message = "Địa chỉ giao hàng không hợp lệ" });
                }
                
                var shippingAddress = $"{address.FirstName} {address.LastName}, {address.Phone ?? ""}, {address.Address1}, {address.City}, {address.State}";

                // Calculate amounts with coupon
                var subtotal = cartItems.Sum(c => c.Price * c.Quantity);
                var shippingFee = await CalculateShippingFeeAsync(shippingMethod, subtotal);
                var tax = CalculateTax(subtotal);
                
                // Apply coupon discount
                var discountAmount = 0m;
                Coupon? appliedCoupon = null;
                
                if (!string.IsNullOrEmpty(couponCode))
                {
                    appliedCoupon = await _context.Coupons
                        .FirstOrDefaultAsync(c => c.Code == couponCode && 
                                           c.IsActive && 
                                           (!c.EndDate.HasValue || c.EndDate > DateTime.UtcNow) &&
                                           (!c.StartDate.HasValue || c.StartDate <= DateTime.UtcNow));
                    
                    if (appliedCoupon != null)
                    {
                        if (appliedCoupon.Type == "percentage")
                        {
                            discountAmount = subtotal * (appliedCoupon.Value / 100);
                        }
                        else
                        {
                            discountAmount = appliedCoupon.Value;
                        }
                        
                        // Check minimum order amount
                        if (appliedCoupon.MinOrderAmount.HasValue && subtotal < appliedCoupon.MinOrderAmount.Value)
                        {
                            return Json(new { 
                                success = false, 
                                message = $"Đơn hàng tối thiểu {appliedCoupon.MinOrderAmount:C} để sử dụng mã giảm giá này" 
                            });
                        }
                    }
                }
                
                var total = subtotal + shippingFee + tax - discountAmount;

                // Create order
                var order = new Order
                {
                    Id = Guid.NewGuid(),
                    OrderNumber = GenerateOrderNumber(),
                    UserId = userId,
                    Status = "pending",
                    TotalAmount = total,
                    ShippingFee = shippingFee,
                    Tax = tax,
                    PaymentMethod = paymentMethod,
                    PaymentStatus = "pending",
                    Notes = notes + (shippingMethod != null ? $" [Vận chuyển: {shippingMethod}]" : ""),
                    ShippingAddress = shippingAddress,
                    BillingAddress = shippingAddress,
                    CouponCode = appliedCoupon?.Code,
                    DiscountAmount = discountAmount,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Orders.Add(order);

                // Create order items
                foreach (var cartItem in cartItems)
                {
                    var orderItem = new OrderItem
                    {
                        Id = Guid.NewGuid(),
                        OrderId = order.Id,
                        ProductId = cartItem.ProductId,
                        Quantity = cartItem.Quantity,
                        UnitPrice = cartItem.Price,
                        TotalPrice = cartItem.Price * cartItem.Quantity,
                        ProductName = cartItem.Product.Name,
                        ProductSKU = cartItem.Product.SKU,
                        ProductImage = cartItem.Product.FeaturedImageUrl
                    };

                    _context.OrderItems.Add(orderItem);

                    // NOTE: Stock deduction removed from here to prevent double deduction
                    // Stock will be deducted in CheckoutController.UpdateInventoryAsync when order is delivered
                    // This ensures stock is only deducted once when the order is actually fulfilled
                    
                    // OLD CODE (REMOVED):
                    // cartItem.Product.StockQuantity -= cartItem.Quantity;
                    // if (cartItem.Product.StockQuantity <= 0)
                    // {
                    //     cartItem.Product.InStock = false;
                    //     cartItem.Product.Status = "out_of_stock";
                    // }
                }

                // Save order first (before payment processing)
                await _context.SaveChangesAsync();

                // Process payment based on method
                var paymentResult = await ProcessPaymentMethod(order, paymentMethod);
                
                if (paymentResult.Success)
                {
                    // Clear BuyNow TempData after order is created successfully
                    if (isBuyNow)
                    {
                        TempData.Remove("BuyNowItem");
                        TempData.Remove("IsBuyNow");
                        _logger.LogInformation("Cleared BuyNow TempData after order creation");
                    }
                    
                    // For online payment methods (VNPay, MoMo), redirect directly to payment gateway
                    if (paymentMethod.ToLower() == "vnpay" || paymentMethod.ToLower() == "momo")
                    {
                        if (!string.IsNullOrEmpty(paymentResult.RedirectUrl))
                        {
                            _logger.LogInformation("Redirecting to payment gateway: {RedirectUrl}", paymentResult.RedirectUrl);
                            return Json(new { 
                                success = true, 
                                message = paymentResult.Message, 
                                orderId = order.Id,
                                redirectUrl = paymentResult.RedirectUrl,
                                isPaymentGateway = true
                            });
                        }
                        else
                        {
                            _logger.LogWarning("Payment gateway URL is empty for {PaymentMethod}", paymentMethod);
                            return Json(new { 
                                success = false, 
                                message = "Không thể tạo URL thanh toán. Vui lòng thử lại." 
                            });
                        }
                    }
                    
                    // For COD and Bank Transfer, update coupon and clear cart
                    if (appliedCoupon != null)
                    {
                        appliedCoupon.UsageCount++;
                        _context.Coupons.Update(appliedCoupon);
                    }
                    
                    // Clear cart for non-gateway payments (only for normal checkout, not BuyNow)
                    if (!isBuyNow && (paymentMethod.ToLower() == "cod" || paymentMethod.ToLower() == "bank_transfer"))
                    {
                        // Only remove selected items from cart (not all items)
                        var selectedJson = HttpContext.Session.GetString("SelectedCartItems");
                        if (!string.IsNullOrEmpty(selectedJson))
                        {
                            try
                            {
                                var selectedItemIds = System.Text.Json.JsonSerializer.Deserialize<List<Guid>>(selectedJson) ?? new List<Guid>();
                                var itemsToRemove = await _context.ShoppingCartItems
                                    .Where(c => c.UserId == userId && selectedItemIds.Contains(c.Id))
                                    .ToListAsync();
                                
                                if (itemsToRemove.Any())
                                {
                                    _context.ShoppingCartItems.RemoveRange(itemsToRemove);
                                    // Clear selection from session
                                    HttpContext.Session.Remove("SelectedCartItems");
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Error removing selected cart items after payment");
                            }
                        }
                    }
                    
                    await _context.SaveChangesAsync();

                    // Send notifications (async, don't wait)
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await _notificationService.SendOrderNotificationAsync(order);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to send order notification for order {OrderId}", order.Id);
                        }
                    });

                    return Json(new { 
                        success = true, 
                        message = paymentResult.Message, 
                        orderId = order.Id,
                        redirectUrl = Url.Action("OrderConfirmation", new { id = order.Id })
                    });
                }
                else
                {
                    // Remove the order if payment failed
                    _context.Orders.Remove(order);
                    await _context.SaveChangesAsync();
                    return Json(new { success = false, message = paymentResult.Message });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing payment for user {UserId}", userId);
                return Json(new { success = false, message = "Có lỗi xảy ra khi xử lý thanh toán" });
            }
        }

        // GET: Payment/OrderConfirmation/5
        public async Task<IActionResult> OrderConfirmation(Guid id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId);

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        // GET: Payment/VNPay/Return
        public async Task<IActionResult> VNPayReturn()
        {
            try
            {
                // Handle VNPay return
                var vnp_ResponseCode = Request.Query["vnp_ResponseCode"].ToString();
                var vnp_TxnRef = Request.Query["vnp_TxnRef"].ToString();
                var vnp_Amount = Request.Query["vnp_Amount"].ToString();
                var vnp_OrderInfo = Request.Query["vnp_OrderInfo"].ToString();
                var vnp_TransactionNo = Request.Query["vnp_TransactionNo"].ToString();

                _logger.LogInformation("VNPay Return - ResponseCode: {ResponseCode}, TxnRef: {TxnRef}", 
                    vnp_ResponseCode, vnp_TxnRef);

                if (vnp_ResponseCode == "00" && Guid.TryParse(vnp_TxnRef, out var orderId))
                {
                    var order = await _context.Orders
                        .Include(o => o.OrderItems)
                        .FirstOrDefaultAsync(o => o.Id == orderId);
                    
                    if (order != null && order.PaymentStatus != "paid")
                    {
                        order.PaymentStatus = "paid";
                        order.Status = "processing";
                        order.UpdatedAt = DateTime.UtcNow;

                        // Create payment record
                        var payment = new Payment
                        {
                            Id = Guid.NewGuid(),
                            OrderId = orderId,
                            PaymentMethod = "vnpay",
                            Status = "completed",
                            Amount = order.TotalAmount,
                            TransactionId = vnp_TransactionNo,
                            GatewayResponse = JsonSerializer.Serialize(Request.Query.ToDictionary(kv => kv.Key, kv => kv.Value.ToString())),
                            ProcessedAt = DateTime.UtcNow,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };

                        _context.Payments.Add(payment);
                        
                        // Clear cart if not already cleared
                        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                        if (!string.IsNullOrEmpty(userId))
                        {
                            var cartItems = await _context.ShoppingCartItems
                                .Where(c => c.UserId == userId)
                                .ToListAsync();
                            if (cartItems.Any())
                            {
                                _context.ShoppingCartItems.RemoveRange(cartItems);
                            }
                        }
                        
                        await _context.SaveChangesAsync();

                        return RedirectToAction("OrderConfirmation", new { id = orderId });
                    }
                    else if (order != null && order.PaymentStatus == "paid")
                    {
                        // Already paid, just redirect
                        return RedirectToAction("OrderConfirmation", new { id = orderId });
                    }
                }

                TempData["Error"] = "Thanh toán không thành công. Vui lòng thử lại.";
                return RedirectToAction("Checkout");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing VNPay return");
                TempData["Error"] = "Có lỗi xảy ra khi xử lý thanh toán.";
                return RedirectToAction("Checkout");
            }
        }

        // POST: Payment/VNPay/IPN (IPN callback from VNPay)
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> VNPayIPN()
        {
            try
            {
                var vnp_ResponseCode = Request.Form["vnp_ResponseCode"].ToString();
                var vnp_TxnRef = Request.Form["vnp_TxnRef"].ToString();
                
                _logger.LogInformation("VNPay IPN - ResponseCode: {ResponseCode}, TxnRef: {TxnRef}", 
                    vnp_ResponseCode, vnp_TxnRef);

                if (vnp_ResponseCode == "00" && Guid.TryParse(vnp_TxnRef, out var orderId))
                {
                    var order = await _context.Orders.FindAsync(orderId);
                    if (order != null && order.PaymentStatus != "paid")
                    {
                        order.PaymentStatus = "paid";
                        order.Status = "processing";
                        order.UpdatedAt = DateTime.UtcNow;
                        await _context.SaveChangesAsync();
                    }
                }

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing VNPay IPN");
                return BadRequest();
            }
        }

        // GET: Payment/MoMo/Return
        public async Task<IActionResult> MoMoReturn()
        {
            try
            {
                // Handle MoMo return
                var resultCode = Request.Query["resultCode"].ToString();
                var orderId = Request.Query["orderId"].ToString();
                var transId = Request.Query["transId"].ToString();

                _logger.LogInformation("MoMo Return - ResultCode: {ResultCode}, OrderId: {OrderId}", 
                    resultCode, orderId);

                // If no parameters, redirect to home (user accessed URL directly)
                if (string.IsNullOrEmpty(resultCode) && string.IsNullOrEmpty(orderId))
                {
                    _logger.LogWarning("MoMo Return accessed without parameters");
                    TempData["Info"] = "Không tìm thấy thông tin thanh toán.";
                    return RedirectToAction("Index", "Home");
                }

                if (resultCode == "0" && Guid.TryParse(orderId, out var orderGuid))
                {
                    var order = await _context.Orders
                        .Include(o => o.OrderItems)
                        .FirstOrDefaultAsync(o => o.Id == orderGuid);
                    
                    if (order != null && order.PaymentStatus != "paid")
                    {
                        order.PaymentStatus = "paid";
                        order.Status = "processing";
                        order.UpdatedAt = DateTime.UtcNow;

                        // Create payment record
                        var payment = new Payment
                        {
                            Id = Guid.NewGuid(),
                            OrderId = orderGuid,
                            PaymentMethod = "momo",
                            Status = "completed",
                            Amount = order.TotalAmount,
                            TransactionId = transId,
                            GatewayResponse = JsonSerializer.Serialize(Request.Query.ToDictionary(kv => kv.Key, kv => kv.Value.ToString())),
                            ProcessedAt = DateTime.UtcNow,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };

                        _context.Payments.Add(payment);
                        
                        // Clear cart if not already cleared
                        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                        if (!string.IsNullOrEmpty(userId))
                        {
                            var cartItems = await _context.ShoppingCartItems
                                .Where(c => c.UserId == userId)
                                .ToListAsync();
                            if (cartItems.Any())
                            {
                                _context.ShoppingCartItems.RemoveRange(cartItems);
                            }
                        }
                        
                        await _context.SaveChangesAsync();

                        return RedirectToAction("OrderConfirmation", new { id = orderGuid });
                    }
                    else if (order != null && order.PaymentStatus == "paid")
                    {
                        // Already paid, just redirect
                        return RedirectToAction("OrderConfirmation", new { id = orderGuid });
                    }
                }

                TempData["Error"] = "Thanh toán không thành công. Vui lòng thử lại.";
                return RedirectToAction("Checkout");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing MoMo return");
                TempData["Error"] = "Có lỗi xảy ra khi xử lý thanh toán.";
                return RedirectToAction("Checkout");
            }
        }

        // POST: Payment/MoMo/IPN (IPN callback from MoMo)
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> MoMoIPN()
        {
            try
            {
                using var reader = new StreamReader(Request.Body);
                var body = await reader.ReadToEndAsync();
                
                _logger.LogInformation("MoMo IPN - Body: {Body}", body);

                var data = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(body);
                if (data != null && data.TryGetValue("resultCode", out var resultCodeElement))
                {
                    var resultCode = resultCodeElement.GetInt32();
                    if (resultCode == 0 && data.TryGetValue("orderId", out var orderIdElement))
                    {
                        var orderIdStr = orderIdElement.GetString();
                        if (Guid.TryParse(orderIdStr, out var orderId))
                        {
                            var order = await _context.Orders.FindAsync(orderId);
                            if (order != null && order.PaymentStatus != "paid")
                            {
                                order.PaymentStatus = "paid";
                                order.Status = "processing";
                                order.UpdatedAt = DateTime.UtcNow;
                                await _context.SaveChangesAsync();
                            }
                        }
                    }
                }

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing MoMo IPN");
                return BadRequest();
            }
        }

        private decimal CalculateShippingFee(decimal subtotal)
        {
            // Tự động miễn phí vận chuyển cho đơn hàng >= 500,000đ
            if (subtotal >= 500000)
                return 0;
            
            // Standard shipping fee
            return 30000;
        }

        private async Task<decimal> CalculateShippingFeeAsync(string? shippingMethodCode, decimal subtotal)
        {
            if (string.IsNullOrEmpty(shippingMethodCode))
                return CalculateShippingFee(subtotal);

            var method = await _context.ShippingMethods
                .FirstOrDefaultAsync(sm => sm.Code == shippingMethodCode && sm.IsActive);

            if (method == null)
                return CalculateShippingFee(subtotal);

            // Giảm giá theo tầng cho Giao hàng hỏa tốc (SUPER_EXPRESS)
            if (shippingMethodCode == "SUPER_EXPRESS")
            {
                if (subtotal >= 2000000) // >= 2 triệu: Miễn phí 100%
                    return 0;
                else if (subtotal >= 1000000) // >= 1 triệu: Giảm 50%
                    return method.Cost * 0.5m;
                // < 1 triệu: Giá gốc
                return method.Cost;
            }

            // Các phương thức khác: Miễn phí vận chuyển cho đơn hàng >= 500,000đ
            if (subtotal >= 500000)
                return 0;

            // Check minimum order amount for free shipping (if specified in shipping method)
            if (method.MinOrderAmount.HasValue && subtotal >= method.MinOrderAmount.Value)
                return 0;

            return method.Cost;
        }

        private decimal CalculateTax(decimal subtotal)
        {
            // 10% VAT
            return subtotal * 0.1m;
        }

        private string GenerateOrderNumber()
        {
            return $"JH{DateTime.Now:yyyyMMdd}{new Random().Next(1000, 9999)}";
        }

        private async Task<PaymentResult> ProcessPaymentMethod(Order order, string paymentMethod)
        {
            switch (paymentMethod.ToLower())
            {
                case "cod":
                    return await ProcessCODPayment(order);
                case "bank_transfer":
                    return await ProcessBankTransferPayment(order);
                case "vnpay":
                    return await ProcessVNPayPayment(order);
                case "momo":
                    return await ProcessMoMoPayment(order);
                default:
                    return new PaymentResult { Success = false, Message = "Phương thức thanh toán không hợp lệ" };
            }
        }

        private Task<PaymentResult> ProcessCODPayment(Order order)
        {
            // COD: Đơn hàng chờ xác nhận, thanh toán khi nhận hàng
            order.PaymentStatus = "COD Pending"; // Trạng thái đặc biệt cho COD
            order.Status = "pending"; // Đơn hàng chờ xác nhận
            
            var payment = new Payment
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                PaymentMethod = "cod",
                Status = "pending",
                Amount = order.TotalAmount,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Payments.Add(payment);
            return Task.FromResult(new PaymentResult { Success = true, Message = "Đặt hàng thành công! Thanh toán khi nhận hàng." });
        }

        private Task<PaymentResult> ProcessBankTransferPayment(Order order)
        {
            // Bank transfer: Đơn hàng chờ thanh toán và xác nhận
            order.PaymentStatus = "awaiting_transfer"; // Trạng thái đặc biệt cho Bank Transfer
            order.Status = "pending"; // Đơn hàng chờ thanh toán
            
            var payment = new Payment
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                PaymentMethod = "bank_transfer",
                Status = "pending",
                Amount = order.TotalAmount,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Payments.Add(payment);
            return Task.FromResult(new PaymentResult { Success = true, Message = "Đặt hàng thành công! Vui lòng chuyển khoản và gửi ảnh xác nhận." });
        }

        private async Task<PaymentResult> ProcessVNPayPayment(Order order)
        {
            try
            {
                order.PaymentStatus = "pending";
                order.Status = "pending";
                
                // Build full URLs for VNPay (must be absolute URLs)
                var scheme = Request.Scheme;
                var host = Request.Host.Value;
                
                // Ensure http for localhost (VNPay sandbox may not accept https localhost)
                if (!string.IsNullOrEmpty(host) && (host.Contains("localhost") || host.Contains("127.0.0.1")))
                {
                    scheme = "http";
                }
                
                // Use the current scheme (http or https based on how the app is running)
                var returnUrl = $"{scheme}://{host}{Url.Action("VNPayReturn", "Payment")}";
                var notifyUrl = $"{scheme}://{host}{Url.Action("VNPayIPN", "Payment")}";
                
                _logger.LogInformation("VNPay URLs - Scheme: {Scheme}, Host: {Host}, ReturnUrl: {ReturnUrl}, NotifyUrl: {NotifyUrl}", 
                    scheme, host, returnUrl, notifyUrl);
                
                // Use OrderNumber for VNPay TxnRef, but use OrderId (Guid) for validation
                var paymentRequest = new PaymentRequest
                {
                    OrderId = order.Id.ToString(), // Use Guid for validation (matches database)
                    UserId = order.UserId ?? string.Empty,
                    Amount = order.TotalAmount,
                    Currency = "VND",
                    PaymentMethod = "vnpay",
                    OrderInfo = $"Thanh toan don hang {order.OrderNumber ?? order.Id.ToString()}",
                    ReturnUrl = returnUrl,
                    NotifyUrl = notifyUrl,
                    IpAddress = Request.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "",
                    UserAgent = Request.Headers["User-Agent"].ToString()
                };
                
                _logger.LogInformation("VNPay Payment Request - OrderId: {OrderId}, OrderNumber: {OrderNumber}, Amount: {Amount}",
                    paymentRequest.OrderId, order.OrderNumber, paymentRequest.Amount);

                var result = await _paymentService.ProcessPaymentAsync(paymentRequest);
                
                if (result.IsSuccess && !string.IsNullOrEmpty(result.PaymentUrl))
                {
                    return new PaymentResult
                    {
                        Success = true,
                        Message = "Chuyển hướng đến VNPay...",
                        RedirectUrl = result.PaymentUrl
                    };
                }
                else
                {
                    _logger.LogError("VNPay payment failed: {Error}", result.ErrorMessage);
                    return new PaymentResult
                    {
                        Success = false,
                        Message = result.ErrorMessage ?? "Không thể kết nối đến VNPay"
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing VNPay payment for order {OrderId}", order.Id);
                return new PaymentResult
                {
                    Success = false,
                    Message = "Có lỗi xảy ra khi xử lý thanh toán VNPay"
                };
            }
        }

        private async Task<PaymentResult> ProcessMoMoPayment(Order order)
        {
            try
            {
                order.PaymentStatus = "pending";
                order.Status = "pending";
                
                var returnUrl = Url.Action("MoMoReturn", "Payment", null, Request.Scheme) ?? string.Empty;
                var notifyUrl = Url.Action("MoMoIPN", "Payment", null, Request.Scheme) ?? string.Empty;
                
                var paymentRequest = new PaymentRequest
                {
                    OrderId = order.Id.ToString(),
                    UserId = order.UserId ?? string.Empty,
                    Amount = order.TotalAmount,
                    Currency = "VND",
                    PaymentMethod = "momo",
                    OrderInfo = $"Thanh toán đơn hàng #{order.OrderNumber}",
                    ReturnUrl = returnUrl,
                    NotifyUrl = notifyUrl,
                    IpAddress = Request.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "",
                    UserAgent = Request.Headers["User-Agent"].ToString()
                };

                var result = await _paymentService.ProcessPaymentAsync(paymentRequest);
                
                if (result.IsSuccess && !string.IsNullOrEmpty(result.PaymentUrl))
                {
                    return new PaymentResult
                    {
                        Success = true,
                        Message = "Chuyển hướng đến MoMo...",
                        RedirectUrl = result.PaymentUrl
                    };
                }
                else
                {
                    _logger.LogError("MoMo payment failed: {Error}", result.ErrorMessage);
                    return new PaymentResult
                    {
                        Success = false,
                        Message = result.ErrorMessage ?? "Không thể kết nối đến MoMo"
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing MoMo payment for order {OrderId}", order.Id);
                return new PaymentResult
                {
                    Success = false,
                    Message = "Có lỗi xảy ra khi xử lý thanh toán MoMo"
                };
            }
        }

        // POST: Payment/SaveAddress
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveAddress(
            string firstName, 
            string lastName, 
            string phone, 
            string address1, 
            string? address2,
            string city, 
            string state, 
            string postalCode,
            bool isDefault = false)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập" });
            }

            try
            {
                // Validation
                if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
                {
                    return Json(new { success = false, message = "Vui lòng nhập họ tên" });
                }

                if (string.IsNullOrWhiteSpace(phone))
                {
                    return Json(new { success = false, message = "Vui lòng nhập số điện thoại" });
                }

                if (string.IsNullOrWhiteSpace(address1) || string.IsNullOrWhiteSpace(city))
                {
                    return Json(new { success = false, message = "Vui lòng nhập địa chỉ đầy đủ" });
                }

                // If setting as default, unset other default addresses
                if (isDefault)
                {
                    var existingDefaults = await _context.Addresses
                        .Where(a => a.UserId == userId && a.IsDefault)
                        .ToListAsync();
                    
                    foreach (var addr in existingDefaults)
                    {
                        addr.IsDefault = false;
                    }
                }

                // Create new address
                var newAddress = new Address
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    Type = "shipping",
                    FirstName = firstName.Trim(),
                    LastName = lastName.Trim(),
                    Phone = phone.Trim(),
                    Address1 = address1.Trim(),
                    Address2 = address2?.Trim(),
                    City = city.Trim(),
                    State = state?.Trim() ?? "",
                    PostalCode = postalCode?.Trim() ?? "",
                    Country = "Vietnam",
                    IsDefault = isDefault,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Addresses.Add(newAddress);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"New address saved for user {userId}: {newAddress.Id}");

                return Json(new { 
                    success = true, 
                    message = "Đã lưu địa chỉ thành công",
                    addressId = newAddress.Id,
                    addressHtml = RenderAddressHtml(newAddress)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving address");
                return Json(new { success = false, message = "Có lỗi xảy ra khi lưu địa chỉ" });
            }
        }

        private string RenderAddressHtml(Address address)
        {
            var isDefaultBadge = address.IsDefault ? "<span class=\"badge bg-primary ms-2\">Mặc định</span>" : "";
            var address2Line = !string.IsNullOrEmpty(address.Address2) ? $"<br><span class=\"text-muted\">{address.Address2}</span>" : "";
            var phoneLine = !string.IsNullOrEmpty(address.Phone) ? $"<br><span class=\"text-muted\">SĐT: {address.Phone}</span>" : "";

            return $@"
<div class=""form-check border rounded p-3 mb-3"">
    <input class=""form-check-input"" type=""radio"" name=""addressId"" value=""{address.Id}"" 
           id=""address_{address.Id}"" checked>
    <label class=""form-check-label w-100"" for=""address_{address.Id}"">
        <div class=""d-flex justify-content-between"">
            <div>
                <strong>{address.FirstName} {address.LastName}</strong>
                {isDefaultBadge}
                <br>
                <span class=""text-muted"">{address.Address1}</span>
                {address2Line}
                <br>
                <span class=""text-muted"">{address.City}, {address.State} {address.PostalCode}</span>
                {phoneLine}
            </div>
        </div>
    </label>
</div>";
        }

        // GET: Payment/PaymentQR
        public async Task<IActionResult> PaymentQR(Guid orderId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);

            if (order == null)
            {
                TempData["Error"] = "Không tìm thấy đơn hàng.";
                return RedirectToAction("Checkout");
            }

            return View(order);
        }

        // POST: Payment/GeneratePaymentQR
        [HttpPost]
        public async Task<IActionResult> GeneratePaymentQR(Guid orderId, string paymentMethod)
        {
            try
            {
                var order = await _context.Orders.FindAsync(orderId);
                if (order == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy đơn hàng" });
                }

                var returnUrl = Url.Action("VNPayReturn", "Payment", null, Request.Scheme) ?? string.Empty;
                var notifyUrl = Url.Action("VNPayIPN", "Payment", null, Request.Scheme) ?? string.Empty;

                var paymentRequest = new PaymentRequest
                {
                    OrderId = order.Id.ToString(),
                    UserId = order.UserId ?? string.Empty,
                    Amount = order.TotalAmount,
                    Currency = "VND",
                    PaymentMethod = paymentMethod,
                    OrderInfo = $"Thanh toán đơn hàng #{order.OrderNumber}",
                    ReturnUrl = returnUrl,
                    NotifyUrl = notifyUrl,
                    IpAddress = Request.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "",
                    UserAgent = Request.Headers["User-Agent"].ToString()
                };

                var result = await _paymentService.ProcessPaymentAsync(paymentRequest);

                if (result.IsSuccess && !string.IsNullOrEmpty(result.PaymentUrl))
                {
                    // Return payment URL for redirect
                    return Json(new
                    {
                        success = true,
                        paymentUrl = result.PaymentUrl,
                        expiresInSeconds = 900, // 15 minutes
                        message = "Chuyển hướng đến cổng thanh toán..."
                    });
                }
                else
                {
                    return Json(new { success = false, message = result.ErrorMessage ?? "Không thể kết nối đến cổng thanh toán" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating QR code for order {OrderId}", orderId);
                return Json(new { success = false, message = "Có lỗi xảy ra khi tạo mã QR" });
            }
        }

        // GET: Payment/CheckPaymentStatus
        [HttpGet]
        public async Task<IActionResult> CheckPaymentStatus(Guid orderId)
        {
            try
            {
                var order = await _context.Orders.FindAsync(orderId);
                if (order == null)
                {
                    return Json(new { status = "error", message = "Không tìm thấy đơn hàng" });
                }

                if (order.PaymentStatus == "paid")
                {
                    return Json(new { status = "paid", message = "Thanh toán thành công" });
                }

                // Check if order is expired (created more than 15 minutes ago)
                if (order.CreatedAt.AddMinutes(15) < DateTime.UtcNow)
                {
                    return Json(new { status = "expired", message = "Đơn hàng đã hết hạn" });
                }

                return Json(new { status = "pending", message = "Đang chờ thanh toán" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking payment status for order {OrderId}", orderId);
                return Json(new { status = "error", message = "Có lỗi xảy ra" });
            }
        }
    }

    public class PaymentResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? RedirectUrl { get; set; }
    }
}
