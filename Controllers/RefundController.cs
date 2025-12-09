using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JohnHenryFashionWeb.Data;
using JohnHenryFashionWeb.Models;
using JohnHenryFashionWeb.Services;

namespace JohnHenryFashionWeb.Controllers
{
    [Authorize]
    public class RefundController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailService _emailService;
        private readonly INotificationService _notificationService;
        private readonly ILogger<RefundController> _logger;

        public RefundController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IEmailService emailService,
            INotificationService notificationService,
            ILogger<RefundController> logger)
        {
            _context = context;
            _userManager = userManager;
            _emailService = emailService;
            _notificationService = notificationService;
            _logger = logger;
        }

        // GET: /Refund/MyRequests
        [HttpGet]
        public async Task<IActionResult> MyRequests()
        {
            var userId = _userManager.GetUserId(User);
            
            var refundRequests = await _context.RefundRequests
                .Include(r => r.Order)
                    .ThenInclude(o => o.OrderItems)
                        .ThenInclude(oi => oi.Product)
                .Where(r => r.RequestedBy == userId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return View(refundRequests);
        }

        // POST: /Refund/RequestRefund
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestRefund(Guid orderId, string reason)
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                if (userId == null)
                {
                    return Json(new { success = false, message = "Không xác định được người dùng" });
                }
                
                // Validate order exists and belongs to user
                var order = await _context.Orders
                    .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.Product)
                    .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);

                if (order == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy đơn hàng" });
                }

                // Check if order is delivered
                if (order.Status != "delivered")
                {
                    return Json(new { success = false, message = "Chỉ có thể yêu cầu hoàn trả đơn hàng đã giao" });
                }

                // Check if refund window is still valid (7 days)
                if (order.DeliveredAt.HasValue && (DateTime.UtcNow - order.DeliveredAt.Value).TotalDays > 7)
                {
                    return Json(new { success = false, message = "Đã quá thời hạn yêu cầu hoàn trả (7 ngày)" });
                }

                // Check if refund request already exists
                var existingRefund = await _context.RefundRequests
                    .AnyAsync(r => r.OrderId == orderId);

                if (existingRefund)
                {
                    return Json(new { success = false, message = "Đơn hàng này đã có yêu cầu hoàn trả" });
                }

                // Create refund request
                var refundRequest = new RefundRequest
                {
                    Id = Guid.NewGuid(),
                    OrderId = orderId,
                    Order = order,
                    Amount = order.TotalAmount,
                    Reason = reason,
                    Status = "pending",
                    RequestedBy = userId,
                    CreatedAt = DateTime.UtcNow
                };

                _context.RefundRequests.Add(refundRequest);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Refund request created: {RefundId} for Order {OrderId} by User {UserId}", 
                    refundRequest.Id, orderId, userId);

                // Send notification to admin
                var adminUsers = await _userManager.GetUsersInRoleAsync("Admin");
                foreach (var admin in adminUsers)
                {
                    await _notificationService.CreateNotificationAsync(
                        admin.Id,
                        "Yêu cầu hoàn trả mới",
                        $"Đơn hàng #{order.OrderNumber} có yêu cầu hoàn trả",
                        $"/Admin/RefundDetails/{refundRequest.Id}"
                    );
                }

                // Send email to customer
                try
                {
                    var user = await _userManager.FindByIdAsync(userId);
                    if (user != null && !string.IsNullOrEmpty(user.Email))
                    {
                        await _emailService.SendRefundRequestedEmailAsync(
                            user.Email,
                            user.FullName ?? user.UserName ?? "Khách hàng",
                            order.OrderNumber,
                            refundRequest.Amount
                        );
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send refund request email");
                }

                return Json(new { 
                    success = true, 
                    message = "Yêu cầu hoàn trả đã được gửi. Chúng tôi sẽ xử lý trong vòng 24-48 giờ.",
                    refundId = refundRequest.Id
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating refund request for order {OrderId}", orderId);
                return Json(new { success = false, message = "Có lỗi xảy ra. Vui lòng thử lại sau." });
            }
        }

        // GET: /Refund/Details/{id}
        [HttpGet]
        public async Task<IActionResult> Details(Guid id)
        {
            var userId = _userManager.GetUserId(User);
            var isAdmin = User.IsInRole("Admin");

            var refundRequest = await _context.RefundRequests
                .Include(r => r.Order)
                    .ThenInclude(o => o.OrderItems)
                        .ThenInclude(oi => oi.Product)
                .Include(r => r.Order.User)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (refundRequest == null)
            {
                return NotFound();
            }

            // Check authorization
            if (!isAdmin && refundRequest.RequestedBy != userId)
            {
                return Forbid();
            }

            return View(refundRequest);
        }

        // ========== ADMIN ACTIONS ==========

        // GET: /Refund/AdminList
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> AdminList(string? status = null)
        {
            var query = _context.RefundRequests
                .Include(r => r.Order)
                    .ThenInclude(o => o.User)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(r => r.Status == status);
            }

            var refundRequests = await query
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            ViewBag.StatusFilter = status;
            return View(refundRequests);
        }

        // POST: /Refund/Approve
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(Guid refundId, string? adminNotes)
        {
            try
            {
                var adminId = _userManager.GetUserId(User);
                
                var refundRequest = await _context.RefundRequests
                    .Include(r => r.Order)
                        .ThenInclude(o => o.OrderItems)
                            .ThenInclude(oi => oi.Product)
                    .FirstOrDefaultAsync(r => r.Id == refundId);

                if (refundRequest == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy yêu cầu hoàn trả" });
                }

                if (refundRequest.Status != "pending")
                {
                    return Json(new { success = false, message = "Yêu cầu này đã được xử lý" });
                }

                // Update refund status
                refundRequest.Status = "approved";
                refundRequest.ProcessedAt = DateTime.UtcNow;
                refundRequest.ProcessedBy = adminId;
                refundRequest.AdminNotes = adminNotes;

                // Restore stock for all items in the order
                foreach (var item in refundRequest.Order.OrderItems)
                {
                    if (item.Product != null)
                    {
                        var oldStock = item.Product.StockQuantity;
                        item.Product.StockQuantity += item.Quantity;
                        
                        // Update InStock status
                        if (item.Product.StockQuantity > 0)
                        {
                            item.Product.InStock = true;
                            if (item.Product.Status == "out_of_stock")
                            {
                                item.Product.Status = "active";
                            }
                        }

                        _logger.LogInformation("Stock restored for refund: Product {SKU}, Old: {Old}, New: {New}, Quantity: {Qty}, RefundId: {RefundId}",
                            item.Product.SKU, oldStock, item.Product.StockQuantity, item.Quantity, refundId);
                    }
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Refund approved: {RefundId} by Admin {AdminId}", refundId, adminId);

                // Send notification to customer
                await _notificationService.CreateNotificationAsync(
                    refundRequest.RequestedBy,
                    "Yêu cầu hoàn trả được chấp nhận",
                    $"Yêu cầu hoàn trả cho đơn hàng #{refundRequest.Order.OrderNumber} đã được chấp nhận",
                    $"/Refund/Details/{refundId}"
                );

                // Send email to customer
                try
                {
                    var customer = await _userManager.FindByIdAsync(refundRequest.RequestedBy);
                    if (customer != null && !string.IsNullOrEmpty(customer.Email))
                    {
                        await _emailService.SendRefundApprovedEmailAsync(
                            customer.Email,
                            customer.FullName ?? customer.UserName ?? "Khách hàng",
                            refundRequest.Order.OrderNumber,
                            refundRequest.Amount
                        );
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send refund approved email");
                }

                return Json(new { 
                    success = true, 
                    message = "Yêu cầu hoàn trả đã được chấp nhận và stock đã được khôi phục" 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving refund {RefundId}", refundId);
                return Json(new { success = false, message = "Có lỗi xảy ra. Vui lòng thử lại." });
            }
        }

        // POST: /Refund/Reject
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(Guid refundId, string rejectionReason, string? adminNotes)
        {
            try
            {
                var adminId = _userManager.GetUserId(User);
                
                var refundRequest = await _context.RefundRequests
                    .Include(r => r.Order)
                    .FirstOrDefaultAsync(r => r.Id == refundId);

                if (refundRequest == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy yêu cầu hoàn trả" });
                }

                if (refundRequest.Status != "pending")
                {
                    return Json(new { success = false, message = "Yêu cầu này đã được xử lý" });
                }

                if (string.IsNullOrWhiteSpace(rejectionReason))
                {
                    return Json(new { success = false, message = "Vui lòng nhập lý do từ chối" });
                }

                // Update refund status
                refundRequest.Status = "rejected";
                refundRequest.ProcessedAt = DateTime.UtcNow;
                refundRequest.ProcessedBy = adminId;
                refundRequest.RejectionReason = rejectionReason;
                refundRequest.AdminNotes = adminNotes;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Refund rejected: {RefundId} by Admin {AdminId}, Reason: {Reason}", 
                    refundId, adminId, rejectionReason);

                // Send notification to customer
                await _notificationService.CreateNotificationAsync(
                    refundRequest.RequestedBy,
                    "Yêu cầu hoàn trả bị từ chối",
                    $"Yêu cầu hoàn trả cho đơn hàng #{refundRequest.Order.OrderNumber} bị từ chối",
                    $"/Refund/Details/{refundId}"
                );

                // Send email to customer
                try
                {
                    var customer = await _userManager.FindByIdAsync(refundRequest.RequestedBy);
                    if (customer != null && !string.IsNullOrEmpty(customer.Email))
                    {
                        await _emailService.SendRefundRejectedEmailAsync(
                            customer.Email,
                            customer.FullName ?? customer.UserName ?? "Khách hàng",
                            refundRequest.Order.OrderNumber,
                            rejectionReason
                        );
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send refund rejected email");
                }

                return Json(new { 
                    success = true, 
                    message = "Yêu cầu hoàn trả đã bị từ chối" 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting refund {RefundId}", refundId);
                return Json(new { success = false, message = "Có lỗi xảy ra. Vui lòng thử lại." });
            }
        }

        // GET: /Refund/GetStats (API for dashboard)
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> GetStats()
        {
            var stats = new
            {
                pending = await _context.RefundRequests.CountAsync(r => r.Status == "pending"),
                approved = await _context.RefundRequests.CountAsync(r => r.Status == "approved"),
                rejected = await _context.RefundRequests.CountAsync(r => r.Status == "rejected"),
                totalAmount = await _context.RefundRequests
                    .Where(r => r.Status == "approved")
                    .SumAsync(r => r.Amount)
            };

            return Json(stats);
        }
    }
}
