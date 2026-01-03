using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using JohnHenryFashionWeb.Data;
using JohnHenryFashionWeb.Models;
using JohnHenryFashionWeb.ViewModels;
using JohnHenryFashionWeb.Services;

namespace JohnHenryFashionWeb.Controllers
{
    public class SupportController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly INotificationService _notificationService;
        private readonly ILogger<SupportController> _logger;

        public SupportController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            INotificationService notificationService,
            ILogger<SupportController> logger)
        {
            _context = context;
            _userManager = userManager;
            _notificationService = notificationService;
            _logger = logger;
        }

        // GET: /Support
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var model = new SupportViewModel();

            // Pre-fill if user is logged in
            if (User.Identity?.IsAuthenticated == true)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    model.Name = $"{user.FirstName} {user.LastName}".Trim();
                    model.Email = user.Email ?? "";
                }
            }

            return View(model);
        }

        // POST: /Support
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(SupportViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var userId = User.Identity?.IsAuthenticated == true 
                    ? _userManager.GetUserId(User) 
                    : null;

                var ticket = new SupportTicket
                {
                    TicketNumber = GenerateTicketNumber(),
                    UserId = userId,
                    Subject = model.Subject,
                    Category = model.Category,
                    Priority = model.Priority ?? "Medium",
                    Description = model.Description,
                    Status = "Open",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.SupportTickets.Add(ticket);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"New support ticket created: {ticket.TicketNumber} by {model.Email}");

                // Send notification to admins
                var admins = await _userManager.GetUsersInRoleAsync("Admin");
                foreach (var admin in admins)
                {
                    await _notificationService.CreateNotificationAsync(
                        admin.Id,
                        "Yêu cầu hỗ trợ mới",
                        $"Ticket #{ticket.TicketNumber}: {ticket.Subject}",
                        $"/SupportManagement/TicketDetails/{ticket.Id}",
                        "info"
                    );
                }

                TempData["SuccessMessage"] = $"Yêu cầu hỗ trợ của bạn đã được gửi thành công! Mã ticket: {ticket.TicketNumber}";
                return RedirectToAction("Success");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating support ticket");
                ModelState.AddModelError("", "Có lỗi xảy ra khi gửi yêu cầu. Vui lòng thử lại sau.");
                return View(model);
            }
        }

        // GET: /Support/Success
        [HttpGet]
        public IActionResult Success()
        {
            return View();
        }

        private string GenerateTicketNumber()
        {
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            var random = new Random().Next(1000, 9999);
            return $"TKT{timestamp}{random}";
        }
    }
}
