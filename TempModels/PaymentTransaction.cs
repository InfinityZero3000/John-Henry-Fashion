using System;
using System.Collections.Generic;

namespace JohnHenryFashionWeb.TempModels;

public partial class PaymentTransaction
{
    public Guid Id { get; set; }

    public Guid OrderId { get; set; }

    public string UserId { get; set; } = null!;

    public string? SellerId { get; set; }

    public decimal Amount { get; set; }

    public decimal PlatformFee { get; set; }

    public decimal SellerAmount { get; set; }

    public string PaymentMethod { get; set; } = null!;

    public string Status { get; set; } = null!;

    public string? TransactionReference { get; set; }

    public string? PaymentGateway { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public DateTime? RefundedAt { get; set; }

    public virtual Order Order { get; set; } = null!;

    public virtual AspNetUser? Seller { get; set; }

    public virtual AspNetUser User { get; set; } = null!;
}
