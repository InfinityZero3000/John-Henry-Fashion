using Microsoft.EntityFrameworkCore;
using JohnHenryFashionWeb.Data;
using JohnHenryFashionWeb.Models;

namespace JohnHenryFashionWeb.Services
{
    public interface IOptimizedDataService
    {
        Task<IEnumerable<Product>> GetFeaturedProductsAsync(int count = 10);
        Task<IEnumerable<Product>> GetPopularProductsAsync(int count = 10);
        Task<IEnumerable<Category>> GetActiveCategoriesAsync();
        Task<IEnumerable<Product>> GetProductsByCategoryAsync(Guid categoryId, int page = 1, int pageSize = 20);
        Task<Product?> GetProductWithDetailsAsync(Guid productId);
        Task<IEnumerable<Product>> SearchProductsOptimizedAsync(string? searchTerm, Guid? categoryId, decimal? minPrice, decimal? maxPrice, string? sortBy, int page = 1, int pageSize = 20);
        Task<Dictionary<Guid, int>> GetCategoryProductCountsAsync();
        Task<IEnumerable<BlogPost>> GetRecentBlogPostsAsync(int count = 5);
        Task<ApplicationUser?> GetUserWithDetailsAsync(string userId);
        Task UpdateProductViewCountAsync(Guid productId);
        Task<SiteStatistics> GetSiteStatisticsAsync();
        Task<IEnumerable<Product>> GetRelatedProductsAsync(Guid productId, int count = 4);
    }

    public class OptimizedDataService : IOptimizedDataService
    {
        private readonly ApplicationDbContext _context;
        private readonly ICacheService _cacheService;
        private readonly ILogger<OptimizedDataService> _logger;

        public OptimizedDataService(
            ApplicationDbContext context,
            ICacheService cacheService,
            ILogger<OptimizedDataService> logger)
        {
            _context = context;
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<IEnumerable<Product>> GetFeaturedProductsAsync(int count = 10)
        {
            return await _cacheService.GetOrSetAsync(
                CacheKeys.FeaturedProducts,
                async () =>
                {
                    return await _context.Products
                        .Where(p => p.IsFeatured && p.IsActive)
                        .Include(p => p.Category)
                        .Include(p => p.Brand)
                        .OrderByDescending(p => p.ViewCount)
                        .Take(count)
                        .AsNoTracking()
                        .ToListAsync();
                },
                TimeSpan.FromMinutes(30)
            );
        }

        public async Task<IEnumerable<Product>> GetPopularProductsAsync(int count = 10)
        {
            return await _cacheService.GetOrSetAsync(
                CacheKeys.PopularProducts,
                async () =>
                {
                    return await _context.Products
                        .Where(p => p.IsActive)
                        .Include(p => p.Category)
                        .Include(p => p.Brand)
                        .OrderByDescending(p => p.ViewCount)
                        .ThenByDescending(p => p.Rating)
                        .Take(count)
                        .AsNoTracking()
                        .ToListAsync();
                },
                TimeSpan.FromMinutes(15)
            );
        }

        public async Task<IEnumerable<Category>> GetActiveCategoriesAsync()
        {
            return await _cacheService.GetOrSetAsync(
                CacheKeys.Categories,
                async () =>
                {
                    return await _context.Categories
                        .Where(c => c.IsActive)
                        .OrderBy(c => c.SortOrder)
                        .ThenBy(c => c.Name)
                        .AsNoTracking()
                        .ToListAsync();
                },
                TimeSpan.FromHours(1)
            );
        }

        public async Task<IEnumerable<Product>> GetProductsByCategoryAsync(Guid categoryId, int page = 1, int pageSize = 20)
        {
            var cacheKey = $"{CacheKeys.ProductsByCategory(categoryId)}_page_{page}_size_{pageSize}";
            
            return await _cacheService.GetOrSetAsync(
                cacheKey,
                async () =>
                {
                    return await _context.Products
                        .Where(p => p.CategoryId == categoryId && p.IsActive)
                        .Include(p => p.Category)
                        .Include(p => p.Brand)
                        .OrderByDescending(p => p.IsFeatured)
                        .ThenByDescending(p => p.ViewCount)
                        .Skip((page - 1) * pageSize)
                        .Take(pageSize)
                        .AsNoTracking()
                        .ToListAsync();
                },
                TimeSpan.FromMinutes(10)
            );
        }

        public async Task<Product?> GetProductWithDetailsAsync(Guid productId)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.ProductImages)
                .Include(p => p.ProductReviews.Where(r => r.IsApproved))
                    .ThenInclude(r => r.User)
                .FirstOrDefaultAsync(p => p.Id == productId && p.IsActive);

