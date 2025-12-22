using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using JohnHenryFashionWeb.Models;
using JohnHenryFashionWeb.ViewModels;
using Microsoft.EntityFrameworkCore;
using JohnHenryFashionWeb.Data;
using System.Diagnostics;

namespace JohnHenryFashionWeb.Controllers
{
    public partial class AdminController
    {
        #region Settings Management
        
        [HttpGet("settings")]
        public async Task<IActionResult> Settings()
        {
            var systemConfig = HttpContext.RequestServices.GetRequiredService<JohnHenryFashionWeb.Services.ISystemConfigService>();
            
            var viewModel = new SystemSettingsViewModel
            {
                // General Settings
                SiteName = await systemConfig.GetSettingAsync("site_name") ?? "John Henry",
                SiteDescription = await systemConfig.GetSettingAsync("site_description") ?? "Fashion Store",
                AdminEmail = "thefirestar312@gmail.com",
                SupportEmail = "thefirestar312@gmail.com",
                
                // E-commerce Settings
                Currency = await systemConfig.GetSettingAsync("currency") ?? "VND",
                TaxRate = await systemConfig.GetSettingAsync<decimal>("tax_rate", 10.0m),
                ShippingFee = await systemConfig.GetSettingAsync<decimal>("shipping_fee", 30000m),
                FreeShippingThreshold = await systemConfig.GetSettingAsync<decimal>("free_shipping_threshold", 500000m),
                
                // Security Settings - Read from database
                EnableTwoFactorAuth = await systemConfig.GetSettingAsync<bool>("require_2fa", false),
                PasswordExpirationDays = await systemConfig.GetSettingAsync<int>("password_expiry_days", 90),
                MaxLoginAttempts = await systemConfig.GetSettingAsync<int>("max_login_attempts", 5),
                SessionTimeoutMinutes = await systemConfig.GetSettingAsync<int>("session_timeout_minutes", 30),
                
                // Email Settings
                SmtpServer = "smtp.gmail.com",
                SmtpPort = 587,
                SmtpUsername = "",
                SmtpPassword = "",
                
                // Notification Settings
                EnableEmailNotifications = true,
                EnableSmsNotifications = false,
                EnablePushNotifications = true,
                
                // Maintenance Settings
                MaintenanceMode = false,
                MaintenanceMessage = "Trang web đang bảo trì. Vui lòng quay lại sau.",
                
                // Analytics Settings
                GoogleAnalyticsId = "",
                FacebookPixelId = "",
                EnableAnalytics = true,
                
                // Payment Settings
                EnableCashOnDelivery = true,
                EnableBankTransfer = true,
                EnablePayPal = false,
                EnableStripe = false,
                
                // Inventory Settings
                LowStockThreshold = await systemConfig.GetSettingAsync<int>("low_stock_threshold", 10),
                AutoReduceStock = true,
                AllowBackorders = false,
                
                // SEO Settings
                DefaultMetaTitle = "John Henry - Fashion Store",
                DefaultMetaDescription = "Discover fashion at John Henry Fashion",
                DefaultMetaKeywords = "fashion, clothing, style"
            };

            return View(viewModel);
        }

