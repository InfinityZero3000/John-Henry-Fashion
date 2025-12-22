using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using JohnHenryFashionWeb.Data;
using JohnHenryFashionWeb.Models;

namespace JohnHenryFashionWeb.Controllers;

[Authorize(Roles = "Admin")]
[Route("admin/products")]
public class AdminProductsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _webHostEnvironment;
    private readonly ILogger<AdminProductsController> _logger;

    public AdminProductsController(
        ApplicationDbContext context,
        IWebHostEnvironment webHostEnvironment,
        ILogger<AdminProductsController> logger)
    {
        _context = context;
        _webHostEnvironment = webHostEnvironment;
        _logger = logger;
    }

    // GET: admin/products
    [HttpGet("")]
    public async Task<IActionResult> Index(string? search, Guid? categoryId, int page = 1, int pageSize = 20)
    {
        var query = _context.Products
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .AsQueryable();

        // Filter by search
        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(p => p.Name.Contains(search) || p.SKU.Contains(search));
        }

        // Filter by category
        if (categoryId.HasValue)
        {
            query = query.Where(p => p.CategoryId == categoryId.Value);
        }

        var totalItems = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

        var products = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        // Pass data to view
        ViewBag.Search = search;
        ViewBag.CategoryId = categoryId;
        ViewBag.Page = page;
        ViewBag.PageSize = pageSize;
        ViewBag.TotalItems = totalItems;
        ViewBag.TotalPages = totalPages;
        ViewBag.Categories = await _context.Categories.ToListAsync();

        return View(products);
    }

    // GET: admin/products/create
    [HttpGet("create")]
    public async Task<IActionResult> Create()
    {
        ViewBag.Categories = new SelectList(await _context.Categories.ToListAsync(), "Id", "Name");
        ViewBag.Brands = new SelectList(await _context.Brands.ToListAsync(), "Id", "Name");
        return View();
    }

    // POST: admin/products/create
    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Product product, IFormFile? imageFile)
    {
        // FIX 1: Validate SalePrice < Price
        if (product.SalePrice.HasValue && product.SalePrice >= product.Price)
        {
            ModelState.AddModelError("SalePrice", "Giá khuyến mãi phải nhỏ hơn giá gốc!");
        }
        
        // FIX 2: Validate SKU unique
        if (await _context.Products.AnyAsync(p => p.SKU == product.SKU))
        {
            ModelState.AddModelError("SKU", "Mã SKU đã tồn tại trong hệ thống!");
        }
        
        // FIX 3: Validate CategoryId exists
        if (!await _context.Categories.AnyAsync(c => c.Id == product.CategoryId))
        {
            ModelState.AddModelError("CategoryId", "Danh mục không tồn tại!");
        }
        
        // FIX 4: Validate BrandId if provided
        if (product.BrandId.HasValue && !await _context.Brands.AnyAsync(b => b.Id == product.BrandId))
        {
            ModelState.AddModelError("BrandId", "Thương hiệu không tồn tại!");
        }
        
        if (ModelState.IsValid)
        {
            // FIX 5: Start transaction for atomicity
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Handle image upload
                if (imageFile != null && imageFile.Length > 0)
                {
                    var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "products");
                    Directory.CreateDirectory(uploadsFolder);

                    var uniqueFileName = $"{Guid.NewGuid()}_{imageFile.FileName}";
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(fileStream);
                    }

                    product.FeaturedImageUrl = $"~/images/products/{uniqueFileName}";
                }

                // FIX 6: Generate unique slug
                product.Slug = await GenerateUniqueSlug(product.Name);
                product.CreatedAt = DateTime.UtcNow;
                product.UpdatedAt = DateTime.UtcNow;
                product.IsActive = true;

                _context.Add(product);
                await _context.SaveChangesAsync();
                
                // Commit transaction
                await transaction.CommitAsync();

                TempData["Success"] = "Tạo sản phẩm thành công!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                // Rollback transaction on error
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error creating product: {Message}", ex.Message);
                
                ModelState.AddModelError("", "Có lỗi xảy ra khi tạo sản phẩm. Vui lòng thử lại!");
            }
        }

        ViewBag.Categories = new SelectList(await _context.Categories.ToListAsync(), "Id", "Name", product.CategoryId);
        ViewBag.Brands = new SelectList(await _context.Brands.ToListAsync(), "Id", "Name", product.BrandId);
        return View(product);
    }

    // GET: admin/products/edit/{id}
    [HttpGet("edit/{id}")]
    public async Task<IActionResult> Edit(Guid id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null)
        {
            return NotFound();
        }

        ViewBag.Categories = new SelectList(await _context.Categories.ToListAsync(), "Id", "Name", product.CategoryId);
        ViewBag.Brands = new SelectList(await _context.Brands.ToListAsync(), "Id", "Name", product.BrandId);
        return View(product);
    }

    // POST: admin/products/edit/{id}
    [HttpPost("edit/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, Product product, IFormFile? imageFile)
    {
        if (id != product.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                var existingProduct = await _context.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
                if (existingProduct == null)
                {
                    return NotFound();
                }

                // Handle image upload
                if (imageFile != null && imageFile.Length > 0)
                {
                    // Delete old image if exists
                    if (!string.IsNullOrEmpty(existingProduct.FeaturedImageUrl))
                    {
                        var oldImagePath = existingProduct.FeaturedImageUrl.Replace("~/", "");
                        var oldImageFullPath = Path.Combine(_webHostEnvironment.WebRootPath, oldImagePath);
                        if (System.IO.File.Exists(oldImageFullPath))
                        {
                            System.IO.File.Delete(oldImageFullPath);
                        }
                    }

                    var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "products");
                    Directory.CreateDirectory(uploadsFolder);

                    var uniqueFileName = $"{Guid.NewGuid()}_{imageFile.FileName}";
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(fileStream);
                    }

                    product.FeaturedImageUrl = $"~/images/products/{uniqueFileName}";
                }
                else
                {
                    // Keep existing image
                    product.FeaturedImageUrl = existingProduct.FeaturedImageUrl;
                }

                // Update slug if name changed
                if (product.Name != existingProduct.Name)
                {
                    product.Slug = GenerateSlug(product.Name);
                }
                else
                {
                    product.Slug = existingProduct.Slug;
                }

                product.CreatedAt = existingProduct.CreatedAt;
                product.UpdatedAt = DateTime.UtcNow;

                _context.Update(product);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Cập nhật sản phẩm thành công!";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ProductExists(product.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
        }

        ViewBag.Categories = new SelectList(await _context.Categories.ToListAsync(), "Id", "Name", product.CategoryId);
        ViewBag.Brands = new SelectList(await _context.Brands.ToListAsync(), "Id", "Name", product.BrandId);
        return View(product);
    }

    // POST: admin/products/{id}/delete
    [HttpPost("{id}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null)
        {
            return NotFound();
        }

        // Delete image file if exists
        if (!string.IsNullOrEmpty(product.FeaturedImageUrl))
        {
            var imagePath = product.FeaturedImageUrl.Replace("~/", "");
            var imageFullPath = Path.Combine(_webHostEnvironment.WebRootPath, imagePath);
            if (System.IO.File.Exists(imageFullPath))
            {
                System.IO.File.Delete(imageFullPath);
            }
        }

        _context.Products.Remove(product);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Xóa sản phẩm thành công!";
        return RedirectToAction(nameof(Index));
    }

    private bool ProductExists(Guid id)
    {
        return _context.Products.Any(e => e.Id == id);
    }

    private string GenerateSlug(string name)
    {
        // Simple slug generation - remove accents and special characters
        var slug = name.ToLower();
        slug = System.Text.RegularExpressions.Regex.Replace(slug, @"[^a-z0-9\s-]", "");
        slug = System.Text.RegularExpressions.Regex.Replace(slug, @"\s+", "-");
        slug = System.Text.RegularExpressions.Regex.Replace(slug, @"-+", "-");
        return slug.Trim('-');
    }
    
    // NEW: Generate unique slug by appending counter if needed
    private async Task<string> GenerateUniqueSlug(string name)
    {
        var baseSlug = GenerateSlug(name);
        var slug = baseSlug;
        var counter = 1;
        
        while (await _context.Products.AnyAsync(p => p.Slug == slug))
        {
            slug = $"{baseSlug}-{counter}";
            counter++;
        }
        
        return slug;
    }
}
