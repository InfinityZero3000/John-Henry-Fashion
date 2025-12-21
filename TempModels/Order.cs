using System;
using System.Collections.Generic;

namespace JohnHenryFashionWeb.TempModels;

public partial class Order
{
    public Guid Id { get; set; }

    public string OrderNumber { get; set; } = null!;

    public string UserId { get; set; } = null!;

    public string Status { get; set; } = null!;

    public decimal TotalAmount { get; set; }

    public decimal ShippingFee { get; set; }

    public decimal Tax { get; set; }

    public decimal DiscountAmount { get; set; }

    public string? CouponCode { get; set; }

    public string PaymentMethod { get; set; } = null!;

    public string PaymentStatus { get; set; } = null!;

    public string? Notes { get; set; }

    public string ShippingAddress { get; set; } = null!;

    public string BillingAddress { get; set; } = null!;

    public DateTime? ShippedAt { get; set; }

    public DateTime? DeliveredAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string? SellerId { get; set; }

    /// <summary>
    /// Seller đã xác nhận đơn hàng chưa
    /// </summary>
    public bool? IsSellerConfirmed { get; set; }

    public DateTime? SellerConfirmedAt { get; set; }

    public string? SellerConfirmedBy { get; set; }

    /// <summary>
    /// User đã xác nhận nhận hàng chưa
    /// </summary>
    public bool? IsUserConfirmedDelivery { get; set; }

    public DateTime? UserConfirmedDeliveryAt { get; set; }

    /// <summary>
    /// Đã tính revenue cho admin chưa
    /// </summary>
    public bool? IsRevenueCalculated { get; set; }

    public DateTime? RevenueCalculatedAt { get; set; }

    public virtual ICollection<ConversionEvent> ConversionEvents { get; set; } = new List<ConversionEvent>();

    public virtual ICollection<Dispute> Disputes { get; set; } = new List<Dispute>();

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual OrderRevenue? OrderRevenue { get; set; }

    public virtual ICollection<OrderStatusHistory> OrderStatusHistories { get; set; } = new List<OrderStatusHistory>();

    public virtual ICollection<PaymentAttempt> PaymentAttempts { get; set; } = new List<PaymentAttempt>();

    public virtual ICollection<PaymentTransaction> PaymentTransactions { get; set; } = new List<PaymentTransaction>();

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual ICollection<RefundRequest> RefundRequests { get; set; } = new List<RefundRequest>();

    public virtual ICollection<SupportTicket> SupportTickets { get; set; } = new List<SupportTicket>();

    public virtual AspNetUser User { get; set; } = null!;
}
