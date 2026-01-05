using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using JohnHenryFashionWeb.Data;
using JohnHenryFashionWeb.Models;
using JohnHenryFashionWeb.Services;

namespace JohnHenryFashionWeb.Controllers;

[Authorize(Roles = "Admin")]
[Route("admin/products")]
public class AdminProductsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _webHostEnvironment;
    private readonly ICloudinaryService _cloudinaryService;
    private readonly ILogger<AdminProductsController> _logger;

    public AdminProductsController(
        ApplicationDbContext context,
        IWebHostEnvironment webHostEnvironment,
        ICloudinaryService cloudinaryService,
        ILogger<AdminProductsController> logger)
    {
        _context = context;
        _webHostEnvironment = webHostEnvironment;
        _cloudinaryService = cloudinaryService;
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
        _logger.LogInformation("=== CREATE PRODUCT STARTED ===");
        _logger.LogInformation("Product Name: {Name}, SKU: {SKU}, Price: {Price}", product.Name, product.SKU, product.Price);
        
        try
        {
            // Remove model state for fields that will be set automatically
            ModelState.Remove("Slug");
            ModelState.Remove("CreatedAt");
            ModelState.Remove("UpdatedAt");
            ModelState.Remove("FeaturedImageUrl");
            ModelState.Remove("ViewCount");
            ModelState.Remove("Rating");
            ModelState.Remove("ReviewCount");
            
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
            
            // Log ModelState errors
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("ModelState is invalid:");
                foreach (var key in ModelState.Keys)
                {
                    var state = ModelState[key];
                    if (state?.Errors?.Count > 0)
                    {
                        foreach (var error in state.Errors)
                        {
                            _logger.LogWarning("  {Key}: {Error}", key, error.ErrorMessage);
                        }
                    }
                }
            }
            
            if (ModelState.IsValid)
            {
                // FIX 5: Start transaction for atomicity
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // Handle image upload with Cloudinary
                    if (imageFile != null && imageFile.Length > 0)
                    {
                        _logger.LogInformation("Processing image upload: {FileName}, Size: {Size}", imageFile.FileName, imageFile.Length);
                        
                        try
                        {
                            // Upload to Cloudinary
                            var uploadResult = await _cloudinaryService.UploadImageAsync(imageFile, "products");
                            
                            if (uploadResult != null && !string.IsNullOrEmpty(uploadResult.SecureUrl?.ToString()))
                            {
                                product.FeaturedImageUrl = uploadResult.SecureUrl.ToString();
                                product.ImageUrl = uploadResult.SecureUrl.ToString(); // Also set ImageUrl for compatibility
                                
                                // Store PublicId for future deletion (optional: add PublicId field to Product model)
                                // product.CloudinaryPublicId = uploadResult.PublicId;
                                
                                _logger.LogInformation("Image uploaded to Cloudinary: {Url}, PublicId: {PublicId}", 
                                    product.FeaturedImageUrl, uploadResult.PublicId);
                            }
                            else
                            {
                                throw new InvalidOperationException("Cloudinary upload failed");
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error uploading image to Cloudinary");
                            ModelState.AddModelError("imageFile", "Không thể upload ảnh. Vui lòng thử lại!");
                            throw;
                        }

                        /* LOCAL FILE UPLOAD - Commented out for production (using Cloudinary)
                        // Uncomment this block to use local file storage instead of Cloudinary
                        
                        // Validate image
                        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                        var extension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
                        
                        if (!allowedExtensions.Contains(extension))
                        {
                            ModelState.AddModelError("imageFile", "Chỉ chấp nhận file ảnh JPG, PNG, GIF!");
                            throw new InvalidOperationException("Invalid image format");
                        }
                        
                        if (imageFile.Length > 5 * 1024 * 1024)
                        {
                            ModelState.AddModelError("imageFile", "Kích thước file không được vượt quá 5MB!");
                            throw new InvalidOperationException("File too large");
                        }

                        var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "products");
                        Directory.CreateDirectory(uploadsFolder);

                        var uniqueFileName = $"{Guid.NewGuid()}{extension}";
                        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await imageFile.CopyToAsync(fileStream);
                        }

                        product.FeaturedImageUrl = $"~/images/products/{uniqueFileName}";
                        _logger.LogInformation("Image saved: {Path}", product.FeaturedImageUrl);
                        */
                    }

                    // FIX 6: Generate unique slug
                    product.Slug = await GenerateUniqueSlug(product.Name);
                    product.CreatedAt = DateTime.UtcNow;
                    product.UpdatedAt = DateTime.UtcNow;
                    product.IsActive = product.IsActive; // Keep the value from form
                    product.ViewCount = 0;
                    product.Rating = null;
                    product.ReviewCount = 0;

                    _logger.LogInformation("Adding product to database...");
                    _context.Add(product);
                    await _context.SaveChangesAsync();
                    
                    // Commit transaction
                    await transaction.CommitAsync();

                    _logger.LogInformation("Product created successfully with ID: {Id}", product.Id);
                    TempData["Success"] = "Tạo sản phẩm thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    // Rollback transaction on error
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Error creating product: {Message}", ex.Message);
                    
                    ModelState.AddModelError("", $"Có lỗi xảy ra khi tạo sản phẩm: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in Create action: {Message}", ex.Message);
            ModelState.AddModelError("", "Có lỗi không mong muốn xảy ra. Vui lòng thử lại!");
        }

        // If we got here, something failed, redisplay form
        _logger.LogWarning("Create failed, redisplaying form");
        ViewBag.Categories = new SelectList(await _context.Categories.ToListAsync(), "Id", "Name", product.CategoryId);
        ViewBag.Brands = new SelectList(await _context.Brands.ToListAsync(), "Id", "Name", product.BrandId);
        return View(product);
    }

    // GET: admin/products/edit/{id}
    [HttpGet("edit/{id}")]
    public async Task<IActionResult> Edit(Guid id)
    {
        try
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .FirstOrDefaultAsync(p => p.Id == id);
                
            if (product == null)
            {
                TempData["Error"] = "Không tìm thấy sản phẩm!";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Categories = new SelectList(
                await _context.Categories.ToListAsync(), 
                "Id", 
                "Name", 
                product.CategoryId
            );
            
            ViewBag.Brands = new SelectList(
                await _context.Brands.ToListAsync(), 
                "Id", 
                "Name", 
                product.BrandId
            );
            
            return View(product);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading product {Id} for edit", id);
            TempData["Error"] = "Có lỗi xảy ra khi tải sản phẩm!";
            return RedirectToAction(nameof(Index));
        }
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

        // Server-side validations
        if (product.SalePrice.HasValue && product.SalePrice >= product.Price)
        {
            ModelState.AddModelError("SalePrice", "Giá khuyến mãi phải nhỏ hơn giá gốc!");
        }
        
        if (await _context.Products.AnyAsync(p => p.SKU == product.SKU && p.Id != id))
        {
            ModelState.AddModelError("SKU", "Mã SKU đã tồn tại trong hệ thống!");
        }
        
        if (!await _context.Categories.AnyAsync(c => c.Id == product.CategoryId))
        {
            ModelState.AddModelError("CategoryId", "Danh mục không tồn tại!");
        }
        
        if (product.BrandId.HasValue && !await _context.Brands.AnyAsync(b => b.Id == product.BrandId))
        {
            ModelState.AddModelError("BrandId", "Thương hiệu không tồn tại!");
        }

        if (ModelState.IsValid)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var existingProduct = await _context.Products.FindAsync(id);
                if (existingProduct == null)
                {
                    return NotFound();
                }

                // Update properties from the form onto the existing entity
                existingProduct.Name = product.Name;
                existingProduct.Description = product.Description;
                existingProduct.SKU = product.SKU;
                existingProduct.Price = product.Price;
                existingProduct.SalePrice = product.SalePrice;
                existingProduct.StockQuantity = product.StockQuantity;
                existingProduct.Status = product.Status;
                existingProduct.Size = product.Size;
                existingProduct.Color = product.Color;
                existingProduct.Material = product.Material;
                existingProduct.Weight = product.Weight;
                existingProduct.IsFeatured = product.IsFeatured;
                existingProduct.IsActive = product.IsActive;
                existingProduct.CategoryId = product.CategoryId;
                existingProduct.BrandId = product.BrandId;
                existingProduct.UpdatedAt = DateTime.UtcNow;

                // Handle image upload with Cloudinary
                if (imageFile != null && imageFile.Length > 0)
                {
                    try
                    {
                        // Delete old image from Cloudinary if exists
                        if (!string.IsNullOrEmpty(existingProduct.FeaturedImageUrl) && 
                            existingProduct.FeaturedImageUrl.Contains("cloudinary.com"))
                        {
                            // Extract PublicId from URL (format: https://res.cloudinary.com/.../products/filename_abc123)
                            var uri = new Uri(existingProduct.FeaturedImageUrl);
                            var segments = uri.Segments;
                            if (segments.Length >= 2)
                            {
                                var publicId = string.Join("", segments.Skip(segments.Length - 2)).Replace("/", "");
                                publicId = publicId.Substring(0, publicId.LastIndexOf('.')); // Remove extension
                                
                                try
                                {
                                    await _cloudinaryService.DeleteImageAsync($"products/{publicId}");
                                    _logger.LogInformation("Old image deleted from Cloudinary: {PublicId}", publicId);
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogWarning(ex, "Could not delete old image from Cloudinary");
                                }
                            }
                        }

                        // Upload new image to Cloudinary
                        var uploadResult = await _cloudinaryService.UploadImageAsync(imageFile, "products");
                        
                        if (uploadResult != null && !string.IsNullOrEmpty(uploadResult.SecureUrl?.ToString()))
                        {
                            existingProduct.FeaturedImageUrl = uploadResult.SecureUrl.ToString();
                            existingProduct.ImageUrl = uploadResult.SecureUrl.ToString();
                            _logger.LogInformation("New image uploaded to Cloudinary: {Url}", existingProduct.FeaturedImageUrl);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error uploading image to Cloudinary");
                        ModelState.AddModelError("imageFile", "Không thể upload ảnh. Vui lòng thử lại!");
                        throw;
                    }

                    /* LOCAL FILE UPLOAD - Commented out for production (using Cloudinary)
                    // Uncomment this block to use local file storage instead of Cloudinary
                    
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                    var extension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
                    
                    if (!allowedExtensions.Contains(extension) || imageFile.Length > 5 * 1024 * 1024)
                    {
                        ModelState.AddModelError("imageFile", "File ảnh không hợp lệ (chỉ .jpg, .png, .gif và < 5MB).");
                        throw new InvalidOperationException("Invalid image file.");
                    }

                    // Delete old image
                    if (!string.IsNullOrEmpty(existingProduct.FeaturedImageUrl))
                    {
                        var oldImagePath = existingProduct.FeaturedImageUrl.Replace("~/", "").Replace("/", Path.DirectorySeparatorChar.ToString());
                        var oldImageFullPath = Path.Combine(_webHostEnvironment.WebRootPath, oldImagePath);
                        if (System.IO.File.Exists(oldImageFullPath))
                        {
                            try { System.IO.File.Delete(oldImageFullPath); }
                            catch (Exception ex) { _logger.LogWarning(ex, "Could not delete old image: {Path}", oldImageFullPath); }
                        }
                    }

                    var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "products");
                    Directory.CreateDirectory(uploadsFolder);
                    var uniqueFileName = $"{Guid.NewGuid()}{extension}";
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(fileStream);
                    }
                    existingProduct.FeaturedImageUrl = $"~/images/products/{uniqueFileName}";
                    */
                }

                // Update slug if name changed
                if (product.Name != existingProduct.Name)
                {
                    existingProduct.Slug = await GenerateUniqueSlugForEdit(product.Name, id);
                }
                
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["Success"] = "Cập nhật sản phẩm thành công!";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                await transaction.RollbackAsync();
                if (!ProductExists(product.Id))
                {
                    return NotFound();
                }
                else
                {
                    _logger.LogError("Concurrency error updating product {Id}", id);
                    ModelState.AddModelError("", "Sản phẩm đã được cập nhật bởi người dùng khác. Vui lòng tải lại trang!");
                }
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error updating product {Id}: {Message}", id, ex.Message);
                ModelState.AddModelError("", $"Có lỗi xảy ra khi cập nhật sản phẩm: {ex.Message}");
            }
        }

        // If we got here, something failed, redisplay form
        ViewBag.Categories = new SelectList(await _context.Categories.ToListAsync(), "Id", "Name", product.CategoryId);
        ViewBag.Brands = new SelectList(await _context.Brands.ToListAsync(), "Id", "Name", product.BrandId);
        return View(product);
    }

    // Helper methods
    private bool ProductExists(Guid id)
    {
        return _context.Products.Any(e => e.Id == id);
    }

    private string GenerateSlug(string name)
    {
        var slug = name.ToLower();
        slug = System.Text.RegularExpressions.Regex.Replace(slug, @"[^a-z0-9\s-]", "");
        slug = System.Text.RegularExpressions.Regex.Replace(slug, @"\s+", "-");
        slug = System.Text.RegularExpressions.Regex.Replace(slug, @"-+", "-");
        return slug.Trim('-');
    }

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

    private async Task<string> GenerateUniqueSlugForEdit(string name, Guid currentProductId)
    {
        var baseSlug = GenerateSlug(name);
        var slug = baseSlug;
        var counter = 1;
        
        while (await _context.Products.AnyAsync(p => p.Slug == slug && p.Id != currentProductId))
        {
            slug = $"{baseSlug}-{counter}";
            counter++;
        }
        
        return slug;
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

}
