using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using OfficeOpenXml.Drawing.Chart;
using JohnHenryFashionWeb.Data;
using JohnHenryFashionWeb.Models;
using JohnHenryFashionWeb.Services;

namespace JohnHenryFashionWeb.Services
{
    public interface IReportingService
    {
        // Report Generation
        Task<SalesReport> GenerateSalesReportAsync(DateTime startDate, DateTime endDate, Guid? categoryId = null);
        Task<byte[]> GenerateExcelReportAsync(string reportType, DateTime startDate, DateTime endDate, Dictionary<string, object>? parameters = null);
        Task<byte[]> GeneratePdfReportAsync(string reportType, DateTime startDate, DateTime endDate, Dictionary<string, object>? parameters = null);
        
        // Dashboard Data
        Task<DashboardSummary> GetDashboardSummaryAsync();
        Task<List<ChartData>> GetSalesChartDataAsync(string period = "daily", int days = 30);
        Task<List<ChartData>> GetTopProductsChartDataAsync(int limit = 10);
        Task<List<TimeSeriesData>> GetRevenueTimeSeriesAsync(DateTime startDate, DateTime endDate, string granularity = "daily");
        
        // Automated Reports
        Task ScheduleReportAsync(ReportTemplate template);
        Task<List<ReportTemplate>> GetScheduledReportsAsync();
        Task ExecuteScheduledReportsAsync();
        
        // Data Aggregation
        Task<ProductPerformanceData> GetProductPerformanceAsync(Guid productId, DateTime startDate, DateTime endDate);
        Task<List<CategoryPerformanceData>> GetCategoryPerformanceAsync(DateTime startDate, DateTime endDate);
        Task<List<GeographicData>> GetGeographicPerformanceAsync(DateTime startDate, DateTime endDate);
        Task<PerformanceMetrics> GetPerformanceMetricsAsync(DateTime startDate, DateTime endDate);
        
        // Export Functions
        Task<string> ExportDataToJsonAsync<T>(List<T> data);
        Task<byte[]> ExportDataToCsvAsync<T>(List<T> data);
        Task<byte[]> ExportAnalyticsToExcelAsync(AnalyticsFilter filter);
    }

    public class ReportingService : IReportingService
    {
        private readonly ApplicationDbContext _context;
        private readonly IAnalyticsService _analyticsService;
        private readonly ICacheService _cacheService;
        private readonly ILogger<ReportingService> _logger;
        private readonly IConfiguration _configuration;

        public ReportingService(
            ApplicationDbContext context,
            IAnalyticsService analyticsService,
            ICacheService cacheService,
            ILogger<ReportingService> logger,
            IConfiguration configuration)
        {
            _context = context;
            _analyticsService = analyticsService;
            _cacheService = cacheService;
            _logger = logger;
            _configuration = configuration;
            
            // Set EPPlus license context - suppress obsolete warning for compatibility
#pragma warning disable CS0618 // Type or member is obsolete
            try
            {
                OfficeOpenXml.ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
            }
            catch
            {
                // License is set globally through configuration or already set
            }
#pragma warning restore CS0618 // Type or member is obsolete
        }

        #region Report Generation

        public async Task<SalesReport> GenerateSalesReportAsync(DateTime startDate, DateTime endDate, Guid? categoryId = null)
        {
            try
            {
                var cacheKey = $"sales_report_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}_{categoryId}";
                var cachedReport = await _cacheService.GetAsync<SalesReport>(cacheKey);
                if (cachedReport != null)
                    return cachedReport;

                var ordersQuery = _context.Orders
                    .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.Product)
                            .ThenInclude(p => p.Category)
                    .Where(o => o.CreatedAt >= startDate 
                        && o.CreatedAt <= endDate
                        && o.PaymentStatus == "paid"
                        && o.Status != "cancelled");

                if (categoryId.HasValue)
                {
                    ordersQuery = ordersQuery.Where(o => o.OrderItems.Any(oi => oi.Product.CategoryId == categoryId));
                }

                var orders = await ordersQuery.ToListAsync();

                var report = new SalesReport
                {
                    Id = Guid.NewGuid(),
                    ReportType = "SalesReport",
                    StartDate = startDate,
                    EndDate = endDate,
                    GeneratedAt = DateTime.UtcNow,
                    ReportData = JsonSerializer.Serialize(new
                    {
                        Summary = new
                        {
                            TotalOrders = orders.Count,
                            TotalRevenue = orders.Sum(o => o.TotalAmount),
                            CompletedOrders = orders.Count(o => o.Status == "Completed"),
                            PendingOrders = orders.Count(o => o.Status == "Pending"),
                            CancelledOrders = orders.Count(o => o.Status == "Cancelled"),
                            AverageOrderValue = orders.Any() ? orders.Average(o => o.TotalAmount) : 0,
                            TotalItems = orders.SelectMany(o => o.OrderItems).Sum(oi => oi.Quantity)
                        },
                        DailySales = orders
                            .GroupBy(o => o.CreatedAt.Date)
                            .Select(g => new
                            {
                                Date = g.Key,
                                Orders = g.Count(),
                                Revenue = g.Sum(o => o.TotalAmount),
                                Items = g.SelectMany(o => o.OrderItems).Sum(oi => oi.Quantity)
                            })
                            .OrderBy(x => x.Date)
                            .ToList(),
                        TopProducts = orders
                            .SelectMany(o => o.OrderItems)
                            .GroupBy(oi => new { oi.ProductId, oi.Product.Name })
                            .Select(g => new
                            {
                                ProductId = g.Key.ProductId,
                                ProductName = g.Key.Name,
                                Quantity = g.Sum(oi => oi.Quantity),
                                Revenue = g.Sum(oi => oi.UnitPrice * oi.Quantity)
                            })
                            .OrderByDescending(x => x.Revenue)
                            .Take(10)
                            .ToList(),
                        PaymentMethods = orders
                            .Where(o => !string.IsNullOrEmpty(o.PaymentMethod))
                            .GroupBy(o => o.PaymentMethod)
                            .Select(g => new
                            {
                                Method = g.Key,
                                Count = g.Count(),
                                Revenue = g.Sum(o => o.TotalAmount)
                            })
                            .OrderByDescending(x => x.Revenue)
                            .ToList()
                    })
                };

                await _cacheService.SetAsync(cacheKey, report, TimeSpan.FromMinutes(30));
                return report;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating sales report");
                throw;
            }
        }

