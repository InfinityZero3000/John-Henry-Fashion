using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JohnHenryFashionWeb.Data;
using JohnHenryFashionWeb.Models;

namespace JohnHenryFashionWeb.Controllers
{
    public partial class AdminController
    {
        #region Orders Management
        // GET: Admin/Orders
        [HttpGet]
        [Route("orders")]
        public async Task<IActionResult> Orders(int page = 1, int pageSize = 20, string? search = null, string? status = null)
        {
            var query = _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .AsQueryable();

            // Filter by search term
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(o => o.OrderNumber.Contains(search) ||
                                        (o.User.FirstName != null && o.User.FirstName.Contains(search)) ||
                                        (o.User.LastName != null && o.User.LastName.Contains(search)) ||
                                        (o.User.Email != null && o.User.Email.Contains(search)));
            }

            // Filter by status
            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(o => o.Status == status);
            }

            var totalOrders = await query.CountAsync();
            var orders = await query
                .OrderByDescending(o => o.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.TotalOrders = totalOrders;
            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalOrders / pageSize);
            ViewBag.Search = search;
            ViewBag.Status = status;

            return View(orders);
        }

        // GET: Admin/OrderDetails/{id}
        [Route("order-details/{id}")]
        public async Task<IActionResult> OrderDetails(Guid id)
        {
            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        // POST: Admin/UpdateOrderStatus
        [HttpPost]
        [Route("update-order-status")]
        public async Task<IActionResult> UpdateOrderStatus(Guid orderId, string status)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null)
            {
                return Json(new { success = false, message = "Không tìm thấy đơn hàng" });
            }

            if (!IsValidStatusTransition(order.Status, status))
            {
                return Json(new { success = false, message = $"Không thể chuyển trạng thái từ '{order.Status}' sang '{status}'" });
            }

            order.Status = status;
            order.UpdatedAt = DateTime.UtcNow;

            if (status == "shipped")
            {
                order.ShippedAt = DateTime.UtcNow;
            }
            else if (status == "delivered")
            {
                order.DeliveredAt = DateTime.UtcNow;
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                return Json(new { success = false, message = "Đơn hàng đã được cập nhật bởi người khác. Vui lòng tải lại trang." });
            }

            return Json(new { success = true, message = "Cập nhật trạng thái đơn hàng thành công" });
        }

        // GET: Admin/Orders/Details/{id} - API endpoint for AJAX
        [HttpGet]
        [Route("orders/details/{id}")]
        public async Task<IActionResult> GetOrderDetailsJson(Guid id)
        {
            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                return Json(new { success = false, message = "Không tìm thấy đơn hàng" });
            }

            return Json(new
            {
                success = true,
                order = new
                {
                    id = order.Id,
                    orderNumber = order.OrderNumber,
                    createdAt = order.CreatedAt,
                    status = order.Status,
                    paymentStatus = order.PaymentStatus,
                    paymentMethod = order.PaymentMethod,
                    totalAmount = order.TotalAmount,
                    discountAmount = order.DiscountAmount,
                    shippingFee = order.ShippingFee,
                    tax = order.Tax,
                    shippingAddress = order.ShippingAddress,
                    billingAddress = order.BillingAddress,
                    notes = order.Notes,
                    user = order.User != null ? new
                    {
                        fullName = $"{order.User.FirstName} {order.User.LastName}".Trim(),
                        email = order.User.Email,
                        phone = order.User.Phone
                    } : null,
                    orderItems = order.OrderItems.Select(oi => new
                    {
                        productName = oi.ProductName,
                        productSku = oi.ProductSKU,
                        productImage = oi.ProductImage,
                        quantity = oi.Quantity,
                        unitPrice = oi.UnitPrice,
                        totalPrice = oi.TotalPrice
                    }).ToList()
                }
            });
        }

        // POST: Admin/Orders/UpdateStatus - Enhanced with note
        [HttpPost]
        [Route("orders/update-status")]
        public async Task<IActionResult> UpdateOrderStatusEnhanced([FromBody] UpdateOrderStatusRequest request)
        {
            try
            {
                var order = await _context.Orders
                    .Include(o => o.User)
                    .FirstOrDefaultAsync(o => o.Id == request.OrderId);

                if (order == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy đơn hàng" });
                }

                if (!IsValidStatusTransition(order.Status, request.Status))
                {
                    return Json(new { success = false, message = $"Không thể chuyển trạng thái từ '{order.Status}' sang '{request.Status}'" });
                }

                var oldStatus = order.Status;
                order.Status = request.Status;
                order.UpdatedAt = DateTime.UtcNow;

                // Update specific timestamps based on status
                switch (request.Status?.ToLower())
                {
                    case "shipped":
                        order.ShippedAt = DateTime.UtcNow;
                        break;
                    case "delivered":
                        order.DeliveredAt = DateTime.UtcNow;
                        break;
                }

                // Add note if provided
                if (!string.IsNullOrEmpty(request.Note))
                {
                    order.Notes = string.IsNullOrEmpty(order.Notes) 
                        ? $"[{DateTime.UtcNow:dd/MM/yyyy HH:mm}] {request.Note}"
                        : $"{order.Notes}\n[{DateTime.UtcNow:dd/MM/yyyy HH:mm}] {request.Note}";
                }

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    return Json(new { success = false, message = "Đơn hàng đã được cập nhật bởi người khác. Vui lòng tải lại trang." });
                }

                _logger.LogInformation("Order {OrderNumber} status changed from {OldStatus} to {NewStatus}", order.OrderNumber, oldStatus, request.Status);

                // Send email notification to customer
                if (order.User != null && !string.IsNullOrEmpty(order.User.Email))
                {
                    try
                    {
                        await _emailService.SendOrderStatusUpdateEmailAsync(order.User.Email, order);
                        _logger.LogInformation($"Order status update email sent to {order.User.Email} for order {order.OrderNumber}");
                    }
                    catch (Exception emailEx)
                    {
                        _logger.LogError(emailEx, $"Failed to send order status update email for order {order.OrderNumber}");
                        // Don't fail the whole request if email fails
                    }
                }

                return Json(new { success = true, message = "Cập nhật trạng thái đơn hàng thành công" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order status");
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        // POST: Admin/Orders/Cancel
        [HttpPost]
        [Route("orders/cancel")]
        public async Task<IActionResult> CancelOrder([FromBody] CancelOrderRequest request)
        {
            try
            {
                var order = await _context.Orders
                    .Include(o => o.User)
                    .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                    .FirstOrDefaultAsync(o => o.Id == request.OrderId);

                if (order == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy đơn hàng" });
                }

                if (order.Status == "delivered")
                {
                    return Json(new { success = false, message = "Không thể hủy đơn hàng đã giao" });
                }

                order.Status = "cancelled";
                order.UpdatedAt = DateTime.UtcNow;

                // Add cancellation note
                var cancelNote = $"[{DateTime.UtcNow:dd/MM/yyyy HH:mm}] Đơn hàng đã được hủy";
                if (!string.IsNullOrEmpty(request.Reason))
                {
                    cancelNote += $" - Lý do: {request.Reason}";
                }

                order.Notes = string.IsNullOrEmpty(order.Notes) 
                    ? cancelNote
                    : $"{order.Notes}\n{cancelNote}";

                // Restore product stock
                foreach (var item in order.OrderItems)
                {
                    if (item.Product != null && item.Product.ManageStock)
                    {
                        item.Product.StockQuantity += item.Quantity;
                    }
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Order {order.OrderNumber} cancelled. Reason: {request.Reason}");

                // Create refund request if customer already paid
                if (order.PaymentStatus == "paid" || order.PaymentStatus == "completed")
                {
                    var refundRequest = new Models.RefundRequest
                    {
                        Id = Guid.NewGuid(),
                        OrderId = order.Id,
                        PaymentId = string.Empty,
                        Amount = order.TotalAmount,
                        Reason = $"Hủy đơn hàng bởi admin. Lý do: {request.Reason ?? "Không có lý do"}",
                        Status = "pending",
                        CreatedAt = DateTime.UtcNow,
                        RequestedBy = User.Identity?.Name ?? "admin"
                    };
                    _context.RefundRequests.Add(refundRequest);
                    await _context.SaveChangesAsync();
                    _logger.LogWarning("Refund request {RefundId} created for cancelled order {OrderNumber}. Amount: {Amount} VND. Review at /admin/payments.",
                        refundRequest.Id, order.OrderNumber, order.TotalAmount);
                }

                // TODO: Send cancellation email to customer

                return Json(new { success = true, message = "Đơn hàng đã được hủy thành công" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling order");
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        // GET: Admin/Orders/Invoice/{id}
        [HttpGet]
        [Route("orders/invoice/{id}")]
        public async Task<IActionResult> PrintInvoice(Guid id)
        {
            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                return NotFound();
            }

            return View("Invoice", order);
        }

        #endregion

        #region Helper Classes
        public class UpdateOrderStatusRequest
        {
            public Guid OrderId { get; set; }
            public string Status { get; set; } = string.Empty;
            public string? Note { get; set; }
        }

        public class CancelOrderRequest
        {
            public Guid OrderId { get; set; }
            public string? Reason { get; set; }
        }

        private static readonly Dictionary<string, string[]> AllowedOrderStatusTransitions = new()
        {
            { "pending",    new[] { "confirmed", "cancelled" } },
            { "confirmed",  new[] { "processing", "cancelled" } },
            { "processing", new[] { "shipped", "cancelled" } },
            { "shipped",    new[] { "delivered", "cancelled" } },
            { "delivered",  new[] { "completed" } },
            { "completed",  System.Array.Empty<string>() },
            { "cancelled",  System.Array.Empty<string>() }
        };

        private static bool IsValidStatusTransition(string? currentStatus, string? newStatus)
        {
            if (string.IsNullOrEmpty(newStatus)) return false;
            var current = currentStatus?.ToLower() ?? "";
            var next = newStatus.ToLower();
            if (!AllowedOrderStatusTransitions.TryGetValue(current, out var allowed))
                return true; // Unknown current status - allow for backward compat
            return allowed.Contains(next);
        }
        #endregion
    }
}
