using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JohnHenryFashionWeb.Data;
using JohnHenryFashionWeb.Models;

namespace JohnHenryFashionWeb.Controllers
{
    [Authorize(Roles = UserRoles.Admin, AuthenticationSchemes = "Identity.Application")]
    [Route("admin/marketing")]
    public class MarketingManagementController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<MarketingManagementController> _logger;

        public MarketingManagementController(ApplicationDbContext context, ILogger<MarketingManagementController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Trang tổng quan marketing
        /// </summary>
        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            ViewData["CurrentSection"] = "Marketing";
            ViewData["Title"] = "Quản lý Marketing";

            // Thống kê
            var totalFlashSales = await _context.FlashSales.CountAsync();
            var activeFlashSales = await _context.FlashSales.CountAsync(f => f.IsActive && f.StartDate <= DateTime.UtcNow && f.EndDate >= DateTime.UtcNow);
            
            var totalEmailCampaigns = await _context.EmailCampaigns.CountAsync();
            var sentEmailCampaigns = await _context.EmailCampaigns.CountAsync(e => e.Status == "sent");
            
            var totalPushCampaigns = await _context.PushNotificationCampaigns.CountAsync();
            var sentPushCampaigns = await _context.PushNotificationCampaigns.CountAsync(p => p.Status == "sent");

            ViewBag.TotalFlashSales = totalFlashSales;
            ViewBag.ActiveFlashSales = activeFlashSales;
            ViewBag.TotalEmailCampaigns = totalEmailCampaigns;
            ViewBag.SentEmailCampaigns = sentEmailCampaigns;
            ViewBag.TotalPushCampaigns = totalPushCampaigns;
            ViewBag.SentPushCampaigns = sentPushCampaigns;

            return View("~/Views/Admin/Marketing.cshtml");
        }

        #region Email Campaigns
        [HttpGet("emails")]
        public async Task<IActionResult> EmailCampaigns()
        {
            var items = await _context.EmailCampaigns.OrderByDescending(e => e.CreatedAt).ToListAsync();
            return View(items);
        }

        [HttpGet("email/create")]
        public IActionResult CreateEmail() => View(new EmailCampaign());

