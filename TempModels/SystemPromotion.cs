using System;
using System.Collections.Generic;

namespace JohnHenryFashionWeb.TempModels;

public partial class SystemPromotion
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public string Code { get; set; } = null!;

    public string? Description { get; set; }

    public string Type { get; set; } = null!;

    public decimal Value { get; set; }

    public decimal? MinOrderAmount { get; set; }

    public decimal? MaxDiscountAmount { get; set; }

    public int? UsageLimit { get; set; }

    public int? UsageLimitPerUser { get; set; }

    public int UsageCount { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public bool IsActive { get; set; }

    public string? ApplicableCategories { get; set; }

    public string? ApplicableProducts { get; set; }

    public string? ApplicableUserGroups { get; set; }

    public string? BannerImageUrl { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string? CreatedBy { get; set; }

    public virtual AspNetUser? CreatedByNavigation { get; set; }
}
