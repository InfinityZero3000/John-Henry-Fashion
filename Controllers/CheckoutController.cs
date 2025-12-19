using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using JohnHenryFashionWeb.Models;
using JohnHenryFashionWeb.Data;
using JohnHenryFashionWeb.Services;
using JohnHenryFashionWeb.ViewModels;
using System.Text.Json;

namespace JohnHenryFashionWeb.Controllers
{
    public class CheckoutController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IPaymentService _paymentService;
        private readonly IEmailService _emailService;
        private readonly INotificationService _notificationService;
        private readonly ICacheService _cacheService;
        private readonly ILogger<CheckoutController> _logger;

        public CheckoutController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IPaymentService paymentService,
            IEmailService emailService,
            INotificationService notificationService,
            ICacheService cacheService,
            ILogger<CheckoutController> logger)
        {
            _context = context;
            _userManager = userManager;
            _paymentService = paymentService;
            _emailService = emailService;
            _notificationService = notificationService;
            _cacheService = cacheService;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Index(string? mode)
        {
            // Checkout cũ đã được thay thế bởi Payment/Checkout.
            // Giữ controller này chỉ để redirect cho các link cũ /Checkout, /Checkout?mode=buynow...
            _logger.LogInformation("Legacy CheckoutController.Index hit. Redirecting to Payment/Checkout with mode = {Mode}", mode);
            return RedirectToAction("Checkout", "Payment", new { mode });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateSession([FromBody] CheckoutCreateViewModel model)
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                
                // Validate model
                if (!ModelState.IsValid)
                {
                    var errors = ModelState
                        .Where(x => x.Value?.Errors.Count > 0)
                        .Select(x => new 
                        { 
                            Field = x.Key, 
                            Errors = x.Value?.Errors.Select(e => e.ErrorMessage ?? "").Where(m => !string.IsNullOrEmpty(m)).ToList() ?? new List<string>()
                        })
                        .Where(x => x.Errors.Any())
                        .ToList();
                    
                    _logger.LogWarning("CreateSession ModelState invalid for user {UserId}. Errors: {Errors}", 
                        userId ?? "anonymous", 
                        System.Text.Json.JsonSerializer.Serialize(errors));
                    
                    // Get unique error messages
                    var uniqueErrors = errors
                        .SelectMany(e => e.Errors)
                        .Distinct()
                        .Take(5) // Limit to first 5 unique errors
                        .ToList();
                    
                    var errorMessage = uniqueErrors.Any() 
                        ? string.Join("; ", uniqueErrors)
                        : "Vui lòng điền đầy đủ thông tin bắt buộc";
                    
                    // Log detailed field errors for debugging
                    foreach (var error in errors)
                    {
                        _logger.LogWarning("Field '{Field}' has errors: {Errors}", error.Field, string.Join(", ", error.Errors));
                    }
                    
                    return Json(new { success = false, message = errorMessage, fieldErrors = errors });
                }
                
                // Validate shipping address
                if (model.ShippingAddress == null)
                {
                    _logger.LogWarning("CreateSession: ShippingAddress is null for user {UserId}", userId ?? "anonymous");
                    return Json(new { success = false, message = "Thông tin địa chỉ giao hàng không hợp lệ" });
                }
                
                // Check required fields in shipping address
                if (string.IsNullOrWhiteSpace(model.ShippingAddress.FullName) ||
                    string.IsNullOrWhiteSpace(model.ShippingAddress.PhoneNumber) ||
                    string.IsNullOrWhiteSpace(model.ShippingAddress.Address) ||
                    string.IsNullOrWhiteSpace(model.ShippingAddress.Ward) ||
                    string.IsNullOrWhiteSpace(model.ShippingAddress.District) ||
                    string.IsNullOrWhiteSpace(model.ShippingAddress.City))
                {
                    _logger.LogWarning("CreateSession: Missing required shipping address fields for user {UserId}", userId ?? "anonymous");
                    return Json(new { success = false, message = "Vui lòng điền đầy đủ thông tin địa chỉ giao hàng (Họ tên, Số điện thoại, Địa chỉ, Phường/Xã, Quận/Huyện, Tỉnh/Thành phố)" });
                }

                // Get cart items (consistent with Index action)
                List<CartItemViewModel> cartItems;
                if (string.IsNullOrEmpty(userId))
                {
                    // Anonymous user - get from session
                    cartItems = GetCartFromSession() ?? new List<CartItemViewModel>();
                    _logger.LogInformation("Anonymous user - Cart from session: {Count} items", cartItems.Count);
                }
                else
                {
                    // Authenticated user - get selected items from database
                    var selectedJson = HttpContext.Session.GetString("SelectedCartItems");
                    List<Guid> selectedIds;
                    
                    if (!string.IsNullOrEmpty(selectedJson))
                    {
                        try
                        {
                            selectedIds = System.Text.Json.JsonSerializer.Deserialize<List<Guid>>(selectedJson) ?? new List<Guid>();
                            _logger.LogInformation("Found SelectedCartItems in session: {Count} items", selectedIds.Count);
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
                            _logger.LogInformation("No SelectedCartItems in session, using all cart items as fallback: {Count}", selectedIds.Count);
                            
                            // Save to session for consistency
                            HttpContext.Session.SetString("SelectedCartItems", 
                                System.Text.Json.JsonSerializer.Serialize(selectedIds));
                        }
                    }

                    if (!selectedIds.Any())
                    {
                        _logger.LogWarning("No cart items found in database for user {UserId}", userId);
                        return Json(new { success = false, message = "Giỏ hàng trống. Vui lòng thêm sản phẩm vào giỏ hàng." });
                    }

                    var dbCartItems = await _context.ShoppingCartItems
                        .Include(c => c.Product)
                        .Where(c => c.UserId == userId && selectedIds.Contains(c.Id))
                        .ToListAsync();

                    _logger.LogInformation("Retrieved cart items from DB: {Count} items for user {UserId}", dbCartItems.Count, userId);

                    cartItems = dbCartItems.Select(c => new CartItemViewModel
                    {
                        ProductId = c.ProductId,
                        ProductName = c.Product.Name,
                        Price = c.Price,
                        Quantity = c.Quantity,
                        Size = c.Size,
                        Color = c.Color,
                        ImageUrl = c.Product.FeaturedImageUrl
                    }).ToList();
                }

                if (!cartItems.Any())
                {
                    _logger.LogWarning("CreateSession called with empty cart for user {UserId}", userId ?? "anonymous");
                    return Json(new { success = false, message = "Giỏ hàng trống. Vui lòng thêm sản phẩm vào giỏ hàng." });
                }

                // Calculate totals
                var subtotal = cartItems.Sum(item => item.Price * item.Quantity);
                var shippingFee = await CalculateShippingFeeAsync(model.ShippingMethod, subtotal);
                
                // Validate and calculate discount
                var (discountAmount, couponError) = await ValidateAndCalculateDiscountAsync(model.CouponCode, subtotal);
                if (!string.IsNullOrEmpty(couponError))
                {
                    return Json(new { success = false, message = couponError });
                }
                
                // Auto discount for orders >= 500k (5% discount)
                var autoDiscount = CalculateAutoDiscount(subtotal);
                var totalDiscount = discountAmount + autoDiscount;
                
                var tax = CalculateTax(subtotal);
                var total = subtotal + shippingFee + tax - totalDiscount;

                // Create checkout session
                var session = new CheckoutSession
                {
                    Id = Guid.NewGuid(),
                    UserId = userId ?? string.Empty,
                    Email = model.Email ?? string.Empty,
                    Status = "active",
                    TotalAmount = total,
                    ShippingFee = shippingFee,
                    Tax = tax,
                    DiscountAmount = totalDiscount,
                    CouponCode = model.CouponCode,
                    ShippingMethod = model.ShippingMethod,
                    PaymentMethod = model.PaymentMethod,
                    ShippingAddress = JsonSerializer.Serialize(model.ShippingAddress),
                    BillingAddress = JsonSerializer.Serialize(model.BillingAddress),
                    Notes = model.Notes,
                    ExpiresAt = DateTime.UtcNow.AddHours(1)
                };

                _context.CheckoutSessions.Add(session);

                // Add session items
                foreach (var item in cartItems)
                {
                    var sessionItem = new CheckoutSessionItem
                    {
                        CheckoutSessionId = session.Id,
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        UnitPrice = item.Price,
                        TotalPrice = item.Price * item.Quantity,
                        Size = item.Size,
                        Color = item.Color,
                        ProductName = item.ProductName,
                        ProductImage = item.ImageUrl
                    };

                    _context.CheckoutSessionItems.Add(sessionItem);
                }

                await _context.SaveChangesAsync();

                return Json(new 
                { 
                    success = true, 
                    sessionId = session.Id.ToString(),
                    message = "Phiên thanh toán đã được tạo thành công"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating checkout session");
                return Json(new { success = false, message = "Đã xảy ra lỗi khi tạo phiên thanh toán" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Payment(string sessionId, string? paymentMethod = null)
        {
            if (string.IsNullOrEmpty(sessionId) || !Guid.TryParse(sessionId, out var sessionGuid))
            {
                TempData["ErrorMessage"] = "Phiên thanh toán không hợp lệ.";
                return RedirectToAction("Index");
            }

            var session = await _context.CheckoutSessions
                .Include(s => s.Items)
                .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(s => s.Id == sessionGuid && s.Status == "active");

            if (session == null || session.ExpiresAt < DateTime.UtcNow)
            {
                TempData["ErrorMessage"] = "Phiên thanh toán đã hết hạn hoặc không tồn tại.";
                return RedirectToAction("Index");
            }

            var paymentMethods = await _paymentService.GetAvailablePaymentMethodsAsync();
            var shippingMethods = await GetAvailableShippingMethodsAsync();

            var model = new PaymentViewModel
            {
                SessionId = sessionId,
                Session = session,
                PaymentMethods = paymentMethods,
                ShippingMethods = shippingMethods,
                TotalAmount = session.TotalAmount,
                SelectedPaymentMethod = paymentMethod ?? session.PaymentMethod ?? ""
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessPayment(ProcessPaymentViewModel model)
        {
            try
            {
                // Log incoming request for debugging
                _logger.LogInformation("ProcessPayment called - SessionId: {SessionId}, PaymentMethod: {PaymentMethod}", 
                    model.SessionId, model.PaymentMethod);
                
                if (!ModelState.IsValid)
                {
                    var errors = ModelState
                        .Where(x => x.Value?.Errors.Count > 0)
                        .Select(x => new 
                        { 
                            Field = x.Key, 
                            Errors = x.Value?.Errors.Select(e => e.ErrorMessage ?? "").ToList() ?? new List<string>()
                        })
                        .ToList();
                    
                    _logger.LogWarning("ProcessPayment ModelState invalid. Errors: {Errors}", 
                        System.Text.Json.JsonSerializer.Serialize(errors));
                    
                    return Json(new { 
                        success = false, 
                        message = "Dữ liệu thanh toán không hợp lệ",
                        errors = errors
                    });
                }
                
                // Validate required fields manually
                if (string.IsNullOrWhiteSpace(model.SessionId))
                {
                    _logger.LogWarning("ProcessPayment: SessionId is empty");
                    return Json(new { success = false, message = "Session ID không hợp lệ" });
                }
                
                if (string.IsNullOrWhiteSpace(model.PaymentMethod))
                {
                    _logger.LogWarning("ProcessPayment: PaymentMethod is empty");
                    return Json(new { success = false, message = "Vui lòng chọn phương thức thanh toán" });
                }

                // Get checkout session
                var session = await _context.CheckoutSessions
                    .Include(s => s.Items)
                    .FirstOrDefaultAsync(s => s.Id == Guid.Parse(model.SessionId) && s.Status == "active");

                if (session == null || session.ExpiresAt < DateTime.UtcNow)
                {
                    return Json(new { success = false, message = "Phiên thanh toán đã hết hạn" });
                }

                // Create order first
                var order = await CreateOrderFromSessionAsync(session);

                // Update session
                session.PaymentMethod = model.PaymentMethod;
                session.Status = "processing";
                await _context.SaveChangesAsync();

                // Process payment
                var paymentRequest = new PaymentRequest
                {
                    OrderId = order.Id.ToString(),
                    UserId = session.UserId ?? "",
                    Amount = session.TotalAmount,
                    Currency = "VND",
                    PaymentMethod = model.PaymentMethod,
                    OrderInfo = $"Thanh toán đơn hàng #{order.OrderNumber}",
                    ReturnUrl = Url.Action("PaymentReturn", "Checkout", null, Request.Scheme) ?? string.Empty,
                    NotifyUrl = Url.Action("PaymentNotify", "Checkout", null, Request.Scheme) ?? string.Empty,
                    IpAddress = Request.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "",
                    UserAgent = Request.Headers["User-Agent"].ToString(),
                    PaymentMethodId = model.PaymentMethodId
                };

                var paymentResult = await _paymentService.ProcessPaymentAsync(paymentRequest);

                if (paymentResult.IsSuccess)
                {
                    // Update order payment status
                    order.PaymentStatus = "pending";
                    order.Status = "pending";
                    await _context.SaveChangesAsync();

                    if (!string.IsNullOrEmpty(paymentResult.PaymentUrl))
                    {
                        // Redirect to payment gateway
                        return Json(new 
                        { 
                            success = true, 
                            redirectUrl = paymentResult.PaymentUrl,
                            message = paymentResult.Message
                        });
                    }
                    else
                    {
                        // Direct payment (like COD)
                        await CompleteOrderAsync(order, paymentResult.TransactionId);
                        return Json(new 
                        { 
                            success = true, 
                            redirectUrl = Url.Action("Success", "Checkout", new { orderId = order.Id }),
                            message = paymentResult.Message
                        });
                    }
                }
                else
                {
                    // Payment failed
                    order.PaymentStatus = "failed";
                    order.Status = "cancelled";
                    await _context.SaveChangesAsync();

                    return Json(new { success = false, message = paymentResult.ErrorMessage });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing payment for session {SessionId}", model.SessionId);
                return Json(new { success = false, message = "Đã xảy ra lỗi khi xử lý thanh toán" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> PaymentReturn(string vnp_ResponseCode, string vnp_TxnRef, string vnp_TransactionNo)
        {
            try
            {
                // Handle VNPay return
                if (!string.IsNullOrEmpty(vnp_TxnRef))
                {
                    var order = await _context.Orders
                        .FirstOrDefaultAsync(o => o.Id.ToString() == vnp_TxnRef);

                    if (order != null)
                    {
                        if (vnp_ResponseCode == "00") // Success
                        {
                            await CompleteOrderAsync(order, vnp_TransactionNo);
                            return RedirectToAction("Success", "Checkout", new { orderId = order.Id });
                        }
                        else
                        {
                            // Payment failed
                            order.PaymentStatus = "failed";
                            order.Status = "cancelled";
                            await _context.SaveChangesAsync();

                            TempData["ErrorMessage"] = "Thanh toán không thành công. Vui lòng thử lại.";
                            return RedirectToAction("Failed", "Checkout", new { orderId = order.Id });
                        }
                    }
                }

                TempData["ErrorMessage"] = "Không tìm thấy thông tin đơn hàng.";
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling payment return");
                TempData["ErrorMessage"] = "Đã xảy ra lỗi khi xử lý kết quả thanh toán.";
                return RedirectToAction("Index", "Home");
            }
        }

        [HttpPost]
        public async Task<IActionResult> PaymentNotify()
        {
            try
            {
                // Handle payment gateway notifications (webhooks)
                // This is called by payment gateways to notify payment status
                
                using var reader = new StreamReader(Request.Body);
                var body = await reader.ReadToEndAsync();
                
                // Process webhook based on payment method
                // Implementation depends on specific payment gateway
                
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling payment notification");
                return BadRequest();
            }
        }

        [HttpGet]
        public async Task<IActionResult> Success(Guid orderId)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy đơn hàng.";
                return RedirectToAction("Index", "Home");
            }

            // Clear cart for authenticated users
            var userId = _userManager.GetUserId(User);
            if (!string.IsNullOrEmpty(userId) && userId == order.UserId)
            {
                var cartItems = await _context.ShoppingCartItems
                    .Where(c => c.UserId == userId)
                    .ToListAsync();

                _context.ShoppingCartItems.RemoveRange(cartItems);
                await _context.SaveChangesAsync();
            }
            else
            {
                // Clear session cart
                HttpContext.Session.Remove("Cart");
            }

            return View(order);
        }

        [HttpGet]
        public async Task<IActionResult> Failed(Guid orderId)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            return View(order);
        }

        // Helper methods
        private async Task<CheckoutViewModel> CreateCheckoutViewModelAsync(string? userId, List<CartItemViewModel> cartItems)
        {
            var model = new CheckoutViewModel
            {
                CartItems = cartItems,
                Subtotal = cartItems.Sum(item => item.Price * item.Quantity)
            };

            if (!string.IsNullOrEmpty(userId))
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user != null)
                {
                    model.Email = user.Email ?? "";
                    model.FirstName = user.FirstName ?? string.Empty;
                    model.LastName = user.LastName ?? string.Empty;
                    model.PhoneNumber = user.PhoneNumber ?? "";

                    // Get user addresses
                    model.SavedAddresses = await _context.Addresses
                        .Where(a => a.UserId == userId)
                        .ToListAsync();
                }
            }

            // Get available shipping methods
            model.ShippingMethods = await GetAvailableShippingMethodsAsync();

            return model;
        }

        private List<CartItemViewModel>? GetCartFromSession()
        {
            var cartJson = HttpContext.Session.GetString("Cart");
            if (string.IsNullOrEmpty(cartJson))
                return null;

            return JsonSerializer.Deserialize<List<CartItemViewModel>>(cartJson);
        }

        private async Task<decimal> CalculateShippingFeeAsync(string? shippingMethod, decimal subtotal)
        {
            if (string.IsNullOrEmpty(shippingMethod))
                return 0;

            var method = await _context.ShippingMethods
                .FirstOrDefaultAsync(sm => sm.Code == shippingMethod && sm.IsActive);

            if (method == null)
                return 0;

            // Giảm giá theo tầng cho Giao hàng hỏa tốc (SUPER_EXPRESS)
            if (shippingMethod == "SUPER_EXPRESS")
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

        private async Task<(decimal discount, string? error)> ValidateAndCalculateDiscountAsync(string? couponCode, decimal subtotal)
        {
            if (string.IsNullOrEmpty(couponCode))
                return (0, null);

            var coupon = await _context.Coupons
                .FirstOrDefaultAsync(c => c.Code.ToUpper() == couponCode.ToUpper());

            if (coupon == null)
                return (0, "Mã giảm giá không tồn tại");

            if (!coupon.IsActive)
                return (0, "Mã giảm giá đã bị vô hiệu hóa");

            if (coupon.StartDate.HasValue && coupon.StartDate > DateTime.UtcNow)
                return (0, $"Mã giảm giá chưa có hiệu lực. Có hiệu lực từ {coupon.StartDate.Value:dd/MM/yyyy}");

            if (coupon.EndDate.HasValue && coupon.EndDate < DateTime.UtcNow)
                return (0, $"Mã giảm giá đã hết hạn vào ngày {coupon.EndDate.Value:dd/MM/yyyy}");

            if (coupon.UsageLimit.HasValue && coupon.UsageCount >= coupon.UsageLimit.Value)
                return (0, "Mã giảm giá đã được sử dụng hết");

            if (coupon.MinOrderAmount.HasValue && subtotal < coupon.MinOrderAmount.Value)
                return (0, $"Đơn hàng tối thiểu {coupon.MinOrderAmount.Value:N0}₫ để sử dụng mã này");

            // Calculate discount
            var discount = coupon.Type switch
            {
                "percentage" => subtotal * (coupon.Value / 100),
                "fixed_amount" => coupon.Value,
                _ => 0
            };

            return (discount, null);
        }

        private async Task<decimal> CalculateDiscountAsync(string? couponCode, decimal subtotal)
        {
            if (string.IsNullOrEmpty(couponCode))
                return 0;

            // Sử dụng bảng Coupons thay vì Promotions để tích hợp với trang Admin/Coupons
            var coupon = await _context.Coupons
                .FirstOrDefaultAsync(c => c.Code.ToUpper() == couponCode.ToUpper() && 
                                        c.IsActive && 
                                        (c.StartDate == null || c.StartDate <= DateTime.UtcNow) &&
                                        (c.EndDate == null || c.EndDate >= DateTime.UtcNow) &&
                                        (c.UsageLimit == null || c.UsageCount < c.UsageLimit) &&
                                        (c.MinOrderAmount == null || subtotal >= c.MinOrderAmount));

            if (coupon == null)
                return 0;

            var discount = coupon.Type switch
            {
                "percentage" => subtotal * (coupon.Value / 100),
                "fixed_amount" => coupon.Value,
                _ => 0
            };

            // Note: Coupon model có MaxDiscountAmount nhưng là NotMapped, nên không áp dụng limit
            // Nếu cần giới hạn giảm giá tối đa, có thể thêm vào database schema sau

            return discount;
        }

        private decimal CalculateTax(decimal subtotal)
        {
            // VAT 10% for Vietnam
            return subtotal * 0.1m;
        }

        private decimal CalculateAutoDiscount(decimal subtotal)
        {
            // Auto discount 5% for orders >= 500,000đ
            if (subtotal >= 500000)
            {
                var discount = subtotal * 0.05m; // 5% discount
                // Cap maximum discount at 200,000đ
                return Math.Min(discount, 200000);
            }
            return 0;
        }

        private async Task<List<ShippingMethod>> GetAvailableShippingMethodsAsync()
        {
            return await _context.ShippingMethods
                .Where(sm => sm.IsActive)
                .OrderBy(sm => sm.SortOrder)
                .ToListAsync();
        }

        private async Task<Order> CreateOrderFromSessionAsync(CheckoutSession session)
        {
            // IMPORTANT: Validate stock availability BEFORE creating order
            // This prevents overselling when multiple users checkout simultaneously
            // MUST reload from database with AsNoTracking to bypass EF cache and get fresh data
            foreach (var item in session.Items)
            {
                var freshProduct = await _context.Products
                    .AsNoTracking()
                    .Where(p => p.Id == item.ProductId)
                    .Select(p => new { p.StockQuantity, p.Name })
                    .FirstOrDefaultAsync();
                
                if (freshProduct == null)
                {
                    throw new InvalidOperationException($"Sản phẩm {item.ProductName} không tồn tại");
                }
                
                if (freshProduct.StockQuantity < item.Quantity)
                {
                    // Only show detailed message if there IS some stock but not enough
                    var message = freshProduct.StockQuantity == 0
                        ? $"Sản phẩm {freshProduct.Name} hiện đã hết hàng. Vui lòng cập nhật giỏ hàng."
                        : $"Sản phẩm {freshProduct.Name} chỉ còn {freshProduct.StockQuantity} sản phẩm, " +
                          $"không đủ cho số lượng bạn đặt ({item.Quantity}). Vui lòng cập nhật giỏ hàng.";
                    throw new InvalidOperationException(message);
                }
                // If stock is sufficient, continue without any message (no need to notify user)
            }
            
            var order = new Order
            {
                Id = Guid.NewGuid(),
                OrderNumber = GenerateOrderNumber(),
                UserId = session.UserId ?? "",
                Status = "pending",
                TotalAmount = session.TotalAmount,
                ShippingFee = session.ShippingFee,
                Tax = session.Tax,
                DiscountAmount = session.DiscountAmount,
                CouponCode = session.CouponCode,
                PaymentMethod = session.PaymentMethod ?? "",
                PaymentStatus = "pending",
                ShippingAddress = session.ShippingAddress ?? "",
                BillingAddress = session.BillingAddress ?? "",
                Notes = session.Notes,
                CreatedAt = DateTime.UtcNow
            };

            _context.Orders.Add(order);

            // Add order items
            foreach (var item in session.Items)
            {
                var orderItem = new OrderItem
                {
                    Id = Guid.NewGuid(),
                    OrderId = order.Id,
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    TotalPrice = item.TotalPrice,
                    ProductName = item.ProductName,
                    ProductImage = item.ProductImage
                };

                _context.OrderItems.Add(orderItem);
            }

            await _context.SaveChangesAsync();
            return order;
        }

        private async Task CompleteOrderAsync(Order order, string? transactionId)
        {
            order.PaymentStatus = "paid";
            order.Status = "processing";
            
            // Add status history
            var statusHistory = new OrderStatusHistory
            {
                OrderId = order.Id,
                Status = "processing",
                Notes = "Đơn hàng đã được thanh toán thành công",
                CreatedAt = DateTime.UtcNow
            };

            _context.OrderStatusHistories.Add(statusHistory);

            // Tăng UsageCount của coupon nếu có sử dụng
            if (!string.IsNullOrEmpty(order.CouponCode))
            {
                var coupon = await _context.Coupons
                    .FirstOrDefaultAsync(c => c.Code.ToUpper() == order.CouponCode.ToUpper());
                
                if (coupon != null)
                {
                    coupon.UsageCount++;
                    coupon.UpdatedAt = DateTime.UtcNow;
                    
                    // Tạo bản ghi lịch sử sử dụng coupon
                    if (!string.IsNullOrEmpty(order.UserId))
                    {
                        var couponUsage = new CouponUsage
                        {
                            Id = Guid.NewGuid(),
                            CouponId = coupon.Id,
                            UserId = order.UserId,
                            OrderId = order.Id,
                            DiscountAmount = order.DiscountAmount,
                            UsedAt = DateTime.UtcNow
                        };
                        _context.CouponUsages.Add(couponUsage);
                    }
                    
                    _logger.LogInformation($"Coupon {order.CouponCode} used. Count: {coupon.UsageCount}");
                }
            }

            await _context.SaveChangesAsync();

            // Reload order with User for email template
            order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.Id == order.Id) ?? order;

            // Send confirmation email
            if (!string.IsNullOrEmpty(order.UserId))
            {
                var user = await _userManager.FindByIdAsync(order.UserId);
                if (user?.Email != null)
                {
                    _logger.LogInformation("Attempting to send order confirmation email to {Email} for order #{OrderNumber}", 
                        user.Email, order.OrderNumber);
                    
                    var emailSent = await _emailService.SendOrderConfirmationEmailAsync(user.Email, order);
                    
                    if (emailSent)
                    {
                        _logger.LogInformation("Order confirmation email sent successfully to {Email}", user.Email);
                    }
                    else
                    {
                        _logger.LogWarning("Failed to send order confirmation email to {Email} for order #{OrderNumber}", 
                            user.Email, order.OrderNumber);
                    }
                }
                else
                {
                    _logger.LogWarning("Cannot send order confirmation email - User or email is null for order #{OrderNumber}", 
                        order.OrderNumber);
                }

                // Send notification to customer
                await _notificationService.SendNotificationAsync(order.UserId,
                    "Đơn hàng đã được xác nhận",
                    $"Đơn hàng #{order.OrderNumber} đã được thanh toán và xác nhận thành công",
                    "order_confirmed");

                // Send notification to all admin/seller users
                var adminUsers = await _userManager.GetUsersInRoleAsync("Admin");
                var sellerUsers = await _userManager.GetUsersInRoleAsync("Seller");
                var notifyUsers = adminUsers.Union(sellerUsers).Distinct();
                
                foreach (var adminUser in notifyUsers)
                {
                    var customerName = user != null ? $"{user.FirstName} {user.LastName}" : "Khách hàng";
                    await _notificationService.SendNotificationAsync(adminUser.Id,
                        "Đơn hàng mới",
                        $"Có đơn hàng mới #{order.OrderNumber} từ khách hàng {customerName}. Tổng giá trị: {order.TotalAmount:N0}₫",
                        "new_order",
                        $"/seller/orders?orderNumber={order.OrderNumber}");
                }
            }

            // Update inventory
            await UpdateInventoryAsync(order);
        }

        private async Task UpdateInventoryAsync(Order order)
        {
            var orderItems = await _context.OrderItems
                .Where(oi => oi.OrderId == order.Id)
                .ToListAsync();

            foreach (var item in orderItems)
            {
                var product = await _context.Products.FindAsync(item.ProductId);
                if (product != null)
                {
                    product.StockQuantity -= item.Quantity;
                    if (product.StockQuantity <= 0)
                    {
                        product.InStock = false;
                        product.Status = "out_of_stock";
                    }
                }
            }

            await _context.SaveChangesAsync();
        }

        private string GenerateOrderNumber()
        {
            var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            var random = new Random().Next(1000, 9999);
            return $"JH{timestamp}{random}";
        }

        // ===================================
        // QR Code Generation API Endpoints
        // ===================================

        [HttpPost]
        [Route("Checkout/GeneratePaymentQR")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> GeneratePaymentQR([FromBody] GenerateQRRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.SessionId) || string.IsNullOrEmpty(request.PaymentMethod))
                {
                    return Json(new { success = false, message = "Thiếu thông tin bắt buộc" });
                }

                // Get checkout session
                var session = await _context.CheckoutSessions
                    .Include(s => s.Items)
                    .FirstOrDefaultAsync(s => s.Id == Guid.Parse(request.SessionId) && s.Status == "active");

                if (session == null || session.ExpiresAt < DateTime.UtcNow)
                {
                    return Json(new { success = false, message = "Phiên thanh toán đã hết hạn" });
                }

                // Get client IP
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
                
                // Generate URLs - IMPORTANT: Must match the actual controller endpoints
                var baseUrl = $"{Request.Scheme}://{Request.Host}";
                var returnUrl = $"{baseUrl}/Checkout/PaymentReturn";
                var notifyUrl = $"{baseUrl}/Checkout/PaymentNotify";

                QRCodeResult result;

                switch (request.PaymentMethod.ToLower())
                {
                    case "vnpay":
                        var vnpayRequest = new VNPayQRRequest
                        {
                            SessionId = request.SessionId,
                            Amount = session.TotalAmount,
                            OrderId = session.Id.ToString(),
                            OrderInfo = $"Thanh toan don hang {session.Id}",
                            IpAddress = ipAddress,
                            ReturnUrl = returnUrl,
                            NotifyUrl = notifyUrl
                        };
                        result = await _paymentService.GenerateVNPayQRCodeAsync(vnpayRequest);
                        break;

                    case "momo":
                        var momoRequest = new MoMoQRRequest
                        {
                            SessionId = request.SessionId,
                            Amount = session.TotalAmount,
                            OrderId = session.Id.ToString(),
                            OrderInfo = $"Thanh toan don hang {session.Id}",
                            ReturnUrl = returnUrl,
                            NotifyUrl = notifyUrl
                        };
                        result = await _paymentService.GenerateMoMoQRCodeAsync(momoRequest);
                        break;

                    default:
                        return Json(new { success = false, message = "Phương thức thanh toán không hỗ trợ QR code" });
                }

                if (result.IsSuccess)
                {
                    _logger.LogInformation("QR code generated successfully for session {SessionId} using {PaymentMethod}", 
                        request.SessionId, request.PaymentMethod);

                    return Json(new
                    {
                        success = true,
                        qrCodeUrl = result.QRCodeUrl,
                        qrDataUrl = result.QRDataUrl,
                        deepLink = result.DeepLink,
                        paymentUrl = result.PaymentUrl,
                        transactionId = result.TransactionId,
                        expiresAt = result.ExpiresAt,
                        expiresInSeconds = result.ExpiresInSeconds
                    });
                }
                else
                {
                    _logger.LogError("Failed to generate QR code: {Error}", result.ErrorMessage);
                    return Json(new { success = false, message = result.ErrorMessage });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating payment QR code");
                return Json(new { success = false, message = "Có lỗi xảy ra khi tạo mã QR" });
            }
        }

        [HttpGet]
        [Route("Checkout/CheckPaymentStatus")]
        public async Task<IActionResult> CheckPaymentStatus(string sessionId)
        {
            try
            {
                if (string.IsNullOrEmpty(sessionId))
                {
                    return Json(new { status = "error", message = "Session ID không hợp lệ" });
                }

                // Get checkout session
                var session = await _context.CheckoutSessions
                    .FirstOrDefaultAsync(s => s.Id == Guid.Parse(sessionId));

                if (session == null)
                {
                    return Json(new { status = "error", message = "Không tìm thấy phiên thanh toán" });
                }

                // Check if order was created and paid
                var order = await _context.Orders
                    .FirstOrDefaultAsync(o => o.Id.ToString() == session.Id.ToString() || 
                                              o.OrderNumber.Contains(session.Id.ToString()));

                if (order != null && order.PaymentStatus == "paid")
                {
                    return Json(new
                    {
                        status = "paid",
                        message = "Thanh toán thành công",
                        redirectUrl = Url.Action("OrderConfirmation", "Orders", new { orderId = order.Id })
                    });
                }

                // Check if session expired
                if (session.ExpiresAt < DateTime.UtcNow)
                {
                    return Json(new { status = "expired", message = "Phiên thanh toán đã hết hạn" });
                }

                // Still pending
                return Json(new { status = "pending", message = "Đang chờ thanh toán" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking payment status for session {SessionId}", sessionId);
                return Json(new { status = "error", message = "Có lỗi xảy ra" });
            }
        }
    }

    // Request model for QR generation
    public class GenerateQRRequest
    {
        public string SessionId { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;
    }
}
