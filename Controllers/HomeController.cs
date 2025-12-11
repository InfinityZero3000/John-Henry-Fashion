using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JohnHenryFashionWeb.Models;
using JohnHenryFashionWeb.Services;
using JohnHenryFashionWeb.Data;
using JohnHenryFashionWeb.ViewModels;

namespace JohnHenryFashionWeb.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly SeoService _seoService;
    private readonly ApplicationDbContext _context;

    public HomeController(ILogger<HomeController> logger, SeoService seoService, ApplicationDbContext context)
    {
        _logger = logger;
        _seoService = seoService;
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        // Generate SEO data for homepage
        var websiteJsonLd = _seoService.GenerateWebsiteJsonLd();
        var organizationJsonLd = _seoService.GenerateOrganizationJsonLd();

        // Generate breadcrumbs for homepage
        var breadcrumbs = new List<BreadcrumbItem>
        {
            new BreadcrumbItem { Name = "Trang chủ", Url = "" }
        };

        var breadcrumbJsonLd = _seoService.GenerateBreadcrumbJsonLd(breadcrumbs);

        ViewBag.WebsiteJsonLd = websiteJsonLd;
        ViewBag.OrganizationJsonLd = organizationJsonLd;
        ViewBag.BreadcrumbJsonLd = breadcrumbJsonLd;
        ViewBag.MetaTitle = "John Henry Fashion - Thời trang nam nữ cao cấp";
        ViewBag.MetaDescription = "Khám phá bộ sưu tập thời trang nam nữ cao cấp tại John Henry Fashion. Phong cách hiện đại, chất lượng tuyệt vời với giá cả hợp lý.";

        // Load featured products for homepage collections
        var johnHenrySKUs = new[] {
            "JK25FH04C-PA", "KS25SS20P-SC", "WS25FH63P-LC", "JK25FH02P-CT",
            "KS25FH49C-SLWK", "KS25SS31T-SCWK", "WS25FH58C-CFBB", "JK25FH10T-KA"
        };

        var freelancerSKUs = new[] {
            "FWBZ23SS06C", "FWWS25SS02G", "FWTS24FH03G", "FWTS25SS14C",
            "FWDR24FH01G", "FWDR25SS29G", "FWDR25FH04C", "FWSK23SS14G"
        };

        var bestSellerSKUs = new[] {
            "FWSK25FH12C", "FWSK24FH13C", "KP25SS06T-NMWFSL", "FWDP24SS06C",
            "JK25FH03C-CT", "BZ25FH06C-SL", "BZ24FH02P-SL", "WS24SS15P-SCRG"
        };

        // Load John Henry featured products
        var johnHenryProducts = await _context.Products
            .Where(p => p.IsActive && p.IsFeatured && johnHenrySKUs.Contains(p.SKU))
            .OrderBy(p => Array.IndexOf(johnHenrySKUs, p.SKU))
            .Take(8)
            .ToListAsync();

        // Load Freelancer featured products
        var freelancerProducts = await _context.Products
            .Where(p => p.IsActive && p.IsFeatured && freelancerSKUs.Contains(p.SKU))
            .OrderBy(p => Array.IndexOf(freelancerSKUs, p.SKU))
            .Take(8)
            .ToListAsync();

        // Load Best Seller featured products
        var bestSellerProducts = await _context.Products
            .Where(p => p.IsActive && p.IsFeatured && bestSellerSKUs.Contains(p.SKU))
            .OrderBy(p => Array.IndexOf(bestSellerSKUs, p.SKU))
            .Take(8)
            .ToListAsync();

        ViewBag.JohnHenryProducts = johnHenryProducts;
        ViewBag.FreelancerProducts = freelancerProducts;
        ViewBag.BestSellerProducts = bestSellerProducts;

        // Load latest blog posts for homepage
        var latestBlogPosts = await _context.BlogPosts
            .Include(b => b.Category)
            .Include(b => b.Author)
            .Where(b => b.Status == "published")
            .OrderByDescending(b => b.PublishedAt ?? b.CreatedAt)
            .Take(3)
            .ToListAsync();

        ViewBag.LatestBlogPosts = latestBlogPosts;

        // Load active marketing banners for homepage
        var now = DateTime.UtcNow;
        _logger.LogInformation($"Loading banners at {now:yyyy-MM-dd HH:mm:ss} UTC");
        
        // Hero carousel banners (main full-width banners)
        var heroCarouselBanners = await _context.MarketingBanners
            .Where(b => b.IsActive 
                && b.Position == "home_main"
                && b.StartDate <= now
                && (b.EndDate == null || b.EndDate >= now))
            .OrderBy(b => b.SortOrder)
            .ToListAsync();

        _logger.LogInformation($"Found {heroCarouselBanners.Count} hero carousel banners for home_main");
        
        ViewBag.HeroCarouselBanners = heroCarouselBanners;

        // Small banners (3 columns below hero carousel)
        var smallBanners = await _context.MarketingBanners
            .Where(b => b.IsActive 
                && b.Position == "home_side"
                && b.StartDate <= now
                && (b.EndDate == null || b.EndDate >= now))
            .OrderBy(b => b.SortOrder)
            .Take(3)
            .ToListAsync();

        ViewBag.SmallBanners = smallBanners;

        // Load collection hero banners for Index page collections
        var johnHenryCollectionBanner = await _context.MarketingBanners
            .Where(b => b.IsActive 
                && b.Position == "collection_hero"
                && b.TargetPage == "JohnHenry"
                && b.StartDate <= now
                && (b.EndDate == null || b.EndDate >= now))
            .OrderBy(b => b.SortOrder)
            .Take(1)
            .FirstOrDefaultAsync();

        ViewBag.JohnHenryCollectionBanner = johnHenryCollectionBanner;

        var freelancerCollectionBanner = await _context.MarketingBanners
            .Where(b => b.IsActive 
                && b.Position == "collection_hero"
                && b.TargetPage == "Freelancer"
                && b.StartDate <= now
                && (b.EndDate == null || b.EndDate >= now))
            .OrderBy(b => b.SortOrder)
            .Take(1)
            .FirstOrDefaultAsync();

        ViewBag.FreelancerCollectionBanner = freelancerCollectionBanner;

        var bestSellerCollectionBanner = await _context.MarketingBanners
            .Where(b => b.IsActive 
                && b.Position == "collection_hero"
                && b.TargetPage == "BestSeller"
                && b.StartDate <= now
                && (b.EndDate == null || b.EndDate >= now))
            .OrderBy(b => b.SortOrder)
            .Take(1)
            .FirstOrDefaultAsync();

        ViewBag.BestSellerCollectionBanner = bestSellerCollectionBanner;

        // Load category banners (Product Categories Section)
        var aoNamBanner = await _context.MarketingBanners
            .Where(b => b.IsActive 
                && b.Position == "category"
                && b.TargetPage == "AoNam"
                && b.StartDate <= now
                && (b.EndDate == null || b.EndDate >= now))
            .OrderBy(b => b.SortOrder)
            .Take(1)
            .FirstOrDefaultAsync();

        ViewBag.AoNamBanner = aoNamBanner;

        var aoNuBanner = await _context.MarketingBanners
            .Where(b => b.IsActive 
                && b.Position == "category"
                && b.TargetPage == "AoNu"
                && b.StartDate <= now
                && (b.EndDate == null || b.EndDate >= now))
            .OrderBy(b => b.SortOrder)
            .Take(1)
            .FirstOrDefaultAsync();

        ViewBag.AoNuBanner = aoNuBanner;

        return View();
    }

    // Debug endpoint to check product counts
    public async Task<IActionResult> DebugHomepage()
    {
        var freelancerSKUs = new[] {
            "FWBZ23SS06C", "FWWS25SS02G", "FWTS24FH03G", "FWTS25SS14C",
            "FWDR24FH01G", "FWDR25SS29G", "FWDR25FH04C", "FWSK23SS14G"
        };

        var freelancerProducts = await _context.Products
            .Where(p => p.IsActive && p.IsFeatured && freelancerSKUs.Contains(p.SKU))
            .OrderBy(p => Array.IndexOf(freelancerSKUs, p.SKU))
            .Select(p => new { p.SKU, p.Name, p.IsFeatured, p.IsActive })
            .ToListAsync();

        var missingSkus = freelancerSKUs.Where(sku => !freelancerProducts.Any(p => p.SKU == sku)).ToList();

        return Json(new
        {
            expectedCount = 8,
            actualCount = freelancerProducts.Count,
            products = freelancerProducts,
            missingSkus = missingSkus
        });
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public async Task<IActionResult> JohnHenry(string? category = null, string? color = null, 
        decimal? minPrice = null, decimal? maxPrice = null, string? size = null, 
        string? sort = "default", int page = 1)
    {
        const int pageSize = 40; // 10 rows x 4 products per row

        // Generate breadcrumbs for John Henry page
        var breadcrumbs = new List<BreadcrumbItem>
        {
            new BreadcrumbItem { Name = "Trang chủ", Url = Url.Action("Index", "Home") ?? "/" },
            new BreadcrumbItem { Name = "JOHN HENRY", Url = "" }
        };

        var breadcrumbJsonLd = _seoService.GenerateBreadcrumbJsonLd(breadcrumbs);

        ViewBag.BreadcrumbJsonLd = breadcrumbJsonLd;
        ViewBag.Breadcrumbs = breadcrumbs;
        ViewBag.MetaTitle = "JOHN HENRY - Thời trang nam nữ cao cấp";
        ViewBag.MetaDescription = "Khám phá bộ sưu tập JOHN HENRY với các sản phẩm thời trang nam nữ chất lượng cao, phong cách hiện đại và sang trọng.";
        
        // Pass filter params to view
        ViewBag.SelectedCategory = category;
        ViewBag.SelectedColor = color;
        ViewBag.MinPrice = minPrice;
        ViewBag.MaxPrice = maxPrice;
        ViewBag.SelectedSize = size;
        ViewBag.Sort = sort;

        // Load active marketing banners for John Henry collection page
        var now = DateTime.UtcNow;
        var collectionBanners = await _context.MarketingBanners
            .Where(b => b.IsActive 
                && b.Position == "collection_hero"
                && b.TargetPage == "JohnHenry"
                && b.StartDate <= now
                && (b.EndDate == null || b.EndDate >= now))
            .OrderBy(b => b.SortOrder)
            .ToListAsync();

        ViewBag.CollectionBanners = collectionBanners;

        // Build query with filters
        var productsQuery = _context.Products
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .Where(p => p.IsActive && !p.SKU.StartsWith("FW"));

        // Filter by category - Support Vietnamese case-insensitive matching + subcategory keywords
        if (!string.IsNullOrWhiteSpace(category))
        {
            var categoryTrimmed = category.Trim();
            
            // Define subcategory keywords mapping (case-insensitive)
            var subcategoryKeywords = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
            {
                // John Henry subcategories
                {"Áo Polo", new[] {"polo"}},
                {"Áo Sơ mi", new[] {"sơ mi", "so mi"}},
                {"Áo Thun", new[] {"thun", "t-shirt", "tshirt"}},
                {"Áo Len", new[] {"len", "sweater"}},
                {"Áo Khoác", new[] {"khoác", "jacket"}},
                {"Áo Vest", new[] {"vest", "blazer"}},
                {"Quần Khaki", new[] {"khaki", "kaki"}},
                {"Quần Jeans", new[] {"jeans", "jean"}},
                {"Quần Tây", new[] {"tây", "tay"}},
                {"Quần Short", new[] {"short"}},
                // Freelancer subcategories
                {"Áo Blouse", new[] {"blouse"}},
                {"Áo T-Shirt Nữ", new[] {"t-shirt", "tshirt", "thun"}},
                {"Áo Polo Nữ", new[] {"polo"}},
                {"Áo Blazer", new[] {"blazer"}},
                {"Áo Len Nữ", new[] {"len", "sweater"}},
                {"Quần Jean Nữ", new[] {"jean"}},
                {"Quần Âu Nữ", new[] {"âu", "au"}},
                {"Quần Short Nữ", new[] {"short"}},
                {"Đầm Công Sở", new[] {"đầm", "dam"}},
                {"Đầm Dạ Tiệc", new[] {"đầm", "dam"}},
                {"Đầm Maxi", new[] {"đầm", "dam", "maxi"}},
                {"Váy Bút Chì", new[] {"váy", "vay", "bút chì", "but chi"}},
                {"Váy Xòe", new[] {"váy", "vay", "xòe", "xoe"}},
                {"Váy Midi", new[] {"váy", "vay", "midi"}}
            };
            
            // Check if this is a subcategory request
            string[]? keywords = null;
            string? parentCategoryName = null;
            
            if (subcategoryKeywords.ContainsKey(categoryTrimmed))
            {
                keywords = subcategoryKeywords[categoryTrimmed];
                
                // Determine parent category based on subcategory
                if (categoryTrimmed.Contains("Áo") || categoryTrimmed.Contains("Blouse") || categoryTrimmed.Contains("Polo") || categoryTrimmed.Contains("Blazer"))
                {
                    parentCategoryName = categoryTrimmed.Contains("Nữ") || categoryTrimmed.Contains("Blouse") || categoryTrimmed.Contains("Blazer") ? "Áo nữ" : "Áo nam";
                }
                else if (categoryTrimmed.Contains("Quần"))
                {
                    parentCategoryName = categoryTrimmed.Contains("Nữ") ? "Quần nữ" : "Quần nam";
                }
                else if (categoryTrimmed.Contains("Đầm"))
                {
                    parentCategoryName = "Đầm nữ";
                }
                else if (categoryTrimmed.Contains("Váy"))
                {
                    parentCategoryName = "Chân váy nữ";
                }
            }
            else
            {
                // This is a parent category
                parentCategoryName = categoryTrimmed;
            }
            
            // Find parent category
            if (!string.IsNullOrWhiteSpace(parentCategoryName))
            {
                var lowerCategory = parentCategoryName.ToLower();
                var upperCategory = parentCategoryName.ToUpper();
                var capitalizedCategory = parentCategoryName.Length > 0 
                    ? char.ToUpper(parentCategoryName[0]) + parentCategoryName.Substring(1).ToLower() 
                    : parentCategoryName;

                var categoryEntity = await _context.Categories
                    .FirstOrDefaultAsync(c => c.Slug == parentCategoryName || 
                        EF.Functions.ILike(c.Name, lowerCategory) || 
                        EF.Functions.ILike(c.Name, upperCategory) ||
                        EF.Functions.ILike(c.Name, capitalizedCategory));
                
                if (categoryEntity != null)
                {
                    productsQuery = productsQuery.Where(p => p.CategoryId == categoryEntity.Id);
                    
                    // Apply subcategory keyword filtering if keywords exist
                    if (keywords != null && keywords.Length > 0)
                    {
                        // Build OR condition for all keywords - product name must contain ANY keyword
                        // Filter products where name contains any of the keywords (with case variations)
                        // Use a helper to build the condition properly for EF Core
                        productsQuery = ApplyKeywordFilter(productsQuery, keywords);
                    }
                }
            }
        }

        // Filter by color
        if (!string.IsNullOrWhiteSpace(color))
        {
            productsQuery = productsQuery.Where(p => p.Color != null && p.Color.ToLower().Contains(color.ToLower()));
        }

        // Filter by price range
        if (minPrice.HasValue)
        {
            productsQuery = productsQuery.Where(p => p.Price >= minPrice.Value);
        }
        if (maxPrice.HasValue)
        {
            productsQuery = productsQuery.Where(p => p.Price <= maxPrice.Value);
        }

        // Filter by size
        if (!string.IsNullOrWhiteSpace(size))
        {
            productsQuery = productsQuery.Where(p => p.Size != null && p.Size.Contains(size));
        }

        // Apply sorting
        productsQuery = sort switch
        {
            "name" => productsQuery.OrderBy(p => p.Name),
            "price-low" => productsQuery.OrderBy(p => p.Price),
            "price-high" => productsQuery.OrderByDescending(p => p.Price),
            "newest" => productsQuery.OrderByDescending(p => p.CreatedAt),
            _ => productsQuery.OrderByDescending(p => p.IsFeatured).ThenByDescending(p => p.CreatedAt)
        };

        // Get total count for pagination
        var totalProducts = await productsQuery.CountAsync();

        // Calculate pagination
        var totalPages = (int)Math.Ceiling(totalProducts / (double)pageSize);
        page = Math.Max(1, Math.Min(page, Math.Max(1, totalPages)));

        var products = await productsQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        // Map to ProductViewModel
        var productViewModels = products.Select(p => MapToProductViewModel(p)).ToList();

        // Pass pagination info to view
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = totalPages;
        ViewBag.TotalProducts = totalProducts;
        ViewBag.PageSize = pageSize;

        return View(productViewModels);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public async Task<IActionResult> Freelancer(string? category = null, string? color = null, 
        decimal? minPrice = null, decimal? maxPrice = null, string? size = null, 
        string? sort = "default", int page = 1)
    {
        const int pageSize = 40; // 10 rows x 4 products per row

        // Generate breadcrumbs for Freelancer page
        var breadcrumbs = new List<BreadcrumbItem>
        {
            new BreadcrumbItem { Name = "Trang chủ", Url = Url.Action("Index", "Home") ?? "/" },
            new BreadcrumbItem { Name = "FREELANCER", Url = "" }
        };

        var breadcrumbJsonLd = _seoService.GenerateBreadcrumbJsonLd(breadcrumbs);

        ViewBag.BreadcrumbJsonLd = breadcrumbJsonLd;
        ViewBag.Breadcrumbs = breadcrumbs;
        ViewBag.MetaTitle = "FREELANCER - Thời trang nữ hiện đại";
        ViewBag.MetaDescription = "Khám phá bộ sưu tập FREELANCER với các sản phẩm thời trang nữ chất lượng cao, phong cách hiện đại và sang trọng dành riêng cho phái đẹp.";
        
        // Pass filter params to view
        ViewBag.SelectedCategory = category;
        ViewBag.SelectedColor = color;
        ViewBag.MinPrice = minPrice;
        ViewBag.MaxPrice = maxPrice;
        ViewBag.SelectedSize = size;
        ViewBag.Sort = sort;

        // Load active marketing banners for Freelancer collection page
        var now = DateTime.UtcNow;
        var collectionBanners = await _context.MarketingBanners
            .Where(b => b.IsActive 
                && b.Position == "collection_hero"
                && b.TargetPage == "Freelancer"
                && b.StartDate <= now
                && (b.EndDate == null || b.EndDate >= now))
            .OrderBy(b => b.SortOrder)
            .ToListAsync();

        ViewBag.CollectionBanners = collectionBanners;

        // Build query with filters
        var productsQuery = _context.Products
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .Where(p => p.IsActive && p.SKU.StartsWith("FW"));

        // Filter by category - Support Vietnamese case-insensitive matching + subcategory keywords
        if (!string.IsNullOrWhiteSpace(category))
        {
            var categoryTrimmed = category.Trim();
            
            // Define subcategory keywords mapping (case-insensitive)
            var subcategoryKeywords = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
            {
                {"Áo Polo", new[] {"polo"}},
                {"Áo Sơ mi", new[] {"sơ mi", "so mi"}},
                {"Áo Thun", new[] {"thun", "t-shirt", "tshirt"}},
                {"Áo Len", new[] {"len", "sweater"}},
                {"Áo Khoác", new[] {"khoác", "jacket"}},
                {"Áo Vest", new[] {"vest", "blazer"}},
                {"Quần Khaki", new[] {"khaki", "kaki"}},
                {"Quần Jeans", new[] {"jeans", "jean"}},
                {"Quần Tây", new[] {"tây", "tay"}},
                {"Quần Short", new[] {"short"}},
                {"Áo Blouse", new[] {"blouse"}},
                {"Áo T-Shirt Nữ", new[] {"t-shirt", "tshirt", "thun"}},
                {"Áo Sơ Mi Nữ", new[] {"sơ mi", "so mi"}},
                {"Áo Polo Nữ", new[] {"polo"}},
                {"Áo Blazer", new[] {"blazer"}},
                {"Áo Len Nữ", new[] {"len", "sweater"}},
                {"Quần Jean Nữ", new[] {"jean"}},
                {"Quần Tây Nữ", new[] {"tây", "tay"}},
                {"Quần Âu Nữ", new[] {"âu", "au"}},
                {"Quần Short Nữ", new[] {"short"}},
                {"Quần Legging Nữ", new[] {"legging"}},
                {"Đầm Công Sở", new[] {"đầm", "dam"}},
                {"Đầm Dạ Tiệc", new[] {"đầm", "dam"}},
                {"Đầm Maxi", new[] {"đầm", "dam", "maxi"}},
                {"Váy Bút Chì", new[] {"váy", "vay", "bút chì", "but chi"}},
                {"Váy Xòe", new[] {"váy", "vay", "xòe", "xoe"}},
                {"Váy Midi", new[] {"váy", "vay", "midi"}}
            };
            
            // Check if this is a subcategory request
            string[]? keywords = null;
            string? parentCategoryName = null;
            
            if (subcategoryKeywords.ContainsKey(categoryTrimmed))
            {
                keywords = subcategoryKeywords[categoryTrimmed];
                
                // Determine parent category based on subcategory
                if (categoryTrimmed.Contains("Áo") || categoryTrimmed.Contains("Blouse") || categoryTrimmed.Contains("Polo") || categoryTrimmed.Contains("Blazer"))
                {
                    parentCategoryName = categoryTrimmed.Contains("Nữ") || categoryTrimmed.Contains("Blouse") || categoryTrimmed.Contains("Blazer") ? "Áo nữ" : "Áo nam";
                }
                else if (categoryTrimmed.Contains("Quần"))
                {
                    parentCategoryName = categoryTrimmed.Contains("Nữ") ? "Quần nữ" : "Quần nam";
                }
                else if (categoryTrimmed.Contains("Đầm"))
                {
                    parentCategoryName = "Đầm nữ";
                }
                else if (categoryTrimmed.Contains("Váy"))
                {
                    parentCategoryName = "Chân váy nữ";
                }
            }
            else
            {
                // This is a parent category
                parentCategoryName = categoryTrimmed;
            }
            
            // Find parent category
            if (!string.IsNullOrWhiteSpace(parentCategoryName))
            {
                var lowerCategory = parentCategoryName.ToLower();
                var upperCategory = parentCategoryName.ToUpper();
                var capitalizedCategory = parentCategoryName.Length > 0 
                    ? char.ToUpper(parentCategoryName[0]) + parentCategoryName.Substring(1).ToLower() 
                    : parentCategoryName;

                var categoryEntity = await _context.Categories
                    .FirstOrDefaultAsync(c => c.Slug == parentCategoryName || 
                        EF.Functions.ILike(c.Name, lowerCategory) || 
                        EF.Functions.ILike(c.Name, upperCategory) ||
                        EF.Functions.ILike(c.Name, capitalizedCategory));
                
                if (categoryEntity != null)
                {
                    productsQuery = productsQuery.Where(p => p.CategoryId == categoryEntity.Id);
                    
                    // Apply subcategory keyword filtering if keywords exist
                    if (keywords != null && keywords.Length > 0)
                    {
                        // Build OR condition for all keywords - product name must contain ANY keyword
                        // Filter products where name contains any of the keywords (with case variations)
                        // Use a helper to build the condition properly for EF Core
                        productsQuery = ApplyKeywordFilter(productsQuery, keywords);
                    }
                }
            }
        }

        // Filter by color
        if (!string.IsNullOrWhiteSpace(color))
        {
            productsQuery = productsQuery.Where(p => p.Color != null && p.Color.ToLower().Contains(color.ToLower()));
        }

        // Filter by price range
        if (minPrice.HasValue)
        {
            productsQuery = productsQuery.Where(p => p.Price >= minPrice.Value);
        }
        if (maxPrice.HasValue)
        {
            productsQuery = productsQuery.Where(p => p.Price <= maxPrice.Value);
        }

        // Filter by size
        if (!string.IsNullOrWhiteSpace(size))
        {
            productsQuery = productsQuery.Where(p => p.Size != null && p.Size.Contains(size));
        }

        // Apply sorting
        productsQuery = sort switch
        {
            "name" => productsQuery.OrderBy(p => p.Name),
            "price-low" => productsQuery.OrderBy(p => p.Price),
            "price-high" => productsQuery.OrderByDescending(p => p.Price),
            "newest" => productsQuery.OrderByDescending(p => p.CreatedAt),
            _ => productsQuery.OrderByDescending(p => p.IsFeatured).ThenByDescending(p => p.CreatedAt)
        };

        // Get total count for pagination
        var totalProducts = await productsQuery.CountAsync();

        // Calculate pagination
        var totalPages = (int)Math.Ceiling(totalProducts / (double)pageSize);
        page = Math.Max(1, Math.Min(page, Math.Max(1, totalPages)));

        var products = await productsQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        // Map to ProductViewModel
        var productViewModels = products.Select(p => MapToProductViewModel(p)).ToList();

        // Pass pagination info to view
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = totalPages;
        ViewBag.TotalProducts = totalProducts;
        ViewBag.PageSize = pageSize;

        return View(productViewModels);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public async Task<IActionResult> FreelancerDress(int page = 1)
    {
        const int pageSize = 40; // 10 rows x 4 products per row

        // Generate breadcrumbs for Freelancer Dress page
        var breadcrumbs = new List<BreadcrumbItem>
        {
            new BreadcrumbItem { Name = "Trang chủ", Url = Url.Action("Index", "Home") ?? "/" },
            new BreadcrumbItem { Name = "FREELANCER", Url = Url.Action("Freelancer", "Home") ?? "/Home/Freelancer" },
            new BreadcrumbItem { Name = "VÁY", Url = "" }
        };

        var breadcrumbJsonLd = _seoService.GenerateBreadcrumbJsonLd(breadcrumbs);

        ViewBag.BreadcrumbJsonLd = breadcrumbJsonLd;
        ViewBag.Breadcrumbs = breadcrumbs;
        ViewBag.MetaTitle = "Váy FREELANCER - Thời trang nữ cao cấp";
        ViewBag.MetaDescription = "Khám phá bộ sưu tập váy FREELANCER với đa dạng các loại váy công sở, váy dạ tiệc, váy maxi chất lượng cao, phong cách hiện đại.";

        // Load women's dresses from database BY SUBCATEGORY AND SKU PATTERN
        var dressCategory = await FindCategoryByNameAsync("Đầm nữ");

        if (dressCategory == null)
        {
            return View(new List<Product>());
        }

        // Get total count for pagination - Filter by category AND Freelancer SKU pattern
        var totalProducts = await _context.Products
            .Where(p => p.IsActive && p.CategoryId == dressCategory.Id && p.SKU.StartsWith("FW"))
            .CountAsync();

        // Calculate pagination
        var totalPages = (int)Math.Ceiling(totalProducts / (double)pageSize);
        page = Math.Max(1, Math.Min(page, totalPages));

        var products = await _context.Products
            .Where(p => p.IsActive && p.CategoryId == dressCategory.Id && p.SKU.StartsWith("FW"))
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        // Pass pagination info to view
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = totalPages;
        ViewBag.TotalProducts = totalProducts;
        ViewBag.PageSize = pageSize;

        return View(products);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public async Task<IActionResult> FreelancerShirt(int page = 1)
    {
        const int pageSize = 40; // 10 rows x 4 products per row

        // Generate breadcrumbs for Freelancer Shirt page
        var breadcrumbs = new List<BreadcrumbItem>
        {
            new BreadcrumbItem { Name = "Trang chủ", Url = Url.Action("Index", "Home") ?? "/" },
            new BreadcrumbItem { Name = "FREELANCER", Url = Url.Action("Freelancer", "Home") ?? "/Home/Freelancer" },
            new BreadcrumbItem { Name = "ÁO NỮ", Url = "" }
        };

        var breadcrumbJsonLd = _seoService.GenerateBreadcrumbJsonLd(breadcrumbs);

        ViewBag.BreadcrumbJsonLd = breadcrumbJsonLd;
        ViewBag.Breadcrumbs = breadcrumbs;
        ViewBag.MetaTitle = "Áo Nữ FREELANCER - Thời trang nữ cao cấp";
        ViewBag.MetaDescription = "Khám phá bộ sưu tập áo nữ FREELANCER với đa dạng các loại áo blouse, áo thun, áo len, áo khoác chất lượng cao, phong cách hiện đại.";

        // Load women's shirts from database BY SUBCATEGORY AND SKU PATTERN
        var shirtCategory = await FindCategoryByNameAsync("Áo nữ");

        if (shirtCategory == null)
        {
            return View(new List<Product>());
        }

        // Get total count for pagination - Filter by category AND Freelancer SKU pattern
        var totalProducts = await _context.Products
            .Where(p => p.IsActive && p.CategoryId == shirtCategory.Id && p.SKU.StartsWith("FW"))
            .CountAsync();

        // Calculate pagination
        var totalPages = (int)Math.Ceiling(totalProducts / (double)pageSize);
        page = Math.Max(1, Math.Min(page, totalPages));

        var products = await _context.Products
            .Where(p => p.IsActive && p.CategoryId == shirtCategory.Id && p.SKU.StartsWith("FW"))
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        // Pass pagination info to view
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = totalPages;
        ViewBag.TotalProducts = totalProducts;
        ViewBag.PageSize = pageSize;

        // Load category banner
        var now = DateTime.UtcNow;
        var categoryBanner = await _context.MarketingBanners
            .Where(b => b.IsActive 
                && b.Position == "category_banner"
                && b.TargetPage == "AoSoMiNu"
                && b.StartDate <= now
                && (b.EndDate == null || b.EndDate >= now))
            .OrderBy(b => b.SortOrder)
            .FirstOrDefaultAsync();

        ViewBag.CategoryBanner = categoryBanner;

        return View(products);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public async Task<IActionResult> FreelancerTrousers(int page = 1)
    {
        const int pageSize = 40; // 10 rows x 4 products per row

        // Generate breadcrumbs for Freelancer Trousers page
        var breadcrumbs = new List<BreadcrumbItem>
        {
            new BreadcrumbItem { Name = "Trang chủ", Url = Url.Action("Index", "Home") ?? "/" },
            new BreadcrumbItem { Name = "FREELANCER", Url = Url.Action("Freelancer", "Home") ?? "/Home/Freelancer" },
            new BreadcrumbItem { Name = "QUẦN NỮ", Url = "" }
        };

        var breadcrumbJsonLd = _seoService.GenerateBreadcrumbJsonLd(breadcrumbs);

        ViewBag.BreadcrumbJsonLd = breadcrumbJsonLd;
        ViewBag.Breadcrumbs = breadcrumbs;
        ViewBag.MetaTitle = "Quần Nữ FREELANCER - Thời trang nữ cao cấp";
        ViewBag.MetaDescription = "Khám phá bộ sưu tập quần nữ FREELANCER với đa dạng các loại quần jean, quần âu, quần short chất lượng cao, phong cách hiện đại.";

        // Load women's trousers from database BY SUBCATEGORY AND SKU PATTERN
        var trousersCategory = await FindCategoryByNameAsync("Quần nữ");

        if (trousersCategory == null)
        {
            return View(new List<Product>());
        }

        // Get total count for pagination - Filter by category AND Freelancer SKU pattern
        var totalProducts = await _context.Products
            .Where(p => p.IsActive && p.CategoryId == trousersCategory.Id && p.SKU.StartsWith("FW"))
            .CountAsync();

        // Calculate pagination
        var totalPages = (int)Math.Ceiling(totalProducts / (double)pageSize);
        page = Math.Max(1, Math.Min(page, totalPages));

        var products = await _context.Products
            .Where(p => p.IsActive && p.CategoryId == trousersCategory.Id && p.SKU.StartsWith("FW"))
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        // Pass pagination info to view
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = totalPages;
        ViewBag.TotalProducts = totalProducts;
        ViewBag.PageSize = pageSize;

        // Load category banner
        var now = DateTime.UtcNow;
        var categoryBanner = await _context.MarketingBanners
            .Where(b => b.IsActive 
                && b.Position == "category_banner"
                && b.TargetPage == "QuanShortNu"
                && b.StartDate <= now
                && (b.EndDate == null || b.EndDate >= now))
            .OrderBy(b => b.SortOrder)
            .FirstOrDefaultAsync();

        ViewBag.CategoryBanner = categoryBanner;

        return View(products);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public async Task<IActionResult> FreelancerSkirt(int page = 1)
    {
        const int pageSize = 40; // 10 rows x 4 products per row

        // Generate breadcrumbs for Freelancer Skirt page
        var breadcrumbs = new List<BreadcrumbItem>
        {
            new BreadcrumbItem { Name = "Trang chủ", Url = Url.Action("Index", "Home") ?? "/" },
            new BreadcrumbItem { Name = "FREELANCER", Url = Url.Action("Freelancer", "Home") ?? "/Home/Freelancer" },
            new BreadcrumbItem { Name = "CHÂN VÁY", Url = "" }
        };

        var breadcrumbJsonLd = _seoService.GenerateBreadcrumbJsonLd(breadcrumbs);

        ViewBag.BreadcrumbJsonLd = breadcrumbJsonLd;
        ViewBag.Breadcrumbs = breadcrumbs;
        ViewBag.MetaTitle = "Chân Váy FREELANCER - Thời trang nữ cao cấp";
        ViewBag.MetaDescription = "Khám phá bộ sưu tập chân váy FREELANCER với đa dạng các loại chân váy bút chì, chân váy xòe, chân váy midi chất lượng cao, phong cách hiện đại.";

        // Load women's skirts from database BY SUBCATEGORY AND SKU PATTERN
        var skirtCategory = await FindCategoryByNameAsync("Chân váy nữ");

        if (skirtCategory == null)
        {
            return View(new List<Product>());
        }

        // Get total count for pagination - Filter by category AND Freelancer SKU pattern
        var totalProducts = await _context.Products
            .Where(p => p.IsActive && p.CategoryId == skirtCategory.Id && p.SKU.StartsWith("FW"))
            .CountAsync();

        // Calculate pagination
        var totalPages = (int)Math.Ceiling(totalProducts / (double)pageSize);
        page = Math.Max(1, Math.Min(page, totalPages));

        var products = await _context.Products
            .Where(p => p.IsActive && p.CategoryId == skirtCategory.Id && p.SKU.StartsWith("FW"))
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        // Pass pagination info to view
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = totalPages;
        ViewBag.TotalProducts = totalProducts;
        ViewBag.PageSize = pageSize;

        // Load category banner
        var now = DateTime.UtcNow;
        var categoryBanner = await _context.MarketingBanners
            .Where(b => b.IsActive 
                && b.Position == "category_banner"
                && b.TargetPage == "ChanVayNu"
                && b.StartDate <= now
                && (b.EndDate == null || b.EndDate >= now))
            .OrderBy(b => b.SortOrder)
            .FirstOrDefaultAsync();

        ViewBag.CategoryBanner = categoryBanner;

        return View(products);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public async Task<IActionResult> FreelancerAccessories(int page = 1)
    {
        const int pageSize = 40; // 10 rows x 4 products per row

        // Generate breadcrumbs for Freelancer Accessories page
        var breadcrumbs = new List<BreadcrumbItem>
        {
            new BreadcrumbItem { Name = "Trang chủ", Url = Url.Action("Index", "Home") ?? "/" },
            new BreadcrumbItem { Name = "FREELANCER", Url = Url.Action("Freelancer", "Home") ?? "/Home/Freelancer" },
            new BreadcrumbItem { Name = "PHỤ KIỆN NỮ", Url = "" }
        };

        var breadcrumbJsonLd = _seoService.GenerateBreadcrumbJsonLd(breadcrumbs);

        ViewBag.BreadcrumbJsonLd = breadcrumbJsonLd;
        ViewBag.Breadcrumbs = breadcrumbs;
        ViewBag.MetaTitle = "Phụ Kiện Nữ FREELANCER - Thời trang nữ cao cấp";
        ViewBag.MetaDescription = "Khám phá bộ sưu tập phụ kiện nữ FREELANCER với đa dạng túi xách, ví, thắt lưng, mắt kính, khăn, đồng hồ chất lượng cao, phong cách hiện đại.";

        // Load women's accessories from database BY SUBCATEGORY AND SKU PATTERN
        var accessoriesCategory = await FindCategoryByNameAsync("Phụ kiện nữ");

        if (accessoriesCategory == null)
        {
            return View(new List<Product>());
        }

        // Get total count for pagination - Filter by category AND Freelancer SKU pattern
        var totalProducts = await _context.Products
            .Where(p => p.IsActive && p.CategoryId == accessoriesCategory.Id && p.SKU.StartsWith("FW"))
            .CountAsync();

        // Calculate pagination
        var totalPages = (int)Math.Ceiling(totalProducts / (double)pageSize);
        page = Math.Max(1, Math.Min(page, totalPages));

        var products = await _context.Products
            .Where(p => p.IsActive && p.CategoryId == accessoriesCategory.Id && p.SKU.StartsWith("FW"))
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        // Pass pagination info to view
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = totalPages;
        ViewBag.TotalProducts = totalProducts;
        ViewBag.PageSize = pageSize;

        // Load category banner
        var now = DateTime.UtcNow;
        var categoryBanner = await _context.MarketingBanners
            .Where(b => b.IsActive 
                && b.Position == "category_banner"
                && b.TargetPage == "PhuKienNu"
                && b.StartDate <= now
                && (b.EndDate == null || b.EndDate >= now))
            .OrderBy(b => b.SortOrder)
            .FirstOrDefaultAsync();

        ViewBag.CategoryBanner = categoryBanner;

        return View(products);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public async Task<IActionResult> JohnHenryShirt(int page = 1)
    {
        const int pageSize = 40; // 10 rows x 4 products per row

        // Generate breadcrumbs for John Henry Shirt page
        var breadcrumbs = new List<BreadcrumbItem>
        {
            new BreadcrumbItem { Name = "Trang chủ", Url = Url.Action("Index", "Home") ?? "/" },
            new BreadcrumbItem { Name = "JOHN HENRY", Url = Url.Action("JohnHenry", "Home") ?? "/Home/JohnHenry" },
            new BreadcrumbItem { Name = "ÁO NAM", Url = "" }
        };

        var breadcrumbJsonLd = _seoService.GenerateBreadcrumbJsonLd(breadcrumbs);

        ViewBag.BreadcrumbJsonLd = breadcrumbJsonLd;
        ViewBag.Breadcrumbs = breadcrumbs;
        ViewBag.MetaTitle = "Áo Nam JOHN HENRY - Thời trang nam cao cấp";
        ViewBag.MetaDescription = "Khám phá bộ sưu tập áo nam JOHN HENRY với đa dạng các loại áo polo, áo sơ mi, áo thun, áo len chất lượng cao, phong cách hiện đại.";

        // Load men's shirts from database BY SUBCATEGORY AND SKU PATTERN
        var shirtCategory = await FindCategoryByNameAsync("Áo nam");

        if (shirtCategory == null)
        {
            return View(new List<Product>());
        }

        // Get total count for pagination - Filter by category AND John Henry SKU pattern (NOT starting with FW)
        var totalProducts = await _context.Products
            .Where(p => p.IsActive && p.CategoryId == shirtCategory.Id && !p.SKU.StartsWith("FW"))
            .CountAsync();

        // Calculate pagination
        var totalPages = (int)Math.Ceiling(totalProducts / (double)pageSize);
        page = Math.Max(1, Math.Min(page, totalPages));

        var products = await _context.Products
            .Where(p => p.IsActive && p.CategoryId == shirtCategory.Id && !p.SKU.StartsWith("FW"))
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        // Pass pagination info to view
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = totalPages;
        ViewBag.TotalProducts = totalProducts;
        ViewBag.PageSize = pageSize;

        // Load category banner
        var now = DateTime.UtcNow;
        var categoryBanner = await _context.MarketingBanners
            .Where(b => b.IsActive 
                && b.Position == "category_banner"
                && b.TargetPage == "AoSoMiNam"
                && b.StartDate <= now
                && (b.EndDate == null || b.EndDate >= now))
            .OrderBy(b => b.SortOrder)
            .FirstOrDefaultAsync();

        ViewBag.CategoryBanner = categoryBanner;

        return View(products);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public async Task<IActionResult> JohnHenryTrousers(int page = 1)
    {
        const int pageSize = 40; // 10 rows x 4 products per row

        // Generate breadcrumbs for John Henry Trousers page
        var breadcrumbs = new List<BreadcrumbItem>
        {
            new BreadcrumbItem { Name = "Trang chủ", Url = Url.Action("Index", "Home") ?? "/" },
            new BreadcrumbItem { Name = "JOHN HENRY", Url = Url.Action("JohnHenry", "Home") ?? "/Home/JohnHenry" },
            new BreadcrumbItem { Name = "QUẦN NAM", Url = "" }
        };

        var breadcrumbJsonLd = _seoService.GenerateBreadcrumbJsonLd(breadcrumbs);

        ViewBag.BreadcrumbJsonLd = breadcrumbJsonLd;
        ViewBag.Breadcrumbs = breadcrumbs;
        ViewBag.MetaTitle = "Quần Nam JOHN HENRY - Thời trang nam cao cấp";
        ViewBag.MetaDescription = "Khám phá bộ sưu tập quần nam JOHN HENRY với đa dạng các loại quần jean, quần khaki, quần short chất lượng cao, phong cách hiện đại.";

        // Load men's trousers from database BY SUBCATEGORY AND SKU PATTERN
        var trousersCategory = await FindCategoryByNameAsync("Quần nam");

        if (trousersCategory == null)
        {
            return View(new List<Product>());
        }

        // Get total count for pagination - Filter by category AND John Henry SKU pattern (NOT starting with FW)
        var totalProducts = await _context.Products
            .Where(p => p.IsActive && p.CategoryId == trousersCategory.Id && !p.SKU.StartsWith("FW"))
            .CountAsync();

        // Calculate pagination
        var totalPages = (int)Math.Ceiling(totalProducts / (double)pageSize);
        page = Math.Max(1, Math.Min(page, totalPages));

        var products = await _context.Products
            .Where(p => p.IsActive && p.CategoryId == trousersCategory.Id && !p.SKU.StartsWith("FW"))
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        // Pass pagination info to view
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = totalPages;
        ViewBag.TotalProducts = totalProducts;
        ViewBag.PageSize = pageSize;

        // Load category banner
        var now = DateTime.UtcNow;
        var categoryBanner = await _context.MarketingBanners
            .Where(b => b.IsActive 
                && b.Position == "category_banner"
                && b.TargetPage == "QuanTayNam"
                && b.StartDate <= now
                && (b.EndDate == null || b.EndDate >= now))
            .OrderBy(b => b.SortOrder)
            .FirstOrDefaultAsync();

        ViewBag.CategoryBanner = categoryBanner;

        return View(products);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public async Task<IActionResult> JohnHenryAccessories(int page = 1)
    {
        const int pageSize = 40; // 10 rows x 4 products per row

        // Generate breadcrumbs for John Henry Accessories page
        var breadcrumbs = new List<BreadcrumbItem>
        {
            new BreadcrumbItem { Name = "Trang chủ", Url = Url.Action("Index", "Home") ?? "/" },
            new BreadcrumbItem { Name = "JOHN HENRY", Url = Url.Action("JohnHenry", "Home") ?? "/Home/JohnHenry" },
            new BreadcrumbItem { Name = "PHỤ KIỆN NAM", Url = "" }
        };

        var breadcrumbJsonLd = _seoService.GenerateBreadcrumbJsonLd(breadcrumbs);

        ViewBag.BreadcrumbJsonLd = breadcrumbJsonLd;
        ViewBag.Breadcrumbs = breadcrumbs;
        ViewBag.MetaTitle = "Phụ Kiện Nam JOHN HENRY - Thời trang nam cao cấp";
        ViewBag.MetaDescription = "Khám phá bộ sưu tập phụ kiện nam JOHN HENRY với đa dạng các loại thắt lưng, cà vạt, ví da, túi xách chất lượng cao, phong cách hiện đại.";

        // Load men's accessories from database BY SUBCATEGORY AND SKU PATTERN
        var accessoriesCategory = await FindCategoryByNameAsync("Phụ kiện nam");

        if (accessoriesCategory == null)
        {
            return View(new List<Product>());
        }

        // Get total count for pagination - Filter by category AND John Henry SKU pattern (NOT starting with FW)
        var totalProducts = await _context.Products
            .Where(p => p.IsActive && p.CategoryId == accessoriesCategory.Id && !p.SKU.StartsWith("FW"))
            .CountAsync();

        // Calculate pagination
        var totalPages = (int)Math.Ceiling(totalProducts / (double)pageSize);
        page = Math.Max(1, Math.Min(page, totalPages));

        var products = await _context.Products
            .Where(p => p.IsActive && p.CategoryId == accessoriesCategory.Id && !p.SKU.StartsWith("FW"))
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        // Pass pagination info to view
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = totalPages;
        ViewBag.TotalProducts = totalProducts;
        ViewBag.PageSize = pageSize;

        // Load category banner
        var now = DateTime.UtcNow;
        var categoryBanner = await _context.MarketingBanners
            .Where(b => b.IsActive 
                && b.Position == "category_banner"
                && b.TargetPage == "PhuKienNam"
                && b.StartDate <= now
                && (b.EndDate == null || b.EndDate >= now))
            .OrderBy(b => b.SortOrder)
            .FirstOrDefaultAsync();

        ViewBag.CategoryBanner = categoryBanner;

        return View(products);
    }

    // API endpoint for search suggestions
    [HttpGet]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> SearchSuggestions(string query)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
        {
            return Json(new { suggestions = new List<object>() });
        }

        // Normalize query: trim and create pattern for both cases
        var queryTrimmed = query.Trim();
        var lowerQuery = $"%{queryTrimmed.ToLower()}%";
        var upperQuery = $"%{queryTrimmed.ToUpper()}%";
        var capitalizedQuery = $"%{char.ToUpper(queryTrimmed[0]) + queryTrimmed.Substring(1).ToLower()}%";

        // Get matching products - using ILIKE with multiple case variations for Vietnamese text
        var products = await _context.Products
            .Where(p => p.IsActive && 
                (EF.Functions.ILike(p.Name, lowerQuery) || 
                 EF.Functions.ILike(p.Name, upperQuery) ||
                 EF.Functions.ILike(p.Name, capitalizedQuery) ||
                 EF.Functions.ILike(p.SKU, lowerQuery)))
            .OrderByDescending(p => p.IsFeatured)
            .ThenByDescending(p => p.ViewCount)
            .Take(5)
            .Select(p => new
            {
                name = p.Name,
                sku = p.SKU,
                price = p.Price,
                image = p.FeaturedImageUrl ?? "/images/default-product.jpg"
            })
            .ToListAsync();

        return Json(new { suggestions = products });
    }

    public IActionResult Blog()
    {
        // Generate breadcrumbs for Blog page
        var breadcrumbs = new List<BreadcrumbItem>
        {
            new BreadcrumbItem { Name = "Trang chủ", Url = Url.Action("Index", "Home") ?? "/" },
            new BreadcrumbItem { Name = "BLOG", Url = "" }
        };

        var breadcrumbJsonLd = _seoService.GenerateBreadcrumbJsonLd(breadcrumbs);

        ViewBag.BreadcrumbJsonLd = breadcrumbJsonLd;
        ViewBag.Breadcrumbs = breadcrumbs;
        ViewBag.MetaTitle = "BLOG - John Henry Fashion";
        ViewBag.MetaDescription = "Khám phá những bài viết mới nhất về thời trang, phong cách sống và xu hướng hiện đại.";

        return View();
    }

    public IActionResult Uniform()
    {
        // Generate breadcrumbs for Uniform page
        var breadcrumbs = new List<BreadcrumbItem>
        {
            new BreadcrumbItem { Name = "Trang chủ", Url = Url.Action("Index", "Home") ?? "/" },
            new BreadcrumbItem { Name = "UNIFORM", Url = "" }
        };

        var breadcrumbJsonLd = _seoService.GenerateBreadcrumbJsonLd(breadcrumbs);

        ViewBag.BreadcrumbJsonLd = breadcrumbJsonLd;
        ViewBag.Breadcrumbs = breadcrumbs;
        ViewBag.MetaTitle = "UNIFORM - Đồng phục chuyên nghiệp";
        ViewBag.MetaDescription = "Khám phá bộ sưu tập đồng phục chuyên nghiệp cho doanh nghiệp, salon làm đẹp, khách sạn với chất lượng cao và thiết kế hiện đại.";

        return View();
    }

    public IActionResult SeoTest()
    {
        // Generate breadcrumbs for SEO test page
        var breadcrumbs = new List<BreadcrumbItem>
        {
            new BreadcrumbItem { Name = "Trang chủ", Url = Url.Action("Index", "Home") ?? "/" },
            new BreadcrumbItem { Name = "SEO Test", Url = "" }
        };

        var breadcrumbJsonLd = _seoService.GenerateBreadcrumbJsonLd(breadcrumbs);

        ViewBag.BreadcrumbJsonLd = breadcrumbJsonLd;
        ViewBag.Breadcrumbs = breadcrumbs;
        ViewBag.MetaTitle = "SEO Test - John Henry Fashion";
        ViewBag.MetaDescription = "This is a test page to demonstrate SEO implementation with structured data and meta tags";

        return View();
    }

    private ProductViewModel MapToProductViewModel(Product product)
    {
        return new ProductViewModel
        {
            Id = product.Id,
            Name = product.Name,
            SKU = product.SKU,
            Price = product.Price, // Chỉ dùng giá gốc từ database
            FeaturedImageUrl = product.FeaturedImageUrl ?? "/images/default-product.jpg",
            Images = product.GalleryImages?.ToList() ?? new List<string>(),
            StockQuantity = product.StockQuantity,
            CategoryName = product.Category?.Name ?? "Chưa phân loại",
            BrandName = product.Brand?.Name,
            Rating = product.Rating,
            ReviewCount = product.ReviewCount,
            IsNew = product.CreatedAt > DateTime.UtcNow.AddDays(-30),
            IsFeatured = product.IsFeatured,
            // Parse comma-separated colors and sizes from Product model
            AvailableColors = !string.IsNullOrEmpty(product.Color) 
                ? product.Color.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(c => c.Trim())
                    .ToList()
                : new List<string>(),
            AvailableSizes = !string.IsNullOrEmpty(product.Size)
                ? product.Size.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim())
                    .ToList()
                : new List<string>()
        };
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    // Temporary endpoint to update featured products
    public async Task<IActionResult> UpdateFeaturedProducts()
    {
        var johnHenrySKUs = new[] {
            "JK25FH04C-PA", "KS25SS20P-SC", "WS25FH63P-LC", "JK25FH02P-CT",
            "KS25FH49C-SLWK", "KS25SS31T-SCWK", "WS25FH58C-CFBB", "JK25FH10T-KA"
        };

        var freelancerSKUs = new[] {
            "FWBZ23SS06C", "FWWS25SS02G", "FWTS24FH03G", "FWTS25SS14C",
            "FWDR24FH01G", "FWDR25SS29G", "FWDR25FH04C", "FWSK23SS14G"
        };

        var bestSellerSKUs = new[] {
            "FWSK25FH12C", "FWSK24FH13C", "KP25SS06T-NMWFSL", "FWDP24SS06C",
            "JK25FH03C-CT", "BZ25FH06C-SL", "BZ24FH02P-SL", "WS24SS15P-SCRG"
        };

        // Reset all products to not featured
        await _context.Database.ExecuteSqlRawAsync("UPDATE \"Products\" SET \"IsFeatured\" = false");

        // Update John Henry products
        var allSKUs = johnHenrySKUs.Concat(freelancerSKUs).Concat(bestSellerSKUs).ToArray();

        var products = await _context.Products
            .Where(p => allSKUs.Contains(p.SKU))
            .ToListAsync();

        foreach (var product in products)
        {
            product.IsFeatured = true;
            product.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        return Json(new
        {
            success = true,
            message = $"Updated {products.Count} products as featured",
            johnHenryCount = products.Count(p => johnHenrySKUs.Contains(p.SKU)),
            freelancerCount = products.Count(p => freelancerSKUs.Contains(p.SKU)),
            bestSellerCount = products.Count(p => bestSellerSKUs.Contains(p.SKU)),
            products = products.Select(p => new { p.SKU, p.Name, p.IsFeatured })
        });
    }

    // Search products
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public async Task<IActionResult> Search(string query, string? category = null, string? color = null, 
        decimal? minPrice = null, decimal? maxPrice = null, string? size = null, 
        string? sort = "default", int page = 1)
    {
        const int pageSize = 40;

        // Generate breadcrumbs for Search page
        var breadcrumbs = new List<BreadcrumbItem>
        {
            new BreadcrumbItem { Name = "Trang chủ", Url = Url.Action("Index", "Home") ?? "/" },
            new BreadcrumbItem { Name = $"Tìm kiếm: {query}", Url = "" }
        };

        var breadcrumbJsonLd = _seoService.GenerateBreadcrumbJsonLd(breadcrumbs);

        ViewBag.BreadcrumbJsonLd = breadcrumbJsonLd;
        ViewBag.Breadcrumbs = breadcrumbs;
        ViewBag.MetaTitle = $"Tìm kiếm: {query} - John Henry Fashion";
        ViewBag.MetaDescription = $"Kết quả tìm kiếm cho '{query}' tại John Henry Fashion";
        ViewBag.SearchQuery = query;
        ViewBag.SelectedCategory = category;
        ViewBag.SelectedColor = color;
        ViewBag.MinPrice = minPrice;
        ViewBag.MaxPrice = maxPrice;
        ViewBag.SelectedSize = size;
        ViewBag.Sort = sort;

        // Build search query
        var productsQuery = _context.Products
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .Where(p => p.IsActive);

        // Search by query (name, SKU, description) - using multiple case variations for Vietnamese text
        if (!string.IsNullOrWhiteSpace(query))
        {
            var queryTrimmed = query.Trim();
            var lowerQuery = $"%{queryTrimmed.ToLower()}%";
            var upperQuery = $"%{queryTrimmed.ToUpper()}%";
            var capitalizedQuery = $"%{char.ToUpper(queryTrimmed[0]) + queryTrimmed.Substring(1).ToLower()}%";

            productsQuery = productsQuery.Where(p =>
                EF.Functions.ILike(p.Name, lowerQuery) ||
                EF.Functions.ILike(p.Name, upperQuery) ||
                EF.Functions.ILike(p.Name, capitalizedQuery) ||
                EF.Functions.ILike(p.SKU, lowerQuery) ||
                (p.Description != null && (
                    EF.Functions.ILike(p.Description, lowerQuery) ||
                    EF.Functions.ILike(p.Description, upperQuery) ||
                    EF.Functions.ILike(p.Description, capitalizedQuery)
                ))
            );
        }

        // Filter by category
        if (!string.IsNullOrWhiteSpace(category))
        {
            var categorySlug = category.Trim();
            var categoryEntity = await _context.Categories
                .FirstOrDefaultAsync(c => c.Slug == categorySlug || EF.Functions.ILike(c.Name, categorySlug));
            
            if (categoryEntity != null)
            {
                productsQuery = productsQuery.Where(p => p.CategoryId == categoryEntity.Id);
            }
        }

        // Filter by color
        if (!string.IsNullOrWhiteSpace(color))
        {
            var colorQuery = $"%{color.Trim()}%";
            productsQuery = productsQuery.Where(p => p.Color != null && EF.Functions.ILike(p.Color, colorQuery));
        }

        // Filter by price range
        if (minPrice.HasValue)
        {
            productsQuery = productsQuery.Where(p => p.Price >= minPrice.Value);
        }
        if (maxPrice.HasValue)
        {
            productsQuery = productsQuery.Where(p => p.Price <= maxPrice.Value);
        }

        // Filter by size
        if (!string.IsNullOrWhiteSpace(size))
        {
            productsQuery = productsQuery.Where(p => p.Size != null && p.Size.Contains(size));
        }

        // Apply sorting
        productsQuery = sort switch
        {
            "name" => productsQuery.OrderBy(p => p.Name),
            "price-low" => productsQuery.OrderBy(p => p.Price),
            "price-high" => productsQuery.OrderByDescending(p => p.Price),
            "newest" => productsQuery.OrderByDescending(p => p.CreatedAt),
            _ => productsQuery.OrderByDescending(p => p.IsFeatured).ThenByDescending(p => p.CreatedAt)
        };

        // Get total count for pagination
        var totalProducts = await productsQuery.CountAsync();

        // Calculate pagination
        var totalPages = (int)Math.Ceiling(totalProducts / (double)pageSize);
        page = Math.Max(1, Math.Min(page, Math.Max(1, totalPages)));

        // Get products for current page
        var products = await productsQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        // Map to ProductViewModel
        var productViewModels = products.Select(p => MapToProductViewModel(p)).ToList();

        // Pass pagination info to view
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = totalPages;
        ViewBag.TotalProducts = totalProducts;
        ViewBag.PageSize = pageSize;

        // Get all categories for filter sidebar
        ViewBag.Categories = await _context.Categories
            .Where(c => c.ParentId == null && c.IsActive)
            .OrderBy(c => c.Name)
            .ToListAsync();

        return View(productViewModels);
    }

    // Debug endpoint to analyze category distribution
    public async Task<IActionResult> CategoryAnalysis()
    {
        // Get all categories with product counts
        var categories = await _context.Categories
            .Select(c => new
            {
                CategoryId = c.Id,
                CategoryName = c.Name,
                CategorySlug = c.Slug,
                TotalProducts = _context.Products.Count(p => p.CategoryId == c.Id && p.IsActive),
                SampleProducts = _context.Products
                    .Where(p => p.CategoryId == c.Id && p.IsActive)
                    .OrderBy(p => p.SKU)
                    .Take(5)
                    .Select(p => new { p.SKU, p.Name, BrandName = p.Brand != null ? p.Brand.Name : null })
                    .ToList()
            })
            .OrderByDescending(c => c.TotalProducts)
            .ToListAsync();

        // Get brand distribution
        var brands = await _context.Brands
            .Select(b => new
            {
                BrandId = b.Id,
                BrandName = b.Name,
                TotalProducts = _context.Products.Count(p => p.BrandId == b.Id && p.IsActive)
            })
            .OrderByDescending(b => b.TotalProducts)
            .ToListAsync();

        // Check products with SKU patterns
        var johnHenryPattern = await _context.Products
            .Where(p => p.IsActive && !p.SKU.StartsWith("FW"))
            .GroupBy(p => p.Category!.Name)
            .Select(g => new { Category = g.Key, Count = g.Count() })
            .ToListAsync();

        var freelancerPattern = await _context.Products
            .Where(p => p.IsActive && p.SKU.StartsWith("FW"))
            .GroupBy(p => p.Category!.Name)
            .Select(g => new { Category = g.Key, Count = g.Count() })
            .ToListAsync();

        return Json(new
        {
            totalActiveProducts = await _context.Products.CountAsync(p => p.IsActive),
            categoriesAnalysis = categories,
            brandsAnalysis = brands,
            johnHenryPatternByCategory = johnHenryPattern,
            freelancerPatternByCategory = freelancerPattern,
            message = "Products are filtered by CategoryId. Check if category assignments match SKU patterns."
        });
    }

    /// <summary>
    /// Fix sunglasses products category assignment (Public endpoint for testing)
    /// Moves FWSG* products from "Chân váy nữ" to "Phụ kiện nữ"
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> FixSunglassesCategory()
    {
        try
        {
            // Get the correct category ID for "Phụ kiện nữ"
            var accessoriesCategory = await FindCategoryByNameAsync("Phụ kiện nữ");
            
            if (accessoriesCategory == null)
            {
                return Json(new
                {
                    success = false,
                    message = "Category 'Phụ kiện nữ' not found"
                });
            }

            // Find all sunglasses products (SKU starting with FWSG)
            var sunglassesProducts = await _context.Products
                .Where(p => p.SKU.StartsWith("FWSG") && p.IsActive)
                .ToListAsync();

            if (!sunglassesProducts.Any())
            {
                return Json(new
                {
                    success = true,
                    message = "No sunglasses products found to update",
                    productsUpdated = 0
                });
            }

            // Get old categories for reporting
            var oldCategoryIds = new Dictionary<Guid, string>();
            foreach (var product in sunglassesProducts)
            {
                var oldCategory = await _context.Categories
                    .FirstOrDefaultAsync(c => c.Id == product.CategoryId);
                
                oldCategoryIds[product.Id] = oldCategory?.Name ?? "Unknown";
                product.CategoryId = accessoriesCategory.Id;
            }

            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                message = $"Successfully updated {sunglassesProducts.Count} sunglasses products from wrong category to 'Phụ kiện nữ'",
                productsUpdated = sunglassesProducts.Count,
                products = sunglassesProducts.Select(p => new
                {
                    sku = p.SKU,
                    name = p.Name,
                    oldCategory = oldCategoryIds[p.Id],
                    newCategory = "Phụ kiện nữ"
                }).ToList(),
                nextSteps = new[]
                {
                    "Restart application to clear cache",
                    "Check FreelancerSkirt page - should show only skirts",
                    "Check FreelancerAccessories page - should show 5 products"
                }
            });
        }
        catch (Exception ex)
        {
            return Json(new
            {
                success = false,
                message = $"Error: {ex.Message}",
                stackTrace = ex.StackTrace
            });
        }
    }

    // Helper method: Find category by name with Vietnamese case-insensitive matching
    private async Task<Category?> FindCategoryByNameAsync(string categoryName)
    {
        if (string.IsNullOrWhiteSpace(categoryName))
            return null;

        var categoryTrimmed = categoryName.Trim();
        var lowerCategory = categoryTrimmed.ToLower();
        var upperCategory = categoryTrimmed.ToUpper();
        var capitalizedCategory = categoryTrimmed.Length > 0 
            ? char.ToUpper(categoryTrimmed[0]) + categoryTrimmed.Substring(1).ToLower() 
            : categoryTrimmed;

        return await _context.Categories
            .FirstOrDefaultAsync(c => 
                EF.Functions.ILike(c.Name, lowerCategory) || 
                EF.Functions.ILike(c.Name, upperCategory) ||
                EF.Functions.ILike(c.Name, capitalizedCategory));
    }

    // Helper method: Apply keyword filter with OR logic (product name must contain ANY keyword)
    private IQueryable<Product> ApplyKeywordFilter(IQueryable<Product> query, string[] keywords)
    {
        if (keywords == null || keywords.Length == 0)
            return query;

        // Build all search patterns for all keywords (lowercase, uppercase, capitalized)
        var patterns = new List<string>();
        foreach (var keyword in keywords)
        {
            patterns.Add($"%{keyword.ToLower()}%");
            patterns.Add($"%{keyword.ToUpper()}%");
            if (keyword.Length > 0)
            {
                patterns.Add($"%{char.ToUpper(keyword[0]) + keyword.Substring(1).ToLower()}%");
            }
        }

        // Apply single Where clause with OR conditions for all patterns
        // Product name matches if it contains ANY pattern
        // Handle up to 12 patterns (4 keywords x 3 variations) which should be sufficient
        return query.Where(p =>
            EF.Functions.ILike(p.Name, patterns[0]) ||
            (patterns.Count > 1 && EF.Functions.ILike(p.Name, patterns[1])) ||
            (patterns.Count > 2 && EF.Functions.ILike(p.Name, patterns[2])) ||
            (patterns.Count > 3 && EF.Functions.ILike(p.Name, patterns[3])) ||
            (patterns.Count > 4 && EF.Functions.ILike(p.Name, patterns[4])) ||
            (patterns.Count > 5 && EF.Functions.ILike(p.Name, patterns[5])) ||
            (patterns.Count > 6 && EF.Functions.ILike(p.Name, patterns[6])) ||
            (patterns.Count > 7 && EF.Functions.ILike(p.Name, patterns[7])) ||
            (patterns.Count > 8 && EF.Functions.ILike(p.Name, patterns[8])) ||
            (patterns.Count > 9 && EF.Functions.ILike(p.Name, patterns[9])) ||
            (patterns.Count > 10 && EF.Functions.ILike(p.Name, patterns[10])) ||
            (patterns.Count > 11 && EF.Functions.ILike(p.Name, patterns[11]))
        );
    }
}
