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

        public ContactController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<ContactController> logger,
            IEmailService emailService)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
            _emailService = emailService;
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
                    if (User.Identity?.IsAuthenticated == true)
                    {
                        contactMessage.UserId = _userManager.GetUserId(User);
                    }

                    _context.ContactMessages.Add(contactMessage);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("New contact message received from {Email} with subject: {Subject}", 
                        model.Email, model.Subject);

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