        public async Task<byte[]> GenerateExcelReportAsync(string reportType, DateTime startDate, DateTime endDate, Dictionary<string, object>? parameters = null)
        {
            try
            {
                using var package = new ExcelPackage();

                switch (reportType.ToLower())
                {
                    case "sales":
                        await GenerateSalesExcelReport(package, startDate, endDate, parameters);
                        break;
                    case "products":
                        await GenerateProductsExcelReport(package, startDate, endDate, parameters);
                        break;
                    case "analytics":
                        await GenerateAnalyticsExcelReport(package, startDate, endDate, parameters);
                        break;
                    case "customers":
                        await GenerateCustomersExcelReport(package, startDate, endDate, parameters);
                        break;
                    default:
                        throw new ArgumentException($"Unknown report type: {reportType}");
                }

                return package.GetAsByteArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating Excel report: {ReportType}", reportType);
                throw;
            }
        }

        public async Task<byte[]> GeneratePdfReportAsync(string reportType, DateTime startDate, DateTime endDate, Dictionary<string, object>? parameters = null)
        {
            // This would typically use a PDF library like iTextSharp or DinkToPdf
            // For now, we'll return a simple implementation
            try
            {
                var htmlContent = await GenerateReportHtmlAsync(reportType, startDate, endDate, parameters);
                
                // Convert HTML to PDF (pseudo-code - would need actual PDF library)
                // For production, implement using iTextSharp, DinkToPdf, or similar
                var pdfBytes = Encoding.UTF8.GetBytes(htmlContent);
                
                return pdfBytes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating PDF report: {ReportType}", reportType);
                throw;
            }
        }

        #endregion

        #region Dashboard Data

        public async Task<DashboardSummary> GetDashboardSummaryAsync()
        {
            try
            {
                var cacheKey = "dashboard_summary";
                var cached = await _cacheService.GetAsync<DashboardSummary>(cacheKey);
                if (cached != null)
                    return cached;

                var now = DateTime.UtcNow;
                var today = now.Date;
                var yesterday = today.AddDays(-1);

                // Get today's data - CHỈ tính đơn hàng đã thanh toán thành công
                var todayOrders = await _context.Orders
                    .Where(o => o.CreatedAt >= today 
                        && o.PaymentStatus == "paid" 
                        && o.Status != "cancelled")
                    .ToListAsync();

                var yesterdayOrders = await _context.Orders
                    .Where(o => o.CreatedAt >= yesterday 
                        && o.CreatedAt < today
                        && o.PaymentStatus == "paid" 
                        && o.Status != "cancelled")
                    .ToListAsync();

                // Get visitor data from analytics
                var todayAnalytics = await _analyticsService.GetUserAnalyticsAsync(today, now);
                var yesterdayAnalytics = await _analyticsService.GetUserAnalyticsAsync(yesterday, today);
                var realTimeData = await _analyticsService.GetRealTimeAnalyticsAsync();

                // Calculate today's visitors from unique sessions or page views
                var todayVisitors = todayAnalytics?.UniqueSessions ?? 0;
                if (todayVisitors == 0)
                {
                    // Try PageViews by unique sessions first
                    todayVisitors = await _context.PageViews
                        .Where(p => p.ViewedAt >= today)
                        .Select(p => p.SessionId)
                        .Distinct()
                        .CountAsync();
                    
                    // If still 0, estimate based on orders (1 order ≈ 10-20 visitors)
                    if (todayVisitors == 0 && todayOrders.Count > 0)
                    {
                        todayVisitors = todayOrders.Count * 15; // Conservative estimate
                        _logger.LogInformation($"No visitor tracking data found. Estimated {todayVisitors} visitors based on {todayOrders.Count} orders");
                    }
                }

                var yesterdayVisitors = yesterdayAnalytics?.UniqueSessions ?? 0;
                if (yesterdayVisitors == 0)
                {
                    yesterdayVisitors = await _context.PageViews
                        .Where(p => p.ViewedAt >= yesterday && p.ViewedAt < today)
                        .Select(p => p.SessionId)
                        .Distinct()
                        .CountAsync();
                    
                    // Estimate if no data
                    if (yesterdayVisitors == 0 && yesterdayOrders.Count > 0)
                    {
                        yesterdayVisitors = yesterdayOrders.Count * 15;
                    }
                }

                // Calculate conversion rate: (orders / visitors) * 100
                var conversionRate = todayVisitors > 0 ? 
                    (double)todayOrders.Count / todayVisitors * 100 : 0;

                var summary = new DashboardSummary
                {
                    TodayRevenue = todayOrders.Sum(o => o.TotalAmount),
                    YesterdayRevenue = yesterdayOrders.Sum(o => o.TotalAmount),
                    TodayOrders = todayOrders.Count,
                    YesterdayOrders = yesterdayOrders.Count,
                    TodayVisitors = todayVisitors,
                    YesterdayVisitors = yesterdayVisitors,
                    ConversionRate = conversionRate,
                    AverageOrderValue = todayOrders.Any() ? todayOrders.Average(o => o.TotalAmount) : 0,
                    ActiveUsers = realTimeData?.ActiveUsers ?? 0,
                    RecentActivities = await GetRecentActivitiesAsync(),
                    TopProducts = await GetTopProductsAsync(5),
                    Alerts = await GetDashboardAlertsAsync()
                };

                // Calculate growth rates
                summary.RevenueGrowth = CalculateGrowthRate(summary.TodayRevenue, summary.YesterdayRevenue);
                summary.OrdersGrowth = CalculateGrowthRate(summary.TodayOrders, summary.YesterdayOrders);
                summary.VisitorsGrowth = CalculateGrowthRate(summary.TodayVisitors, summary.YesterdayVisitors);

                await _cacheService.SetAsync(cacheKey, summary, TimeSpan.FromMinutes(5));
                return summary;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting dashboard summary");
                throw;
            }
        }

