using System;
using System.Collections.Generic;

namespace JohnHenryFashionWeb.TempModels;

public partial class StockMovement
{
    public Guid Id { get; set; }

    public Guid ProductId { get; set; }

    public string Type { get; set; } = null!;

    public int Quantity { get; set; }

    public string? Reason { get; set; }

    public string? Reference { get; set; }

    public string UserId { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public Guid? InventoryItemId { get; set; }

    public virtual InventoryItem? InventoryItem { get; set; }

    public virtual Product Product { get; set; } = null!;

    public virtual AspNetUser User { get; set; } = null!;
}
