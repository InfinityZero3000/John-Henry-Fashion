using JohnHenryFashionWeb.Models;

namespace JohnHenryFashionWeb.ViewModels
{
    // Reports ViewModel for Reports page
    public class ReportsViewModel
    {
        // KPI Data
        public KPIData KPIData { get; set; } = new();
        
        // Sales Analytics
        public SalesAnalyticsData SalesAnalytics { get; set; } = new();
        
        // Customer Analytics
        public CustomerAnalyticsData CustomerAnalytics { get; set; } = new();
        
        // Product Analytics
        public ProductAnalyticsData ProductAnalytics { get; set; } = new();
        
        // Marketing Analytics
        public MarketingAnalyticsData MarketingAnalytics { get; set; } = new();
        
        // Chart Data
        public List<Models.ChartData> RevenueChartData { get; set; } = new();
        public List<Models.ChartData> TopProductsData { get; set; } = new();
        public List<Models.ChartData> CategorySalesData { get; set; } = new();
        public List<Models.ChartData> PaymentMethodData { get; set; } = new();
        public List<Models.ChartData> CategoryPerformanceData { get; set; } = new();
        public List<Models.ChartData> ProductLifecycleData { get; set; } = new();
        public List<Models.ChartData> TrafficSourceData { get; set; } = new();
        public List<Models.ChartData> ChannelROIData { get; set; } = new();
        
        // Customer Segments
        public List<CustomerSegment> CustomerSegments { get; set; } = new();
        
        // Time range info
        public DateTime StartDate { get; set; } = DateTime.UtcNow.AddDays(-30);
        public DateTime EndDate { get; set; } = DateTime.UtcNow;
        public string DateRange { get; set; } = "30 ngày qua";
        
        // Comparison data
        public ComparisonPeriodData ComparisonData { get; set; } = new();
    }

    // KPI Data for dashboard cards
    public class KPIData
    {
        public decimal TotalRevenue { get; set; }
        public decimal RevenueGrowth { get; set; }
        public int CompletedOrders { get; set; }
        public decimal OrdersGrowth { get; set; }
        public int NewCustomers { get; set; }
        public decimal CustomersGrowth { get; set; }
        public decimal ConversionRate { get; set; }
        public decimal ConversionGrowth { get; set; }
    }

    // Customer Segment for table data
    public class CustomerSegment
    {
        public string Name { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal Percentage { get; set; }
        public decimal Revenue { get; set; }
        public decimal AverageOrderValue { get; set; }
        public decimal GrowthRate { get; set; }
    }

    // Comparison data between periods
    public class ComparisonPeriodData
    {
        public decimal PreviousRevenue { get; set; }
        public int PreviousOrders { get; set; }
        public int PreviousCustomers { get; set; }
        public decimal PreviousConversionRate { get; set; }
    }

    // Analytics Data Classes for Advanced Reports
    public class SalesAnalyticsData
    {
        public decimal TotalRevenue { get; set; }
        public int TotalOrders { get; set; }
        public decimal AverageOrderValue { get; set; }
        public decimal GrowthRate { get; set; }
        public decimal MonthlyGrowthRate { get; set; }
        public List<MonthlyData> MonthlyData { get; set; } = new();
    }

    public class CustomerAnalyticsData
    {
        public int TotalCustomers { get; set; }
        public int NewCustomers { get; set; }
        public decimal ReturnRate { get; set; }
        public decimal CustomerLifetimeValue { get; set; }
        public decimal GrowthRate { get; set; }
        public List<CustomerGrowthData> GrowthData { get; set; } = new();
    }

    public class ProductAnalyticsData
    {
        public int TotalProducts { get; set; }
        public int NewProducts { get; set; }
        public int OutOfStock { get; set; }
        public decimal AvailabilityRate { get; set; }
        public decimal GrowthRate { get; set; }
        public List<ProductCategoryData> CategoryData { get; set; } = new();
    }

    public class MarketingAnalyticsData
    {
        public int TotalVisits { get; set; }
        public decimal ConversionRate { get; set; }
        public decimal AdvertisingCost { get; set; }
        public decimal ROAS { get; set; }
        public decimal GrowthRate { get; set; }
        public List<TrafficSourceData> TrafficSources { get; set; } = new();
    }

    public class MonthlyData
    {
        public string Month { get; set; } = "";
        public decimal Revenue { get; set; }
        public int Orders { get; set; }
        public DateTime Date { get; set; }
    }

