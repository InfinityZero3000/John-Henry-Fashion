using System;
using System.Collections.Generic;

namespace JohnHenryFashionWeb.TempModels;

public partial class SellerSettlement
{
    public Guid Id { get; set; }

    public string SellerId { get; set; } = null!;

    public string SettlementNumber { get; set; } = null!;

    public decimal TotalRevenue { get; set; }

    public decimal PlatformFee { get; set; }

    public decimal NetAmount { get; set; }

    public decimal PreviousBalance { get; set; }

    public decimal FinalBalance { get; set; }

    public DateTime PeriodStart { get; set; }

    public DateTime PeriodEnd { get; set; }

    public string Status { get; set; } = null!;

    public DateTime? SettledAt { get; set; }

    public string? SettledBy { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual AspNetUser Seller { get; set; } = null!;

    public virtual AspNetUser? SettledByNavigation { get; set; }
}