            return product;
        }

        public async Task<IEnumerable<Product>> SearchProductsOptimizedAsync(
            string? searchTerm, 
            Guid? categoryId, 
            decimal? minPrice, 
            decimal? maxPrice, 
            string? sortBy, 
            int page = 1, 
            int pageSize = 20)
        {
            var cacheKey = $"search_{searchTerm}_{categoryId}_{minPrice}_{maxPrice}_{sortBy}_{page}_{pageSize}";
            
            return await _cacheService.GetOrSetAsync(
                cacheKey,
                async () =>
                {
                    var query = _context.Products
                        .Where(p => p.IsActive)
                        .Include(p => p.Category)
                        .Include(p => p.Brand)
                        .AsQueryable();

                    // Apply filters
                    if (!string.IsNullOrWhiteSpace(searchTerm))
                    {
                        var searchTermLower = searchTerm.ToLower();
                        query = query.Where(p => 
                            p.Name.ToLower().Contains(searchTermLower) ||
                            p.Description!.ToLower().Contains(searchTermLower) ||
                            p.Category.Name.ToLower().Contains(searchTermLower));
                    }

                    if (categoryId.HasValue)
                    {
                        query = query.Where(p => p.CategoryId == categoryId.Value);
                    }

                    if (minPrice.HasValue)
                    {
                        query = query.Where(p => p.Price >= minPrice.Value);
                    }

                    if (maxPrice.HasValue)
                    {
                        query = query.Where(p => p.Price <= maxPrice.Value);
                    }

                    // Apply sorting
                    query = sortBy?.ToLower() switch
                    {
                        "price_asc" => query.OrderBy(p => p.Price),
                        "price_desc" => query.OrderByDescending(p => p.Price),
                        "name_asc" => query.OrderBy(p => p.Name),
                        "name_desc" => query.OrderByDescending(p => p.Name),
                        "rating" => query.OrderByDescending(p => p.Rating),
                        "newest" => query.OrderByDescending(p => p.CreatedAt),
                        "popular" => query.OrderByDescending(p => p.ViewCount),
                        _ => query.OrderByDescending(p => p.IsFeatured).ThenByDescending(p => p.ViewCount)
                    };

                    return await query
                        .Skip((page - 1) * pageSize)
                        .Take(pageSize)
                        .AsNoTracking()
                        .ToListAsync();
                },
                TimeSpan.FromMinutes(5)
            );
        }

        public async Task<Dictionary<Guid, int>> GetCategoryProductCountsAsync()
        {
            return await _cacheService.GetOrSetAsync(
                "category_product_counts",
                async () =>
                {
                    return await _context.Products
                        .Where(p => p.IsActive)
                        .GroupBy(p => p.CategoryId)
                        .ToDictionaryAsync(g => g.Key, g => g.Count());
                },
                TimeSpan.FromMinutes(30)
            );
        }

