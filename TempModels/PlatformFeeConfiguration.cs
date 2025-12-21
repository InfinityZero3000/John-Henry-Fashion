using System;
using System.Collections.Generic;

namespace JohnHenryFashionWeb.TempModels;

public partial class PlatformFeeConfiguration
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public string? SellerTier { get; set; }

    public Guid? CategoryId { get; set; }

    public decimal FeePercent { get; set; }

    public decimal? MinFee { get; set; }

    public decimal? MaxFee { get; set; }

    public bool IsActive { get; set; }

    public DateTime EffectiveFrom { get; set; }

    public DateTime? EffectiveTo { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual Category? Category { get; set; }
}
