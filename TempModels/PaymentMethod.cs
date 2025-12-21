using System;
using System.Collections.Generic;

namespace JohnHenryFashionWeb.TempModels;

public partial class PaymentMethod
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string Code { get; set; } = null!;

    public string Description { get; set; } = null!;

    public string? IconUrl { get; set; }

    public bool IsActive { get; set; }

    public bool RequiresRedirect { get; set; }

    public decimal? MinAmount { get; set; }

    public decimal? MaxAmount { get; set; }

    public string? SupportedCurrencies { get; set; }

    public int SortOrder { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
