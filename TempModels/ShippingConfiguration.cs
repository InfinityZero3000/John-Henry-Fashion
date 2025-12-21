using System;
using System.Collections.Generic;

namespace JohnHenryFashionWeb.TempModels;

public partial class ShippingConfiguration
{
    public Guid Id { get; set; }

    public string ProviderName { get; set; } = null!;

    public string ProviderCode { get; set; } = null!;

    public string? Description { get; set; }

    public string? LogoUrl { get; set; }

    public bool IsActive { get; set; }

    public decimal BaseRate { get; set; }

    public decimal PerKgRate { get; set; }

    public decimal? FreeShippingThreshold { get; set; }

    public string? ZoneRates { get; set; }

    public string? ApiUrl { get; set; }

    public string? ApiKey { get; set; }

    public string? ApiSecret { get; set; }

    public string? ApiConfiguration { get; set; }

    public int EstimatedDeliveryDays { get; set; }

    public int SortOrder { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
