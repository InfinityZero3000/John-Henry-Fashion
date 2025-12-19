using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JohnHenryFashionWeb.Data;
using JohnHenryFashionWeb.Models;
using JohnHenryFashionWeb.ViewModels;
using JohnHenryFashionWeb.Services;

namespace JohnHenryFashionWeb.Controllers
{
    [Authorize]
    [Route("user/support")]
    public class UserSupportController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly INotificationService _notificationService;
        private readonly ILogger<UserSupportController> _logger;

        public UserSupportController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            INotificationService notificationService,
            ILogger<UserSupportController> logger)
        {
            _context = context;
            _userManager = userManager;
            _notificationService = notificationService;
            _logger = logger;
        }

        /// <summary>
        /// Dashboard - Danh sách tickets của user
        /// </summary>
        [HttpGet("")]
        public async Task<IActionResult> Index(string? status = null, string? category = null)
        {
            var userId = _userManager.GetUserId(User);
            
            var query = _context.SupportTickets
                .Where(t => t.UserId == userId)
                .Include(t => t.Replies)
                .Include(t => t.RelatedOrder)
                .Include(t => t.RelatedProduct)
                .Include(t => t.AssignedAdmin)
                .AsQueryable();

            // Filter by status
            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(t => t.Status.ToLower() == status.ToLower());
            }

            // Filter by category
            if (!string.IsNullOrEmpty(category))
            {
                query = query.Where(t => t.Category.ToLower() == category.ToLower());
            }

            var tickets = await query
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            // Statistics
            var allTickets = await _context.SupportTickets
                .Where(t => t.UserId == userId)
                .ToListAsync();

            ViewBag.TotalTickets = allTickets.Count;
            ViewBag.OpenTickets = allTickets.Count(t => t.Status == "Open");
            ViewBag.InProgressTickets = allTickets.Count(t => t.Status == "InProgress" || t.Status == "In_Progress");
            ViewBag.ResolvedTickets = allTickets.Count(t => t.Status == "Resolved");
            ViewBag.ClosedTickets = allTickets.Count(t => t.Status == "Closed");
            ViewBag.StatusFilter = status;
            ViewBag.CategoryFilter = category;

            return View(tickets);
        }

        /// <summary>
        /// Form tạo ticket mới
        /// </summary>
        [HttpGet("create")]
        public async Task<IActionResult> Create(Guid? orderId = null, Guid? productId = null)
        {
            var model = new CreateTicketViewModel
            {
                RelatedOrderId = orderId,
                RelatedProductId = productId
            };

            // Load user's orders for dropdown
            var userId = _userManager.GetUserId(User);
            ViewBag.Orders = await _context.Orders
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.OrderDate)
                .Select(o => new { 
                    o.Id, 
                    o.OrderNumber,
                    o.OrderDate,
                    o.TotalAmount 
                })
                .Take(20) // Chỉ lấy 20 đơn gần nhất
                .ToListAsync();

            // Load user's recent products from orders
            ViewBag.Products = await _context.OrderItems
                .Where(oi => oi.Order.UserId == userId)
                .Select(oi => oi.Product)
                .Distinct()
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => new {
                    p.Id,
                    p.Name,
                    p.SKU
                })
                .Take(20)
                .ToListAsync();

            return View(model);
        }

        /// <summary>
        /// Xử lý tạo ticket
        /// </summary>
        [HttpPost("create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateTicketViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var userId = _userManager.GetUserId(User);
                    var user = await _userManager.GetUserAsync(User);
                    var ticketNumber = $"TKT-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper()}";

                    var ticket = new SupportTicket
                    {
                        Id = Guid.NewGuid(),
                        TicketNumber = ticketNumber,
                        UserId = userId!,
                        UserType = "customer",
                        Subject = model.Subject,
                        Description = model.Description,
                        Category = model.Category.ToLower(),
                        Priority = model.Priority.ToLower(),
                        Status = "Open",
                        RelatedOrderId = model.RelatedOrderId,
                        RelatedProductId = model.RelatedProductId,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    _context.SupportTickets.Add(ticket);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("User {UserId} created support ticket {TicketNumber}", userId, ticketNumber);

                    // Send notification to admins
                    try
                    {
                        var adminUsers = await _userManager.GetUsersInRoleAsync("Admin");
                        var userName = $"{user?.FirstName} {user?.LastName}".Trim();
                        if (string.IsNullOrEmpty(userName)) userName = user?.Email ?? "Khách hàng";

                        var categoryDisplay = GetCategoryDisplay(ticket.Category);

                        foreach (var admin in adminUsers)
                        {
                            await _notificationService.CreateNotificationAsync(
                                admin.Id,
                                "Yêu cầu hỗ trợ mới",
                                $"{userName} đã tạo yêu cầu hỗ trợ mới #{ticketNumber}. Danh mục: {categoryDisplay}",
                                "support_ticket",
                                $"/admin/support?ticketNumber={ticketNumber}");
                        }
                        
                        _logger.LogInformation("Notifications sent to {AdminCount} admins for ticket {TicketNumber}", 
                            adminUsers.Count, ticketNumber);
                    }
                    catch (Exception notifEx)
                    {
                        _logger.LogError(notifEx, "Failed to send notifications for ticket {TicketNumber}", ticketNumber);
                    }

                    TempData["SuccessMessage"] = $"Đã tạo yêu cầu hỗ trợ #{ticketNumber} thành công! Chúng tôi sẽ phản hồi trong thời gian sớm nhất.";
                    return RedirectToAction("Details", new { id = ticket.Id });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating support ticket");
                    ModelState.AddModelError("", "Có lỗi xảy ra khi tạo yêu cầu hỗ trợ. Vui lòng thử lại!");
                }
            }

            // Reload data if validation fails
            var currentUserId = _userManager.GetUserId(User);
            ViewBag.Orders = await _context.Orders
                .Where(o => o.UserId == currentUserId)
                .OrderByDescending(o => o.OrderDate)
                .Select(o => new { o.Id, o.OrderNumber, o.OrderDate, o.TotalAmount })
                .Take(20)
                .ToListAsync();

            ViewBag.Products = await _context.OrderItems
                .Where(oi => oi.Order.UserId == currentUserId)
                .Select(oi => oi.Product)
                .Distinct()
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => new { p.Id, p.Name, p.SKU })
                .Take(20)
                .ToListAsync();

            return View(model);
        }

        /// <summary>
        /// Chi tiết ticket và conversation
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> Details(Guid id)
        {
            var userId = _userManager.GetUserId(User);
            
            var ticket = await _context.SupportTickets
                .Include(t => t.Replies.OrderBy(r => r.CreatedAt))
                    .ThenInclude(r => r.User)
                .Include(t => t.RelatedOrder)
                .Include(t => t.RelatedProduct)
                .Include(t => t.AssignedAdmin)
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

            if (ticket == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy yêu cầu hỗ trợ này!";
                return RedirectToAction("Index");
            }

            var viewModel = new TicketDetailViewModel
            {
                Ticket = ticket,
                Replies = ticket.Replies.ToList(),
                RelatedOrder = ticket.RelatedOrder,
                RelatedProduct = ticket.RelatedProduct,
                AssignedAdmin = ticket.AssignedAdmin
            };

            return View(viewModel);
        }

        /// <summary>
        /// Thêm reply vào ticket
        /// </summary>
        [HttpPost("{id}/reply")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddReply(Guid id, string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                TempData["ErrorMessage"] = "Vui lòng nhập nội dung phản hồi!";
                return RedirectToAction("Details", new { id });
            }

            var userId = _userManager.GetUserId(User);
            var ticket = await _context.SupportTickets
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

            if (ticket == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy yêu cầu hỗ trợ này!";
                return RedirectToAction("Index");
            }

            try
            {
                var reply = new TicketReply
                {
                    Id = Guid.NewGuid(),
                    TicketId = id,
                    UserId = userId!,
                    Message = message.Trim(),
                    IsAdminReply = false,
                    IsInternal = false,
                    CreatedAt = DateTime.UtcNow
                };

                _context.TicketReplies.Add(reply);
                
                // Update ticket
                ticket.ReplyCount++;
                ticket.UpdatedAt = DateTime.UtcNow;
                
                // Reopen ticket if it was resolved/closed
                if (ticket.Status == "Resolved" || ticket.Status == "Closed")
                {
                    ticket.Status = "Open";
                    _logger.LogInformation("Ticket {TicketNumber} reopened due to customer reply", ticket.TicketNumber);
                }

                await _context.SaveChangesAsync();

                // Notify assigned admin if any
                if (!string.IsNullOrEmpty(ticket.AssignedTo))
                {
                    try
                    {
                        var user = await _userManager.GetUserAsync(User);
                        var userName = $"{user?.FirstName} {user?.LastName}".Trim();
                        
                        await _notificationService.CreateNotificationAsync(
                            ticket.AssignedTo,
                            "Phản hồi mới từ khách hàng",
                            $"Ticket #{ticket.TicketNumber} có phản hồi mới từ {userName}",
                            "ticket_reply",
                            $"/admin/support/{ticket.Id}");
                    }
                    catch (Exception notifEx)
                    {
                        _logger.LogError(notifEx, "Failed to send notification for reply on ticket {TicketId}", id);
                    }
                }

                TempData["SuccessMessage"] = "Đã gửi phản hồi thành công!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding reply to ticket {TicketId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi gửi phản hồi. Vui lòng thử lại!";
            }

            return RedirectToAction("Details", new { id });
        }

        /// <summary>
        /// Đóng ticket (user satisfaction)
        /// </summary>
        [HttpPost("{id}/close")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CloseTicket(Guid id)
        {
            var userId = _userManager.GetUserId(User);
            var ticket = await _context.SupportTickets
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

            if (ticket == null)
            {
                return NotFound();
            }

            ticket.Status = "Closed";
            ticket.ClosedAt = DateTime.UtcNow;
            ticket.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đã đóng yêu cầu hỗ trợ thành công!";
            return RedirectToAction("Index");
        }

        #region Helper Methods

        private string GetCategoryDisplay(string category)
        {
            return category?.ToLower() switch
            {
                "order" => "Đơn hàng",
                "product" => "Sản phẩm",
                "payment" => "Thanh toán",
                "account" => "Tài khoản",
                "refund" => "Hoàn tiền",
                "technical" => "Kỹ thuật",
                "contact" => "Liên hệ",
                _ => "Chung"
            };
        }

        #endregion
    }
}
