using System;
using System.Collections.Generic;

namespace JohnHenryFashionWeb.TempModels;

public partial class CheckoutSession
{
    public Guid Id { get; set; }

    public string UserId { get; set; } = null!;

    public string? Email { get; set; }

    public string Status { get; set; } = null!;

    public decimal TotalAmount { get; set; }

    public decimal ShippingFee { get; set; }

    public decimal Tax { get; set; }

    public decimal DiscountAmount { get; set; }

    public string? CouponCode { get; set; }

    public string? ShippingMethod { get; set; }

    public string? PaymentMethod { get; set; }

    public string? ShippingAddress { get; set; }

    public string? BillingAddress { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public DateTime ExpiresAt { get; set; }

    public virtual ICollection<CheckoutSessionItem> CheckoutSessionItems { get; set; } = new List<CheckoutSessionItem>();

    public virtual AspNetUser User { get; set; } = null!;
}
