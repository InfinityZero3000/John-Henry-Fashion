using System;
using System.Collections.Generic;

namespace JohnHenryFashionWeb.TempModels;

public partial class WithdrawalRequest
{
    public Guid Id { get; set; }

    public string SellerId { get; set; } = null!;

    public string WithdrawalNumber { get; set; } = null!;

    public decimal Amount { get; set; }

    public string BankName { get; set; } = null!;

    public string AccountNumber { get; set; } = null!;

    public string AccountName { get; set; } = null!;

    public string? Branch { get; set; }

    public string Status { get; set; } = null!;

    public string? RejectionReason { get; set; }

    public string? TransactionReference { get; set; }

    public DateTime RequestedAt { get; set; }

    public DateTime? ApprovedAt { get; set; }

    public DateTime? ProcessedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public string? ProcessedBy { get; set; }

    public string? AdminNotes { get; set; }

    public virtual AspNetUser? ProcessedByNavigation { get; set; }

    public virtual AspNetUser Seller { get; set; } = null!;
}
