using System;
using System.Collections.Generic;

namespace JohnHenryFashionWeb.TempModels;

public partial class SalesReport
{
    public Guid Id { get; set; }

    public string ReportType { get; set; } = null!;

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public decimal TotalRevenue { get; set; }

    public int TotalOrders { get; set; }

    public int TotalProducts { get; set; }

    public decimal AverageOrderValue { get; set; }

    public string? ReportData { get; set; }

    public DateTime GeneratedAt { get; set; }

    public string? GeneratedBy { get; set; }

    public string Status { get; set; } = null!;

    public string? GeneratedByUserId { get; set; }

    public virtual AspNetUser? GeneratedByUser { get; set; }
}
