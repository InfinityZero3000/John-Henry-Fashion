using System;
using System.Collections.Generic;

namespace JohnHenryFashionWeb.TempModels;

public partial class ShippingMethod
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string Code { get; set; } = null!;

    public string Description { get; set; } = null!;

    public decimal Cost { get; set; }

    public int EstimatedDays { get; set; }

    public bool IsActive { get; set; }

    public decimal? MinOrderAmount { get; set; }

    public decimal? MaxWeight { get; set; }

    public string? AvailableRegions { get; set; }

    public int SortOrder { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
