using System;
using System.Collections.Generic;

namespace JohnHenryFashionWeb.TempModels;

public partial class FlashSale
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public string? BannerImageUrl { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public bool IsActive { get; set; }

    public decimal? DiscountPercentage { get; set; }

    public string? ProductIds { get; set; }

    public int? StockLimit { get; set; }

    public int SoldCount { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string? CreatedBy { get; set; }

    public virtual AspNetUser? CreatedByNavigation { get; set; }
}
