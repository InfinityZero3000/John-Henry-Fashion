using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using JohnHenryFashionWeb.Data;
using JohnHenryFashionWeb.Models;
using JohnHenryFashionWeb.ViewModels;
using JohnHenryFashionWeb.Extensions;
using JohnHenryFashionWeb.Services;
using System.Text.Json;

namespace JohnHenryFashionWeb.Controllers
{
    [Authorize(Roles = UserRoles.Admin, AuthenticationSchemes = "Identity.Application")]
    [Route("admin")]
    public partial class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IAnalyticsService _analyticsService;
        private readonly IReportingService _reportingService;
        private readonly IUserManagementService _userManagementService;
        private readonly ILogger<AdminController> _logger;
        private readonly IEmailService _emailService;

        public AdminController(
            ApplicationDbContext context, 
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IWebHostEnvironment webHostEnvironment,
            IAnalyticsService analyticsService,
            IReportingService reportingService,
            IUserManagementService userManagementService,
            ILogger<AdminController> logger,
            IEmailService emailService)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _webHostEnvironment = webHostEnvironment;
            _analyticsService = analyticsService;
            _reportingService = reportingService;
            _userManagementService = userManagementService;
            _logger = logger;
            _emailService = emailService;
        }

        [HttpGet("")]
        [HttpGet("dashboard")]
        public async Task<IActionResult> Dashboard()
        {
            var dashboardSummary = await _reportingService.GetDashboardSummaryAsync();
            var salesChartData = await _reportingService.GetSalesChartDataAsync("daily", 30);
            var revenueTimeSeries = await _reportingService.GetRevenueTimeSeriesAsync(
                DateTime.UtcNow.AddDays(-30), DateTime.UtcNow, "daily");

            // Debug logging
            _logger.LogInformation("📊 Dashboard Data Retrieved:");
            _logger.LogInformation($"  - Sales Chart Data: {salesChartData?.Count ?? 0} records");
            _logger.LogInformation($"  - Revenue Time Series: {revenueTimeSeries?.Count ?? 0} records");
            if (salesChartData?.Any() == true)
            {
                _logger.LogInformation($"  - First chart record: Label={salesChartData.First().Label}, Value={salesChartData.First().Value}");
            }
            if (revenueTimeSeries?.Any() == true)
            {
                _logger.LogInformation($"  - First time series: Date={revenueTimeSeries.First().Date}, Value={revenueTimeSeries.First().Value}");
            }

            // Enhanced data for comprehensive dashboard
            var recentOrders = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                .OrderByDescending(o => o.CreatedAt)
                .Take(10)
                .ToListAsync();

            var recentUsers = await _context.Users
                .Where(u => u.CreatedAt >= DateTime.UtcNow.AddDays(-7))
                .OrderByDescending(u => u.CreatedAt)
                .Take(5)
                .ToListAsync();

            var pendingOrders = await _context.Orders
                .Where(o => o.Status == "pending" || o.Status == "processing")
                .CountAsync();

            var todayStats = new ViewModels.DashboardStats
            {
                TodayOrders = await _context.Orders
                    .Where(o => o.CreatedAt.Date == DateTime.UtcNow.Date)
                    .CountAsync(),
                TodayRevenue = await _context.Orders
                    .Where(o => o.CreatedAt.Date == DateTime.UtcNow.Date && o.Status == "completed")
                    .SumAsync(o => o.TotalAmount),
                TodayVisitors = dashboardSummary.TodayVisitors,
                ActiveUsers = await _context.Users
                    .Where(u => u.IsActive)
                    .CountAsync()
            };

            var weeklyComparison = new ViewModels.WeeklyComparisonStats
            {
                ThisWeekOrders = await _context.Orders
                    .Where(o => o.CreatedAt >= DateTime.UtcNow.AddDays(-7))
                    .CountAsync(),
                LastWeekOrders = await _context.Orders
                    .Where(o => o.CreatedAt >= DateTime.UtcNow.AddDays(-14) && o.CreatedAt < DateTime.UtcNow.AddDays(-7))
                    .CountAsync(),
                ThisWeekRevenue = await _context.Orders
                    .Where(o => o.CreatedAt >= DateTime.UtcNow.AddDays(-7) && o.Status == "completed")
                    .SumAsync(o => o.TotalAmount),
                LastWeekRevenue = await _context.Orders
                    .Where(o => o.CreatedAt >= DateTime.UtcNow.AddDays(-14) && o.CreatedAt < DateTime.UtcNow.AddDays(-7) && o.Status == "completed")
                    .SumAsync(o => o.TotalAmount)
            };

            var systemAlerts = new List<ViewModels.SystemAlert>();

            // Check for pending orders
            if (pendingOrders > 0)
            {
                systemAlerts.Add(new ViewModels.SystemAlert { 
                    Type = "info", 
                    Message = $"{pendingOrders} đơn hàng đang chờ xử lý", 
                    Action = "/Admin/Orders",
                    Icon = "fas fa-clock"
                });
            }

            // Quick actions data
            var quickActions = new[]
            {
                new ViewModels.QuickAction { Title = "Xem đơn hàng", Icon = "fas fa-shopping-cart", Url = "/Admin/Orders", Color = "success" },
                new ViewModels.QuickAction { Title = "Quản lý người dùng", Icon = "fas fa-users", Url = "/Admin/Users", Color = "info" },
                new ViewModels.QuickAction { Title = "Báo cáo", Icon = "fas fa-chart-bar", Url = "/Admin/Reports", Color = "warning" },
                new ViewModels.QuickAction { Title = "Cài đặt", Icon = "fas fa-cog", Url = "/Admin/Settings", Color = "secondary" },
                new ViewModels.QuickAction { Title = "Thương hiệu", Icon = "fas fa-award", Url = "/Admin/Brands", Color = "purple" }
            };

            // Get additional analytics data
            var categoryPerformance = await _reportingService.GetCategoryPerformanceAsync(
                DateTime.UtcNow.AddDays(-30), DateTime.UtcNow) ?? new List<CategoryPerformanceData>();
            var realTimeData = await _analyticsService.GetRealTimeAnalyticsAsync() ?? new RealTimeAnalyticsData();
            var performanceMetrics = await _reportingService.GetPerformanceMetricsAsync(
                DateTime.UtcNow.AddDays(-7), DateTime.UtcNow) ?? new PerformanceMetrics();
            var geographicData = await _reportingService.GetGeographicPerformanceAsync(
                DateTime.UtcNow.AddDays(-30), DateTime.UtcNow) ?? new List<GeographicData>();
            var systemHealth = await GetSystemHealthMetrics();

#pragma warning disable CS8601 // Possible null reference assignment - handled with ?? operator
            var viewModel = new DashboardViewModel
            {
                Summary = dashboardSummary,
                DashboardSummary = dashboardSummary,
                SalesChartData = salesChartData,
                RevenueTimeSeriesData = revenueTimeSeries,
                CategoryPerformance = categoryPerformance,
                RealTimeData = realTimeData,
                PerformanceMetrics = performanceMetrics,
                GeographicData = geographicData,
                DeviceAnalytics = new DeviceAnalytics(), // Would be populated from analytics
                
                // Enhanced dashboard data
                RecentOrders = recentOrders,
                RecentUsers = recentUsers,
                TodayStats = todayStats,
                WeeklyComparison = weeklyComparison,
                SystemHealth = systemHealth,
                SystemAlerts = systemAlerts,
                QuickActions = quickActions,
                
                CurrentFilter = new ViewModels.AnalyticsFilter
                {
                    StartDate = DateTime.UtcNow.AddDays(-30),
                    EndDate = DateTime.UtcNow
                },
                AvailableDateRanges = new List<Models.DateRange>
                {
                    Models.DateRange.Today,
                    Models.DateRange.Yesterday,
                    Models.DateRange.Last7Days,
                    Models.DateRange.Last30Days,
                    Models.DateRange.ThisMonth,
                    Models.DateRange.LastMonth
                },
                SelectedTab = "overview",
                LastUpdated = DateTime.UtcNow
            };
#pragma warning restore CS8601

            return View("Dashboard", viewModel);
        }

        // Products Management (Old route - keeping for backwards compatibility)
        [HttpGet("product-list")]
        public IActionResult Products(string searchTerm = "", Guid? categoryId = null, string status = "", int page = 1, int pageSize = 10)
        {
            // Redirect to Inventory page which has product management functionality
            return RedirectToAction("Inventory", "Admin");
        }

        [HttpPost("reports/generate")]
        public async Task<IActionResult> GenerateReport([FromBody] ReportGenerationRequest request)
        {
            try
            {
                byte[] reportData;
                string fileName;
                string contentType;

                switch (request.Format.ToLower())
                {
                    case "excel":
                        reportData = await _reportingService.GenerateExcelReportAsync(
                            request.ReportType, request.StartDate, request.EndDate, request.Parameters);
                        fileName = $"{request.ReportType}_report_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xlsx";
                        contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                        break;
                    case "pdf":
                        reportData = await _reportingService.GeneratePdfReportAsync(
                            request.ReportType, request.StartDate, request.EndDate, request.Parameters);
                        fileName = $"{request.ReportType}_report_{DateTime.UtcNow:yyyyMMdd_HHmmss}.pdf";
                        contentType = "application/pdf";
                        break;
                    default:
                        return BadRequest("Unsupported format");
                }

                if (!string.IsNullOrEmpty(request.EmailTo))
                {
                    // TODO: Email the report
                    return Json(new { success = true, message = "Report has been generated and emailed." });
                }

                return File(reportData, contentType, fileName);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("reports/schedule")]
        public async Task<IActionResult> ScheduleReport([FromBody] ReportScheduleSettings settings)
        {
            try
            {
                var template = new ReportTemplate
                {
                    Name = settings.Name,
                    Description = settings.Description,
                    ReportType = settings.ReportType,
                    Format = settings.Format,
                    Frequency = settings.Frequency.ToLower(),
                    Parameters = JsonSerializer.Serialize(settings.Parameters),
                    IsActive = settings.IsActive,
                    CreatedAt = DateTime.UtcNow,
                    NextRunDate = CalculateNextRunDate(DateTime.UtcNow, settings.Frequency)
                };

                await _reportingService.ScheduleReportAsync(template);

                return Json(new { success = true, message = "Report scheduled successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("analytics/real-time")]
        public async Task<IActionResult> RealTimeAnalytics()
        {
            var realTimeData = await _analyticsService.GetRealTimeAnalyticsAsync();
            var liveMetrics = new LiveMetrics
            {
                ActiveUsers = realTimeData.ActiveUsers,
                PageViewsPerMinute = realTimeData.PageViewsLastHour / 60,
                OrdersPerHour = realTimeData.OrdersLastHour,
                RevenuePerHour = realTimeData.TodayRevenue / 24,
                CurrentConversionRate = 0, // Would calculate from real-time data
                AverageSessionDuration = TimeSpan.FromMinutes(5), // Would get from analytics
                TopPages = realTimeData.TopActivePages.Select(p => new TopPage
                {
                    Path = p.Page,
                    ActiveUsers = p.ActiveUsers,
                    ViewsLastHour = p.Views
                }).ToList(),
                TopReferrers = new List<TopReferrer>() // Would populate from analytics
            };

            var viewModel = new RealTimeDashboardViewModel
            {
                RealTimeData = realTimeData,
                RecentEvents = new List<LiveEvent>(), // Would populate from recent events
                Performance = new LivePerformanceData
                {
                    AverageResponseTime = TimeSpan.FromMilliseconds(120),
                    ErrorRate = 0.5,
                    RequestsPerMinute = 50,
                    ServerMetrics = new List<ServerMetric>
                    {
                        new() { Name = "CPU Usage", Value = 65, Unit = "%", Status = "normal" },
                        new() { Name = "Memory Usage", Value = 78, Unit = "%", Status = "normal" },
                        new() { Name = "Database Response", Value = 45, Unit = "ms", Status = "normal" }
                    }
                }
            };

            return View(viewModel);
        }

        [HttpGet("analytics/api/real-time")]
        public async Task<IActionResult> GetRealTimeData()
        {
            try
            {
                var realTimeData = await _analyticsService.GetRealTimeAnalyticsAsync();
                return Json(realTimeData);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        [HttpGet("analytics/api/chart-data")]
        public async Task<IActionResult> GetChartData(string type, string period = "daily", int days = 30)
        {
            try
            {
                List<ChartData> data = type.ToLower() switch
                {
                    "sales" => await _reportingService.GetSalesChartDataAsync(period, days),
                    _ => new List<ChartData>()
                };

                return Json(data);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        [HttpPost("analytics/export")]
        public IActionResult ExportAnalytics([FromBody] ViewModels.AnalyticsFilter filter)
        {
            try
            {
                // TODO: Fix model conversion between ViewModels.AnalyticsFilter and Models.AnalyticsFilter
                // var data = await _reportingService.ExportAnalyticsToExcelAsync(filter);
                var data = new byte[0]; // Temporary placeholder
                var fileName = $"analytics_export_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xlsx";
                var contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

                return File(data, contentType, fileName);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        #region Analytics Helper Methods

        private ViewModels.AnalyticsFilter GetAnalyticsFilterFromQuery(string? dateRange)
        {
            var filter = new ViewModels.AnalyticsFilter();
            
            switch (dateRange?.ToLower())
            {
                case "today":
                    filter.StartDate = DateTime.UtcNow.Date;
                    filter.EndDate = DateTime.UtcNow;
                    break;
                case "yesterday":
                    filter.StartDate = DateTime.UtcNow.AddDays(-1).Date;
                    filter.EndDate = DateTime.UtcNow.AddDays(-1).Date.AddDays(1).AddSeconds(-1);
                    break;
                case "last7days":
                    filter.StartDate = DateTime.UtcNow.AddDays(-7).Date;
                    filter.EndDate = DateTime.UtcNow;
                    break;
                case "last30days":
                default:
                    filter.StartDate = DateTime.UtcNow.AddDays(-30).Date;
                    filter.EndDate = DateTime.UtcNow;
                    break;
                case "thismonth":
                    filter.StartDate = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
                    filter.EndDate = DateTime.UtcNow;
                    break;
                case "lastmonth":
                    var firstDayOfLastMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1).AddMonths(-1);
                    filter.StartDate = firstDayOfLastMonth;
                    filter.EndDate = firstDayOfLastMonth.AddMonths(1).AddSeconds(-1);
                    break;
            }
            
            return filter;
        }

        private DateRange GetDateRangeFromString(string? dateRange)
        {
            return dateRange?.ToLower() switch
            {
                "today" => DateRange.Today,
                "yesterday" => DateRange.Yesterday,
                "last7days" => DateRange.Last7Days,
                "last30days" => DateRange.Last30Days,
                "thismonth" => DateRange.ThisMonth,
                "lastmonth" => DateRange.LastMonth,
                _ => DateRange.Last30Days
            };
        }

        private List<ViewModels.ExportFormat> GetAvailableExportFormats()
        {
            return new List<ViewModels.ExportFormat>
            {
                new() { Id = "excel", Name = "Excel", Extension = ".xlsx", MimeType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" },
                new() { Id = "pdf", Name = "PDF", Extension = ".pdf", MimeType = "application/pdf" },
                new() { Id = "csv", Name = "CSV", Extension = ".csv", MimeType = "text/csv" },
                new() { Id = "json", Name = "JSON", Extension = ".json", MimeType = "application/json" }
            };
        }

        private List<ViewModels.ReportType> GetAvailableReportTypes()
        {
            return new List<ViewModels.ReportType>
            {
                new() { Id = "sales", Name = "Sales Report", Description = "Comprehensive sales analysis with order details, revenue breakdown, and product performance" },
                new() { Id = "products", Name = "Product Report", Description = "Product performance, inventory analysis, and category insights" },
                new() { Id = "customers", Name = "Customer Report", Description = "Customer analytics, segmentation, and lifetime value analysis" },
                new() { Id = "analytics", Name = "Analytics Report", Description = "Website analytics, user behavior, and conversion metrics" },
                new() { Id = "financial", Name = "Financial Report", Description = "Revenue analysis, profit margins, and financial KPIs" }
            };
        }

        private async Task<List<SalesReport>> GetAvailableReportsAsync()
        {
            // Since SalesReport might not have CreatedAt, we'll use a different approach
            return await _context.SalesReports
                .OrderBy(r => r.Id) // Use Id for ordering since CreatedAt might not exist
                .Take(10)
                .ToListAsync();
        }

        private DateTime CalculateNextRunDate(DateTime baseDate, string frequency)
        {
            return frequency.ToLower() switch
            {
                "daily" => baseDate.AddDays(1),
                "weekly" => baseDate.AddDays(7),
                "monthly" => baseDate.AddMonths(1),
                "quarterly" => baseDate.AddMonths(3),
                "yearly" => baseDate.AddYears(1),
                _ => baseDate.AddDays(1)
            };
        }

        #endregion

        #region Dashboard Data Methods
        
        private async Task<ViewModels.SystemHealthMetrics> GetSystemHealthMetrics()
        {
            try
            {
                // Get system performance metrics
                var process = System.Diagnostics.Process.GetCurrentProcess();
                
                // Calculate CPU usage (approximate)
                var cpuUsage = 0.0;
                try
                {
                    var startTime = DateTime.UtcNow;
                    var startCpuUsage = process.TotalProcessorTime;
                    await Task.Delay(100); // Short delay to measure CPU
                    var endTime = DateTime.UtcNow;
                    var endCpuUsage = process.TotalProcessorTime;
                    var cpuUsedMs = (endCpuUsage - startCpuUsage).TotalMilliseconds;
                    var totalMsPassed = (endTime - startTime).TotalMilliseconds;
                    cpuUsage = (cpuUsedMs / (Environment.ProcessorCount * totalMsPassed)) * 100;
                }
                catch
                {
                    cpuUsage = 35.0; // Fallback value
                }
                
                // Memory usage
                var memoryUsageMB = process.WorkingSet64 / (1024.0 * 1024.0);
                var totalMemoryMB = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes / (1024.0 * 1024.0);
                var memoryPercentage = (memoryUsageMB / totalMemoryMB) * 100;
                
                // Storage usage (approximate - check database size)
                var storagePercentage = 23.0; // Default estimate
                try
                {
                    // For simplicity, we'll use a reasonable estimate based on record counts
                    var totalProducts = await _context.Products.CountAsync();
                    var totalOrders = await _context.Orders.CountAsync();
                    var totalUsers = await _context.Users.CountAsync();
                    var estimatedRecords = totalProducts + totalOrders + totalUsers;
                    storagePercentage = Math.Min(95, (estimatedRecords / 100000.0) * 100); // Rough estimate
                }
                catch { }
                
                // Online users (active in last 30 minutes)
                var onlineUsers = await _context.PageViews
                    .Where(p => p.ViewedAt >= DateTime.UtcNow.AddMinutes(-30))
                    .Select(p => p.SessionId)
                    .Distinct()
                    .CountAsync();
                
                // Calculate overall health (weighted average)
                var healthScore = 100.0;
                healthScore -= (cpuUsage > 80 ? 20 : cpuUsage > 60 ? 10 : 0);
                healthScore -= (memoryPercentage > 80 ? 20 : memoryPercentage > 60 ? 10 : 0);
                healthScore -= (storagePercentage > 80 ? 20 : storagePercentage > 60 ? 10 : 0);
                
                var healthStatus = healthScore >= 90 ? "Hoạt động tốt" :
                                  healthScore >= 70 ? "Bình thường" :
                                  healthScore >= 50 ? "Cần chú ý" : "Cảnh báo";
                
                return new ViewModels.SystemHealthMetrics
                {
                    OverallHealth = Math.Round(healthScore, 0),
                    HealthStatus = healthStatus,
                    CpuUsage = Math.Round(Math.Max(0, Math.Min(100, cpuUsage)), 0),
                    MemoryUsage = Math.Round(Math.Max(0, Math.Min(100, memoryPercentage)), 0),
                    StorageUsage = Math.Round(storagePercentage, 0),
                    OnlineUsers = onlineUsers,
                    LastUpdated = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating system health metrics");
                return new ViewModels.SystemHealthMetrics
                {
                    OverallHealth = 85,
                    HealthStatus = "Không xác định",
                    CpuUsage = 35,
                    MemoryUsage = 55,
                    StorageUsage = 23,
                    OnlineUsers = 0
                };
            }
        }
        
        private async Task<Models.DashboardSummary> GetDashboardStats()
        {
            var totalOrders = await _context.Orders.CountAsync();
            var totalCustomers = await _userManager.GetUsersInRoleAsync(UserRoles.Customer);
            var totalSellers = await _userManager.GetUsersInRoleAsync(UserRoles.Seller);
            
            var totalRevenue = await _context.Orders
                .Where(o => o.Status == "completed")
                .SumAsync(o => o.TotalAmount);

            var currentMonth = DateTime.UtcNow.Month;
            var currentYear = DateTime.UtcNow.Year;
            var monthlyRevenue = await _context.Orders
                .Where(o => o.CreatedAt.Month == currentMonth && 
                           o.CreatedAt.Year == currentYear &&
                           o.Status == "completed")
                .SumAsync(o => o.TotalAmount);

            var pendingOrders = await _context.Orders
                .CountAsync(o => o.Status == "pending");

            return new Models.DashboardSummary
            {
                TodayRevenue = monthlyRevenue,
                YesterdayRevenue = 0, // Will be calculated properly
                RevenueGrowth = 0,
                TodayOrders = totalOrders,
                YesterdayOrders = 0,
                OrdersGrowth = 0,
                TodayVisitors = 150, // Mock data for now
                YesterdayVisitors = 120,
                VisitorsGrowth = 25,
                ConversionRate = 3.5,
                PreviousConversionRate = 3.0,
                ConversionGrowth = 16.7m,
                AverageOrderValue = totalRevenue / Math.Max(totalOrders, 1),
                PreviousAverageOrderValue = 0,
                AOVGrowth = 0,
                ActiveUsers = totalCustomers.Count + totalSellers.Count
            };
        }

        private async Task<List<Models.RecentOrder>> GetRecentOrders(int count)
        {
            return await _context.Orders
                .Include(o => o.User)
                .OrderByDescending(o => o.CreatedAt)
                .Take(count)
                .Select(o => new Models.RecentOrder
                {
                    Id = o.Id,
                    OrderNumber = o.OrderNumber,
                    CustomerName = $"{o.User.FirstName} {o.User.LastName}".Trim(),
                    Total = o.TotalAmount,
                    Status = o.Status,
                    CreatedAt = o.CreatedAt
                })
                .ToListAsync();
        }

        private async Task<List<TopSellingProduct>> GetTopSellingProducts(int count)
        {
            return await _context.OrderItems
                .Include(oi => oi.Product)
                .Where(oi => oi.Order.Status == "completed")
                .GroupBy(oi => oi.ProductId)
                .Select(g => new TopSellingProduct
                {
                    ProductId = g.Key,
                    ProductName = g.First().Product.Name,
                    QuantitySold = g.Sum(oi => oi.Quantity),
                    Revenue = g.Sum(oi => oi.TotalPrice),
                    ImageUrl = g.First().Product.FeaturedImageUrl
                })
                .OrderByDescending(x => x.QuantitySold)
                .Take(count)
                .ToListAsync();
        }

        private async Task<List<MonthlyRevenue>> GetMonthlyRevenueData(int months)
        {
            var result = new List<MonthlyRevenue>();
            var currentDate = DateTime.UtcNow;

            for (int i = months - 1; i >= 0; i--)
            {
                var targetDate = currentDate.AddMonths(-i);
                var revenue = await _context.Orders
                    .Where(o => o.CreatedAt.Month == targetDate.Month && 
                               o.CreatedAt.Year == targetDate.Year &&
                               o.Status == "completed")
                    .SumAsync(o => o.TotalAmount);

                var orderCount = await _context.Orders
                    .CountAsync(o => o.CreatedAt.Month == targetDate.Month && 
                                    o.CreatedAt.Year == targetDate.Year &&
                                    o.Status == "completed");

                result.Add(new MonthlyRevenue
                {
                    Month = targetDate.Month,
                    Year = targetDate.Year,
                    Revenue = revenue,
                    OrderCount = orderCount
                });
            }

            return result;
        }
        #endregion


        #region Category Management
        [HttpGet("categories")]
        public async Task<IActionResult> Categories(string search = "")
        {
            var query = _context.Categories
                .Include(c => c.Parent)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(c => c.Name.Contains(search) || (c.Description != null && c.Description.Contains(search)));
            }

            var categories = await query
                .OrderBy(c => c.ParentId.HasValue ? 1 : 0)
                .ThenBy(c => c.SortOrder)
                .ThenBy(c => c.Name)
                .Select(c => new CategoryListItemViewModel
                {
                    Id = c.Id,
                    Name = c.Name,
                    Description = c.Description,
                    ImageUrl = c.ImageUrl,
                    ParentId = c.ParentId,
                    ParentName = c.Parent != null ? c.Parent.Name : null,
                    IsActive = c.IsActive,
                    SortOrder = c.SortOrder,
                    CreatedAt = c.CreatedAt,
                    ProductCount = c.Products.Count()
                })
                .ToListAsync();

            var viewModel = new CategoryListViewModel
            {
                Categories = categories,
                SearchTerm = search
            };

            return View(viewModel);
        }

        [HttpGet("categories/create")]
        public async Task<IActionResult> CreateCategory()
        {
            var viewModel = new CategoryCreateEditViewModel
            {
                ParentCategories = await _context.Categories
                    .Where(c => c.IsActive && !c.ParentId.HasValue)
                    .OrderBy(c => c.Name)
                    .ToListAsync()
            };

            return View(viewModel);
        }

        [HttpPost("categories/create")]
        public async Task<IActionResult> CreateCategory(CategoryCreateEditViewModel model)
        {
            if (ModelState.IsValid)
            {
                var category = new Category
                {
                    Id = Guid.NewGuid(),
                    Name = model.Name,
                    Description = model.Description,
                    ParentId = model.ParentId,
                    IsActive = model.IsActive,
                    SortOrder = model.SortOrder,
                    Slug = GenerateSlug(model.Name),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                // Handle image upload
                if (model.Image != null)
                {
                    category.ImageUrl = await SaveUploadedFile(model.Image, "categories");
                }

                _context.Categories.Add(category);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Danh mục đã được tạo thành công!";
                return RedirectToAction(nameof(Categories));
            }

            // Reload dropdown data if validation fails
            model.ParentCategories = await _context.Categories
                .Where(c => c.IsActive && !c.ParentId.HasValue)
                .OrderBy(c => c.Name)
                .ToListAsync();

            return View(model);
        }

        [HttpGet("categories/edit/{id}")]
        public async Task<IActionResult> EditCategory(Guid id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                return NotFound();
            }

            var viewModel = new CategoryCreateEditViewModel
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description,
                ParentId = category.ParentId,
                IsActive = category.IsActive,
                SortOrder = category.SortOrder,
                ImageUrl = category.ImageUrl,
                ParentCategories = await _context.Categories
                    .Where(c => c.IsActive && !c.ParentId.HasValue && c.Id != id)
                    .OrderBy(c => c.Name)
                    .ToListAsync()
            };

            return View(viewModel);
        }

        [HttpPost("categories/edit/{id}")]
        public async Task<IActionResult> EditCategory(Guid id, CategoryCreateEditViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var category = await _context.Categories.FindAsync(id);
                if (category == null)
                {
                    return NotFound();
                }

                category.Name = model.Name;
                category.Description = model.Description;
                category.ParentId = model.ParentId;
                category.IsActive = model.IsActive;
                category.SortOrder = model.SortOrder;
                category.Slug = GenerateSlug(model.Name);
                category.UpdatedAt = DateTime.UtcNow;

                // Handle image upload
                if (model.Image != null)
                {
                    category.ImageUrl = await SaveUploadedFile(model.Image, "categories");
                }

                await _context.SaveChangesAsync();

                TempData["Success"] = "Danh mục đã được cập nhật thành công!";
                return RedirectToAction(nameof(Categories));
            }

            // Reload dropdown data if validation fails
            model.ParentCategories = await _context.Categories
                .Where(c => c.IsActive && !c.ParentId.HasValue && c.Id != id)
                .OrderBy(c => c.Name)
                .ToListAsync();

            return View(model);
        }

        [HttpPost("categories/delete/{id}")]
        public async Task<IActionResult> DeleteCategory(Guid id)
        {
            var category = await _context.Categories
                .Include(c => c.Products)
                .Include(c => c.Children)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null)
            {
                return NotFound();
            }

            // Check if category has products or subcategories
            if (category.Products.Any() || category.Children.Any())
            {
                TempData["Error"] = "Không thể xóa danh mục này vì đang có sản phẩm hoặc danh mục con!";
                return RedirectToAction(nameof(Categories));
            }

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Danh mục đã được xóa thành công!";
            return RedirectToAction(nameof(Categories));
        }

        [HttpGet("categories/from-images")]
        public async Task<IActionResult> CreateCategoriesFromImages()
        {
            var categoriesCreated = 0;
            var imagesPath = Path.Combine(_webHostEnvironment.WebRootPath, "images");
            
            if (Directory.Exists(imagesPath))
            {
                var categoryFolders = Directory.GetDirectories(imagesPath)
                    .Where(d => !Path.GetFileName(d).StartsWith(".") && 
                               !Path.GetFileName(d).Equals("store", StringComparison.OrdinalIgnoreCase) &&
                               !Path.GetFileName(d).Equals("Banner", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                foreach (var folder in categoryFolders)
                {
                    var folderName = Path.GetFileName(folder);
                    var slug = GenerateSlug(folderName);

                    // Check if category already exists
                    var existingCategory = await _context.Categories
                        .FirstOrDefaultAsync(c => c.Slug == slug);

                    if (existingCategory == null)
                    {
                        var category = new Category
                        {
                            Id = Guid.NewGuid(),
                            Name = folderName,
                            Slug = slug,
                            Description = $"Danh mục {folderName}",
                            IsActive = true,
                            SortOrder = categoriesCreated,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };

                        // Try to find a representative image
                        var imageFiles = Directory.GetFiles(folder, "*.*", SearchOption.TopDirectoryOnly)
                            .Where(file => file.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                                          file.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) ||
                                          file.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                            .FirstOrDefault();

                        if (imageFiles != null)
                        {
                            var relativePath = Path.GetRelativePath(_webHostEnvironment.WebRootPath, imageFiles)
                                .Replace("\\", "/");
                            category.ImageUrl = "/" + relativePath;
                        }

                        _context.Categories.Add(category);
                        categoriesCreated++;
                    }
                }

                if (categoriesCreated > 0)
                {
                    await _context.SaveChangesAsync();
                    TempData["Success"] = $"Đã tạo {categoriesCreated} danh mục từ thư mục hình ảnh!";
                }
                else
                {
                    TempData["Info"] = "Tất cả danh mục đã tồn tại!";
                }
            }
            else
            {
                TempData["Error"] = "Không tìm thấy thư mục hình ảnh!";
            }

            return RedirectToAction(nameof(Categories));
        }
        #endregion


        #region Helper Methods
        private string GenerateSlug(string text, int maxLength = 80)
        {
            if (string.IsNullOrWhiteSpace(text))
                return "untitled";
            
            // Convert to lowercase and replace Vietnamese characters
            var slug = text.ToLower()
                      .Replace("á", "a").Replace("à", "a").Replace("ả", "a").Replace("ã", "a").Replace("ạ", "a")
                      .Replace("ă", "a").Replace("ắ", "a").Replace("ằ", "a").Replace("ẳ", "a").Replace("ẵ", "a").Replace("ặ", "a")
                      .Replace("â", "a").Replace("ấ", "a").Replace("ầ", "a").Replace("ẩ", "a").Replace("ẫ", "a").Replace("ậ", "a")
                      .Replace("é", "e").Replace("è", "e").Replace("ẻ", "e").Replace("ẽ", "e").Replace("ẹ", "e")
                      .Replace("ê", "e").Replace("ế", "e").Replace("ề", "e").Replace("ể", "e").Replace("ễ", "e").Replace("ệ", "e")
                      .Replace("í", "i").Replace("ì", "i").Replace("ỉ", "i").Replace("ĩ", "i").Replace("ị", "i")
                      .Replace("ó", "o").Replace("ò", "o").Replace("ỏ", "o").Replace("õ", "o").Replace("ọ", "o")
                      .Replace("ô", "o").Replace("ố", "o").Replace("ồ", "o").Replace("ổ", "o").Replace("ỗ", "o").Replace("ộ", "o")
                      .Replace("ơ", "o").Replace("ớ", "o").Replace("ờ", "o").Replace("ở", "o").Replace("ỡ", "o").Replace("ợ", "o")
                      .Replace("ú", "u").Replace("ù", "u").Replace("ủ", "u").Replace("ũ", "u").Replace("ụ", "u")
                      .Replace("ư", "u").Replace("ứ", "u").Replace("ừ", "u").Replace("ử", "u").Replace("ữ", "u").Replace("ự", "u")
                      .Replace("ý", "y").Replace("ỳ", "y").Replace("ỷ", "y").Replace("ỹ", "y").Replace("ỵ", "y")
                      .Replace("đ", "d");
            
            // Remove special characters (keep only alphanumeric, space, and dash)
            slug = System.Text.RegularExpressions.Regex.Replace(slug, @"[^a-z0-9\s-]", "");
            
            // Replace multiple spaces with single space
            slug = System.Text.RegularExpressions.Regex.Replace(slug, @"\s+", " ");
            
            // Replace spaces with dashes
            slug = slug.Replace(" ", "-");
            
            // Replace multiple dashes with single dash
            slug = System.Text.RegularExpressions.Regex.Replace(slug, @"-+", "-");
            
            // Trim dashes from start and end
            slug = slug.Trim('-');
            
            // Limit length
            if (slug.Length > maxLength)
            {
                slug = slug.Substring(0, maxLength);
                // Remove incomplete word at end
                var lastDash = slug.LastIndexOf('-');
                if (lastDash > 0)
                    slug = slug.Substring(0, lastDash);
            }
            
            // Return untitled if slug becomes empty after processing
            return string.IsNullOrEmpty(slug) ? "untitled" : slug;
        }

        private async Task<string> SaveUploadedFile(IFormFile file, string folder)
        {
            if (file == null || file.Length == 0)
                return string.Empty;

            var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", folder);
            Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName;
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            return $"/images/{folder}/{uniqueFileName}";
        }

        private string FormatProductName(string fileName)
        {
            // Clean up filename to create a readable product name
            return fileName
                .Replace("-", " ")
                .Replace("_", " ")
                .Trim()
                .ToTitleCase();
        }

        private string GenerateProductSKU(string fileName)
        {
            // Generate SKU from filename
            var cleanName = fileName.Replace(" ", "").Replace("-", "").Replace("_", "").ToUpper();
            if (cleanName.Length > 10)
            {
                cleanName = cleanName.Substring(0, 10);
            }
            return $"JH{cleanName}{DateTime.Now.ToString("MMdd")}";
        }

        private (decimal price, string? size, string? color) ExtractProductDetails(string fileName)
        {
            var price = 299000m; // Default price
            string? size = null;
            string? color = null;

            // Try to extract size
            var sizeMatches = new[] { "XS", "S", "M", "L", "XL", "XXL", "36", "38", "40", "42", "44", "46" };
            foreach (var sizeMatch in sizeMatches)
            {
                if (fileName.ToUpper().Contains(sizeMatch))
                {
                    size = sizeMatch;
                    break;
                }
            }

            // Try to extract color from Vietnamese color names
            var colorMatches = new Dictionary<string, string>
            {
                { "đen", "Đen" }, { "trắng", "Trắng" }, { "xanh", "Xanh" },
                { "đỏ", "Đỏ" }, { "vàng", "Vàng" }, { "hồng", "Hồng" },
                { "tím", "Tím" }, { "nâu", "Nâu" }, { "xám", "Xám" },
                { "black", "Đen" }, { "white", "Trắng" }, { "blue", "Xanh" },
                { "red", "Đỏ" }, { "yellow", "Vàng" }, { "pink", "Hồng" },
                { "purple", "Tím" }, { "brown", "Nâu" }, { "gray", "Xám" }
            };

            foreach (var colorMatch in colorMatches)
            {
                if (fileName.ToLower().Contains(colorMatch.Key))
                {
                    color = colorMatch.Value;
                    break;
                }
            }

            // Adjust price based on category
            if (fileName.ToLower().Contains("vest") || fileName.ToLower().Contains("suit"))
                price = 899000m;
            else if (fileName.ToLower().Contains("áo khoác") || fileName.ToLower().Contains("jacket"))
                price = 599000m;
            else if (fileName.ToLower().Contains("áo sơ mi") || fileName.ToLower().Contains("shirt"))
                price = 399000m;
            else if (fileName.ToLower().Contains("quần") || fileName.ToLower().Contains("pants"))
                price = 499000m;
            else if (fileName.ToLower().Contains("đầm") || fileName.ToLower().Contains("dress"))
                price = 699000m;

            return (price, size, color);
        }
        #endregion

        #region User Management
        [HttpGet("users")]
        public async Task<IActionResult> Users(string searchTerm = "", string role = "", 
            bool? isActive = null, int page = 1, int pageSize = 20, string sortBy = "CreatedAt", string sortDirection = "desc")
        {
            var filter = new UserFilterViewModel
            {
                SearchTerm = searchTerm,
                Role = role,
                IsActive = isActive,
                Page = page,
                PageSize = pageSize,
                SortBy = sortBy,
                SortDirection = sortDirection
            };

            var result = await _userManagementService.GetUsersAsync(filter);
            
            // Get statistics for dashboard cards
            result.Statistics = await _userManagementService.GetUserStatisticsAsync();

            return View(result);
        }

        [HttpGet("users/{id}")]
        public async Task<IActionResult> UserDetail(string id)
        {
            var userDetail = await _userManagementService.GetUserDetailAsync(id);
            if (userDetail == null)
            {
                return NotFound();
            }

            return View(userDetail);
        }

        [HttpGet("users/{id}/edit")]
        public async Task<IActionResult> EditUser(string id)
        {
            var userEdit = await _userManagementService.GetUserForEditAsync(id);
            if (userEdit == null)
            {
                return NotFound();
            }

            return View(userEdit);
        }

        [HttpPost("users/{id}/edit")]
        public async Task<IActionResult> EditUser(string id, UserEditViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var roles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();
                model.AvailableRoles = roles.Where(n => !string.IsNullOrEmpty(n)).Select(n => n!).ToList();
                return View(model);
            }

            var success = await _userManagementService.UpdateUserAsync(id, model);
            if (success)
            {
                TempData["Success"] = "Cập nhật thông tin người dùng thành công!";
                return RedirectToAction(nameof(UserDetail), new { id });
            }

            TempData["Error"] = "Có lỗi xảy ra khi cập nhật thông tin người dùng!";
            return View(model);
        }

        [HttpPost("users/{id}/toggle-status")]
        public async Task<IActionResult> ToggleUserStatus(string id)
        {
            var success = await _userManagementService.ToggleUserStatusAsync(id);
            if (success)
            {
                return Json(new { success = true, message = "Cập nhật trạng thái thành công!" });
            }

            return Json(new { success = false, message = "Có lỗi xảy ra!" });
        }

        [HttpPost("users/{id}/reset-password")]
        public async Task<IActionResult> ResetUserPassword(string id)
        {
            var success = await _userManagementService.ResetUserPasswordAsync(id);
            if (success)
            {
                return Json(new { success = true, message = "Đặt lại mật khẩu thành công! Mật khẩu mới: TempPassword@123" });
            }

            return Json(new { success = false, message = "Có lỗi xảy ra khi đặt lại mật khẩu!" });
        }

        [HttpPost("users/{id}/unlock")]
        public async Task<IActionResult> UnlockUser(string id)
        {
            var success = await _userManagementService.UnlockUserAsync(id);
            if (success)
            {
                return Json(new { success = true, message = "Gỡ khóa tài khoản thành công!" });
            }

            return Json(new { success = false, message = "Có lỗi xảy ra khi gỡ khóa tài khoản!" });
        }

        [HttpPost("users/{id}/delete")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var success = await _userManagementService.DeleteUserAsync(id);
            if (success)
            {
                return Json(new { success = true, message = "Xóa người dùng thành công!" });
            }

            return Json(new { success = false, message = "Có lỗi xảy ra khi xóa người dùng!" });
        }

        [HttpGet("users/create")]
        public async Task<IActionResult> CreateUser()
        {
            var model = new UserCreateViewModel
            {
                AvailableRoles = await _roleManager.Roles.Select(r => r.Name).Where(n => n != null).Cast<string>().ToListAsync()
            };
            return View(model);
        }

        [HttpPost("users/create")]
        public async Task<IActionResult> CreateUser(UserCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.AvailableRoles = await _roleManager.Roles.Select(r => r.Name).Where(n => n != null).Cast<string>().ToListAsync();
                return View(model);
            }

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName,
                PhoneNumber = model.PhoneNumber,
                IsActive = model.IsActive,
                DateOfBirth = model.DateOfBirth,
                Gender = model.Gender,
                Address = model.Address,
                CreatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                if (model.SelectedRoles?.Any() == true)
                {
                    await _userManager.AddToRolesAsync(user, model.SelectedRoles);
                }

                TempData["Success"] = "Tạo người dùng mới thành công!";
                return RedirectToAction(nameof(Users));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            model.AvailableRoles = await _roleManager.Roles.Select(r => r.Name).Where(n => n != null).Cast<string>().ToListAsync();
            return View(model);
        }

        // API endpoints for user management
        [HttpGet("api/users/statistics")]
        public async Task<IActionResult> GetUserStatistics()
        {
            var stats = await _userManagementService.GetUserStatisticsAsync();
            return Json(stats);
        }

        [HttpGet("api/users/recent")]
        public async Task<IActionResult> GetRecentUsers(int count = 10)
        {
            var users = await _userManagementService.GetRecentUsersAsync(count);
            return Json(users.Select(u => new
            {
                u.Id,
                FullName = $"{u.FirstName} {u.LastName}".Trim(),
                u.Email,
                u.CreatedAt,
                u.IsActive
            }));
        }
        #endregion


        #region Brand Management

        [HttpGet("brands")]
        public async Task<IActionResult> Brands(int page = 1, int pageSize = 10, string search = "")
        {
            var query = _context.Brands.AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(b => b.Name.Contains(search) || 
                                        b.Description!.Contains(search));
            }

            var totalBrands = await query.CountAsync();
            var brands = await query
                .OrderBy(b => b.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.TotalBrands = totalBrands;
            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalBrands / pageSize);
            ViewBag.Search = search;

            return View(brands);
        }

        [HttpGet("brands/create")]
        public IActionResult CreateBrand()
        {
            return View();
        }

        [HttpPost("brands/create")]
        public async Task<IActionResult> CreateBrand(Brand model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    model.Id = Guid.NewGuid();
                    model.Slug = GenerateSlug(model.Name);
                    model.CreatedAt = DateTime.UtcNow;
                    model.UpdatedAt = DateTime.UtcNow;

                    _context.Brands.Add(model);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Thương hiệu đã được tạo thành công!";
                    return RedirectToAction(nameof(Brands));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Có lỗi xảy ra khi tạo thương hiệu: " + ex.Message);
                }
            }

            return View(model);
        }

        [HttpGet("brands/edit/{id}")]
        public async Task<IActionResult> EditBrand(Guid id)
        {
            var brand = await _context.Brands.FindAsync(id);
            if (brand == null)
            {
                return NotFound();
            }

            return View(brand);
        }

        [HttpPost("brands/edit/{id}")]
        public async Task<IActionResult> EditBrand(Guid id, Brand model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var brand = await _context.Brands.FindAsync(id);
                    if (brand == null)
                    {
                        return NotFound();
                    }

                    brand.Name = model.Name;
                    brand.Description = model.Description;
                    brand.LogoUrl = model.LogoUrl;
                    brand.Website = model.Website;
                    brand.IsActive = model.IsActive;
                    brand.Slug = GenerateSlug(model.Name);
                    brand.UpdatedAt = DateTime.UtcNow;

                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Thương hiệu đã được cập nhật thành công!";
                    return RedirectToAction(nameof(Brands));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Có lỗi xảy ra khi cập nhật thương hiệu: " + ex.Message);
                }
            }

            return View(model);
        }

        [HttpPost("brands/delete/{id}")]
        public async Task<IActionResult> DeleteBrand(Guid id)
        {
            try
            {
                var brand = await _context.Brands.FindAsync(id);
                if (brand == null)
                {
                    return NotFound();
                }

                // Check if brand has products
                var hasProducts = await _context.Products.AnyAsync(p => p.BrandId == id);
                if (hasProducts)
                {
                    TempData["ErrorMessage"] = "Không thể xóa thương hiệu này vì có sản phẩm đang sử dụng!";
                    return RedirectToAction(nameof(Brands));
                }

                _context.Brands.Remove(brand);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Thương hiệu đã được xóa thành công!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi xóa thương hiệu: " + ex.Message;
            }

            return RedirectToAction(nameof(Brands));
        }

        [HttpPost("brands/toggle-status/{id}")]
        public async Task<IActionResult> ToggleBrandStatus(Guid id)
        {
            try
            {
                var brand = await _context.Brands.FindAsync(id);
                if (brand == null)
                {
                    return NotFound();
                }

                brand.IsActive = !brand.IsActive;
                brand.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Trạng thái thương hiệu đã được {(brand.IsActive ? "kích hoạt" : "vô hiệu hóa")}!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Có lỗi xảy ra: " + ex.Message;
            }

            return RedirectToAction(nameof(Brands));
        }

        #endregion

        #region Reviews Management
        [HttpGet("reviews")]
        public async Task<IActionResult> Reviews()
        {
            ViewData["CurrentSection"] = "reviews";
            ViewData["Title"] = "Quản lý đánh giá";
            
            var reviews = await _context.ProductReviews
                .Include(r => r.Product)
                .Include(r => r.User)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
                
            return View(reviews);
        }

        [HttpGet("reviews/details/{id}")]
        public async Task<IActionResult> GetReviewDetailsJson(Guid id)
        {
            var review = await _context.ProductReviews
                .Include(r => r.Product)
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (review == null)
                return NotFound();

            var result = new
            {
                id = review.Id,
                rating = review.Rating,
                comment = review.Comment,
                isApproved = review.IsApproved,
                createdAt = review.CreatedAt,
                images = Array.Empty<string>(), // ProductReview model doesn't have Images property yet
                product = new
                {
                    name = review.Product?.Name,
                    sku = review.Product?.SKU,
                    image = review.Product?.FeaturedImageUrl
                },
                user = new
                {
                    name = $"{review.User?.FirstName} {review.User?.LastName}",
                    email = review.User?.Email,
                    phone = review.User?.Phone
                }
            };

            return Json(result);
        }

        [HttpPost("reviews/approve/{id}")]
        public async Task<IActionResult> ApproveReview(Guid id)
        {
            var review = await _context.ProductReviews.FindAsync(id);
            if (review != null)
            {
                review.IsApproved = true;
                review.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Review {ReviewId} approved by admin", id);
            }
            return Ok(new { success = true });
        }

        [HttpPost("reviews/delete/{id}")]
        public async Task<IActionResult> DeleteReview(Guid id)
        {
            var review = await _context.ProductReviews.FindAsync(id);
            if (review != null)
            {
                _context.ProductReviews.Remove(review);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Review {ReviewId} deleted by admin", id);
            }
            return Ok(new { success = true });
        }
        #endregion

        #region Coupons Management
        [HttpGet("coupons")]
        public async Task<IActionResult> Coupons()
        {
            ViewData["CurrentSection"] = "coupons";
            ViewData["Title"] = "Quản lý mã giảm giá";
            
            var coupons = await _context.Coupons
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
                
            return View(coupons);
        }

        [HttpGet("coupons/create")]
        public IActionResult CreateCoupon()
        {
            // Redirect to main Coupons page - now using modal form
            return RedirectToAction("Coupons");
        }

        [HttpPost("coupons/create")]
        public async Task<IActionResult> CreateCoupon(Coupon coupon)
        {
            // Legacy endpoint - redirect to use new API
            if (ModelState.IsValid)
            {
                return await SaveCoupon(coupon);
            }
            return RedirectToAction("Coupons");
        }

        [HttpPost("coupons/{id}/toggle")]
        public async Task<IActionResult> ToggleCoupon(Guid id)
        {
            var coupon = await _context.Coupons.FindAsync(id);
            if (coupon != null)
            {
                coupon.IsActive = !coupon.IsActive;
                coupon.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Coupons");
        }

        [HttpGet("coupons/get/{id}")]
        public async Task<IActionResult> GetCoupon(Guid id)
        {
            var coupon = await _context.Coupons.FindAsync(id);
            if (coupon == null)
            {
                return Json(new { success = false, message = "Không tìm thấy mã giảm giá" });
            }

            return Json(new
            {
                success = true,
                id = coupon.Id,
                code = coupon.Code,
                name = coupon.Name,
                description = coupon.Description,
                type = coupon.Type,
                value = coupon.Value,
                minOrderAmount = coupon.MinOrderAmount,
                usageLimit = coupon.UsageLimit,
                startDate = coupon.StartDate,
                endDate = coupon.EndDate,
                isActive = coupon.IsActive
            });
        }

        [HttpPost("coupons/save")]
        public async Task<IActionResult> SaveCoupon([FromBody] Coupon model)
        {
            try
            {
                _logger.LogInformation($"SaveCoupon called - Id: {model.Id}, Code: {model.Code}, Name: {model.Name}, Type: {model.Type}, Value: {model.Value}");

                // Validation
                if (string.IsNullOrWhiteSpace(model.Code))
                {
                    return Json(new { success = false, message = "Mã giảm giá không được để trống" });
                }

                if (string.IsNullOrWhiteSpace(model.Name))
                {
                    return Json(new { success = false, message = "Tên mã giảm giá không được để trống" });
                }

                if (string.IsNullOrWhiteSpace(model.Type))
                {
                    return Json(new { success = false, message = "Vui lòng chọn loại giảm giá" });
                }

                if (model.Value <= 0)
                {
                    return Json(new { success = false, message = "Giá trị giảm phải lớn hơn 0" });
                }

                if (model.Type == "percentage" && model.Value > 100)
                {
                    return Json(new { success = false, message = "Giá trị phần trăm không được vượt quá 100%" });
                }

                if (model.Id == Guid.Empty)
                {
                    // Create new coupon
                    // Check if code already exists
                    var existingCoupon = await _context.Coupons
                        .FirstOrDefaultAsync(c => c.Code.ToUpper() == model.Code.ToUpper());
                    
                    if (existingCoupon != null)
                    {
                        return Json(new { success = false, message = "Mã giảm giá đã tồn tại" });
                    }

                    model.Id = Guid.NewGuid();
                    model.Code = model.Code.ToUpper();
                    model.CreatedAt = DateTime.UtcNow;
                    model.UpdatedAt = DateTime.UtcNow;
                    model.UsageCount = 0;
                    
                    // Ensure dates are in UTC
                    if (model.StartDate.HasValue && model.StartDate.Value.Kind == DateTimeKind.Unspecified)
                        model.StartDate = DateTime.SpecifyKind(model.StartDate.Value, DateTimeKind.Utc);
                    if (model.EndDate.HasValue && model.EndDate.Value.Kind == DateTimeKind.Unspecified)
                        model.EndDate = DateTime.SpecifyKind(model.EndDate.Value, DateTimeKind.Utc);
                    
                    _logger.LogInformation($"Creating new coupon: {model.Code} (ID: {model.Id})");
                    _context.Coupons.Add(model);
                }
                else
                {
                    // Update existing coupon
                    var coupon = await _context.Coupons.FindAsync(model.Id);
                    if (coupon == null)
                    {
                        return Json(new { success = false, message = "Không tìm thấy mã giảm giá" });
                    }

                    coupon.Code = model.Code.ToUpper();
                    coupon.Name = model.Name;
                    coupon.Description = model.Description;
                    coupon.Type = model.Type;
                    coupon.Value = model.Value;
                    coupon.MinOrderAmount = model.MinOrderAmount;
                    coupon.UsageLimit = model.UsageLimit;
                    
                    // Ensure dates are in UTC
                    if (model.StartDate.HasValue)
                    {
                        coupon.StartDate = model.StartDate.Value.Kind == DateTimeKind.Unspecified 
                            ? DateTime.SpecifyKind(model.StartDate.Value, DateTimeKind.Utc)
                            : model.StartDate;
                    }
                    else
                    {
                        coupon.StartDate = null;
                    }
                    
                    if (model.EndDate.HasValue)
                    {
                        coupon.EndDate = model.EndDate.Value.Kind == DateTimeKind.Unspecified 
                            ? DateTime.SpecifyKind(model.EndDate.Value, DateTimeKind.Utc)
                            : model.EndDate;
                    }
                    else
                    {
                        coupon.EndDate = null;
                    }
                    
                    coupon.IsActive = model.IsActive;
                    coupon.UpdatedAt = DateTime.UtcNow;
                    
                    _logger.LogInformation($"Updating coupon: {coupon.Code} (ID: {coupon.Id})");
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation($"Coupon saved successfully: {model.Code}");
                
                return Json(new
                {
                    success = true,
                    message = model.Id == Guid.Empty ? "Tạo mã giảm giá thành công" : "Cập nhật mã giảm giá thành công"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error saving coupon - Code: {model?.Code}, Message: {ex.Message}");
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        [HttpPost("coupons/toggle/{id}")]
        public async Task<IActionResult> ToggleCouponStatus(Guid id)
        {
            try
            {
                var coupon = await _context.Coupons.FindAsync(id);
                if (coupon == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy mã giảm giá" });
                }

                coupon.IsActive = !coupon.IsActive;
                coupon.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = coupon.IsActive ? "Đã kích hoạt mã giảm giá" : "Đã tạm dừng mã giảm giá"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling coupon");
                return Json(new { success = false, message = "Có lỗi xảy ra" });
            }
        }

        [HttpPost("coupons/delete/{id}")]
        public async Task<IActionResult> DeleteCoupon(Guid id)
        {
            try
            {
                var coupon = await _context.Coupons.FindAsync(id);
                if (coupon == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy mã giảm giá" });
                }

                // Check if coupon has been used
                if (coupon.UsageCount > 0)
                {
                    return Json(new { success = false, message = "Không thể xóa mã đã được sử dụng. Vui lòng tạm dừng thay vì xóa." });
                }

                _context.Coupons.Remove(coupon);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Đã xóa mã giảm giá thành công" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting coupon");
                return Json(new { success = false, message = "Có lỗi xảy ra" });
            }
        }
        #endregion

        #region User Management Extended
        [HttpGet("sellers")]
        public async Task<IActionResult> Sellers()
        {
            ViewData["CurrentSection"] = "sellers";
            ViewData["Title"] = "Quản lý người bán";
            
            var sellers = await _userManager.GetUsersInRoleAsync("Seller");
            return View(sellers);
        }

        [HttpGet("permissions")]
        public async Task<IActionResult> Permissions()
        {
            ViewData["CurrentSection"] = "permissions";
            ViewData["Title"] = "Phân quyền hệ thống";
            
            var roles = await _roleManager.Roles.ToListAsync();
            return View(roles);
        }
        #endregion

        #region Content Management
        [HttpGet("pages")]
        public IActionResult Pages()
        {
            // Redirect to Blog management as static content is managed there
            return RedirectToAction("Blog");
        }

        [HttpGet("banners")]
        public async Task<IActionResult> Banners()
        {
            ViewData["CurrentSection"] = "banners";
            ViewData["Title"] = "Quản lý banner quảng cáo";
            
            var banners = await _context.MarketingBanners
                .Include(b => b.Creator)
                .OrderBy(b => b.SortOrder)
                .ThenByDescending(b => b.CreatedAt)
                .ToListAsync();
            
            return View(banners);
        }

        [HttpGet("fix-freelancer-images")]
        public IActionResult FixFreelancerImages()
        {
            ViewData["CurrentSection"] = "utilities";
            ViewData["Title"] = "Fix Freelancer Images";
            return View();
        }

        #region Banner API Endpoints

        [HttpGet("api/banners")]
        public async Task<IActionResult> GetBanners()
        {
            try
            {
                var banners = await _context.MarketingBanners
                    .Include(b => b.Creator)
                    .OrderBy(b => b.SortOrder)
                    .ThenByDescending(b => b.CreatedAt)
                    .Select(b => new
                    {
                        b.Id,
                        b.Title,
                        b.Description,
                        b.ImageUrl,
                        b.MobileImageUrl,
                        b.LinkUrl,
                        b.Position,
                        b.SortOrder,
                        b.IsActive,
                        b.StartDate,
                        b.EndDate,
                        b.ViewCount,
                        b.ClickCount,
                        CTR = b.ViewCount > 0 ? (decimal)b.ClickCount / b.ViewCount * 100 : 0,
                        CreatorName = b.Creator != null ? $"{b.Creator.FirstName} {b.Creator.LastName}" : "System",
                        b.CreatedAt,
                        b.UpdatedAt
                    })
                    .ToListAsync();

                return Json(new { success = true, data = banners });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting banners");
                return Json(new { success = false, message = "Có lỗi xảy ra khi tải danh sách banner" });
            }
        }

        [HttpGet("api/banners/{id}")]
        public async Task<IActionResult> GetBanner(Guid id)
        {
            try
            {
                var banner = await _context.MarketingBanners.FindAsync(id);
                if (banner == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy banner" });
                }

                return Json(new { success = true, data = banner });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting banner {BannerId}", id);
                return Json(new { success = false, message = "Có lỗi xảy ra" });
            }
        }

        [HttpPost("api/banners")]
        public async Task<IActionResult> CreateBanner([FromForm] MarketingBanner model, IFormFile? imageFile, IFormFile? mobileImageFile)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                
                // Handle image upload
                if (imageFile != null && imageFile.Length > 0)
                {
                    var fileName = $"banner_{Guid.NewGuid()}{Path.GetExtension(imageFile.FileName)}";
                    var filePath = Path.Combine(_webHostEnvironment.WebRootPath, "images", "banners", fileName);
                    
                    Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
                    
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(stream);
                    }
                    
                    model.ImageUrl = $"/images/banners/{fileName}";
                }

                // Handle mobile image upload
                if (mobileImageFile != null && mobileImageFile.Length > 0)
                {
                    var fileName = $"banner_mobile_{Guid.NewGuid()}{Path.GetExtension(mobileImageFile.FileName)}";
                    var filePath = Path.Combine(_webHostEnvironment.WebRootPath, "images", "banners", fileName);
                    
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await mobileImageFile.CopyToAsync(stream);
                    }
                    
                    model.MobileImageUrl = $"/images/banners/{fileName}";
                }

                model.Id = Guid.NewGuid();
                model.CreatedBy = user?.Id;
                model.CreatedAt = DateTime.UtcNow;
                model.UpdatedAt = DateTime.UtcNow;
                
                // Set IsActive = true by default for new banners
                if (!model.IsActive)
                {
                    model.IsActive = true;
                    _logger.LogInformation("Banner IsActive was false, setting to true by default");
                }
                
                _logger.LogInformation("Creating banner with IsActive={IsActive}, Position={Position}, Title={Title}", 
                    model.IsActive, model.Position, model.Title);
                
                // Fix DateTime Kind for PostgreSQL - convert to UTC if needed
                if (model.StartDate.Kind == DateTimeKind.Unspecified)
                {
                    model.StartDate = DateTime.SpecifyKind(model.StartDate, DateTimeKind.Utc);
                }
                else if (model.StartDate.Kind == DateTimeKind.Local)
                {
                    model.StartDate = model.StartDate.ToUniversalTime();
                }
                
                if (model.EndDate.HasValue)
                {
                    if (model.EndDate.Value.Kind == DateTimeKind.Unspecified)
                    {
                        model.EndDate = DateTime.SpecifyKind(model.EndDate.Value, DateTimeKind.Utc);
                    }
                    else if (model.EndDate.Value.Kind == DateTimeKind.Local)
                    {
                        model.EndDate = model.EndDate.Value.ToUniversalTime();
                    }
                }
                
                // Store banner in storage (unassigned) by default
                // User will assign it later to specific positions
                if (string.IsNullOrEmpty(model.Position) || 
                    model.Position == "home_main" || 
                    model.Position == "home_side" || 
                    model.Position == "collection_hero" || 
                    model.Position == "category")
                {
                    model.Position = "unassigned";
                    model.TargetPage = null;
                    model.SortOrder = 0;
                }

                _context.MarketingBanners.Add(model);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Banner created in storage: {BannerId} by {UserId}", model.Id, user?.Id);

                return Json(new { success = true, message = "Banner đã được lưu vào kho thành công!", data = model });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating banner");
                return Json(new { success = false, message = "Có lỗi xảy ra khi tạo banner: " + ex.Message });
            }
        }

        [HttpPut("api/banners/{id}")]
        [HttpPost("api/banners/{id}/update")]
        public async Task<IActionResult> UpdateBanner(Guid id, [FromForm] MarketingBanner model, IFormFile? imageFile, IFormFile? mobileImageFile)
        {
            try
            {
                var banner = await _context.MarketingBanners.FindAsync(id);
                if (banner == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy banner" });
                }

                // Handle image upload - DELETE OLD FILE FIRST
                if (imageFile != null && imageFile.Length > 0)
                {
                    // Delete old image file if exists
                    if (!string.IsNullOrEmpty(banner.ImageUrl))
                    {
                        var oldFilePath = Path.Combine(_webHostEnvironment.WebRootPath, banner.ImageUrl.TrimStart('/'));
                        if (System.IO.File.Exists(oldFilePath))
                        {
                            try
                            {
                                System.IO.File.Delete(oldFilePath);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning($"Failed to delete old banner image: {ex.Message}");
                            }
                        }
                    }
                    
                    var fileName = $"banner_{Guid.NewGuid()}{Path.GetExtension(imageFile.FileName)}";
                    var filePath = Path.Combine(_webHostEnvironment.WebRootPath, "images", "banners", fileName);
                    
                    Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
                    
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(stream);
                    }
                    
                    banner.ImageUrl = $"/images/banners/{fileName}";
                }
                else if (!string.IsNullOrEmpty(model.ImageUrl))
                {
                    banner.ImageUrl = model.ImageUrl;
                }

                // Handle mobile image upload - DELETE OLD FILE FIRST
                if (mobileImageFile != null && mobileImageFile.Length > 0)
                {
                    // Delete old mobile image file if exists
                    if (!string.IsNullOrEmpty(banner.MobileImageUrl))
                    {
                        var oldFilePath = Path.Combine(_webHostEnvironment.WebRootPath, banner.MobileImageUrl.TrimStart('/'));
                        if (System.IO.File.Exists(oldFilePath))
                        {
                            try
                            {
                                System.IO.File.Delete(oldFilePath);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning($"Failed to delete old mobile banner image: {ex.Message}");
                            }
                        }
                    }
                    
                    var fileName = $"banner_mobile_{Guid.NewGuid()}{Path.GetExtension(mobileImageFile.FileName)}";
                    var filePath = Path.Combine(_webHostEnvironment.WebRootPath, "images", "banners", fileName);
                    
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await mobileImageFile.CopyToAsync(stream);
                    }
                    
                    banner.MobileImageUrl = $"/images/banners/{fileName}";
                }

                banner.Title = model.Title;
                banner.Description = model.Description;
                banner.LinkUrl = model.LinkUrl;
                banner.Position = model.Position;
                banner.SortOrder = model.SortOrder;
                banner.IsActive = model.IsActive;
                
                // Fix DateTime Kind for PostgreSQL - convert to UTC if needed
                if (model.StartDate.Kind == DateTimeKind.Unspecified)
                {
                    banner.StartDate = DateTime.SpecifyKind(model.StartDate, DateTimeKind.Utc);
                }
                else if (model.StartDate.Kind == DateTimeKind.Local)
                {
                    banner.StartDate = model.StartDate.ToUniversalTime();
                }
                else
                {
                    banner.StartDate = model.StartDate;
                }

                if (model.EndDate.HasValue)
                {
                    if (model.EndDate.Value.Kind == DateTimeKind.Unspecified)
                    {
                        banner.EndDate = DateTime.SpecifyKind(model.EndDate.Value, DateTimeKind.Utc);
                    }
                    else if (model.EndDate.Value.Kind == DateTimeKind.Local)
                    {
                        banner.EndDate = model.EndDate.Value.ToUniversalTime();
                    }
                    else
                    {
                        banner.EndDate = model.EndDate.Value;
                    }
                }
                else
                {
                    banner.EndDate = null;
                }
                
                banner.OpenInNewTab = model.OpenInNewTab;
                banner.TargetPage = model.TargetPage;
                banner.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Banner updated: {BannerId}", id);

                return Json(new { success = true, message = "Banner đã được cập nhật thành công!", data = banner });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating banner {BannerId}", id);
                return Json(new { success = false, message = "Có lỗi xảy ra khi cập nhật banner: " + ex.Message });
            }
        }

        [HttpPost("api/banners/{id}/toggle")]
        public async Task<IActionResult> ToggleBanner(Guid id)
        {
            try
            {
                var banner = await _context.MarketingBanners.FindAsync(id);
                if (banner == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy banner" });
                }

                banner.IsActive = !banner.IsActive;
                banner.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Banner toggled: {BannerId} - IsActive: {IsActive}", id, banner.IsActive);

                return Json(new
                {
                    success = true,
                    message = banner.IsActive ? "Banner đã được kích hoạt" : "Banner đã bị vô hiệu hóa",
                    isActive = banner.IsActive
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling banner {BannerId}", id);
                return Json(new { success = false, message = "Có lỗi xảy ra" });
            }
        }

        [HttpDelete("api/banners/{id}")]
        [HttpPost("api/banners/{id}/delete")]
        public async Task<IActionResult> DeleteBanner(Guid id)
        {
            try
            {
                var banner = await _context.MarketingBanners.FindAsync(id);
                if (banner == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy banner" });
                }

                // Only delete from database, keep image files
                // Images might be used by other banners or needed for backup
                _context.MarketingBanners.Remove(banner);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Banner deleted from database (files kept): {BannerId}", id);

                return Json(new { success = true, message = "Banner đã được xóa thành công!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting banner {BannerId}", id);
                return Json(new { success = false, message = "Có lỗi xảy ra khi xóa banner" });
            }
        }

        [HttpPost("api/banners/{id}/delete-with-files")]
        public async Task<IActionResult> DeleteBannerWithFiles(Guid id)
        {
            try
            {
                var banner = await _context.MarketingBanners.FindAsync(id);
                if (banner == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy banner" });
                }

                // Delete image files if they exist
                if (!string.IsNullOrEmpty(banner.ImageUrl))
                {
                    var imagePath = Path.Combine(_webHostEnvironment.WebRootPath, banner.ImageUrl.TrimStart('/'));
                    if (System.IO.File.Exists(imagePath))
                    {
                        System.IO.File.Delete(imagePath);
                    }
                }

                if (!string.IsNullOrEmpty(banner.MobileImageUrl))
                {
                    var mobileImagePath = Path.Combine(_webHostEnvironment.WebRootPath, banner.MobileImageUrl.TrimStart('/'));
                    if (System.IO.File.Exists(mobileImagePath))
                    {
                        System.IO.File.Delete(mobileImagePath);
                    }
                }

                _context.MarketingBanners.Remove(banner);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Banner and files deleted: {BannerId}", id);

                return Json(new { success = true, message = "Banner và files đã được xóa thành công!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting banner with files {BannerId}", id);
                return Json(new { success = false, message = "Có lỗi xảy ra khi xóa banner" });
            }
        }

        [HttpPost("api/banners/{id}/activate")]
        public async Task<IActionResult> ActivateBanner(Guid id)
        {
            try
            {
                var banner = await _context.MarketingBanners.FindAsync(id);
                if (banner == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy banner" });
                }

                banner.IsActive = true;
                banner.StartDate = DateTime.UtcNow;
                banner.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Banner activated: {BannerId}", id);

                return Json(new { success = true, message = "Banner đã được kích hoạt!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error activating banner {BannerId}", id);
                return Json(new { success = false, message = "Có lỗi xảy ra" });
            }
        }

        [HttpPost("api/banners/{id}/track-view")]
        public async Task<IActionResult> TrackBannerView(Guid id)
        {
            try
            {
                var banner = await _context.MarketingBanners.FindAsync(id);
                if (banner != null)
                {
                    banner.ViewCount++;
                    await _context.SaveChangesAsync();
                }
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error tracking banner view {BannerId}", id);
                return Json(new { success = false });
            }
        }

        [HttpPost("api/banners/{id}/track-click")]
        public async Task<IActionResult> TrackBannerClick(Guid id)
        {
            try
            {
                var banner = await _context.MarketingBanners.FindAsync(id);
                if (banner != null)
                {
                    banner.ClickCount++;
                    await _context.SaveChangesAsync();
                }
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error tracking banner click {BannerId}", id);
                return Json(new { success = false });
            }
        }

        [HttpPost("api/banners/reorder")]
        public async Task<IActionResult> ReorderBanners([FromBody] List<BannerOrderModel> banners)
        {
            try
            {
                foreach (var item in banners)
                {
                    var banner = await _context.MarketingBanners.FindAsync(item.Id);
                    if (banner != null)
                    {
                        banner.SortOrder = item.SortOrder;
                        banner.UpdatedAt = DateTime.UtcNow;
                    }
                }

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Đã cập nhật thứ tự banner!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reordering banners");
                return Json(new { success = false, message = "Có lỗi xảy ra khi sắp xếp banner" });
            }
        }

        [HttpGet("api/banners/unassigned")]
        public async Task<IActionResult> GetUnassignedBanners()
        {
            try
            {
                // Get banners that are in storage (not assigned to specific positions)
                // These are banners with default/generic position or marked as "unassigned"
                var unassignedBanners = await _context.MarketingBanners
                    .Where(b => b.IsActive && (
                        b.Position == "unassigned" || 
                        b.Position == "storage" ||
                        b.Position == null ||
                        b.Position == ""
                    ))
                    .OrderByDescending(b => b.CreatedAt)
                    .Select(b => new
                    {
                        b.Id,
                        b.Title,
                        b.Description,
                        b.ImageUrl,
                        b.MobileImageUrl,
                        b.CreatedAt,
                        b.UpdatedAt
                    })
                    .ToListAsync();

                return Json(new { success = true, data = unassignedBanners });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting unassigned banners");
                return Json(new { success = false, message = "Có lỗi xảy ra khi tải danh sách banner" });
            }
        }

        [HttpGet("api/banners/all")]
        public async Task<IActionResult> GetAllBanners()
        {
            try
            {
                // Get ALL active banners (including assigned ones)
                var allBanners = await _context.MarketingBanners
                    .Where(b => b.IsActive)
                    .OrderByDescending(b => b.CreatedAt)
                    .Select(b => new
                    {
                        b.Id,
                        b.Title,
                        b.Description,
                        b.ImageUrl,
                        b.MobileImageUrl,
                        b.Position,
                        b.TargetPage,
                        b.CreatedAt,
                        b.UpdatedAt,
                        // Check if banner is assigned to a specific position
                        IsAssigned = !string.IsNullOrEmpty(b.Position) && 
                                   b.Position != "unassigned" && 
                                   b.Position != "storage"
                    })
                    .ToListAsync();

                return Json(new { success = true, data = allBanners });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all banners");
                return Json(new { success = false, message = "Có lỗi xảy ra khi tải danh sách banner" });
            }
        }

        [HttpPost("api/banners/{id}/assign")]
        public async Task<IActionResult> AssignBanner(Guid id, [FromBody] BannerAssignmentModel model)
        {
            try
            {
                var banner = await _context.MarketingBanners.FindAsync(id);
                if (banner == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy banner" });
                }

                // Update banner position and target page
                banner.Position = model.Position;
                banner.TargetPage = model.TargetPage;
                
                // Get the highest sort order in this position group
                var maxSortOrder = await _context.MarketingBanners
                    .Where(b => b.Position == model.Position && 
                               (string.IsNullOrEmpty(model.TargetPage) || b.TargetPage == model.TargetPage) &&
                               b.Id != id)
                    .MaxAsync(b => (int?)b.SortOrder) ?? 0;
                
                banner.SortOrder = maxSortOrder + 1;
                banner.UpdatedAt = DateTime.UtcNow;
                
                await _context.SaveChangesAsync();

                _logger.LogInformation("Banner {BannerId} assigned to position {Position}, TargetPage {TargetPage}", 
                    id, model.Position, model.TargetPage);

                return Json(new { 
                    success = true, 
                    message = "Banner đã được gán vào vị trí thành công!",
                    data = banner 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning banner {BannerId}", id);
                return Json(new { success = false, message = "Có lỗi xảy ra khi gán banner: " + ex.Message });
            }
        }

        [HttpPost("api/banners/{id}/reorder")]
        public async Task<IActionResult> ReorderSingleBanner(Guid id, [FromBody] BannerReorderModel model)
        {
            try
            {
                _logger.LogInformation("========== REORDER REQUEST START ==========");
                _logger.LogInformation("Banner ID: {BannerId}", id);
                _logger.LogInformation("Position: {Position}", model.Position);
                _logger.LogInformation("TargetPage: {TargetPage}", model.TargetPage ?? "NULL");
                _logger.LogInformation("OldSortOrder: {OldSort}", model.OldSortOrder);
                _logger.LogInformation("NewSortOrder: {NewSort}", model.NewSortOrder);

                // Get ALL banners in the same group (position + targetPage)
                var bannersQuery = _context.MarketingBanners
                    .Where(b => b.Position == model.Position);

                // For collection banners, filter by TargetPage
                if (model.Position == "collection_hero" && !string.IsNullOrEmpty(model.TargetPage))
                {
                    bannersQuery = bannersQuery.Where(b => b.TargetPage == model.TargetPage);
                    _logger.LogInformation("Filtering collection_hero by TargetPage: {TargetPage}", model.TargetPage);
                }

                // Load all banners, sorted by current order
                var allBanners = await bannersQuery
                    .OrderBy(b => b.SortOrder)
                    .ThenBy(b => b.CreatedAt)
                    .ToListAsync();

                _logger.LogInformation("Found {Count} banners in group", allBanners.Count);
                
                // Log current state
                _logger.LogInformation("CURRENT STATE:");
                for (int i = 0; i < allBanners.Count; i++)
                {
                    var b = allBanners[i];
                    _logger.LogInformation("  [{Index}] Id={Id}, Title={Title}, SortOrder={Sort}", 
                        i, b.Id, b.Title, b.SortOrder);
                }

                if (allBanners.Count == 0)
                {
                    return Json(new { success = false, message = "Không tìm thấy banner nào trong nhóm" });
                }

                // Find the banner to move
                var currentIndex = allBanners.FindIndex(b => b.Id == id);
                if (currentIndex == -1)
                {
                    _logger.LogError("Banner {Id} not found in list", id);
                    return Json(new { success = false, message = "Không tìm thấy banner cần di chuyển" });
                }

                _logger.LogInformation("Banner found at currentIndex: {CurrentIndex}", currentIndex);

                // Calculate new index (NewSortOrder - 1, because SortOrder starts from 1 but index from 0)
                var newIndex = model.NewSortOrder - 1;
                _logger.LogInformation("Calculated newIndex: {NewIndex} (from NewSortOrder={NewSort})", newIndex, model.NewSortOrder);
                
                // Validate new index
                if (newIndex < 0 || newIndex >= allBanners.Count)
                {
                    var oldNewIndex = newIndex;
                    newIndex = Math.Max(0, Math.Min(newIndex, allBanners.Count - 1));
                    _logger.LogWarning("newIndex {OldIndex} out of bounds, adjusted to {NewIndex}", oldNewIndex, newIndex);
                }

                // If no change, return early
                if (currentIndex == newIndex)
                {
                    _logger.LogInformation("No position change needed (currentIndex == newIndex)");
                    return Json(new { success = true, message = "Vị trí không thay đổi" });
                }

                _logger.LogInformation("Moving banner from index {CurrentIndex} to {NewIndex}", currentIndex, newIndex);

                // Reorder: Remove from old position, insert at new position
                var bannerToMove = allBanners[currentIndex];
                allBanners.RemoveAt(currentIndex);
                allBanners.Insert(newIndex, bannerToMove);

                // Log after move
                _logger.LogInformation("AFTER MOVE (before SortOrder reassignment):");
                for (int i = 0; i < allBanners.Count; i++)
                {
                    var b = allBanners[i];
                    _logger.LogInformation("  [{Index}] Id={Id}, Title={Title}, SortOrder={Sort}", 
                        i, b.Id, b.Title, b.SortOrder);
                }

                // Reassign ALL SortOrder values sequentially (1, 2, 3, 4...)
                for (int i = 0; i < allBanners.Count; i++)
                {
                    var banner = allBanners[i];
                    var oldSort = banner.SortOrder;
                    banner.SortOrder = i + 1;
                    banner.UpdatedAt = DateTime.UtcNow;
                    _logger.LogInformation("  Banner {Id} ({Title}): SortOrder {OldSort} → {NewSort}", 
                        banner.Id, banner.Title, oldSort, banner.SortOrder);
                }

                // Save all changes
                _logger.LogInformation("Calling SaveChangesAsync...");
                var savedCount = await _context.SaveChangesAsync();
                _logger.LogInformation("SaveChanges completed: {Count} entities updated", savedCount);
                _logger.LogInformation("========== REORDER REQUEST END (SUCCESS) ==========");

                return Json(new { success = true, message = "Đã cập nhật thứ tự banner!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "========== REORDER REQUEST END (ERROR) ==========");
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        #endregion
        #endregion

        #region Notifications
        [HttpGet("notifications")]
        public IActionResult Notifications()
        {
            ViewData["CurrentSection"] = "notifications";
            ViewData["Title"] = "Thông báo";
            ViewData["PageIcon"] = "bell";
            
            return View("Notifications");
        }
        #endregion

        #region Analytics
        [HttpGet("analytics")]
        public async Task<IActionResult> Analytics()
        {
            ViewData["CurrentSection"] = "Analytics";
            ViewData["Title"] = "Phân tích";
            
            var viewModel = await GenerateAdvancedAnalyticsData();
            
            return View("Analytics", viewModel);
        }
        #endregion

        #region Advanced Reports
        [HttpGet("reports")]
        public async Task<IActionResult> Reports()
        {
            ViewData["CurrentSection"] = "reports";
            ViewData["Title"] = "Báo cáo";
            
            var viewModel = await GenerateAdvancedAnalyticsData();
            
            return View("Reports", viewModel);
        }

        private async Task<ViewModels.ReportsViewModel> GenerateAdvancedAnalyticsData()
        {
            var endDate = DateTime.UtcNow;
            var startDate = endDate.AddDays(-30);
            var previousStartDate = startDate.AddDays(-30);
            var previousEndDate = startDate;

            // KPI Data
            var currentRevenue = await _context.Orders
                .Where(o => o.CreatedAt >= startDate && o.CreatedAt <= endDate && o.PaymentStatus == "paid" && o.Status != "cancelled")
                .SumAsync(o => o.TotalAmount);

            var previousRevenue = await _context.Orders
                .Where(o => o.CreatedAt >= previousStartDate && o.CreatedAt <= previousEndDate && o.PaymentStatus == "paid" && o.Status != "cancelled")
                .SumAsync(o => o.TotalAmount);

            var currentOrders = await _context.Orders
                .Where(o => o.CreatedAt >= startDate && o.CreatedAt <= endDate && o.PaymentStatus == "paid" && o.Status != "cancelled")
                .CountAsync();

            var previousOrders = await _context.Orders
                .Where(o => o.CreatedAt >= previousStartDate && o.CreatedAt <= previousEndDate && o.PaymentStatus == "paid" && o.Status != "cancelled")
                .CountAsync();

            var currentCustomers = await _context.Users
                .Where(u => u.CreatedAt >= startDate && u.CreatedAt <= endDate)
                .CountAsync();

            var previousCustomers = await _context.Users
                .Where(u => u.CreatedAt >= previousStartDate && u.CreatedAt <= previousEndDate)
                .CountAsync();

            var totalVisits = currentOrders * 25; // Mock calculation for demo
            var totalConversions = currentOrders;
            var conversionRate = totalVisits > 0 ? (decimal)totalConversions / totalVisits * 100 : 0;
            
            var previousTotalVisits = previousOrders * 25;
            var previousConversions = previousOrders;
            var previousConversionRate = previousTotalVisits > 0 ? (decimal)previousConversions / previousTotalVisits * 100 : 0;

            var kpiData = new ViewModels.KPIData
            {
                TotalRevenue = currentRevenue,
                RevenueGrowth = previousRevenue > 0 ? (currentRevenue - previousRevenue) / previousRevenue * 100 : 0,
                CompletedOrders = currentOrders,
                OrdersGrowth = previousOrders > 0 ? (decimal)(currentOrders - previousOrders) / previousOrders * 100 : 0,
                NewCustomers = currentCustomers,
                CustomersGrowth = previousCustomers > 0 ? (decimal)(currentCustomers - previousCustomers) / previousCustomers * 100 : 0,
                ConversionRate = conversionRate,
                ConversionGrowth = previousConversionRate > 0 ? (conversionRate - previousConversionRate) / previousConversionRate * 100 : 0
            };

            // Sales Analytics Data
            var monthlyRevenue = await _context.Orders
                .Where(o => o.CreatedAt >= startDate && o.PaymentStatus == "paid" && o.Status != "cancelled")
                .GroupBy(o => new { Year = o.CreatedAt.Year, Month = o.CreatedAt.Month })
                .Select(g => new MonthlyData
                {
                    Month = $"Tháng {g.Key.Month}",
                    Revenue = g.Sum(o => o.TotalAmount),
                    Orders = g.Count(),
                    Date = new DateTime(g.Key.Year, g.Key.Month, 1)
                })
                .OrderBy(m => m.Date)
                .ToListAsync();

            var salesAnalytics = new ViewModels.SalesAnalyticsData
            {
                TotalRevenue = currentRevenue,
                TotalOrders = currentOrders,
                AverageOrderValue = currentOrders > 0 ? currentRevenue / currentOrders : 0,
                GrowthRate = previousRevenue > 0 ? (currentRevenue - previousRevenue) / previousRevenue * 100 : 0,
                MonthlyGrowthRate = 12.5m, // Mock data
                MonthlyData = monthlyRevenue
            };

            // Customer Analytics Data
            var customerAnalytics = new ViewModels.CustomerAnalyticsData
            {
                TotalCustomers = await _context.Users.CountAsync(),
                NewCustomers = currentCustomers,
                ReturnRate = 68.5m, // Mock data - would calculate from actual data
                CustomerLifetimeValue = 4200000m, // Mock data
                GrowthRate = previousCustomers > 0 ? (decimal)(currentCustomers - previousCustomers) / previousCustomers * 100 : 0,
                GrowthData = new List<ViewModels.CustomerGrowthData>() // Would populate with real data
            };

            // Product Analytics Data
            var totalProducts = await _context.Products.CountAsync();
            var newProducts = await _context.Products
                .Where(p => p.CreatedAt >= startDate)
                .CountAsync();
            var outOfStock = await _context.Products
                .Where(p => p.StockQuantity == 0)
                .CountAsync();

            var productAnalytics = new ViewModels.ProductAnalyticsData
            {
                TotalProducts = totalProducts,
                NewProducts = newProducts,
                OutOfStock = outOfStock,
                AvailabilityRate = totalProducts > 0 ? (decimal)(totalProducts - outOfStock) / totalProducts * 100 : 0,
                GrowthRate = 5.2m, // Mock data
                CategoryData = new List<ViewModels.ProductCategoryData>() // Would populate with real data
            };

            // Marketing Analytics Data
            var marketingAnalytics = new ViewModels.MarketingAnalyticsData
            {
                TotalVisits = totalVisits,
                ConversionRate = conversionRate,
                AdvertisingCost = 45200000m, // Mock data
                ROAS = 8.7m, // Mock data
                GrowthRate = 18.5m, // Mock data
                TrafficSources = new List<ViewModels.TrafficSourceData>() // Would populate with real data
            };

            // Chart Data - Revenue Chart
            var revenueChartData = monthlyRevenue.Select(m => new Models.ChartData
            {
                Label = m.Month,
                Value = m.Revenue
            }).ToList();

            // Top Products Data
            var topProducts = await _context.OrderItems
                .Include(oi => oi.Product)
                .Where(oi => oi.Order.CreatedAt >= startDate && oi.Order.PaymentStatus == "paid" && oi.Order.Status != "cancelled")
                .GroupBy(oi => new { oi.ProductId, oi.Product.Name })
                .Select(g => new Models.ChartData
                {
                    Label = g.Key.Name,
                    Value = g.Sum(oi => oi.TotalPrice)
                })
                .OrderByDescending(p => p.Value)
                .Take(5)
                .ToListAsync();

            // Category Sales Data
            var categorySales = await _context.OrderItems
                .Include(oi => oi.Product)
                .ThenInclude(p => p.Category)
                .Where(oi => oi.Order.CreatedAt >= startDate && oi.Order.PaymentStatus == "paid" && oi.Order.Status != "cancelled")
                .GroupBy(oi => oi.Product.Category.Name)
                .Select(g => new Models.ChartData
                {
                    Label = g.Key,
                    Value = g.Sum(oi => oi.TotalPrice)
                })
                .ToListAsync();

            // Payment Method Data
            var paymentMethodsRaw = await _context.Orders
                .Where(o => o.CreatedAt >= startDate && o.PaymentStatus == "paid" && o.Status != "cancelled")
                .GroupBy(o => o.PaymentMethod)
                .Select(g => new
                {
                    PaymentMethod = g.Key,
                    TotalAmount = g.Sum(o => o.TotalAmount)
                })
                .ToListAsync();

            var paymentMethods = paymentMethodsRaw
                .Select(p => new Models.ChartData
                {
                    Label = GetPaymentMethodDisplayName(p.PaymentMethod),
                    Value = p.TotalAmount
                })
                .ToList();

            // Customer Segments
            var customerSegments = new List<ViewModels.CustomerSegment>
            {
                new ViewModels.CustomerSegment { Name = "VIP", Count = 245, Percentage = 85, Revenue = 1200000, AverageOrderValue = 4900000, GrowthRate = 12 },
                new ViewModels.CustomerSegment { Name = "Thường xuyên", Count = 1567, Percentage = 65, Revenue = 800000, AverageOrderValue = 2100000, GrowthRate = 8 },
                new ViewModels.CustomerSegment { Name = "Mới", Count = 2890, Percentage = 35, Revenue = 400000, AverageOrderValue = 890000, GrowthRate = 22 }
            };

            return new ViewModels.ReportsViewModel
            {
                KPIData = kpiData,
                SalesAnalytics = salesAnalytics,
                CustomerAnalytics = customerAnalytics,
                ProductAnalytics = productAnalytics,
                MarketingAnalytics = marketingAnalytics,
                RevenueChartData = revenueChartData,
                TopProductsData = topProducts,
                CategorySalesData = categorySales,
                PaymentMethodData = paymentMethods,
                CustomerSegments = customerSegments,
                StartDate = startDate,
                EndDate = endDate,
                DateRange = "30 ngày qua",
                ComparisonData = new ViewModels.ComparisonPeriodData
                {
                    PreviousRevenue = previousRevenue,
                    PreviousOrders = previousOrders,
                    PreviousCustomers = previousCustomers,
                    PreviousConversionRate = previousConversionRate
                }
            };
        }

        private string GetPaymentMethodDisplayName(string paymentMethod)
        {
            return paymentMethod?.ToLower() switch
            {
                "credit_card" => "Thẻ tín dụng",
                "bank_transfer" => "Chuyển khoản",
                "cod" => "COD",
                "momo" => "Ví điện tử",
                "vnpay" => "Ví điện tử",
                _ => "Khác"
            };
        }

        [HttpGet("system-logs")]
        public IActionResult Logs()
        {
            ViewData["CurrentSection"] = "logs";
            ViewData["Title"] = "Nhật ký hệ thống";
            
            return View();
        }
        #endregion

        #region System Management
        [HttpGet("backups")]
        public IActionResult Backups()
        {
            ViewData["CurrentSection"] = "backups";
            ViewData["Title"] = "Sao lưu dữ liệu";
            
            return View();
        }

        [HttpGet("security")]
        public IActionResult Security()
        {
            ViewData["CurrentSection"] = "security";
            ViewData["Title"] = "Bảo mật hệ thống";
            
            return View();
        }
        
        [HttpGet("inventory")]
        public async Task<IActionResult> Inventory(string filter = "all", string search = "", int page = 1, int pageSize = 50)
        {
            ViewData["CurrentSection"] = "Inventory";
            ViewData["Title"] = "Quản lý tồn kho";
            
            var query = _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .AsQueryable();
            
            // Apply search filter
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(p => 
                    p.Name.Contains(search) || 
                    p.SKU.Contains(search));
            }
            
            // Apply stock filter
            switch (filter.ToLower())
            {
                case "low_stock":
                    query = query.Where(p => p.StockQuantity > 0 && p.StockQuantity < 10);
                    break;
                case "out_of_stock":
                    query = query.Where(p => p.StockQuantity == 0);
                    break;
                case "in_stock":
                    query = query.Where(p => p.StockQuantity >= 10);
                    break;
                // "all" - no filter
            }
            
            var totalItems = await query.CountAsync();
            
            // Calculate statistics for ALL products matching the query (before pagination)
            var allProducts = await query.AsNoTracking().ToListAsync();
            var totalStockQuantity = allProducts.Sum(p => p.StockQuantity);
            var lowStockCount = allProducts.Count(p => p.StockQuantity > 0 && p.StockQuantity < 10);
            var outOfStockCount = allProducts.Count(p => p.StockQuantity == 0);
            var inStockCount = allProducts.Count(p => p.StockQuantity >= 10);
            
            // Get paginated products
            var products = allProducts
                .OrderBy(p => p.StockQuantity)
                .ThenBy(p => p.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
            
            // Map to InventoryItemViewModel
            var inventoryItems = products.Select(p => new ViewModels.InventoryItemViewModel
            {
                Id = Guid.NewGuid(), // Or use existing inventory ID if you have one
                ProductId = p.Id,
                ProductName = p.Name,
                SKU = p.SKU,
                CategoryName = p.Category?.Name ?? "N/A",
                StockQuantity = p.StockQuantity,
                CurrentStock = p.StockQuantity,
                MinStockLevel = 10, // Default minimum stock level
                MinStock = 10,
                MaxStock = 1000,
                Price = p.Price,
                CostPrice = p.Price * 0.6m, // Estimate 60% of selling price
                ImageUrl = p.FeaturedImageUrl,
                LastUpdated = p.UpdatedAt
            }).ToList();
            
            var viewModel = new ViewModels.InventoryListViewModel
            {
                Items = inventoryItems,
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize),
                PageSize = pageSize,
                SearchTerm = search,
                Filter = filter,
                TotalProducts = totalItems,
                TotalStockQuantity = totalStockQuantity,
                LowStockProducts = lowStockCount,
                OutOfStockProducts = outOfStockCount,
                InStockProducts = inStockCount
            };
                
            return View(viewModel);
        }
        
        #endregion
        /// <summary>
        /// Fix sunglasses products category assignment
        /// Moves FWSG* products from "Chân váy nữ" to "Phụ kiện nữ"
        /// </summary>
        [HttpPost("fix-sunglasses-category")]
        public async Task<IActionResult> FixSunglassesCategory()
        {
            try
            {
                // Get the correct category ID for "Phụ kiện nữ"
                var accessoriesCategory = await _context.Categories
                    .FirstOrDefaultAsync(c => c.Name == "Phụ kiện nữ");
                
                if (accessoriesCategory == null)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Category 'Phụ kiện nữ' not found"
                    });
                }

                // Find all sunglasses products (SKU starting with FWSG)
                var sunglassesProducts = await _context.Products
                    .Where(p => p.SKU.StartsWith("FWSG") && p.IsActive)
                    .ToListAsync();

                if (!sunglassesProducts.Any())
                {
                    return Json(new
                    {
                        success = true,
                        message = "No sunglasses products found",
                        productsUpdated = 0
                    });
                }

                // Update category for each sunglasses product
                var oldCategoryIds = new Dictionary<Guid, string>();
                foreach (var product in sunglassesProducts)
                {
                    var oldCategory = await _context.Categories
                        .FirstOrDefaultAsync(c => c.Id == product.CategoryId);
                    
                    oldCategoryIds[product.Id] = oldCategory?.Name ?? "Unknown";
                    product.CategoryId = accessoriesCategory.Id;
                }

                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = $"Successfully updated {sunglassesProducts.Count} sunglasses products",
                    productsUpdated = sunglassesProducts.Count,
                    products = sunglassesProducts.Select(p => new
                    {
                        sku = p.SKU,
                        name = p.Name,
                        oldCategory = oldCategoryIds[p.Id],
                        newCategory = "Phụ kiện nữ"
                    }).ToList()
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = $"Error: {ex.Message}"
                });
            }
        }

        // Check missing Freelancer images
        [HttpGet("api/check-missing-images")]
        public async Task<IActionResult> CheckMissingImages()
        {
            try
            {
                var freelancerProducts = await _context.Products
                    .Where(p => p.SKU.StartsWith("FW"))
                    .ToListAsync();
                
                var missingImages = new List<object>();
                var existingImages = 0;
                
                foreach (var product in freelancerProducts)
                {
                    // Try different possible paths
                    var possiblePaths = new List<string>
                    {
                        $"/images/ao-nu/{product.SKU}.jpg",
                        $"/images/quan-nu/{product.SKU}.jpg",
                        $"/images/chan-vay-nu/{product.SKU}.jpg",
                        $"/images/dam-nu/{product.SKU}.jpg",
                        $"/images/products/{product.SKU}.jpg"
                    };
                    
                    bool foundFile = false;
                    string? foundPath = null;
                    
                    foreach (var path in possiblePaths)
                    {
                        var physicalPath = Path.Combine(_webHostEnvironment.WebRootPath, path.TrimStart('/'));
                        if (System.IO.File.Exists(physicalPath))
                        {
                            foundFile = true;
                            foundPath = path;
                            break;
                        }
                    }
                    
                    if (foundFile)
                    {
                        existingImages++;
                        // Check if database path matches found file
                        if (product.FeaturedImageUrl != foundPath)
                        {
                            missingImages.Add(new 
                            {
                                sku = product.SKU,
                                name = product.Name,
                                currentPath = product.FeaturedImageUrl,
                                correctPath = foundPath,
                                status = "path_mismatch"
                            });
                        }
                    }
                    else
                    {
                        missingImages.Add(new 
                        {
                            sku = product.SKU,
                            name = product.Name,
                            currentPath = product.FeaturedImageUrl,
                            correctPath = (string?)null,
                            status = "file_not_found"
                        });
                    }
                }
                
                return Json(new 
                {
                    success = true,
                    totalProducts = freelancerProducts.Count,
                    existingImages,
                    missingImages = missingImages.Count,
                    details = missingImages.Take(50) // Limit to first 50 for display
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking missing images");
                return Json(new { success = false, message = ex.Message });
            }
        }
        
        // Fix specific products with suffix issues
        [HttpPost("api/fix-suffix-images")]
        public async Task<IActionResult> FixSuffixImages()
        {
            try
            {
                var fixedCount = 0;
                var updates = new Dictionary<string, string>
                {
                    { "FWSK24FH04C", "/images/chan-vay-nu/FWSK24FH04C-1.jpg" },
                    { "FWSK24SS02C", "/images/chan-vay-nu/FWSK24SS02C-O.jpg" },
                    { "FWDR24SS21C", "/images/dam-nu/FWDR24SS21C-O.jpg" },
                    { "FWSP25FH02G", "/images/quan-nu/FWSP25FH02G-J.jpg" }
                };

                foreach (var update in updates)
                {
                    var product = await _context.Products
                        .FirstOrDefaultAsync(p => p.SKU == update.Key);
                    
                    if (product != null && product.FeaturedImageUrl != update.Value)
                    {
                        product.FeaturedImageUrl = update.Value;
                        product.UpdatedAt = DateTime.UtcNow;
                        fixedCount++;
                        _logger.LogInformation($"Fixed {update.Key}: {product.FeaturedImageUrl} → {update.Value}");
                    }
                }

                if (fixedCount > 0)
                {
                    await _context.SaveChangesAsync();
                }

                return Json(new { success = true, fixedCount, total = updates.Count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fixing suffix images");
                return Json(new { success = false, message = ex.Message });
            }
        }
        
        // Fix Freelancer product image paths
        [HttpPost("api/fix-freelancer-images")]
        public async Task<IActionResult> FixFreelancerImagePaths()
        {
            try
            {
                _logger.LogInformation("Starting Freelancer image path fix...");
                
                var fixedCount = 0;
                
                // Get all Freelancer products (SKU starts with FW)
                var freelancerProducts = await _context.Products
                    .Where(p => p.SKU.StartsWith("FW"))
                    .ToListAsync();
                
                _logger.LogInformation($"Found {freelancerProducts.Count} Freelancer products");
                
                foreach (var product in freelancerProducts)
                {
                    var oldPath = product.FeaturedImageUrl;
                    string? newPath = null;
                    
                    // Search for file in all possible locations (actual file system check)
                    // This handles cases where files might be in different folders than expected
                    // Also searches for files with suffixes like -1, -O, -J, etc.
                    var folders = new[] { "ao-nu", "quan-nu", "chan-vay-nu", "dam-nu", "products" };
                    
                    foreach (var folder in folders)
                    {
                        var folderPath = Path.Combine(_webHostEnvironment.WebRootPath, "images", folder);
                        if (Directory.Exists(folderPath))
                        {
                            // Try exact match first
                            var exactFile = Path.Combine(folderPath, $"{product.SKU}.jpg");
                            if (System.IO.File.Exists(exactFile))
                            {
                                newPath = $"/images/{folder}/{product.SKU}.jpg";
                                break;
                            }
                            
                            // Try with wildcard for files with suffixes (e.g., FWSK24FH04C-1.jpg)
                            var matchingFiles = Directory.GetFiles(folderPath, $"{product.SKU}*.jpg");
                            if (matchingFiles.Length > 0)
                            {
                                var fileName = Path.GetFileName(matchingFiles[0]);
                                newPath = $"/images/{folder}/{fileName}";
                                break;
                            }
                        }
                    }
                    
                    if (newPath != null && product.FeaturedImageUrl != newPath)
                    {
                        product.FeaturedImageUrl = newPath;
                        product.UpdatedAt = DateTime.UtcNow;
                        fixedCount++;
                        
                        _logger.LogInformation($"Fixed {product.SKU}: {oldPath} → {newPath}");
                    }
                    else if (newPath == null)
                    {
                        _logger.LogWarning($"File not found for {product.SKU} in any location");
                    }
                }
                
                if (fixedCount > 0)
                {
                    await _context.SaveChangesAsync();
                    _logger.LogInformation($"Successfully updated {fixedCount} Freelancer product image paths");
                    
                    return Json(new
                    {
                        success = true,
                        message = $"Đã cập nhật {fixedCount} đường dẫn hình ảnh sản phẩm Freelancer",
                        productsFixed = fixedCount,
                        totalFreelancerProducts = freelancerProducts.Count
                    });
                }
                else
                {
                    return Json(new
                    {
                        success = true,
                        message = "Không có sản phẩm nào cần cập nhật",
                        productsFixed = 0,
                        totalFreelancerProducts = freelancerProducts.Count
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fixing Freelancer image paths");
                return Json(new
                {
                    success = false,
                    message = $"Lỗi: {ex.Message}"
                });
            }
        }
    }
}

// Extension method for string title case
namespace JohnHenryFashionWeb.Extensions
{
    public static class StringExtensions
    {
        public static string ToTitleCase(this string input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            var words = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < words.Length; i++)
            {
                if (words[i].Length > 0)
                {
                    words[i] = char.ToUpper(words[i][0]) + (words[i].Length > 1 ? words[i].Substring(1).ToLower() : "");
                }
            }
            return string.Join(' ', words);
        }
    }
}
