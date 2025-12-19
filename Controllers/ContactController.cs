using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using JohnHenryFashionWeb.Data;
using JohnHenryFashionWeb.Models;
using JohnHenryFashionWeb.ViewModels;
using JohnHenryFashionWeb.Services;

namespace JohnHenryFashionWeb.Controllers
{
    public class ContactController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<ContactController> _logger;
        private readonly IEmailService _emailService;
        private readonly INotificationService _notificationService;

        public ContactController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<ContactController> logger,
            IEmailService emailService,
            INotificationService notificationService)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
            _emailService = emailService;
            _notificationService = notificationService;
        }

        [HttpGet]
        public IActionResult Index()
        {
            var model = new ContactViewModel();
            
            // Pre-fill if user is logged in
            if (User.Identity?.IsAuthenticated == true)
            {
                var user = _userManager.GetUserAsync(User).Result;
                if (user != null)
                {
                    model.Name = $"{user.FirstName} {user.LastName}".Trim();
                    model.Email = user.Email ?? "";
                    model.Phone = user.PhoneNumber;
                }
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(ContactViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var contactMessage = new ContactMessage
                    {
                        Id = Guid.NewGuid(),
                        Name = model.Name,
                        Email = model.Email,
                        Phone = model.Phone,
                        Subject = model.Subject,
                        Message = model.Message,
                        CreatedAt = DateTime.UtcNow
                    };

                    // Link to user if logged in
                    string? userId = null;
                    if (User.Identity?.IsAuthenticated == true)
                    {
                        userId = _userManager.GetUserId(User);
                        contactMessage.UserId = userId;
                    }

                    _context.ContactMessages.Add(contactMessage);

                    // Tạo Support Ticket từ Contact Message
                    var ticketNumber = $"TKT-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper()}";
                    var supportTicket = new SupportTicket
                    {
                        Id = Guid.NewGuid(),
                        TicketNumber = ticketNumber,
                        UserId = userId ?? "guest", // Nếu không đăng nhập thì dùng "guest"
                        UserType = "customer",
                        Subject = model.Subject,
                        Description = $"Từ: {model.Name}\nEmail: {model.Email}\n" + 
                                    (string.IsNullOrEmpty(model.Phone) ? "" : $"SĐT: {model.Phone}\n") +
                                    $"\nNội dung:\n{model.Message}",
                        Category = "contact", // Category mới cho liên hệ từ form
                        Priority = "medium",
                        Status = "Open",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    _context.SupportTickets.Add(supportTicket);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("New contact message received from {Email} with subject: {Subject}. Support ticket created: {TicketNumber}", 
                        model.Email, model.Subject, ticketNumber);

                    // Send confirmation email to customer
                    try
                    {
                        await _emailService.SendContactConfirmationEmailAsync(model.Email, contactMessage);
                        _logger.LogInformation("Confirmation email sent to {Email}", model.Email);
                    }
                    catch (Exception emailEx)
                    {
                        _logger.LogError(emailEx, "Failed to send confirmation email to {Email}", model.Email);
                        // Don't fail the whole operation if email fails
                    }

                    // Send notification email to admin
                    try
                    {
                        await _emailService.SendContactNotificationToAdminAsync(contactMessage);
                        _logger.LogInformation("Admin notification sent for contact from {Email}", model.Email);
                    }
                    catch (Exception emailEx)
                    {
                        _logger.LogError(emailEx, "Failed to send admin notification for contact from {Email}", model.Email);
                        // Don't fail the whole operation if email fails
                    }

                    // Send in-app notification to all admin users
                    try
                    {
                        var adminUsers = await _userManager.GetUsersInRoleAsync("Admin");
                        foreach (var admin in adminUsers)
                        {
                            await _notificationService.CreateNotificationAsync(
                                admin.Id,
                                "Tin nhắn liên hệ mới",
                                $"Có tin nhắn liên hệ mới từ {model.Name} ({model.Email}). Chủ đề: {model.Subject}",
                                "contact",
                                $"/admin/support?ticketNumber={ticketNumber}");
                        }
                        _logger.LogInformation("In-app notifications sent to admin users for contact from {Email}", model.Email);
                    }
                    catch (Exception notifEx)
                    {
                        _logger.LogError(notifEx, "Failed to send in-app notifications for contact from {Email}", model.Email);
                        // Don't fail the whole operation if notification fails
                    }

                    TempData["SuccessMessage"] = "Cảm ơn bạn đã liên hệ! Chúng tôi sẽ phản hồi trong thời gian sớm nhất.";
                    
                    // Clear form after successful submission
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error saving contact message from {Email}", model.Email);
                    ModelState.AddModelError("", "Có lỗi xảy ra khi gửi tin nhắn. Vui lòng thử lại!");
                }
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult Success()
        {
            return View();
        }
    }
}
