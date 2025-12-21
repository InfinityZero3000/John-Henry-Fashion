using System;
using System.Collections.Generic;

namespace JohnHenryFashionWeb.TempModels;

public partial class OrderStatusHistory
{
    public int Id { get; set; }

    public Guid OrderId { get; set; }

    public string Status { get; set; } = null!;

    public string? Notes { get; set; }

    public string? ChangedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual AspNetUser? ChangedByNavigation { get; set; }

    public virtual Order Order { get; set; } = null!;
}
