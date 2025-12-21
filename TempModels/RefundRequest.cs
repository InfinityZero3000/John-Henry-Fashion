using System;
using System.Collections.Generic;

namespace JohnHenryFashionWeb.TempModels;

public partial class RefundRequest
{
    public Guid Id { get; set; }

    public string PaymentId { get; set; } = null!;

    public string OrderId { get; set; } = null!;

    public decimal Amount { get; set; }

    public string Reason { get; set; } = null!;

    public string Status { get; set; } = null!;

    public string? AdminNotes { get; set; }

    public string? RefundTransactionId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? ProcessedAt { get; set; }

    public string RequestedBy { get; set; } = null!;

    public string? ProcessedBy { get; set; }

    public Guid OrderId1 { get; set; }

    public string? RejectionReason { get; set; }

    public virtual Order OrderId1Navigation { get; set; } = null!;

    public virtual AspNetUser RequestedByNavigation { get; set; } = null!;
}
