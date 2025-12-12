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
            var totalBanners = await _context.MarketingBanners.CountAsync();
            var activeBanners = await _context.MarketingBanners.CountAsync(b => b.IsActive);
            var totalBlogPosts = await _context.BlogPosts.CountAsync();
            var publishedBlogPosts = await _context.BlogPosts.CountAsync(b => b.Status == "published");
            
            var totalPromotions = await _context.SystemPromotions.CountAsync();
            var activePromotions = await _context.SystemPromotions.CountAsync(p => p.IsActive && p.StartDate <= DateTime.UtcNow && p.EndDate >= DateTime.UtcNow);
            
            var totalFlashSales = await _context.FlashSales.CountAsync();
            var activeFlashSales = await _context.FlashSales.CountAsync(f => f.IsActive && f.StartDate <= DateTime.UtcNow && f.EndDate >= DateTime.UtcNow);
            
            var totalEmailCampaigns = await _context.EmailCampaigns.CountAsync();
            var sentEmailCampaigns = await _context.EmailCampaigns.CountAsync(e => e.Status == "sent");
            
            var totalPushCampaigns = await _context.PushNotificationCampaigns.CountAsync();
            var sentPushCampaigns = await _context.PushNotificationCampaigns.CountAsync(p => p.Status == "sent");

            // Recent banners
            var recentBanners = await _context.MarketingBanners
                .OrderByDescending(b => b.CreatedAt)
                .Take(5)
                .ToListAsync();

            // Recent blog posts
            var recentBlogPosts = await _context.BlogPosts
                .OrderByDescending(b => b.CreatedAt)
                .Take(5)
                .ToListAsync();

            ViewBag.TotalBanners = totalBanners;
            ViewBag.ActiveBanners = activeBanners;
            ViewBag.TotalBlogPosts = totalBlogPosts;
            ViewBag.PublishedBlogPosts = publishedBlogPosts;
            ViewBag.TotalPromotions = totalPromotions;
            ViewBag.ActivePromotions = activePromotions;
            ViewBag.TotalFlashSales = totalFlashSales;
            ViewBag.ActiveFlashSales = activeFlashSales;
            ViewBag.TotalEmailCampaigns = totalEmailCampaigns;
            ViewBag.SentEmailCampaigns = sentEmailCampaigns;
            ViewBag.TotalPushCampaigns = totalPushCampaigns;
            ViewBag.SentPushCampaigns = sentPushCampaigns;
            ViewBag.RecentBanners = recentBanners;
            ViewBag.RecentBlogPosts = recentBlogPosts;

            return View("~/Views/Admin/Marketing.cshtml");
        }

        #region Banners
        [HttpGet("banners")]
        public async Task<IActionResult> Banners()
        {
            var banners = await _context.MarketingBanners
                .OrderBy(b => b.SortOrder)
                .ToListAsync();
            return View(banners);
        }

        [HttpGet("banner/create")]
        public IActionResult CreateBanner()
        {
            return View(new MarketingBanner());
        }

        [HttpPost("banner/create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateBanner(MarketingBanner banner)
        {
            if (!ModelState.IsValid) return View(banner);
            banner.Id = Guid.NewGuid();
            banner.CreatedAt = DateTime.UtcNow;
            banner.UpdatedAt = DateTime.UtcNow;
            _context.MarketingBanners.Add(banner);
            await _context.SaveChangesAsync();
            _logger.LogInformation($"Banner created: {banner.Title} by {User.Identity?.Name}");
            TempData["SuccessMessage"] = "Banner đã được tạo";
            return RedirectToAction("Banners", "Admin");
        }

        [HttpGet("banner/{id}")]
        public async Task<IActionResult> EditBanner(Guid id)
        {
            var banner = await _context.MarketingBanners.FindAsync(id);
            if (banner == null) return NotFound();
            return View(banner);
        }

        [HttpPost("banner/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditBanner(Guid id, MarketingBanner banner)
        {
            var existing = await _context.MarketingBanners.FindAsync(id);
            if (existing == null) return NotFound();
            if (!ModelState.IsValid) return View(banner);
            existing.Title = banner.Title;
            existing.Description = banner.Description;
            existing.ImageUrl = banner.ImageUrl;
            existing.MobileImageUrl = banner.MobileImageUrl;
            existing.LinkUrl = banner.LinkUrl;
            existing.OpenInNewTab = banner.OpenInNewTab;
            existing.Position = banner.Position;
            existing.TargetPage = banner.TargetPage;
            existing.SortOrder = banner.SortOrder;
            existing.IsActive = banner.IsActive;
            existing.StartDate = banner.StartDate;
            existing.EndDate = banner.EndDate;
            existing.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Banner đã được cập nhật";
            return RedirectToAction("Banners", "Admin");
        }

        [HttpPost("banner/{id}/delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteBanner(Guid id)
        {
            var existing = await _context.MarketingBanners.FindAsync(id);
            if (existing == null) return NotFound();
            _context.MarketingBanners.Remove(existing);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Banner đã được xóa";
            return RedirectToAction("Banners", "Admin");
        }
        #endregion

        #region Promotions
        [HttpGet("promotions")]
        public async Task<IActionResult> Promotions()
        {
            var promos = await _context.SystemPromotions.OrderByDescending(p => p.StartDate).ToListAsync();
            return View(promos);
        }

        [HttpGet("promotion/create")]
        public IActionResult CreatePromotion() => View(new SystemPromotion());

        [HttpPost("promotion/create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePromotion(SystemPromotion promo)
        {
            if (!ModelState.IsValid)
            {
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    _logger.LogWarning($"ModelState Error: {error.ErrorMessage}");
                }
                TempData["ErrorMessage"] = "Vui lòng kiểm tra lại thông tin";
                return View(promo);
            }

            try
            {
                promo.Id = Guid.NewGuid();
                promo.CreatedAt = DateTime.UtcNow;
                promo.UpdatedAt = DateTime.UtcNow;
                
                // Ensure dates are in UTC
                if (promo.StartDate.Kind == DateTimeKind.Unspecified)
                    promo.StartDate = DateTime.SpecifyKind(promo.StartDate, DateTimeKind.Utc);
                if (promo.EndDate.Kind == DateTimeKind.Unspecified)
                    promo.EndDate = DateTime.SpecifyKind(promo.EndDate, DateTimeKind.Utc);

                _context.SystemPromotions.Add(promo);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Promotion created";
                return RedirectToAction(nameof(Promotions));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating promotion");
                TempData["ErrorMessage"] = $"Lỗi khi tạo: {ex.Message}";
                return View(promo);
            }
        }

        [HttpGet("promotion/{id}")]
        public async Task<IActionResult> EditPromotion(Guid id)
        {
            var promo = await _context.SystemPromotions.FindAsync(id);
            if (promo == null) return NotFound();
            return View(promo);
        }

        [HttpPost("promotion/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPromotion(Guid id, SystemPromotion promo)
        {
            var existing = await _context.SystemPromotions.FindAsync(id);
            if (existing == null) return NotFound();
            if (!ModelState.IsValid) return View(promo);
            existing.Name = promo.Name;
            existing.Code = promo.Code;
            existing.Description = promo.Description;
            existing.Type = promo.Type;
            existing.Value = promo.Value;
            existing.MinOrderAmount = promo.MinOrderAmount;
            existing.MaxDiscountAmount = promo.MaxDiscountAmount;
            existing.UsageLimit = promo.UsageLimit;
            existing.UsageLimitPerUser = promo.UsageLimitPerUser;
            existing.StartDate = promo.StartDate;
            existing.EndDate = promo.EndDate;
            existing.IsActive = promo.IsActive;
            existing.BannerImageUrl = promo.BannerImageUrl;
            existing.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Promotion updated";
            return RedirectToAction(nameof(Promotions));
        }

        [HttpPost("promotion/{id}/delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePromotion(Guid id)
        {
            var existing = await _context.SystemPromotions.FindAsync(id);
            if (existing == null) return NotFound();
            _context.SystemPromotions.Remove(existing);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Promotion deleted";
            return RedirectToAction(nameof(Promotions));
        }
        #endregion

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
