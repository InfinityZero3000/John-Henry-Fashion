using System;
using System.Collections.Generic;

namespace JohnHenryFashionWeb.TempModels;

public partial class InventoryItem
{
    public Guid Id { get; set; }

    public Guid ProductId { get; set; }

    public string ProductName { get; set; } = null!;

    public string Sku { get; set; } = null!;

    public int CurrentStock { get; set; }

    public int MinStock { get; set; }

    public int MaxStock { get; set; }

    public decimal CostPrice { get; set; }

    public DateTime LastUpdated { get; set; }

    public string? Location { get; set; }

    public string? Notes { get; set; }

    public virtual Product Product { get; set; } = null!;

    public virtual ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();
}
