using System;
using System.Collections.Generic;

namespace JohnHenryFashionWeb.TempModels;

public partial class Product
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public string Slug { get; set; } = null!;

    public string? Description { get; set; }

    public string? ShortDescription { get; set; }

    public string Sku { get; set; } = null!;

    public decimal Price { get; set; }

    public decimal? SalePrice { get; set; }

    public int StockQuantity { get; set; }

    public bool ManageStock { get; set; }

    public bool InStock { get; set; }

    public string? FeaturedImageUrl { get; set; }

    public List<string>? GalleryImages { get; set; }

    public string? Size { get; set; }

    public string? Color { get; set; }

    public string? Material { get; set; }

    public decimal? Weight { get; set; }

    public string? Dimensions { get; set; }

    public bool IsFeatured { get; set; }

    public bool IsActive { get; set; }

    public string Status { get; set; } = null!;

    public int ViewCount { get; set; }

    public decimal? Rating { get; set; }

    public int ReviewCount { get; set; }

    public Guid CategoryId { get; set; }

    public Guid? BrandId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string ApprovalStatus { get; set; } = null!;

    public DateTime? ApprovedAt { get; set; }

    public string? ApprovedBy { get; set; }

    public string? RejectionReason { get; set; }

    public string? SellerId { get; set; }

    public virtual Brand? Brand { get; set; }

    public virtual Category Category { get; set; } = null!;

    public virtual ICollection<CheckoutSessionItem> CheckoutSessionItems { get; set; } = new List<CheckoutSessionItem>();

    public virtual InventoryItem? InventoryItem { get; set; }

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual ICollection<ProductApproval> ProductApprovals { get; set; } = new List<ProductApproval>();

    public virtual ICollection<ProductImage> ProductImages { get; set; } = new List<ProductImage>();

    public virtual ICollection<ProductReview> ProductReviews { get; set; } = new List<ProductReview>();

    public virtual AspNetUser? Seller { get; set; }

    public virtual ICollection<ShoppingCartItem> ShoppingCartItems { get; set; } = new List<ShoppingCartItem>();

    public virtual ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();

    public virtual ICollection<SupportTicket> SupportTickets { get; set; } = new List<SupportTicket>();

    public virtual ICollection<Wishlist> Wishlists { get; set; } = new List<Wishlist>();
}