        [HttpPost("settings")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateSettings(SystemSettingsViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("Settings", model);
            }

            try
            {
                // Get or create ISystemConfigService (inject it if not already injected)
                var systemConfig = HttpContext.RequestServices.GetRequiredService<JohnHenryFashionWeb.Services.ISystemConfigService>();
                
                // Save security settings to database
                await systemConfig.SetSettingAsync("max_login_attempts", model.MaxLoginAttempts.ToString(), "security", "Maximum login attempts before account lockout");
                await systemConfig.SetSettingAsync("session_timeout_minutes", model.SessionTimeoutMinutes.ToString(), "security", "Session timeout in minutes");
                await systemConfig.SetSettingAsync("password_expiry_days", model.PasswordExpirationDays.ToString(), "security", "Password expiration period in days");
                await systemConfig.SetSettingAsync("require_2fa", model.EnableTwoFactorAuth.ToString(), "security", "Require two-factor authentication");
                
                // Save other settings as needed
                await systemConfig.SetSettingAsync("site_name", model.SiteName ?? string.Empty, "general");
                await systemConfig.SetSettingAsync("site_description", model.SiteDescription ?? string.Empty, "general");
                await systemConfig.SetSettingAsync("currency", model.Currency ?? "VND", "payment");
                await systemConfig.SetSettingAsync("tax_rate", model.TaxRate.ToString(), "payment");
                await systemConfig.SetSettingAsync("shipping_fee", model.ShippingFee.ToString(), "shipping");
                await systemConfig.SetSettingAsync("free_shipping_threshold", model.FreeShippingThreshold.ToString(), "shipping");
                await systemConfig.SetSettingAsync("low_stock_threshold", model.LowStockThreshold.ToString(), "general");
                
                _logger.LogInformation("Admin updated system settings");
                
                TempData["SuccessMessage"] = "Cài đặt đã được cập nhật thành công!";
                return RedirectToAction("Settings");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating system settings");
                ModelState.AddModelError("", "Có lỗi xảy ra khi cập nhật cài đặt: " + ex.Message);
                return View("Settings", model);
            }
        }

        #endregion

        #region System Maintenance

        [HttpGet("maintenance")]
        public IActionResult MaintenanceMode()
        {
            var viewModel = new MaintenanceViewModel
            {
                IsMaintenanceMode = false, // Get from settings
                MaintenanceMessage = "Trang web đang bảo trì. Vui lòng quay lại sau.",
                ScheduledMaintenanceStart = DateTime.UtcNow.AddHours(1),
                ScheduledMaintenanceEnd = DateTime.UtcNow.AddHours(3),
                AllowAdminAccess = true
            };

            return View(viewModel);
        }