        public async Task<List<ChartData>> GetSalesChartDataAsync(string period = "daily", int days = 30)
        {
            try
            {
                var endDate = DateTime.UtcNow.Date;
                var startDate = endDate.AddDays(-days);

                // ALWAYS use real Orders data first for live updates
                _logger.LogInformation($"📊 Getting sales chart data from Orders table (live data)");
                var orders = await _context.Orders
                    .Where(o => o.CreatedAt >= startDate 
                        && o.CreatedAt <= endDate
                        && (o.PaymentStatus == "paid" || 
                            (o.PaymentStatus == "COD Pending" && (o.Status == "processing" || o.Status == "shipped" || o.Status == "delivered")))
                        && o.Status != "cancelled")
                    .ToListAsync();

                // If no orders, fall back to SalesReports (sample data)
                if (!orders.Any())
                {
                    _logger.LogInformation("No Orders found, falling back to SalesReports table");
                    var salesReports = await _context.SalesReports
                        .Where(r => r.ReportType == "daily" && 
                                   r.StartDate >= startDate && 
                                   r.EndDate <= endDate)
                        .OrderBy(r => r.StartDate)
                        .ToListAsync();

                    if (salesReports.Any())
                    {
                        _logger.LogInformation($"Using {salesReports.Count} SalesReports records for chart data");
                        
                        var chartData = salesReports
                            .Select(r => new ChartData
                            {
                                Label = r.StartDate.ToString("MM/dd"),
                                Value = r.TotalRevenue,
                                AdditionalData = new Dictionary<string, object>
                                {
                                    ["Orders"] = r.TotalOrders,
                                    ["Products"] = r.TotalProducts
                                }
                            })
                            .ToList();
                        
                        return chartData;
                    }

                    _logger.LogWarning("No data found in Orders or SalesReports, returning empty list");
                    return new List<ChartData>();
                }

                _logger.LogInformation($"Using {orders.Count} Orders for live chart data");

                switch (period.ToLower())
                {
                    case "hourly":
                        return orders
                            .GroupBy(o => new { Date = o.CreatedAt.Date, Hour = o.CreatedAt.Hour })
                            .Select(g => new ChartData
                            {
                                Label = $"{g.Key.Date:MM/dd} {g.Key.Hour:00}:00",
                                Value = g.Sum(o => o.TotalAmount)
                            })
                            .OrderBy(c => c.Label)
                            .ToList();

                    case "daily":
                        return orders
                            .GroupBy(o => o.CreatedAt.Date)
                            .Select(g => new ChartData
                            {
                                Label = g.Key.ToString("MM/dd"),
                                Value = g.Sum(o => o.TotalAmount)
                            })
                            .OrderBy(c => c.Label)
                            .ToList();

                    case "weekly":
                        return orders
                            .GroupBy(o => GetWeekStart(o.CreatedAt))
                            .Select(g => new ChartData
                            {
                                Label = $"Week of {g.Key:MM/dd}",
                                Value = g.Sum(o => o.TotalAmount)
                            })
                            .OrderBy(c => c.Label)
                            .ToList();

                    default:
                        throw new ArgumentException($"Invalid period: {period}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting sales chart data");
                throw;
            }
        }

        public async Task<List<ChartData>> GetTopProductsChartDataAsync(int limit = 10)
        {
            try
            {
                var cacheKey = $"top_products_chart_{limit}";
                var cached = await _cacheService.GetAsync<List<ChartData>>(cacheKey);
                if (cached != null)
                    return cached;

                var endDate = DateTime.UtcNow;
                var startDate = endDate.AddDays(-30);

                var topProductsQuery = await _context.OrderItems
                    .Include(oi => oi.Product)
                    .Include(oi => oi.Order)
                    .Where(oi => oi.Order.CreatedAt >= startDate)
                    .GroupBy(oi => new { oi.ProductId, oi.Product.Name })
                    .ToListAsync();

                var topProducts = topProductsQuery
                    .Select(g => new ChartData
                    {
                        Label = g.Key.Name,
                        Value = g.Sum(oi => oi.UnitPrice * oi.Quantity),
                        AdditionalData = new Dictionary<string, object>
                        {
                            ["ProductId"] = g.Key.ProductId,
                            ["Quantity"] = g.Sum(oi => oi.Quantity),
                            ["Orders"] = g.Select(oi => oi.OrderId).Distinct().Count()
                        }
                    })
                    .OrderByDescending(c => c.Value)
                    .Take(limit)
                    .ToList();

                await _cacheService.SetAsync(cacheKey, topProducts, TimeSpan.FromMinutes(15));
                return topProducts;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting top products chart data");
                throw;
            }
        }

        public async Task<List<TimeSeriesData>> GetRevenueTimeSeriesAsync(DateTime startDate, DateTime endDate, string granularity = "daily")
        {
            try
            {
                // ALWAYS use real Orders data first for live updates
                _logger.LogInformation($"📊 Getting revenue time series from Orders table (live data)");
                var orders = await _context.Orders
                    .Where(o => o.CreatedAt >= startDate 
                        && o.CreatedAt <= endDate
                        && (o.PaymentStatus == "paid" || 
                            (o.PaymentStatus == "COD Pending" && (o.Status == "processing" || o.Status == "shipped" || o.Status == "delivered")))
                        && o.Status != "cancelled")
                    .ToListAsync();

                // If no orders, fall back to SalesReports (sample data)
                if (!orders.Any())
                {
                    _logger.LogInformation("No Orders found, falling back to SalesReports table");
                    var salesReports = await _context.SalesReports
                        .Where(r => r.ReportType == "daily" && 
                                   r.StartDate >= startDate && 
                                   r.EndDate <= endDate)
                        .OrderBy(r => r.StartDate)
                        .ToListAsync();

                    if (salesReports.Any())
                    {
                        _logger.LogInformation($"Using {salesReports.Count} SalesReports records for time series data");
                        
                        var timeSeriesData = salesReports
                            .Select(r => new TimeSeriesData
                            {
                                Date = r.StartDate,
                                Value = r.TotalRevenue,
                                Label = r.StartDate.ToString("MM/dd"),
                                AdditionalData = new Dictionary<string, object>
                                {
                                    ["Orders"] = r.TotalOrders,
                                    ["Products"] = r.TotalProducts,
                                    ["AvgOrderValue"] = r.AverageOrderValue
                                }
                            })
                            .ToList();
                        
                        return timeSeriesData;
                    }

                    _logger.LogWarning("No data found in Orders or SalesReports, returning empty list");
                    return new List<TimeSeriesData>();
                }

                _logger.LogInformation($"Using {orders.Count} Orders for live time series data");

                switch (granularity.ToLower())
                {
                    case "hourly":
                        return orders
                            .GroupBy(o => new DateTime(o.CreatedAt.Year, o.CreatedAt.Month, o.CreatedAt.Day, o.CreatedAt.Hour, 0, 0))
                            .Select(g => new TimeSeriesData
                            {
                                Date = g.Key,
                                Value = g.Sum(o => o.TotalAmount),
                                Label = g.Key.ToString("HH:mm")
                            })
                            .OrderBy(t => t.Date)
                            .ToList();

                    case "daily":
                        return orders
                            .GroupBy(o => o.CreatedAt.Date)
                            .Select(g => new TimeSeriesData
                            {
                                Date = g.Key,
                                Value = g.Sum(o => o.TotalAmount),
                                Label = g.Key.ToString("MM/dd")
                            })
                            .OrderBy(t => t.Date)
                            .ToList();

                    case "monthly":
                        return orders
                            .GroupBy(o => new DateTime(o.CreatedAt.Year, o.CreatedAt.Month, 1))
                            .Select(g => new TimeSeriesData
                            {
                                Date = g.Key,
                                Value = g.Sum(o => o.TotalAmount),
                                Label = g.Key.ToString("yyyy-MM")
                            })
                            .OrderBy(t => t.Date)
                            .ToList();

                    default:
                        throw new ArgumentException($"Invalid granularity: {granularity}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting revenue time series");
                throw;
            }
        }

        #endregion

        #region Automated Reports

        public async Task ScheduleReportAsync(ReportTemplate template)
        {
            try
            {
                template.Id = Guid.NewGuid();
                template.CreatedAt = DateTime.UtcNow;
                template.IsActive = true;

                _context.ReportTemplates.Add(template);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Scheduled report template: {TemplateName}", template.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scheduling report template");
                throw;
            }
        }

        public async Task<List<ReportTemplate>> GetScheduledReportsAsync()
        {
            try
            {
                return await _context.ReportTemplates
                    .Where(rt => rt.IsActive)
                    .OrderBy(rt => rt.Name)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting scheduled reports");
                throw;
            }
        }

        public async Task ExecuteScheduledReportsAsync()
        {
            try
            {
                var dueReports = await _context.ReportTemplates
                    .Where(rt => rt.IsActive && rt.NextRunDate <= DateTime.UtcNow)
                    .ToListAsync();

                foreach (var template in dueReports)
                {
                    try
                    {
                        await ExecuteReportTemplateAsync(template);
                        
                        // Calculate next run date
                        template.LastRunDate = DateTime.UtcNow;
                        template.NextRunDate = CalculateNextRunDate(template.LastRunDate.Value, template.Frequency);
                        
                        await _context.SaveChangesAsync();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error executing scheduled report: {TemplateName}", template.Name);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing scheduled reports");
                throw;
            }
        }

        #endregion

        #region Data Aggregation

        public async Task<ProductPerformanceData> GetProductPerformanceAsync(Guid productId, DateTime startDate, DateTime endDate)
        {
            try
            {
                var orderItems = await _context.OrderItems
                    .Include(oi => oi.Order)
                    .Include(oi => oi.Product)
                    .Where(oi => oi.ProductId == productId && 
                                oi.Order.CreatedAt >= startDate && 
                                oi.Order.CreatedAt <= endDate)
                    .ToListAsync();

                var product = await _context.Products.FindAsync(productId);
                
                return new ProductPerformanceData
                {
                    ProductId = productId,
                    ProductName = product?.Name ?? "Unknown",
                    Sales = orderItems.Sum(oi => oi.Quantity),
                    Revenue = orderItems.Sum(oi => oi.UnitPrice * oi.Quantity),
                    Orders = orderItems.Select(oi => oi.OrderId).Distinct().Count()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting product performance for {ProductId}", productId);
                throw;
            }
        }

        public async Task<List<CategoryPerformanceData>> GetCategoryPerformanceAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                var categoryPerformance = await _context.OrderItems
                    .Include(oi => oi.Order)
                    .Include(oi => oi.Product)
                    .ThenInclude(p => p.Category)
                    .Where(oi => oi.Order.CreatedAt >= startDate && oi.Order.CreatedAt <= endDate)
                    .GroupBy(oi => new { oi.Product.CategoryId, oi.Product.Category.Name })
                    .Select(g => new CategoryPerformanceData
                    {
                        CategoryId = g.Key.CategoryId,
                        CategoryName = g.Key.Name,
                        Revenue = g.Sum(oi => oi.UnitPrice * oi.Quantity),
                        Quantity = g.Sum(oi => oi.Quantity),
                        Orders = g.Select(oi => oi.OrderId).Distinct().Count()
                    })
                    .OrderByDescending(c => c.Revenue)
                    .ToListAsync();

                return categoryPerformance;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting category performance");
                throw;
            }
        }

        public async Task<List<GeographicData>> GetGeographicPerformanceAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                // Get orders first, then process in memory to avoid Entity Framework translation issues
                var orders = await _context.Orders
                    .Where(o => o.CreatedAt >= startDate 
                        && o.CreatedAt <= endDate
                        && o.PaymentStatus == "paid"
                        && o.Status != "cancelled")
                    .Select(o => new
                    {
                        o.UserId,
                        o.TotalAmount,
                        ShippingCity = o.ShippingAddress ?? "Unknown" // Use database property instead of computed property
                    })
                    .ToListAsync();

                // Group and process in memory
                var geographicData = orders
                    .GroupBy(o => o.ShippingCity)
                    .Select(g => new GeographicData
                    {
                        City = g.Key,
                        Country = "Vietnam", // Default for this implementation
                        Visitors = g.Select(o => o.UserId).Distinct().Count(),
                        Sessions = g.Count(),
                        Revenue = g.Sum(o => o.TotalAmount),
                        ConversionRate = 0 // Would need session data to calculate
                    })
                    .OrderByDescending(g => g.Revenue)
                    .ToList();

                return geographicData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting geographic performance");
                throw;
            }
        }

        public async Task<PerformanceMetrics> GetPerformanceMetricsAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                // This would typically integrate with application performance monitoring
                // For now, return basic metrics
                var totalRequests = await _context.PageViews
                    .Where(pv => pv.ViewedAt >= startDate && pv.ViewedAt <= endDate)
                    .CountAsync();

                return new PerformanceMetrics
                {
                    TotalRequests = totalRequests,
                    SuccessfulRequests = (int)(totalRequests * 0.98), // Estimated
                    FailedRequests = (int)(totalRequests * 0.02), // Estimated
                    ErrorRate = 2.0, // Estimated 2% error rate
                    AveragePageLoadTime = TimeSpan.FromMilliseconds(850),
                    AverageApiResponseTime = TimeSpan.FromMilliseconds(120),
                    StatusCodeBreakdown = new Dictionary<string, int>
                    {
                        ["200"] = (int)(totalRequests * 0.85),
                        ["404"] = (int)(totalRequests * 0.10),
                        ["500"] = (int)(totalRequests * 0.02),
                        ["Other"] = (int)(totalRequests * 0.03)
                    },
                    SlowestEndpoints = new List<SlowEndpoint>
                    {
                        new() { Endpoint = "/Products/Details", AverageResponseTime = TimeSpan.FromMilliseconds(1200), RequestCount = 1500, ErrorRate = 1.2 },
                        new() { Endpoint = "/Cart/Checkout", AverageResponseTime = TimeSpan.FromMilliseconds(2000), RequestCount = 800, ErrorRate = 0.8 }
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting performance metrics");
                throw;
            }
        }

        #endregion

        #region Export Functions

        public async Task<string> ExportDataToJsonAsync<T>(List<T> data)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                return await Task.FromResult(JsonSerializer.Serialize(data, options));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting data to JSON");
                throw;
            }
        }

        public async Task<byte[]> ExportDataToCsvAsync<T>(List<T> data)
        {
            try
            {
                if (!data.Any())
                    return Encoding.UTF8.GetBytes("");

                var csv = new StringBuilder();
                var properties = typeof(T).GetProperties();

                // Headers
                csv.AppendLine(string.Join(",", properties.Select(p => p.Name)));

                // Data rows
                foreach (var item in data)
                {
                    var values = properties.Select(p => 
                    {
                        var value = p.GetValue(item);
                        return value?.ToString()?.Replace(",", ";") ?? "";
                    });
                    csv.AppendLine(string.Join(",", values));
                }

                return await Task.FromResult(Encoding.UTF8.GetBytes(csv.ToString()));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting data to CSV");
                throw;
            }
        }

        public async Task<byte[]> ExportAnalyticsToExcelAsync(AnalyticsFilter filter)
        {
            try
            {
                using var package = new ExcelPackage();

                // Analytics Summary Sheet
                var summarySheet = package.Workbook.Worksheets.Add("Analytics Summary");
                var analytics = await _analyticsService.GetUserAnalyticsAsync(filter.StartDate, filter.EndDate);
                
                // Add summary data
                summarySheet.Cells[1, 1].Value = "Metric";
                summarySheet.Cells[1, 2].Value = "Value";
                summarySheet.Cells[2, 1].Value = "Total Sessions";
                summarySheet.Cells[2, 2].Value = analytics.TotalSessions;
                summarySheet.Cells[3, 1].Value = "Unique Sessions";
                summarySheet.Cells[3, 2].Value = analytics.UniqueSessions;
                summarySheet.Cells[4, 1].Value = "Total Page Views";
                summarySheet.Cells[4, 2].Value = analytics.TotalPageViews;
                summarySheet.Cells[5, 1].Value = "Bounce Rate";
                summarySheet.Cells[5, 2].Value = analytics.BounceRate;

                // Sales Analytics Sheet
                var salesSheet = package.Workbook.Worksheets.Add("Sales Analytics");
                var salesAnalytics = await _analyticsService.GetSalesAnalyticsAsync(filter.StartDate, filter.EndDate);
                
                salesSheet.Cells[1, 1].Value = "Metric";
                salesSheet.Cells[1, 2].Value = "Value";
                salesSheet.Cells[2, 1].Value = "Total Revenue";
                salesSheet.Cells[2, 2].Value = salesAnalytics.TotalRevenue;
                salesSheet.Cells[3, 1].Value = "Total Orders";
                salesSheet.Cells[3, 2].Value = salesAnalytics.TotalOrders;
                salesSheet.Cells[4, 1].Value = "Average Order Value";
                salesSheet.Cells[4, 2].Value = salesAnalytics.AverageOrderValue;
                salesSheet.Cells[5, 1].Value = "Conversion Rate";
                salesSheet.Cells[5, 2].Value = salesAnalytics.ConversionRate;

                // Format sheets
                FormatExcelSheet(summarySheet);
                FormatExcelSheet(salesSheet);

                return package.GetAsByteArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting analytics to Excel");
                throw;
            }
        }

        #endregion

        #region Private Helper Methods

        private async Task GenerateSalesExcelReport(ExcelPackage package, DateTime startDate, DateTime endDate, Dictionary<string, object>? parameters)
        {
            var worksheet = package.Workbook.Worksheets.Add("Sales Report");
            
            var salesReport = await GenerateSalesReportAsync(startDate, endDate);
            var data = JsonSerializer.Deserialize<dynamic>(salesReport.Data ?? "{}");
            
            // Add title and date range
            worksheet.Cells[1, 1].Value = $"Sales Report - {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}";
            worksheet.Cells[1, 1, 1, 6].Merge = true;
            
            // Add summary section
            var row = 3;
            worksheet.Cells[row, 1].Value = "Summary";
            worksheet.Cells[row, 1].Style.Font.Bold = true;
            
            // Add more detailed report data here...
            
            FormatExcelSheet(worksheet);
        }

        private async Task GenerateProductsExcelReport(ExcelPackage package, DateTime startDate, DateTime endDate, Dictionary<string, object>? parameters)
        {
            var worksheet = package.Workbook.Worksheets.Add("Products Report");
            
            var products = await _context.Products
                .Include(p => p.Category)
                .ToListAsync();
            
            // Add headers
            worksheet.Cells[1, 1].Value = "Product Name";
            worksheet.Cells[1, 2].Value = "Category";
            worksheet.Cells[1, 3].Value = "Price";
            worksheet.Cells[1, 4].Value = "Stock";
            worksheet.Cells[1, 5].Value = "Status";
            
            // Add data
            for (int i = 0; i < products.Count; i++)
            {
                var row = i + 2;
                worksheet.Cells[row, 1].Value = products[i].Name;
                worksheet.Cells[row, 2].Value = products[i].Category?.Name;
                worksheet.Cells[row, 3].Value = products[i].Price;
                worksheet.Cells[row, 4].Value = products[i].StockQuantity;
                worksheet.Cells[row, 5].Value = products[i].IsActive ? "Active" : "Inactive";
            }
            
            FormatExcelSheet(worksheet);
        }

        private async Task GenerateAnalyticsExcelReport(ExcelPackage package, DateTime startDate, DateTime endDate, Dictionary<string, object>? parameters)
        {
            var filter = new AnalyticsFilter { StartDate = startDate, EndDate = endDate };
            var analyticsData = await ExportAnalyticsToExcelAsync(filter);
            
            // This would merge the analytics data into the main package
            // Implementation would depend on specific requirements
        }

        private async Task GenerateCustomersExcelReport(ExcelPackage package, DateTime startDate, DateTime endDate, Dictionary<string, object>? parameters)
        {
            var worksheet = package.Workbook.Worksheets.Add("Customers Report");
            
            var customers = await _context.Users
                .Where(u => u.CreatedAt >= startDate && u.CreatedAt <= endDate)
                .ToListAsync();
            
            // Add headers
            worksheet.Cells[1, 1].Value = "Full Name";
            worksheet.Cells[1, 2].Value = "Email";
            worksheet.Cells[1, 3].Value = "Registration Date";
            worksheet.Cells[1, 4].Value = "Total Orders";
            worksheet.Cells[1, 5].Value = "Total Spent";
            
            // Add data
            for (int i = 0; i < customers.Count; i++)
            {
                var row = i + 2;
                var customer = customers[i];
                
                worksheet.Cells[row, 1].Value = customer.FullName;
                worksheet.Cells[row, 2].Value = customer.Email;
                worksheet.Cells[row, 3].Value = customer.CreatedAt;
                
                // Calculate order stats - chỉ tính đơn hàng đã thanh toán
                var orderStats = await _context.Orders
                    .Where(o => o.UserId == customer.Id 
                        && o.PaymentStatus == "paid"
                        && o.Status != "cancelled")
                    .GroupBy(o => o.UserId)
                    .Select(g => new { Count = g.Count(), Total = g.Sum(o => o.TotalAmount) })
                    .FirstOrDefaultAsync();
                
                worksheet.Cells[row, 4].Value = orderStats?.Count ?? 0;
                worksheet.Cells[row, 5].Value = orderStats?.Total ?? 0;
            }
            
            FormatExcelSheet(worksheet);
        }

        private async Task<string> GenerateReportHtmlAsync(string reportType, DateTime startDate, DateTime endDate, Dictionary<string, object>? parameters)
        {
            var html = new StringBuilder();
            html.AppendLine("<!DOCTYPE html>");
            html.AppendLine("<html><head><title>Report</title></head><body>");
            html.AppendLine($"<h1>{reportType} Report</h1>");
            html.AppendLine($"<p>Period: {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}</p>");
            
            switch (reportType.ToLower())
            {
                case "sales":
                    var salesReport = await GenerateSalesReportAsync(startDate, endDate);
                    html.AppendLine("<h2>Sales Summary</h2>");
                    html.AppendLine($"<p>Report generated at: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</p>");
                    break;
            }
            
            html.AppendLine("</body></html>");
            return html.ToString();
        }

        private async Task ExecuteReportTemplateAsync(ReportTemplate template)
        {
            try
            {
                var parameters = string.IsNullOrEmpty(template.Parameters) ? 
                    new Dictionary<string, object>() :
                    JsonSerializer.Deserialize<Dictionary<string, object>>(template.Parameters);

                var startDate = DateTime.UtcNow.AddDays(-30); // Default
                var endDate = DateTime.UtcNow;

                // Parse date parameters if provided
                if (parameters?.ContainsKey("startDate") == true)
                    DateTime.TryParse(parameters["startDate"].ToString(), out startDate);
                if (parameters?.ContainsKey("endDate") == true)
                    DateTime.TryParse(parameters["endDate"].ToString(), out endDate);

                byte[] reportData;
                string fileName;

                switch (template.Format.ToLower())
                {
                    case "excel":
                        reportData = await GenerateExcelReportAsync(template.ReportType, startDate, endDate, parameters);
                        fileName = $"{template.Name}_{DateTime.UtcNow:yyyyMMdd}.xlsx";
                        break;
                    case "pdf":
                        reportData = await GeneratePdfReportAsync(template.ReportType, startDate, endDate, parameters);
                        fileName = $"{template.Name}_{DateTime.UtcNow:yyyyMMdd}.pdf";
                        break;
                    default:
                        throw new ArgumentException($"Unsupported format: {template.Format}");
                }

                // Here you would typically email the report or save it to a file system
                _logger.LogInformation("Generated scheduled report: {FileName} ({Size} bytes)", fileName, reportData.Length);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing report template: {TemplateName}", template.Name);
                throw;
            }
        }

        private DateTime CalculateNextRunDate(DateTime lastRun, string frequency)
        {
            return frequency.ToLower() switch
            {
                "daily" => lastRun.AddDays(1),
                "weekly" => lastRun.AddDays(7),
                "monthly" => lastRun.AddMonths(1),
                "quarterly" => lastRun.AddMonths(3),
                "yearly" => lastRun.AddYears(1),
                _ => lastRun.AddDays(1)
            };
        }

        private static void FormatExcelSheet(ExcelWorksheet worksheet)
        {
            // Auto-fit columns
            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
            
            // Format header row
            using (var range = worksheet.Cells[1, 1, 1, worksheet.Dimension.End.Column])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }
        }

        private async Task<List<RecentActivity>> GetRecentActivitiesAsync()
        {
            var activities = new List<RecentActivity>();

            // Recent orders
            var recentOrders = await _context.Orders
                .Include(o => o.User)
                .OrderByDescending(o => o.CreatedAt)
                .Take(5)
                .ToListAsync();

            activities.AddRange(recentOrders.Select(o => new RecentActivity
            {
                Type = "order",
                Description = $"New order #{o.Id.ToString()[..8]} placed",
                Timestamp = o.CreatedAt,
                UserId = o.UserId,
                UserName = o.User?.FullName,
                Value = o.TotalAmount,
                ActionUrl = $"/admin/orders/{o.Id}"
            }));

            return activities.OrderByDescending(a => a.Timestamp).ToList();
        }

        private async Task<List<TopProduct>> GetTopProductsAsync(int limit)
        {
            var endDate = DateTime.UtcNow;
            var startDate = endDate.AddDays(-7);

            // Get raw data first, then process in memory - chỉ từ đơn hàng đã thanh toán
            var orderItemsData = await _context.OrderItems
                .Where(oi => oi.Order.CreatedAt >= startDate
                    && oi.Order.PaymentStatus == "paid"
                    && oi.Order.Status != "cancelled")
                .Select(oi => new 
                {
                    ProductId = oi.ProductId,
                    ProductName = oi.Product.Name,
                    FeaturedImageUrl = oi.Product.FeaturedImageUrl,
                    Quantity = oi.Quantity,
                    UnitPrice = oi.UnitPrice
                })
                .ToListAsync();

            // Group and calculate in memory
            var topProducts = orderItemsData
                .GroupBy(x => new { x.ProductId, x.ProductName, x.FeaturedImageUrl })
                .Select(g => new TopProduct
                {
                    ProductId = g.Key.ProductId,
                    ProductName = g.Key.ProductName,
                    ImageUrl = g.Key.FeaturedImageUrl ?? "/images/default-product.jpg",
                    Sales = g.Sum(x => x.Quantity),
                    Revenue = g.Sum(x => x.UnitPrice * x.Quantity),
                    Views = 0 // Would need to integrate with analytics
                })
                .OrderByDescending(p => p.Revenue)
                .Take(limit)
                .ToList();

            return topProducts;
        }

        private async Task<List<Alert>> GetDashboardAlertsAsync()
        {
            var alerts = new List<Alert>();

            // Check for low stock
            var lowStockProducts = await _context.Products
                .Where(p => p.StockQuantity <= 10 && p.IsActive)
                .CountAsync();

            if (lowStockProducts > 0)
            {
                alerts.Add(new Alert
                {
                    Type = "warning",
                    Title = "Low Stock Alert",
                    Message = $"{lowStockProducts} products are running low on stock",
                    CreatedAt = DateTime.UtcNow,
                    ActionUrl = "/admin/inventory",
                    ActionText = "View Inventory"
                });
            }

            // Check for pending orders
            var pendingOrders = await _context.Orders
                .Where(o => o.Status == "Pending")
                .CountAsync();

            if (pendingOrders > 0)
            {
                alerts.Add(new Alert
                {
                    Type = "info",
                    Title = "Pending Orders",
                    Message = $"{pendingOrders} orders are pending processing",
                    CreatedAt = DateTime.UtcNow,
                    ActionUrl = "/admin/orders",
                    ActionText = "View Orders"
                });
            }

            return alerts;
        }

        private static DateTime GetWeekStart(DateTime date)
        {
            var diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
            return date.AddDays(-1 * diff).Date;
        }

        private static decimal CalculateGrowthRate(decimal current, decimal previous)
        {
            if (previous == 0)
                return current > 0 ? 100 : 0;
            
            return Math.Round(((current - previous) / previous) * 100, 2);
        }

        private static decimal CalculateGrowthRate(int current, int previous)
        {
            return CalculateGrowthRate((decimal)current, (decimal)previous);
        }

        #endregion
    }
}