    public class CustomerGrowthData
    {
        public DateTime Date { get; set; }
        public int NewCustomers { get; set; }
        public int TotalCustomers { get; set; }
        public decimal GrowthRate { get; set; }
    }

    public class ProductCategoryData
    {
        public string CategoryName { get; set; } = "";
        public int ProductCount { get; set; }
        public decimal Revenue { get; set; }
        public decimal GrowthRate { get; set; }
    }

    public class TrafficSourceData
    {
        public string Source { get; set; } = "";
        public int Visitors { get; set; }
        public decimal ConversionRate { get; set; }
        public decimal Revenue { get; set; }
    }

    // Main Dashboard ViewModel
    public class DashboardViewModel
    {
        public DashboardSummary Summary { get; set; } = new();
        public DashboardSummary DashboardSummary { get; set; } = new();
        public List<Models.ChartData> SalesChartData { get; set; } = new();
        public List<Models.ChartData> TopProductsData { get; set; } = new();
        public List<TimeSeriesData> RevenueTimeSeriesData { get; set; } = new();
        public List<CategoryPerformanceData> CategoryPerformance { get; set; } = new();
        public RealTimeAnalyticsData RealTimeData { get; set; } = new();
        public PerformanceMetrics PerformanceMetrics { get; set; } = new();
        public List<GeographicData> GeographicData { get; set; } = new();
        public DeviceAnalytics DeviceAnalytics { get; set; } = new();
        
        // Enhanced dashboard data
        public List<Order> RecentOrders { get; set; } = new();
        public List<ApplicationUser> RecentUsers { get; set; } = new();
        public List<Product> LowStockProducts { get; set; } = new();
        public DashboardStats? TodayStats { get; set; }
        public WeeklyComparisonStats? WeeklyComparison { get; set; }
        public SystemHealthMetrics? SystemHealth { get; set; }
        public List<SystemAlert> SystemAlerts { get; set; } = new();
        public QuickAction[] QuickActions { get; set; } = Array.Empty<QuickAction>();
        
        // Filter options
        public AnalyticsFilter CurrentFilter { get; set; } = new();
        public List<DateRange> AvailableDateRanges { get; set; } = new();
        public List<Product> AvailableProducts { get; set; } = new();
        public List<Category> AvailableCategories { get; set; } = new();
        
        // UI State
        public string SelectedTab { get; set; } = "overview";
        public bool IsLoading { get; set; } = false;
        public string? ErrorMessage { get; set; }
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }

    // Dashboard Support Classes
    public class DashboardStats
    {
        public decimal TodayRevenue { get; set; }
        public int TodayOrders { get; set; }
        public int TodayVisitors { get; set; }
        public int ActiveUsers { get; set; }
    }

    public class SystemHealthMetrics
    {
        public double OverallHealth { get; set; }
        public string HealthStatus { get; set; } = "Hoạt động tốt";
        public double CpuUsage { get; set; }
        public double MemoryUsage { get; set; }
        public double StorageUsage { get; set; }
        public int OnlineUsers { get; set; }
        public long TotalRequests { get; set; }
        public double AverageResponseTime { get; set; }
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }

    public class WeeklyComparisonStats
    {
        public decimal ThisWeekRevenue { get; set; }
        public decimal LastWeekRevenue { get; set; }
        public int ThisWeekOrders { get; set; }
        public int LastWeekOrders { get; set; }
    }

    public class SystemAlert
    {
        public string Type { get; set; } = "";
        public string Message { get; set; } = "";
        public string Action { get; set; } = "";
        public string Icon { get; set; } = "";
    }

    public class QuickAction
    {
        public string Title { get; set; } = "";
        public string Icon { get; set; } = "";
        public string Url { get; set; } = "";
        public string Color { get; set; } = "";
    }

    // Missing classes for AdminController
    public class ReportGenerationRequest
    {
        public string ReportType { get; set; } = "";
        public string Format { get; set; } = "excel";
        public DateTime StartDate { get; set; } = DateTime.UtcNow.AddDays(-30);
        public DateTime EndDate { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> Parameters { get; set; } = new();
        public bool IncludeCharts { get; set; } = true;
        public bool IncludeSummary { get; set; } = true;
        public string? EmailTo { get; set; }
    }

