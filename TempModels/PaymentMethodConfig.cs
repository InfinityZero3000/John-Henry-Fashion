using System;
using System.Collections.Generic;

namespace JohnHenryFashionWeb.TempModels;

public partial class PaymentMethodConfig
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public string Code { get; set; } = null!;

    public string? Description { get; set; }

    public string? LogoUrl { get; set; }

    public bool IsActive { get; set; }

    public decimal TransactionFeePercent { get; set; }

    public decimal TransactionFeeFixed { get; set; }

    public int SortOrder { get; set; }

    public string? ApiConfiguration { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
