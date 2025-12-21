using System;
using System.Collections.Generic;

namespace JohnHenryFashionWeb.TempModels;

public partial class Dispute
{
    public Guid Id { get; set; }

    public string DisputeNumber { get; set; } = null!;

    public Guid OrderId { get; set; }

    public string CustomerId { get; set; } = null!;

    public string? SellerId { get; set; }

    public string Reason { get; set; } = null!;

    public string Description { get; set; } = null!;

    public string Status { get; set; } = null!;

    public decimal DisputedAmount { get; set; }

    public decimal? RefundAmount { get; set; }

    public string? Resolution { get; set; }

    public string? ResolutionType { get; set; }

    public string? ResolvedBy { get; set; }

    public string? EvidenceUrls { get; set; }

    public string? SellerResponse { get; set; }

    public DateTime? SellerRespondedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? ResolvedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual AspNetUser Customer { get; set; } = null!;

    public virtual Order Order { get; set; } = null!;

    public virtual AspNetUser? ResolvedByNavigation { get; set; }

    public virtual AspNetUser? Seller { get; set; }
}
