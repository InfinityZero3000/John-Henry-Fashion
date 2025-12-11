namespace JohnHenryFashionWeb.ViewModels;

public class ProductViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string FeaturedImageUrl { get; set; } = string.Empty;
    public List<string> Images { get; set; } = new();
    public List<string> AvailableColors { get; set; } = new();
    public List<string> AvailableSizes { get; set; } = new();
    public int StockQuantity { get; set; }
    public string Description { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string? BrandName { get; set; }
    public decimal? Rating { get; set; }
    public int ReviewCount { get; set; }
    public bool IsNew { get; set; }
    public bool IsFeatured { get; set; }
    public string? Collection { get; set; }
    
    // Review details
    public List<ProductReviewViewModel> Reviews { get; set; } = new();
    public Dictionary<int, int> RatingStats { get; set; } = new(); // Rating (1-5) -> Count
}

public class ProductReviewViewModel
{
    public Guid Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string? UserAvatar { get; set; }
    public int Rating { get; set; }
    public string? Title { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsVerifiedPurchase { get; set; }
}

public class ProductDetailViewModel
{
    public ProductViewModel Product { get; set; } = new();
    public List<ProductViewModel> RelatedProducts { get; set; } = new();
    public List<string> Breadcrumbs { get; set; } = new();
}

public class HomePageViewModel
{
    public List<ProductViewModel> JohnHenryProducts { get; set; } = new();
    public List<ProductViewModel> FreelancerProducts { get; set; } = new();
    public List<ProductViewModel> BestSellerProducts { get; set; } = new();
    public List<CategoryViewModel> FeaturedCategories { get; set; } = new();
}

public class CategoryViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public int ProductCount { get; set; }
    public string? Description { get; set; }
}
