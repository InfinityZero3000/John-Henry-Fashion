using System;
using System.Collections.Generic;

namespace JohnHenryFashionWeb.TempModels;

public partial class ProductApprovalHistory
{
    public Guid Id { get; set; }

    public Guid ProductApprovalId { get; set; }

    public string Action { get; set; } = null!;

    public string? Notes { get; set; }

    public string PerformedBy { get; set; } = null!;

    public string PerformedByType { get; set; } = null!;

    public string? PreviousStatus { get; set; }

    public string? NewStatus { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual AspNetUser PerformedByNavigation { get; set; } = null!;

    public virtual ProductApproval ProductApproval { get; set; } = null!;
}
