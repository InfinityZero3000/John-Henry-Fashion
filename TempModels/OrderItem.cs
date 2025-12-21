using System;
using System.Collections.Generic;

namespace JohnHenryFashionWeb.TempModels;

public partial class OrderItem
{
    public Guid Id { get; set; }

    public Guid OrderId { get; set; }

    public Guid ProductId { get; set; }

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal TotalPrice { get; set; }

    public string? ProductName { get; set; }

    public string? ProductSku { get; set; }

    public string? ProductImage { get; set; }

    public virtual Order Order { get; set; } = null!;

    public virtual Product Product { get; set; } = null!;
}
