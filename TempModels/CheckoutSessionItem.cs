using System;
using System.Collections.Generic;

namespace JohnHenryFashionWeb.TempModels;

public partial class CheckoutSessionItem
{
    public int Id { get; set; }

    public Guid CheckoutSessionId { get; set; }

    public Guid ProductId { get; set; }

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal TotalPrice { get; set; }

    public string? Size { get; set; }

    public string? Color { get; set; }

    public string? ProductName { get; set; }

    public string? ProductImage { get; set; }

    public virtual CheckoutSession CheckoutSession { get; set; } = null!;

    public virtual Product Product { get; set; } = null!;
}
