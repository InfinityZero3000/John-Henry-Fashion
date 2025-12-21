using System;
using System.Collections.Generic;

namespace JohnHenryFashionWeb.TempModels;

public partial class VwAdminRevenueReport
{
    public Guid? RevenueId { get; set; }

    public Guid? OrderId { get; set; }

    public string? OrderNumber { get; set; }

    public DateTime? RevenueDate { get; set; }

    public DateTime? OrderDate { get; set; }

    public DateTime? DeliveredDate { get; set; }

    public DateTime? UserConfirmedDate { get; set; }

    public decimal? TotalAmount { get; set; }

    public decimal? DiscountAmount { get; set; }

    public decimal? NetRevenue { get; set; }

    public decimal? CommissionRate { get; set; }

    public decimal? PlatformRevenue { get; set; }

    public decimal? SellerEarning { get; set; }

    public string? CustomerEmail { get; set; }

    public string? SellerEmail { get; set; }

    public decimal? DaysToRevenue { get; set; }
}
