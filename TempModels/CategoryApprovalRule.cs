using System;
using System.Collections.Generic;

namespace JohnHenryFashionWeb.TempModels;

public partial class CategoryApprovalRule
{
    public Guid Id { get; set; }

    public Guid CategoryId { get; set; }

    public bool RequiresManualApproval { get; set; }

    public bool RequiresDetailedDescription { get; set; }

    public int MinimumImages { get; set; }

    public bool RequiresBrandVerification { get; set; }

    public bool RequiresCertification { get; set; }

    public string? RequiredFields { get; set; }

    public string? ApprovalTier { get; set; }

    public int ExpectedReviewTimeDays { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual Category Category { get; set; } = null!;
}
