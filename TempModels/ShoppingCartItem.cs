using System;
using System.Collections.Generic;

namespace JohnHenryFashionWeb.TempModels;

public partial class ShoppingCartItem
{
    public Guid Id { get; set; }

    public string UserId { get; set; } = null!;

    public Guid ProductId { get; set; }

    public int Quantity { get; set; }

    public string? Size { get; set; }

    public string? Color { get; set; }

    public decimal Price { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public DateTime ExpiresAt { get; set; }

    public virtual Product Product { get; set; } = null!;

    public virtual AspNetUser User { get; set; } = null!;
}
