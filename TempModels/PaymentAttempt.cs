using System;
using System.Collections.Generic;

namespace JohnHenryFashionWeb.TempModels;

public partial class PaymentAttempt
{
    public int Id { get; set; }

    public string PaymentId { get; set; } = null!;

    public string OrderId { get; set; } = null!;

    public string UserId { get; set; } = null!;

    public decimal Amount { get; set; }

    public string Currency { get; set; } = null!;

    public string PaymentMethod { get; set; } = null!;

    public string Status { get; set; } = null!;

    public string? TransactionId { get; set; }

    public string? ErrorMessage { get; set; }

    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public Guid OrderId1 { get; set; }

    public virtual Order OrderId1Navigation { get; set; } = null!;

    public virtual AspNetUser User { get; set; } = null!;
}
