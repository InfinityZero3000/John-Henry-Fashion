using System;
using System.Collections.Generic;

namespace JohnHenryFashionWeb.TempModels;

public partial class MarketingBanner
{
    public Guid Id { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public string ImageUrl { get; set; } = null!;

    public string? MobileImageUrl { get; set; }

    public string? LinkUrl { get; set; }

    public bool OpenInNewTab { get; set; }

    public string Position { get; set; } = null!;

    public string? TargetPage { get; set; }

    public int SortOrder { get; set; }

    public bool IsActive { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public int ViewCount { get; set; }

    public int ClickCount { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string? CreatedBy { get; set; }

    public virtual AspNetUser? CreatedByNavigation { get; set; }
}
