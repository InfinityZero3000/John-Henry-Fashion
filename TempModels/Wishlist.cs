using System;
using System.Collections.Generic;

namespace JohnHenryFashionWeb.TempModels;

public partial class Wishlist
{
    public Guid Id { get; set; }

    public string UserId { get; set; } = null!;

    public Guid ProductId { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Product Product { get; set; } = null!;

    public virtual AspNetUser User { get; set; } = null!;
}
