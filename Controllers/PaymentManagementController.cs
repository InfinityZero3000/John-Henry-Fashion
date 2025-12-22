using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JohnHenryFashionWeb.Data;
using JohnHenryFashionWeb.Models;
using JohnHenryFashionWeb.ViewModels;

namespace JohnHenryFashionWeb.Controllers
{
    [Authorize(Policy = AdminPolicies.RequireAdminRole)]
    [Route("admin/payments")]
    public class PaymentManagementController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PaymentManagementController> _logger;

        public PaymentManagementController(
            ApplicationDbContext context,
            ILogger<PaymentManagementController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Trang tổng quan thanh toán
        /// </summary>
        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            ViewData["CurrentSection"] = "Payments";
            ViewData["Title"] = "Quản lý thanh toán";

            try
            {
                _logger.LogInformation("Loading payment management dashboard");

                // Thống kê tổng quan
                var totalTransactions = await _context.PaymentTransactions.CountAsync();
                _logger.LogInformation("Total transactions: {Count}", totalTransactions);

                var totalAmount = await _context.PaymentTransactions
                    .Where(t => t.Status == "completed")
                    .SumAsync(t => (decimal?)t.Amount) ?? 0;

                var pendingCount = await _context.PaymentTransactions.CountAsync(t => t.Status == "pending");
                var completedCount = await _context.PaymentTransactions.CountAsync(t => t.Status == "completed");
                var failedCount = await _context.PaymentTransactions.CountAsync(t => t.Status == "failed");

                _logger.LogInformation("Transaction stats - Pending: {Pending}, Completed: {Completed}, Failed: {Failed}", 
                    pendingCount, completedCount, failedCount);

                // Giao dịch gần đây
                var recentTransactions = await _context.PaymentTransactions
                    .Include(t => t.Customer)
                    .Include(t => t.Order)
                    .OrderByDescending(t => t.CreatedAt)
                    .Take(10)
                    .ToListAsync();

                _logger.LogInformation("Loaded {Count} recent transactions", recentTransactions.Count);

                ViewBag.TotalTransactions = totalTransactions;
                ViewBag.TotalAmount = totalAmount;
                ViewBag.PendingCount = pendingCount;
                ViewBag.CompletedCount = completedCount;
                ViewBag.FailedCount = failedCount;
                ViewBag.RecentTransactions = recentTransactions;

                return View("~/Views/Admin/Payments.cshtml");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading payment management dashboard");
                ViewBag.TotalTransactions = 0;
                ViewBag.TotalAmount = 0m;
                ViewBag.PendingCount = 0;
                ViewBag.CompletedCount = 0;
                ViewBag.FailedCount = 0;
                ViewBag.RecentTransactions = new List<PaymentTransaction>();
                return View("~/Views/Admin/Payments.cshtml");
            }
        }

        #region Payment Transactions

        [HttpGet("transactions")]
        public async Task<IActionResult> Transactions(
            int page = 1,
            string? status = null,
            string? paymentMethod = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            string? search = null)
        {
            const int pageSize = 20;

            var query = _context.PaymentTransactions
                .Include(t => t.Customer)
                .Include(t => t.Seller)
                .Include(t => t.Order)
                .AsQueryable();

            // Filters
            if (!string.IsNullOrEmpty(status))  
            {
                query = query.Where(t => t.Status == status);
            }

            if (!string.IsNullOrEmpty(paymentMethod))
            {
                query = query.Where(t => t.PaymentMethod == paymentMethod);
            }

            if (fromDate.HasValue)
            {
                query = query.Where(t => t.CreatedAt >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                query = query.Where(t => t.CreatedAt <= toDate.Value.AddDays(1));
            }

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(t => 
                    (t.TransactionReference != null && t.TransactionReference.Contains(search)) ||
                    t.Customer.Email!.Contains(search) ||
                    (t.Seller != null && t.Seller.Email!.Contains(search)));
            }

            var totalTransactions = await query.CountAsync();
            var transactions = await query
                .OrderByDescending(t => t.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Calculate statistics
            var stats = new
            {
                TotalAmount = await query.SumAsync(t => t.Amount),
                TotalPlatformFee = await query.SumAsync(t => t.PlatformFee),
                TotalSellerAmount = await query.SumAsync(t => t.SellerAmount),
                CompletedCount = await query.CountAsync(t => t.Status == "Completed"),
                PendingCount = await query.CountAsync(t => t.Status == "Pending"),
                FailedCount = await query.CountAsync(t => t.Status == "Failed")
            };

            ViewBag.Statistics = stats;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalTransactions / pageSize);
            ViewBag.StatusFilter = status;
            ViewBag.PaymentMethodFilter = paymentMethod;
            ViewBag.FromDate = fromDate;
            ViewBag.ToDate = toDate;
            ViewBag.SearchTerm = search;

            return View(transactions);
        }