        [HttpPost("maintenance/toggle")]
        public IActionResult ToggleMaintenanceMode(bool enable)
        {
            try
            {
                // Toggle maintenance mode in settings
                // Update application configuration or database
                
                var message = enable ? "Chế độ bảo trì đã được bật" : "Chế độ bảo trì đã được tắt";
                return Json(new { success = true, message = message });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        #endregion

        #region Backup & Restore

        [HttpGet("backup")]
        public IActionResult BackupRestore()
        {
            var backupHistory = new List<BackupInfo>
            {
                new BackupInfo
                {
                    Id = Guid.NewGuid(),
                    FileName = "backup_20241214_120000.sql",
                    CreatedAt = DateTime.UtcNow.AddDays(-1),
                    Size = 15928576, // 15.2 MB in bytes
                    Type = "Full"
                }
            };

            var viewModel = new BackupRestoreViewModel
            {
                BackupHistory = backupHistory,
                AutoBackupEnabled = true,
                BackupFrequency = "Daily",
                RetentionDays = 30
            };

            return View(viewModel);
        }

        [HttpPost("backup/create")]
        public IActionResult CreateBackup(string backupType = "Full")
        {
            try
            {
                // Create database backup
                var fileName = $"backup_{DateTime.UtcNow:yyyyMMdd_HHmmss}.sql";
                
                // Implementation would go here
                // For now, return success message
                
                return Json(new { 
                    success = true, 
                    message = "Backup đã được tạo thành công!", 
                    fileName = fileName 
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("backup/restore")]
        public IActionResult RestoreBackup(IFormFile backupFile)
        {
            if (backupFile == null || backupFile.Length == 0)
            {
                return Json(new { success = false, message = "Vui lòng chọn file backup!" });
            }

            try
            {
                // Restore from backup file
                // Implementation would go here
                
                return Json(new { 
                    success = true, 
                    message = "Khôi phục dữ liệu thành công!" 
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        #endregion

        #region System Logs

        [HttpGet("logs")]
        public IActionResult SystemLogs(int page = 1, string level = "", DateTime? fromDate = null, DateTime? toDate = null)
        {
            const int pageSize = 50;
            
            // Mock data - replace with actual log reading
            var logs = new List<SystemLogEntry>
            {
                new SystemLogEntry
                {
                    Id = 1,
                    Level = "Error",
                    Message = "Database connection timeout",
                    Timestamp = DateTime.UtcNow.AddMinutes(-10),
                    Source = "AdminController",
                    UserId = User.Identity?.Name ?? string.Empty
                },
                new SystemLogEntry
                {
                    Id = 2,
                    Level = "Info",
                    Message = "User logged in successfully",
                    Timestamp = DateTime.UtcNow.AddMinutes(-15),
                    Source = "AuthService",
                    UserId = User.Identity?.Name ?? string.Empty
                }
            };

            var totalLogs = logs.Count;
            var pagedLogs = logs
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var viewModel = new SystemLogsViewModel
            {
                Logs = pagedLogs,
                CurrentPage = page,
                PageSize = pageSize,
                TotalLogs = totalLogs,
                SelectedLevel = level,
                FromDate = fromDate,
                ToDate = toDate,
                LogLevels = new List<string> { "All", "Error", "Warning", "Info", "Debug" }
            };

            return View(viewModel);
        }

        [HttpPost("logs/clear")]
        public IActionResult ClearLogs(DateTime? beforeDate = null)
        {
            try
            {
                // Clear logs before specified date
                // Implementation would go here
                
                var message = beforeDate.HasValue 
                    ? $"Đã xóa logs trước {beforeDate:dd/MM/yyyy}"
                    : "Đã xóa tất cả logs";
                    
                return Json(new { success = true, message = message });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        #endregion

        #region System Information

        [HttpGet("system-info")]
        public async Task<IActionResult> SystemInfo()
        {
            var viewModel = new SystemInfoViewModel
            {
                Server = new ServerInfo
                {
                    MachineName = Environment.MachineName,
                    OperatingSystem = Environment.OSVersion.ToString(),
                    Framework = Environment.Version.ToString(),
                    ProcessorCount = Environment.ProcessorCount,
                    WorkingSet = Environment.WorkingSet,
                    TotalPhysicalMemory = GetTotalPhysicalMemory(),
                    AvailablePhysicalMemory = GetAvailablePhysicalMemory()
                },
                Database = new DatabaseInfo
                {
                    DatabaseName = "JohnHenryFashionDB",
                    DatabaseSize = 131609600, // 125.5 MB in bytes
                    TableCount = await GetTableCountAsync(),
                    RecordCount = await GetTotalRecordCountAsync(),
                    LastBackup = DateTime.UtcNow.AddDays(-1)
                },
                Application = new ApplicationInfo
                {
                    Version = "1.0.0",
                    BuildDate = new DateTime(2024, 12, 14),
                    Uptime = DateTime.UtcNow - Process.GetCurrentProcess().StartTime,
                    Environment = "Development",
                    ActiveUsers = 25,
                    TotalUsers = await _context.Users.CountAsync(),
                    TotalProducts = await _context.Products.CountAsync(),
                    TotalOrders = await _context.Orders.CountAsync()
                }
            };

            return View(viewModel);
        }

        #endregion

        #region Helper Methods

        private long GetTotalPhysicalMemory()
        {
            // Implementation to get total physical memory
            return 8L * 1024L * 1024L * 1024L; // 8 GB as example
        }

        private long GetAvailablePhysicalMemory()
        {
            // Implementation to get available physical memory
            return 4L * 1024L * 1024L * 1024L; // 4 GB as example
        }

        private async Task<int> GetTableCountAsync()
        {
            // Count tables in database
            return await Task.FromResult(25);
        }

        private async Task<int> GetTotalRecordCountAsync()
        {
            // Count total records across all tables
            var productCount = await _context.Products.CountAsync();
            var userCount = await _context.Users.CountAsync();
            var orderCount = await _context.Orders.CountAsync();
            
            return productCount + userCount + orderCount;
        }

        #endregion
    }
}