        [HttpPost("email/create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateEmail(EmailCampaign email)
        {
            if (!ModelState.IsValid) return View(email);
            email.Id = Guid.NewGuid();
            email.CreatedAt = DateTime.UtcNow;
            email.UpdatedAt = DateTime.UtcNow;
            _context.EmailCampaigns.Add(email);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Email campaign created";
            return RedirectToAction(nameof(EmailCampaigns));
        }

        [HttpGet("email/{id}")]
        public async Task<IActionResult> EditEmail(Guid id)
        {
            var item = await _context.EmailCampaigns.FindAsync(id);
            if (item == null) return NotFound();
            return View(item);
        }

        [HttpPost("email/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditEmail(Guid id, EmailCampaign email)
        {
            var existing = await _context.EmailCampaigns.FindAsync(id);
            if (existing == null) return NotFound();
            if (!ModelState.IsValid) return View(email);
            existing.Name = email.Name;
            existing.Subject = email.Subject;
            existing.HtmlContent = email.HtmlContent;
            existing.PlainTextContent = email.PlainTextContent;
            existing.TargetAudience = email.TargetAudience;
            existing.TargetSegmentCriteria = email.TargetSegmentCriteria;
            existing.Status = email.Status;
            existing.ScheduledAt = email.ScheduledAt;
            existing.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Email campaign updated";
            return RedirectToAction(nameof(EmailCampaigns));
        }

        [HttpPost("email/{id}/delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteEmail(Guid id)
        {
            var existing = await _context.EmailCampaigns.FindAsync(id);
            if (existing == null) return NotFound();
            _context.EmailCampaigns.Remove(existing);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Email campaign deleted";
            return RedirectToAction(nameof(EmailCampaigns));
        }
        #endregion

        #region Push Notifications
        [HttpGet("pushes")]
        public async Task<IActionResult> Pushes()
        {
            var items = await _context.PushNotificationCampaigns.OrderByDescending(p => p.CreatedAt).ToListAsync();
            return View(items);
        }

        [HttpGet("push/create")]
        public IActionResult CreatePush() => View(new PushNotificationCampaign());

        [HttpPost("push/create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePush(PushNotificationCampaign push)
        {
            if (!ModelState.IsValid) return View(push);
            push.Id = Guid.NewGuid();
            push.CreatedAt = DateTime.UtcNow;
            push.UpdatedAt = DateTime.UtcNow;
            _context.PushNotificationCampaigns.Add(push);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Push campaign created";
            return RedirectToAction(nameof(Pushes));
        }

        [HttpGet("push/{id}")]
        public async Task<IActionResult> EditPush(Guid id)
        {
            var item = await _context.PushNotificationCampaigns.FindAsync(id);
            if (item == null) return NotFound();
            return View(item);
        }

        [HttpPost("push/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPush(Guid id, PushNotificationCampaign push)
        {
            var existing = await _context.PushNotificationCampaigns.FindAsync(id);
            if (existing == null) return NotFound();
            if (!ModelState.IsValid) return View(push);
            existing.Title = push.Title;
            existing.Message = push.Message;
            existing.ImageUrl = push.ImageUrl;
            existing.ActionUrl = push.ActionUrl;
            existing.TargetAudience = push.TargetAudience;
            existing.TargetUserIds = push.TargetUserIds;
            existing.Status = push.Status;
            existing.ScheduledAt = push.ScheduledAt;
            existing.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Push campaign updated";
            return RedirectToAction(nameof(Pushes));
        }

        [HttpPost("push/{id}/delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePush(Guid id)
        {
            var existing = await _context.PushNotificationCampaigns.FindAsync(id);
            if (existing == null) return NotFound();
            _context.PushNotificationCampaigns.Remove(existing);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Push campaign deleted";
            return RedirectToAction(nameof(Pushes));
        }
        #endregion

        #region FlashSales
        [HttpGet("flashsales")]
        public async Task<IActionResult> FlashSales()
        {
            var items = await _context.FlashSales.OrderByDescending(f => f.StartDate).ToListAsync();
            return View(items);
        }

        [HttpGet("flashsale/create")]
        public IActionResult CreateFlashSale() => View(new FlashSale());

        [HttpPost("flashsale/create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateFlashSale(FlashSale sale)
        {
            if (!ModelState.IsValid) return View(sale);
            sale.Id = Guid.NewGuid();
            sale.CreatedAt = DateTime.UtcNow;
            sale.UpdatedAt = DateTime.UtcNow;
            _context.FlashSales.Add(sale);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Flash sale created";
            return RedirectToAction(nameof(FlashSales));
        }

        [HttpGet("flashsale/{id}")]
        public async Task<IActionResult> EditFlashSale(Guid id)
        {
            var item = await _context.FlashSales.FindAsync(id);
            if (item == null) return NotFound();
            return View(item);
        }

        [HttpPost("flashsale/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditFlashSale(Guid id, FlashSale sale)
        {
            var existing = await _context.FlashSales.FindAsync(id);
            if (existing == null) return NotFound();
            if (!ModelState.IsValid) return View(sale);
            existing.Name = sale.Name;
            existing.Description = sale.Description;
            existing.BannerImageUrl = sale.BannerImageUrl;
            existing.StartDate = sale.StartDate;
            existing.EndDate = sale.EndDate;
            existing.IsActive = sale.IsActive;
            existing.DiscountPercentage = sale.DiscountPercentage;
            existing.ProductIds = sale.ProductIds;
            existing.StockLimit = sale.StockLimit;
            existing.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Flash sale updated";
            return RedirectToAction(nameof(FlashSales));
        }

        [HttpPost("flashsale/{id}/delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteFlashSale(Guid id)
        {
            var existing = await _context.FlashSales.FindAsync(id);
            if (existing == null) return NotFound();
            _context.FlashSales.Remove(existing);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Flash sale deleted";
            return RedirectToAction(nameof(FlashSales));
        }
        #endregion
    }
}
