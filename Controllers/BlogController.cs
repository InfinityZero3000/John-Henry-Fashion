using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using JohnHenryFashionWeb.Data;
using JohnHenryFashionWeb.Models;
using JohnHenryFashionWeb.ViewModels;
using JohnHenryFashionWeb.Services;
using System.Security.Claims;
using Markdig;

namespace JohnHenryFashionWeb.Controllers
{
    public class BlogController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<BlogController> _logger;

        public BlogController(
            ApplicationDbContext context, 
            UserManager<ApplicationUser> userManager,
            ILogger<BlogController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        // GET: Blog
        public async Task<IActionResult> Index(int page = 1, string? category = null, string? search = null)
        {
            try
            {
                const int pageSize = 9;
                
                var query = _context.BlogPosts
                    .Include(b => b.Category)
                    .Include(b => b.Author)
                    .Where(b => b.Status == "published")
                    .AsQueryable();

                // Filter by category
                if (!string.IsNullOrEmpty(category))
                {
                    query = query.Where(b => b.Category != null && b.Category.Slug == category);
                }

                // Search functionality
                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(b => b.Title.Contains(search) || 
                                           b.Content.Contains(search) ||
                                           (b.Excerpt != null && b.Excerpt.Contains(search)));
                }

                var totalPosts = await query.CountAsync();
                var posts = await query
                    .OrderByDescending(b => b.PublishedAt ?? b.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = (int)Math.Ceiling((double)totalPosts / pageSize);
                ViewBag.Category = category;
                ViewBag.Search = search;

                // Get categories for sidebar
                ViewBag.Categories = await _context.BlogCategories
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.SortOrder)
                    .ToListAsync();

                // Get featured posts
                ViewBag.FeaturedPosts = await _context.BlogPosts
                    .Include(b => b.Category)
                    .Where(b => b.Status == "published" && b.IsFeatured)
                    .OrderByDescending(b => b.PublishedAt ?? b.CreatedAt)
                    .Take(3)
                    .ToListAsync();

                // Set up breadcrumbs
                var breadcrumbs = new List<BreadcrumbItem>
                {
                    new BreadcrumbItem { Name = "Trang chủ", Url = Url.Action("Index", "Home") ?? "/" },
                    new BreadcrumbItem { Name = "BLOG", Url = "" }
                };
                ViewBag.Breadcrumbs = breadcrumbs;

                // Load blog page banner
                var now = DateTime.UtcNow;
                var pageBanner = await _context.MarketingBanners
                    .Where(b => b.IsActive 
                        && b.Position == "page_hero"
                        && b.TargetPage == "Blog"
                        && b.StartDate <= now
                        && (b.EndDate == null || b.EndDate >= now))
                    .OrderBy(b => b.SortOrder)
                    .FirstOrDefaultAsync();

                ViewBag.PageBanner = pageBanner;

                return View(posts);
            }
            catch (Exception ex)
            {
                // Log error and return empty blog view
                Console.WriteLine($"Error loading blog posts: {ex.Message}");
                ViewBag.CurrentPage = 1;
                ViewBag.TotalPages = 0;
                ViewBag.Category = category;
                ViewBag.Search = search;
                ViewBag.Categories = new List<BlogCategory>();
                ViewBag.FeaturedPosts = new List<BlogPost>();
                ViewBag.DatabaseError = "Trang blog hiện tại không thể kết nối cơ sở dữ liệu. Vui lòng thử lại sau.";
                
                return View(new List<BlogPost>());
            }
        }

