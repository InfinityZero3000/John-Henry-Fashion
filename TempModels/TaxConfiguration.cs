using System;
using System.Collections.Generic;

namespace JohnHenryFashionWeb.TempModels;

public partial class TaxConfiguration
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public string TaxType { get; set; } = null!;

    public decimal Rate { get; set; }

    public string? Region { get; set; }

    public string? Province { get; set; }

    public bool IsActive { get; set; }

    public bool ApplyToShipping { get; set; }

    public string? ExemptCategories { get; set; }

    public DateTime EffectiveFrom { get; set; }

    public DateTime? EffectiveTo { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
