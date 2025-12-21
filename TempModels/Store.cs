using System;
using System.Collections.Generic;

namespace JohnHenryFashionWeb.TempModels;

public partial class Store
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public string Address { get; set; } = null!;

    public string? Phone { get; set; }

    public string City { get; set; } = null!;

    public string Province { get; set; } = null!;

    public string Brand { get; set; } = null!;

    public string StoreType { get; set; } = null!;

    public double? Latitude { get; set; }

    public double? Longitude { get; set; }

    public string? Description { get; set; }

    public string? ImageUrl { get; set; }

    public bool IsActive { get; set; }

    public string? WorkingHours { get; set; }

    public string? Email { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string? Website { get; set; }

    public string? SocialMedia { get; set; }

    public virtual ICollection<SellerStore> SellerStores { get; set; } = new List<SellerStore>();
}