        public async Task<IEnumerable<BlogPost>> GetRecentBlogPostsAsync(int count = 5)
        {
            return await _cacheService.GetOrSetAsync(
                CacheKeys.RecentPosts,
                async () =>
                {
                    return await _context.BlogPosts
                        .Where(b => b.Status == "published")
                        .Include(b => b.Author)
                        .OrderByDescending(b => b.PublishedAt)
                        .Take(count)
                        .AsNoTracking()
                        .ToListAsync();
                },
                TimeSpan.FromMinutes(15)
            );
        }

        public async Task<ApplicationUser?> GetUserWithDetailsAsync(string userId)
        {
            var user = await _context.Users
                .Include(u => u.Orders.Take(10))
                    .ThenInclude(o => o.OrderItems)
                .Include(u => u.Wishlists.Take(20))
                    .ThenInclude(w => w.Product)
                .FirstOrDefaultAsync(u => u.Id == userId);

            return user;
        }

        public async Task UpdateProductViewCountAsync(Guid productId)
        {
            try
            {
                // Use ExecuteUpdateAsync for type-safe update
                await _context.Products
                    .Where(p => p.Id == productId)
                    .ExecuteUpdateAsync(s => s.SetProperty(p => p.ViewCount, p => p.ViewCount + 1));

                // Clear the product cache
                await _cacheService.RemoveAsync(CacheKeys.ProductById(productId));
                await _cacheService.RemoveByPatternAsync("popular_products");
                await _cacheService.RemoveByPatternAsync("featured_products");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update view count for product {ProductId}", productId);
            }
        }

        public async Task<SiteStatistics> GetSiteStatisticsAsync()
        {
            return await _cacheService.GetOrSetAsync(
                CacheKeys.SiteStatistics,
                async () =>
                {
                    var stats = new SiteStatistics();

                    // Use parallel queries for better performance
                    var tasks = new Task[]
                    {
                        Task.Run(async () => stats.TotalProducts = await _context.Products.Where(p => p.IsActive).CountAsync()),
                        Task.Run(async () => stats.TotalCategories = await _context.Categories.Where(c => c.IsActive).CountAsync()),
                        Task.Run(async () => stats.TotalUsers = await _context.Users.CountAsync()),
                        Task.Run(async () => stats.TotalOrders = await _context.Orders.CountAsync()),
                        Task.Run(async () =>                     stats.TotalBlogPosts = await _context.BlogPosts.Where(b => b.Status == "published").CountAsync())
                    };

                    await Task.WhenAll(tasks);

                    // Calculate additional metrics
                    stats.TotalRevenue = await _context.Orders
                        .Where(o => o.Status == "completed")
                        .SumAsync(o => o.TotalAmount);

                    stats.AverageOrderValue = stats.TotalOrders > 0 
                        ? stats.TotalRevenue / stats.TotalOrders 
                        : 0;

                    return stats;
                },
                TimeSpan.FromHours(1)
            );
        }

        public async Task<IEnumerable<Product>> GetRelatedProductsAsync(Guid productId, int count = 4)
        {
            return await _cacheService.GetOrSetAsync(
                $"related_products_{productId}_{count}",
                async () =>
                {
                    var product = await _context.Products
                        .AsNoTracking()
                        .FirstOrDefaultAsync(p => p.Id == productId);

                    if (product == null) return new List<Product>();

                    return await _context.Products
                        .Where(p => p.Id != productId && 
                                   p.IsActive && 
                                   (p.CategoryId == product.CategoryId || p.BrandId == product.BrandId))
                        .Include(p => p.Category)
                        .Include(p => p.Brand)
                        .OrderByDescending(p => p.ViewCount)
                        .Take(count)
                        .AsNoTracking()
                        .ToListAsync();
                },
                TimeSpan.FromMinutes(30)
            );
        }
    }

    public class SiteStatistics
    {
        public int TotalProducts { get; set; }
        public int TotalCategories { get; set; }
        public int TotalUsers { get; set; }
        public int TotalOrders { get; set; }
        public int TotalBlogPosts { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal AverageOrderValue { get; set; }
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }
}