        // GET: Blog/Details/{slug} - SEO-friendly URL
        [HttpGet("blog/{slug}")]
        public async Task<IActionResult> Details(string slug)
        {
            try
            {
                var post = await _context.BlogPosts
                    .Include(b => b.Category)
                    .Include(b => b.Author)
                    .FirstOrDefaultAsync(b => b.Slug == slug && b.Status == "published");

                if (post == null)
                {
                    return NotFound();
                }

                // Convert Markdown to HTML
                var pipeline = new MarkdownPipelineBuilder()
                    .UseAdvancedExtensions()
                    .Build();
                post.Content = Markdown.ToHtml(post.Content ?? "", pipeline);

                // Increment view count
                post.ViewCount++;
                await _context.SaveChangesAsync();

                // Get related posts - Smart algorithm
                // Priority 1: Same category AND common tags
                // Priority 2: Same category
                // Priority 3: Common tags
                // Priority 4: Latest posts
                var relatedPosts = new List<BlogPost>();
                
                // Try to get posts with same category AND common tags
                if (post.CategoryId.HasValue && post.Tags != null && post.Tags.Any())
                {
                    relatedPosts = await _context.BlogPosts
                        .Include(b => b.Category)
                        .Where(b => b.Id != post.Id && 
                                   b.Status == "published" && 
                                   b.CategoryId == post.CategoryId &&
                                   b.Tags != null && b.Tags.Any(t => post.Tags.Contains(t)))
                        .OrderByDescending(b => b.PublishedAt ?? b.CreatedAt)
                        .Take(3)
                        .ToListAsync();
                }
                
                // If not enough, get posts from same category
                if (relatedPosts.Count < 3 && post.CategoryId.HasValue)
                {
                    var additionalPosts = await _context.BlogPosts
                        .Include(b => b.Category)
                        .Where(b => b.Id != post.Id && 
                                   b.Status == "published" && 
                                   b.CategoryId == post.CategoryId &&
                                   !relatedPosts.Select(r => r.Id).Contains(b.Id))
                        .OrderByDescending(b => b.PublishedAt ?? b.CreatedAt)
                        .Take(3 - relatedPosts.Count)
                        .ToListAsync();
                    
                    relatedPosts.AddRange(additionalPosts);
                }
                
                // If still not enough, get posts with common tags
                if (relatedPosts.Count < 3 && post.Tags != null && post.Tags.Any())
                {
                    var tagPosts = await _context.BlogPosts
                        .Include(b => b.Category)
                        .Where(b => b.Id != post.Id && 
                                   b.Status == "published" && 
                                   b.Tags != null && b.Tags.Any(t => post.Tags.Contains(t)) &&
                                   !relatedPosts.Select(r => r.Id).Contains(b.Id))
                        .OrderByDescending(b => b.PublishedAt ?? b.CreatedAt)
                        .Take(3 - relatedPosts.Count)
                        .ToListAsync();
                    
                    relatedPosts.AddRange(tagPosts);
                }
                
                // If still not enough, get latest published posts
                if (relatedPosts.Count < 3)
                {
                    var latestPosts = await _context.BlogPosts
                        .Include(b => b.Category)
                        .Where(b => b.Id != post.Id && 
                                   b.Status == "published" &&
                                   !relatedPosts.Select(r => r.Id).Contains(b.Id))
                        .OrderByDescending(b => b.PublishedAt ?? b.CreatedAt)
                        .Take(3 - relatedPosts.Count)
                        .ToListAsync();
                    
                    relatedPosts.AddRange(latestPosts);
                }
                
                ViewBag.RelatedPosts = relatedPosts;

                // Set up breadcrumbs
                var breadcrumbs = new List<BreadcrumbItem>
                {
                    new BreadcrumbItem { Name = "Trang chủ", Url = Url.Action("Index", "Home") ?? "/" },
                    new BreadcrumbItem { Name = "BLOG", Url = Url.Action("Index", "Blog") ?? "/Blog" },
                    new BreadcrumbItem { Name = post.Title, Url = "" }
                };
                ViewBag.Breadcrumbs = breadcrumbs;

                return View(post);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading blog post with slug: {Slug}", slug);
                ViewBag.Error = "Có lỗi xảy ra khi tải bài viết.";
                return View("Error");
            }
        }

        // GET: Blog/Details/ById/{id} - Fallback for direct ID access (redirects to slug)
        [HttpGet("blog/details/{id}")]
        public async Task<IActionResult> DetailsById(Guid id)
        {
            var post = await _context.BlogPosts
                .Include(b => b.Category)
                .Include(b => b.Author)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (post == null)
            {
                return NotFound();
            }

            // If post is not published, only allow authenticated users (admins) to view
            if (post.Status != "published" && !(User?.Identity?.IsAuthenticated == true))
            {
                return NotFound();
            }

            // For published posts, redirect to SEO-friendly slug URL
            if (post.Status == "published")
            {
                return RedirectToAction("Details", new { slug = post.Slug });
            }

            // For draft/archived posts, show preview directly (admin only)
            // Get related posts from same category
            var relatedPosts = await _context.BlogPosts
                .Include(b => b.Category)
                .Include(b => b.Author)
                .Where(b => b.Id != id && b.CategoryId == post.CategoryId && b.Status == "published")
                .OrderByDescending(b => b.PublishedAt)
                .Take(3)
                .ToListAsync();

            ViewBag.RelatedPosts = relatedPosts;
            ViewBag.IsPreview = true;

            return View("Details", post);
        }