    public class ReportScheduleSettings
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string ReportType { get; set; } = "";
        public string Format { get; set; } = "excel";
        public string Frequency { get; set; } = "weekly";
        public string? EmailTo { get; set; }
        public Dictionary<string, object> Parameters { get; set; } = new();
        public bool IsActive { get; set; } = true;
        public DateTime StartDate { get; set; } = DateTime.UtcNow;
        public DateTime? EndDate { get; set; }
    }

    public class ExportFormat
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Extension { get; set; } = "";
        public string MimeType { get; set; } = "";
        public bool IsAvailable { get; set; } = true;
    }

    public class ReportType
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public List<string> RequiredParameters { get; set; } = new();
        public List<string> OptionalParameters { get; set; } = new();
    }

    // Additional missing classes for AdminController

    public class AnalyticsViewConfiguration
    {
        public List<string> VisibleCharts { get; set; } = new();
        public Dictionary<string, ChartConfiguration> ChartConfigs { get; set; } = new();
        public bool ShowComparison { get; set; } = true;
        public bool ShowRealTime { get; set; } = true;
        public string DefaultTimeRange { get; set; } = "last30days";
        public string Theme { get; set; } = "light";
    }

    public class ChartConfiguration
    {
        public string Type { get; set; } = "";
        public bool Visible { get; set; } = true;
        public int Order { get; set; } = 0;
        public Dictionary<string, object> Options { get; set; } = new();
    }

    public class LiveMetrics
    {
        public int ActiveUsers { get; set; }
        public int PageViewsPerMinute { get; set; }
        public int OrdersPerHour { get; set; }
        public decimal RevenuePerHour { get; set; }
        public double CurrentConversionRate { get; set; }
        public TimeSpan AverageSessionDuration { get; set; }
        public List<TopPage> TopPages { get; set; } = new();
        public List<TopReferrer> TopReferrers { get; set; } = new();
    }

    public class TopPage
    {
        public string Path { get; set; } = "";
        public int ActiveUsers { get; set; }
        public int ViewsLastHour { get; set; }
    }

    public class TopReferrer
    {
        public string Source { get; set; } = "";
        public int Visitors { get; set; }
        public string Type { get; set; } = "";
    }

    public class RealTimeDashboardViewModel
    {
        public RealTimeAnalyticsData RealTimeData { get; set; } = new();
        public List<LiveEvent> RecentEvents { get; set; } = new();
        public LivePerformanceData Performance { get; set; } = new();
    }

    public class LiveEvent
    {
        public string Type { get; set; } = "";
        public string Description { get; set; } = "";
        public DateTime Timestamp { get; set; }
        public string? UserId { get; set; }
        public string? SessionId { get; set; }
        public Dictionary<string, object> Data { get; set; } = new();
    }

    public class LivePerformanceData
    {
        public TimeSpan AverageResponseTime { get; set; }
        public double ErrorRate { get; set; }
        public int RequestsPerMinute { get; set; }
        public List<ServerMetric> ServerMetrics { get; set; } = new();
    }

    public class ServerMetric
    {
        public string Name { get; set; } = "";
        public decimal Value { get; set; }
        public string Unit { get; set; } = "";
        public string Status { get; set; } = "normal";
    }

    public class RealTimeAlert
    {
        public string Id { get; set; } = "";
        public string Type { get; set; } = "";
        public string Severity { get; set; } = "";
        public string Title { get; set; } = "";
        public string Message { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public bool IsAcknowledged { get; set; }
        public string? ActionUrl { get; set; }
    }

    public class AlertSettings
    {
        public bool EnableAlerts { get; set; } = true;
        public Dictionary<string, AlertThreshold> Thresholds { get; set; } = new();
        public List<string> NotificationChannels { get; set; } = new();
        public bool PlaySounds { get; set; } = true;
        public bool ShowPopups { get; set; } = true;
    }

    public class AlertThreshold
    {
        public decimal WarningValue { get; set; }
        public decimal CriticalValue { get; set; }
        public string ComparisonOperator { get; set; } = "greater_than";
        public bool IsEnabled { get; set; } = true;
    }

    public class UserAnalyticsData
    {
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int NewUsers { get; set; }
        public decimal GrowthRate { get; set; }
    }

    public class ConversionAnalyticsData
    {
        public decimal ConversionRate { get; set; }
        public int TotalConversions { get; set; }
        public decimal Revenue { get; set; }
        public decimal AverageOrderValue { get; set; }
    }
}