        [HttpGet("transactions/{id}")]
        public async Task<IActionResult> TransactionDetails(Guid id)
        {
            var transaction = await _context.PaymentTransactions
                .Include(t => t.Customer)
                .Include(t => t.Seller)
                .Include(t => t.Order)
                    .ThenInclude(o => o!.OrderItems)
                        .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (transaction == null)
            {
                return NotFound();
            }

            return View(transaction);
        }

        #endregion

        #region Seller Settlements

        [HttpGet("settlements")]
        public async Task<IActionResult> Settlements(
            int page = 1,
            string? status = null,
            DateTime? fromDate = null,
            DateTime? toDate = null)
        {
            const int pageSize = 20;

            var query = _context.SellerSettlements
                .Include(s => s.Seller)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(s => s.Status == status);
            }

            if (fromDate.HasValue)
            {
                query = query.Where(s => s.PeriodStart >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                query = query.Where(s => s.PeriodEnd <= toDate.Value.AddDays(1));
            }

            var totalSettlements = await query.CountAsync();
            var settlements = await query
                .OrderByDescending(s => s.PeriodEnd)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalSettlements / pageSize);
            ViewBag.StatusFilter = status;
            ViewBag.FromDate = fromDate;
            ViewBag.ToDate = toDate;

            return View(settlements);
        }

        [HttpGet("settlements/{id}")]
        public async Task<IActionResult> SettlementDetails(Guid id)
        {
            var settlement = await _context.SellerSettlements
                .Include(s => s.Seller)
                .Include(s => s.SettlementAdmin)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (settlement == null)
            {
                return NotFound();
            }

            // Get related transactions
            var transactions = await _context.PaymentTransactions
                .Include(t => t.Order)
                .Where(t => t.SellerId == settlement.SellerId &&
                           t.CreatedAt >= settlement.PeriodStart &&
                           t.CreatedAt <= settlement.PeriodEnd &&
                           t.Status == "Completed")
                .OrderBy(t => t.CreatedAt)
                .ToListAsync();

            ViewBag.Transactions = transactions;

            return View(settlement);
        }

        [HttpPost("settlements/{id}/process")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessSettlement(Guid id)
        {
            var settlement = await _context.SellerSettlements.FindAsync(id);
            if (settlement == null)
            {
                return Json(new { success = false, message = "Không tìm thấy settlement" });
            }

            if (settlement.Status != "Pending")
            {
                return Json(new { success = false, message = "Settlement này đã được xử lý" });
            }

            try
            {
                settlement.Status = "Completed";
                settlement.SettledAt = DateTime.UtcNow;
                settlement.SettledBy = User.Identity!.Name;

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Settlement {id} processed by {User.Identity.Name}");

                return Json(new { success = true, message = "Settlement đã được xử lý thành công!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing settlement {id}");
                return Json(new { success = false, message = $"Có lỗi xảy ra: {ex.Message}" });
            }
        }

        #endregion

        #region Withdrawal Requests

        [HttpGet("withdrawals")]
        public async Task<IActionResult> Withdrawals(
            int page = 1,
            string? status = null,
            DateTime? fromDate = null,
            DateTime? toDate = null)
        {
            const int pageSize = 20;

            var query = _context.WithdrawalRequests
                .Include(w => w.Seller)
                .Include(w => w.Processor)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(w => w.Status == status);
            }

            if (fromDate.HasValue)
            {
                query = query.Where(w => w.RequestedAt >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                query = query.Where(w => w.RequestedAt <= toDate.Value.AddDays(1));
            }

            var totalWithdrawals = await query.CountAsync();
            var withdrawals = await query
                .OrderByDescending(w => w.RequestedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Stats
            var stats = new
            {
                PendingAmount = await query.Where(w => w.Status == "Pending").SumAsync(w => w.Amount),
                ApprovedAmount = await query.Where(w => w.Status == "Approved").SumAsync(w => w.Amount),
                RejectedCount = await query.CountAsync(w => w.Status == "Rejected")
            };

            ViewBag.Statistics = stats;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalWithdrawals / pageSize);
            ViewBag.StatusFilter = status;
            ViewBag.FromDate = fromDate;
            ViewBag.ToDate = toDate;

            return View(withdrawals);
        }

        [HttpGet("withdrawals/{id}")]
        public async Task<IActionResult> WithdrawalDetails(Guid id)
        {
            var withdrawal = await _context.WithdrawalRequests
                .Include(w => w.Seller)
                .Include(w => w.Processor)
                .FirstOrDefaultAsync(w => w.Id == id);

            if (withdrawal == null)
            {
                return NotFound();
            }

            return View(withdrawal);
        }

        [HttpPost("withdrawals/{id}/approve")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveWithdrawal(Guid id, string? notes)
        {
            var withdrawal = await _context.WithdrawalRequests
                .Include(w => w.Seller)
                .FirstOrDefaultAsync(w => w.Id == id);

            if (withdrawal == null)
            {
                return Json(new { success = false, message = "Không tìm thấy yêu cầu rút tiền" });
            }

            if (withdrawal.Status != "Pending")
            {
                return Json(new { success = false, message = "Yêu cầu này đã được xử lý" });
            }

            try
            {
                withdrawal.Status = "Approved";
                withdrawal.ProcessedAt = DateTime.UtcNow;
                withdrawal.ProcessedBy = User.Identity!.Name;
                withdrawal.AdminNotes = notes;

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Withdrawal {id} approved by {User.Identity.Name}");

                // TODO: Trigger actual payment transfer to seller

                return Json(new { success = true, message = "Yêu cầu rút tiền đã được phê duyệt!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error approving withdrawal {id}");
                return Json(new { success = false, message = $"Có lỗi xảy ra: {ex.Message}" });
            }
        }

        [HttpPost("withdrawals/{id}/reject")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectWithdrawal(Guid id, string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
            {
                return Json(new { success = false, message = "Vui lòng nhập lý do từ chối" });
            }

            var withdrawal = await _context.WithdrawalRequests.FindAsync(id);
            if (withdrawal == null)
            {
                return Json(new { success = false, message = "Không tìm thấy yêu cầu rút tiền" });
            }

            if (withdrawal.Status != "Pending")
            {
                return Json(new { success = false, message = "Yêu cầu này đã được xử lý" });
            }

            try
            {
                withdrawal.Status = "Rejected";
                withdrawal.ProcessedAt = DateTime.UtcNow;
                withdrawal.ProcessedBy = User.Identity!.Name;
                withdrawal.AdminNotes = reason;

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Withdrawal {id} rejected by {User.Identity.Name}");

                return Json(new { success = true, message = "Yêu cầu rút tiền đã bị từ chối" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error rejecting withdrawal {id}");
                return Json(new { success = false, message = $"Có lỗi xảy ra: {ex.Message}" });
            }
        }

        #endregion

        #region Payment Method Configurations

        [HttpGet("methods")]
        public async Task<IActionResult> PaymentMethods()
        {
            var methods = await _context.PaymentMethodConfigs
                .OrderBy(m => m.SortOrder)
                .ToListAsync();

            return View(methods);
        }

        [HttpPost("methods/{id}/toggle")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TogglePaymentMethod(Guid id)
        {
            var method = await _context.PaymentMethodConfigs.FindAsync(id);
            if (method == null)
            {
                return Json(new { success = false, message = "Không tìm thấy phương thức thanh toán" });
            }

            try
            {
                method.IsActive = !method.IsActive;
                method.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Json(new { 
                    success = true, 
                    message = $"Phương thức thanh toán đã được {(method.IsActive ? "kích hoạt" : "vô hiệu hóa")}",
                    isActive = method.IsActive
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Có lỗi xảy ra: {ex.Message}" });
            }
        }

        [HttpPost("methods/toggle")]
        public async Task<IActionResult> TogglePaymentMethodV2([FromBody] ToggleMethodRequest request)
        {
            var method = await _context.PaymentMethodConfigs.FindAsync(Guid.Parse(request.Id));
            if (method == null)
            {
                return Json(new { success = false, message = "Không tìm thấy phương thức thanh toán" });
            }

            try
            {
                method.IsActive = !method.IsActive;
                method.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Json(new { 
                    success = true, 
                    message = $"Phương thức thanh toán đã được {(method.IsActive ? "kích hoạt" : "tạm ngừng")}",
                    isActive = method.IsActive
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Có lỗi xảy ra: {ex.Message}" });
            }
        }

        [HttpPost("methods/add-or-update")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddOrUpdatePaymentMethod(PaymentMethodConfig model)
        {
            try
            {
                if (model.Id == Guid.Empty)
                {
                    // Add new method
                    model.Id = Guid.NewGuid();
                    model.CreatedAt = DateTime.UtcNow;
                    model.UpdatedAt = DateTime.UtcNow;
                    _context.PaymentMethodConfigs.Add(model);
                    TempData["SuccessMessage"] = $"Phương thức thanh toán '{model.Name}' đã được thêm thành công!";
                }
                else
                {
                    // Update existing method
                    var existing = await _context.PaymentMethodConfigs.FindAsync(model.Id);
                    if (existing == null)
                    {
                        TempData["ErrorMessage"] = "Không tìm thấy phương thức thanh toán";
                        return RedirectToAction(nameof(PaymentMethods));
                    }

                    existing.Name = model.Name;
                    existing.Code = model.Code;
                    existing.Description = model.Description;
                    existing.TransactionFeePercent = model.TransactionFeePercent;
                    existing.TransactionFeeFixed = model.TransactionFeeFixed;
                    existing.SortOrder = model.SortOrder;
                    existing.LogoUrl = model.LogoUrl;
                    existing.IsActive = model.IsActive;
                    existing.ApiConfiguration = model.ApiConfiguration;
                    existing.UpdatedAt = DateTime.UtcNow;

                    TempData["SuccessMessage"] = $"Phương thức thanh toán '{model.Name}' đã được cập nhật thành công!";
                }

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(PaymentMethods));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Có lỗi xảy ra: {ex.Message}";
                return RedirectToAction(nameof(PaymentMethods));
            }
        }

        [HttpPost("methods/delete/{id}")]
        public async Task<IActionResult> DeletePaymentMethod(Guid id)
        {
            var method = await _context.PaymentMethodConfigs.FindAsync(id);
            if (method == null)
            {
                return Json(new { success = false, message = "Không tìm thấy phương thức thanh toán" });
            }

            try
            {
                _context.PaymentMethodConfigs.Remove(method);
                await _context.SaveChangesAsync();

                return Json(new { 
                    success = true, 
                    message = $"Phương thức thanh toán '{method.Name}' đã được xóa thành công!"
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Có lỗi xảy ra: {ex.Message}" });
            }
        }

        public class ToggleMethodRequest
        {
            public string Id { get; set; } = string.Empty;
        }

        #endregion

        #region Statistics & Reports

        [HttpGet("statistics")]
        public async Task<IActionResult> Statistics(DateTime? fromDate, DateTime? toDate)
        {
            fromDate ??= DateTime.UtcNow.AddMonths(-1);
            toDate ??= DateTime.UtcNow;

            var transactions = await _context.PaymentTransactions
                .Where(t => t.CreatedAt >= fromDate && t.CreatedAt <= toDate)
                .ToListAsync();

            var stats = new
            {
                TotalRevenue = transactions.Sum(t => t.Amount),
                TotalPlatformFee = transactions.Sum(t => t.PlatformFee),
                TotalSellerRevenue = transactions.Sum(t => t.SellerAmount),
                TransactionCount = transactions.Count,
                AverageOrderValue = transactions.Any() ? transactions.Average(t => t.Amount) : 0,
                CompletedTransactions = transactions.Count(t => t.Status == "Completed"),
                PendingTransactions = transactions.Count(t => t.Status == "Pending"),
                FailedTransactions = transactions.Count(t => t.Status == "Failed"),
                PaymentMethodBreakdown = transactions
                    .GroupBy(t => t.PaymentMethod)
                    .Select(g => new { Method = g.Key, Count = g.Count(), Amount = g.Sum(t => t.Amount) })
                    .ToList()
            };

            ViewBag.FromDate = fromDate;
            ViewBag.ToDate = toDate;
            ViewBag.Statistics = stats;

            return View();
        }

        #endregion
    }
}