        // GET: Blog/Category/fashion
        public async Task<IActionResult> Category(string slug, int page = 1)
        {
            const int pageSize = 9;
            
            var category = await _context.BlogCategories
                .FirstOrDefaultAsync(c => c.Slug == slug && c.IsActive);

            if (category == null)
            {
                return NotFound();
            }

            // Get posts for this category
            var query = _context.BlogPosts
                .Include(b => b.Category)
                .Include(b => b.Author)
                .Where(b => b.Status == "published" && b.Category != null && b.Category.Slug == slug);

            var totalPosts = await query.CountAsync();
            var posts = await query
                .OrderByDescending(b => b.PublishedAt ?? b.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Get all categories for sidebar
            var allCategories = await _context.BlogCategories
                .Where(c => c.IsActive)
                .OrderBy(c => c.SortOrder)
                .ToListAsync();

            // Get featured posts for sidebar
            var featuredPosts = await _context.BlogPosts
                .Include(b => b.Category)
                .Where(b => b.Status == "published" && b.IsFeatured)
                .OrderByDescending(b => b.PublishedAt ?? b.CreatedAt)
                .Take(5)
                .ToListAsync();

            var viewModel = new BlogCategoryViewModel
            {
                Category = category,
                Posts = posts,
                AllCategories = allCategories,
                FeaturedPosts = featuredPosts,
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling((double)totalPosts / pageSize),
                TotalPosts = totalPosts
            };

            // Set up breadcrumbs
            var breadcrumbs = new List<BreadcrumbItem>
            {
                new BreadcrumbItem { Name = "Trang chủ", Url = Url.Action("Index", "Home") ?? "/" },
                new BreadcrumbItem { Name = "BLOG", Url = Url.Action("Index", "Blog") ?? "/Blog" },
                new BreadcrumbItem { Name = category.Name, Url = "" }
            };
            ViewBag.Breadcrumbs = breadcrumbs;

            return View(viewModel);
        }

        // GET: Blog/Create
        [Authorize(Roles = UserRoles.Admin)]
        public async Task<IActionResult> Create()
        {
            ViewBag.Categories = await _context.BlogCategories
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .ToListAsync();

            // Set up breadcrumbs
            var breadcrumbs = new List<BreadcrumbItem>
            {
                new BreadcrumbItem { Name = "Trang chủ", Url = Url.Action("Index", "Home") ?? "/" },
                new BreadcrumbItem { Name = "BLOG", Url = Url.Action("Index", "Blog") ?? "/Blog" },
                new BreadcrumbItem { Name = "Tạo bài viết", Url = "" }
            };
            ViewBag.Breadcrumbs = breadcrumbs;

            return View();
        }

        // POST: Blog/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = UserRoles.Admin)]
        public async Task<IActionResult> Create(BlogPost post)
        {
            if (ModelState.IsValid)
            {
                post.Id = Guid.NewGuid();
                post.AuthorId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
                post.CreatedAt = DateTime.UtcNow;
                post.UpdatedAt = DateTime.UtcNow;
                
                // Generate slug from title
                post.Slug = GenerateSlug(post.Title);

                // Set published date if status is published
                if (post.Status == "published" && !post.PublishedAt.HasValue)
                {
                    post.PublishedAt = DateTime.UtcNow;
                }

                _context.BlogPosts.Add(post);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Bài viết đã được tạo thành công!";
                return RedirectToAction(nameof(Details), new { id = post.Id });
            }

            ViewBag.Categories = await _context.BlogCategories
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .ToListAsync();

            return View(post);
        }

        // GET: Blog/Edit/5
        [Authorize(Roles = UserRoles.Admin)]
        public async Task<IActionResult> Edit(Guid id)
        {
            var post = await _context.BlogPosts.FindAsync(id);
            if (post == null)
            {
                return NotFound();
            }

            ViewBag.Categories = await _context.BlogCategories
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .ToListAsync();

            // Set up breadcrumbs
            var breadcrumbs = new List<BreadcrumbItem>
            {
                new BreadcrumbItem { Name = "Trang chủ", Url = Url.Action("Index", "Home") ?? "/" },
                new BreadcrumbItem { Name = "BLOG", Url = Url.Action("Index", "Blog") ?? "/Blog" },
                new BreadcrumbItem { Name = "Chỉnh sửa bài viết", Url = "" }
            };
            ViewBag.Breadcrumbs = breadcrumbs;

            return View(post);
        }

        // POST: Blog/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = UserRoles.Admin)]
        public async Task<IActionResult> Edit(Guid id, BlogPost post)
        {
            if (id != post.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existingPost = await _context.BlogPosts.FindAsync(id);
                    if (existingPost == null)
                    {
                        return NotFound();
                    }

                    // Update fields
                    existingPost.Title = post.Title;
                    existingPost.Slug = GenerateSlug(post.Title);
                    existingPost.Excerpt = post.Excerpt;
                    existingPost.Content = post.Content;
                    existingPost.FeaturedImageUrl = post.FeaturedImageUrl;
                    existingPost.Status = post.Status;
                    existingPost.IsFeatured = post.IsFeatured;
                    existingPost.Tags = post.Tags;
                    existingPost.MetaTitle = post.MetaTitle;
                    existingPost.MetaDescription = post.MetaDescription;
                    existingPost.CategoryId = post.CategoryId;
                    existingPost.UpdatedAt = DateTime.UtcNow;

                    // Set published date if status changed to published
                    if (post.Status == "published" && existingPost.Status != "published")
                    {
                        existingPost.PublishedAt = DateTime.UtcNow;
                    }

                    await _context.SaveChangesAsync();
                    
                    TempData["Success"] = "Bài viết đã được cập nhật thành công!";
                    return RedirectToAction(nameof(Details), new { id = post.Id });
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BlogPostExists(post.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            ViewBag.Categories = await _context.BlogCategories
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .ToListAsync();

            return View(post);
        }

        // POST: Blog/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = UserRoles.Admin)]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var post = await _context.BlogPosts.FindAsync(id);
            if (post != null)
            {
                _context.BlogPosts.Remove(post);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Bài viết đã được xóa thành công!";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool BlogPostExists(Guid id)
        {
            return _context.BlogPosts.Any(e => e.Id == id);
        }

        private string GenerateSlug(string title, int maxLength = 80)
        {
            if (string.IsNullOrWhiteSpace(title))
                return "untitled";
            
            // Convert to lowercase and replace Vietnamese characters
            var slug = title.ToLower()
                      .Replace("á", "a").Replace("à", "a").Replace("ả", "a").Replace("ã", "a").Replace("ạ", "a")
                      .Replace("ă", "a").Replace("ắ", "a").Replace("ằ", "a").Replace("ẳ", "a").Replace("ẵ", "a").Replace("ặ", "a")
                      .Replace("â", "a").Replace("ấ", "a").Replace("ầ", "a").Replace("ẩ", "a").Replace("ẫ", "a").Replace("ậ", "a")
                      .Replace("é", "e").Replace("è", "e").Replace("ẻ", "e").Replace("ẽ", "e").Replace("ẹ", "e")
                      .Replace("ê", "e").Replace("ế", "e").Replace("ề", "e").Replace("ể", "e").Replace("ễ", "e").Replace("ệ", "e")
                      .Replace("í", "i").Replace("ì", "i").Replace("ỉ", "i").Replace("ĩ", "i").Replace("ị", "i")
                      .Replace("ó", "o").Replace("ò", "o").Replace("ỏ", "o").Replace("õ", "o").Replace("ọ", "o")
                      .Replace("ô", "o").Replace("ố", "o").Replace("ồ", "o").Replace("ổ", "o").Replace("ỗ", "o").Replace("ộ", "o")
                      .Replace("ơ", "o").Replace("ớ", "o").Replace("ờ", "o").Replace("ở", "o").Replace("ỡ", "o").Replace("ợ", "o")
                      .Replace("ú", "u").Replace("ù", "u").Replace("ủ", "u").Replace("ũ", "u").Replace("ụ", "u")
                      .Replace("ư", "u").Replace("ứ", "u").Replace("ừ", "u").Replace("ử", "u").Replace("ữ", "u").Replace("ự", "u")
                      .Replace("ý", "y").Replace("ỳ", "y").Replace("ỷ", "y").Replace("ỹ", "y").Replace("ỵ", "y")
                      .Replace("đ", "d");
            
            // Remove special characters (keep only alphanumeric, space, and dash)
            slug = System.Text.RegularExpressions.Regex.Replace(slug, @"[^a-z0-9\s-]", "");
            
            // Replace multiple spaces with single space
            slug = System.Text.RegularExpressions.Regex.Replace(slug, @"\s+", " ");
            
            // Replace spaces with dashes
            slug = slug.Replace(" ", "-");
            
            // Replace multiple dashes with single dash
            slug = System.Text.RegularExpressions.Regex.Replace(slug, @"-+", "-");
            
            // Trim dashes from start and end
            slug = slug.Trim('-');
            
            // Limit length
            if (slug.Length > maxLength)
            {
                slug = slug.Substring(0, maxLength);
                // Remove incomplete word at end
                var lastDash = slug.LastIndexOf('-');
                if (lastDash > 0)
                    slug = slug.Substring(0, lastDash);
            }
            
            // Return untitled if slug becomes empty after processing
            return string.IsNullOrEmpty(slug) ? "untitled" : slug;
        }
    }
}
