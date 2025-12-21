using System;
using System.Collections.Generic;

namespace JohnHenryFashionWeb.TempModels;

public partial class PaymentGatewayConfiguration
{
    public Guid Id { get; set; }

    public string GatewayName { get; set; } = null!;

    public string GatewayCode { get; set; } = null!;

    public string? Description { get; set; }

    public string? LogoUrl { get; set; }

    public bool IsActive { get; set; }

    public bool IsSandbox { get; set; }

    public string? ApiUrl { get; set; }

    public string? MerchantId { get; set; }

    public string? ApiKey { get; set; }

    public string? ApiSecret { get; set; }

    public string? Configuration { get; set; }

    public decimal TransactionFeePercent { get; set; }

    public decimal TransactionFeeFixed { get; set; }

    public int SortOrder { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
