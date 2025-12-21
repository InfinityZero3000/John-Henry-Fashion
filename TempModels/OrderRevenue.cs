using System;
using System.Collections.Generic;

namespace JohnHenryFashionWeb.TempModels;

/// <summary>
/// Bảng lưu revenue chỉ khi user đã xác nhận nhận hàng
/// </summary>
public partial class OrderRevenue
{
    public Guid Id { get; set; }

    public Guid OrderId { get; set; }

    public string? OrderNumber { get; set; }

    public string UserId { get; set; } = null!;

    public string? SellerId { get; set; }

    public decimal TotalAmount { get; set; }

    public decimal? ShippingFee { get; set; }

    public decimal? Tax { get; set; }

    public decimal? DiscountAmount { get; set; }

    public decimal NetRevenue { get; set; }

    public decimal? CommissionRate { get; set; }

    public decimal CommissionAmount { get; set; }

    public decimal SellerEarning { get; set; }

    public DateTime CalculatedAt { get; set; }

    public string? Notes { get; set; }

    public virtual Order Order { get; set; } = null!;
}
