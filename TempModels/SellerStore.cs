using System;
using System.Collections.Generic;

namespace JohnHenryFashionWeb.TempModels;

public partial class SellerStore
{
    public Guid Id { get; set; }

    public string SellerId { get; set; } = null!;

    public Guid StoreId { get; set; }

    public string Role { get; set; } = null!;

    public DateTime AssignedAt { get; set; }

    public bool IsActive { get; set; }

    public virtual AspNetUser Seller { get; set; } = null!;

    public virtual Store Store { get; set; } = null!;
}
