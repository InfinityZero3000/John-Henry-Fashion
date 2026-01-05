using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using JohnHenryFashionWeb.Data;
using JohnHenryFashionWeb.Models;
using JohnHenryFashionWeb.Services;
using System.Security.Claims;

namespace JohnHenryFashionWeb.Controllers;

[Authorize(Roles = "Seller")]
[Route("seller/products")]
public class SellerProductsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _webHostEnvironment;
    private readonly ICloudinaryService _cloudinaryService;
    private readonly ILogger<SellerProductsController> _logger;

    public SellerProductsController(
        ApplicationDbContext context,
        IWebHostEnvironment webHostEnvironment,
        ICloudinaryService cloudinaryService,
        ILogger<SellerProductsController> logger)
    {
        _context = context;
        _webHostEnvironment = webHostEnvironment;
        _cloudinaryService = cloudinaryService;
        _logger = logger;
    }

    // GET: seller/products
    [HttpGet("")]
    public async Task<IActionResult> Index(string? search, Guid? categoryId, string? status, int page = 1, int pageSize = 20)
    {
        var currentUserId = User?.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(currentUserId))
        {
            return RedirectToAction("Login", "Account");
        }
        
        var query = _context.Products
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .AsQueryable();

        // Sellers are allowed to manage all products — do not filter by SellerId here.
        // (Previously we filtered to avoid exposing admin-created products; sellers
        // now have permission to view and manage all products.)

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

        // Filter by status
        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(p => p.Status == status);
        }

        var totalItems = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

        var products = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        // Map to ViewModel
        var viewModel = new JohnHenryFashionWeb.ViewModels.ProductListViewModel
        {
            Products = products.Select(p => new JohnHenryFashionWeb.ViewModels.ProductListItemViewModel
            {
                Id = p.Id,
                Name = p.Name,
                SKU = p.SKU,
                Price = p.Price,
                SalePrice = p.SalePrice,
                StockQuantity = p.StockQuantity,
                Status = p.Status,
                CategoryName = p.Category?.Name ?? "N/A",
                BrandName = p.Brand?.Name,
                FeaturedImageUrl = p.FeaturedImageUrl,
                CreatedAt = p.CreatedAt,
                IsFeatured = p.IsFeatured,
                IsActive = p.IsActive,
                Description = p.Description,
                Category = p.Category
            }).ToList(),
            CurrentPage = page,
            TotalPages = totalPages,
            PageSize = pageSize,
            SearchTerm = search ?? string.Empty,
            CategoryId = categoryId,
            Status = status ?? string.Empty,
            Categories = await _context.Categories.ToListAsync(),
            TotalProducts = totalItems
        };

        return View("~/Views/Seller/Products.cshtml", viewModel);
    }

    // GET: seller/products/create
    [HttpGet("create")]
    public async Task<IActionResult> Create()
    {
        // Provide raw lists to the view so it can enumerate them directly
        ViewBag.Categories = await _context.Categories.ToListAsync();
        ViewBag.Brands = await _context.Brands.ToListAsync();
        return View();
    }

    // POST: seller/products/create
    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([FromForm] Product product, IFormFile? imageFile)
    {
        // Log incoming POST for debugging (helps determine whether the request reaches the server)
        try
        {
            _logger.LogInformation("Incoming POST /seller/products/create invoked by {User}", User?.Identity?.Name ?? "(unauthenticated)");
        }
        catch { }

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
            var currentUserId = User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(currentUserId))
            {
                return RedirectToAction("Login", "Account");
            }

            // Handle image upload with Cloudinary
            if (imageFile != null && imageFile.Length > 0)
            {
                try
                {
                    var uploadResult = await _cloudinaryService.UploadImageAsync(imageFile, "products");
                    
                    if (uploadResult != null && !string.IsNullOrEmpty(uploadResult.SecureUrl?.ToString()))
                    {
                        product.FeaturedImageUrl = uploadResult.SecureUrl.ToString();
                        product.ImageUrl = uploadResult.SecureUrl.ToString();
                        _logger.LogInformation("Seller uploaded product image to Cloudinary: {Url}", product.FeaturedImageUrl);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error uploading image to Cloudinary for seller product");
                    ModelState.AddModelError("imageFile", "Không thể upload ảnh. Vui lòng thử lại!");
                    ViewBag.Categories = await _context.Categories.ToListAsync();
                    ViewBag.Brands = await _context.Brands.ToListAsync();
                    return View(product);
                }
            }

            // Ensure product has an Id (DB may generate, but set client-side to be safe)
            if (product.Id == Guid.Empty)
            {
                product.Id = Guid.NewGuid();
            }

            // Generate unique slug if not provided
            if (string.IsNullOrWhiteSpace(product.Slug))
            {
                var seedName = !string.IsNullOrWhiteSpace(product.Name) ? product.Name : (string.IsNullOrWhiteSpace(product.SKU) ? "product" : product.SKU);
                product.Slug = await GenerateUniqueSlug(seedName);
            }

            product.CreatedAt = DateTime.UtcNow;
            product.UpdatedAt = DateTime.UtcNow;
            product.IsActive = true;
            product.Status = "active";
            product.SellerId = currentUserId;

            // ADMIN APPROVAL FLOW (temporarily disabled)
            // If you want sellers to submit product creation requests for admin approval,
            // replace the direct save below with a request submission flow. The code
            // snippet is provided as a reference and is intentionally commented out
            // so the current behavior (direct creation) remains active.
            /*
            // Example: create a ProductCreationRequest entity and notify admins
            var creationRequest = new ProductCreationRequest
            {
                Id = Guid.NewGuid(),
                SellerId = currentUserId,
                ProductJson = JsonSerializer.Serialize(product), // store payload for review
                CreatedAt = DateTime.UtcNow,
                Status = "pending"
            };

            _context.Set<ProductCreationRequest>().Add(creationRequest);
            await _context.SaveChangesAsync();

            // Notify admins (pseudo-code)
            // await _notificationService.NotifyAdminsAsync($"New product creation request from seller {currentUserId}");

            TempData["Success"] = "Yêu cầu tạo sản phẩm đã gửi đến quản trị viên!";
            return RedirectToAction(nameof(Index));
            */

            // Default: create product directly for now
            _context.Add(product);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Tạo sản phẩm thành công!";
            return RedirectToAction(nameof(Index));
        }

        // Log ModelState errors for debugging
        if (!ModelState.IsValid)
        {
            foreach (var error in ModelState)
            {
                if (error.Value.Errors.Any())
                {
                    _logger.LogWarning("ModelState error for {Key}: {Errors}", error.Key, string.Join(", ", error.Value.Errors.Select(e => e.ErrorMessage)));
                }
            }
        }

        ViewBag.Categories = await _context.Categories.ToListAsync();
        ViewBag.Brands = await _context.Brands.ToListAsync();
        return View(product);
    }

    // GET: seller/products/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> Edit(Guid id)
    {
            var currentUserId = User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(currentUserId))
        {
            return RedirectToAction("Login", "Account");
        }
        
        var product = await _context.Products
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (product == null)
        {
            return NotFound();
        }

        // Sellers are permitted to edit any product; no owner check enforced here.

        ViewBag.Categories = await _context.Categories.ToListAsync();
        ViewBag.Brands = await _context.Brands.ToListAsync();
        return View(product);
    }

    // POST: seller/products/{id}
    [HttpPost("{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, Product product, IFormFile? imageFile)
    {
        if (id != product.Id)
        {
            return NotFound();
        }

        var currentUserId = User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(currentUserId))
        {
            return RedirectToAction("Login", "Account");
        }

        // Server-side validations (mirror create checks)
        if (product.SalePrice.HasValue && product.SalePrice >= product.Price)
        {
            ModelState.AddModelError("SalePrice", "Giá khuyến mãi phải nhỏ hơn giá gốc!");
        }

        if (await _context.Products.AnyAsync(p => p.SKU == product.SKU && p.Id != product.Id))
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
            try
            {
                var existingProduct = await _context.Products.FindAsync(id);
                if (existingProduct == null)
                {
                    return NotFound();
                }

                // Sellers are permitted to edit any product; no owner check enforced here.

                // Update only the fields that can be edited from the form
                existingProduct.Name = product.Name;
                existingProduct.SKU = product.SKU;
                existingProduct.Slug = product.Slug;
                existingProduct.Price = product.Price;
                existingProduct.SalePrice = product.SalePrice;
                existingProduct.StockQuantity = product.StockQuantity;
                existingProduct.CategoryId = product.CategoryId;
                existingProduct.BrandId = product.BrandId;
                existingProduct.Material = product.Material;
                existingProduct.Size = product.Size;
                existingProduct.Color = product.Color;
                existingProduct.IsActive = product.IsActive;
                existingProduct.IsFeatured = product.IsFeatured;
                existingProduct.Description = product.Description;

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

                    existingProduct.FeaturedImageUrl = $"~/images/products/{uniqueFileName}";
                }
                // If no new image, keep existing image

                // Update slug if name changed
                if (product.Name != existingProduct.Name)
                {
                    existingProduct.Slug = GenerateSlug(product.Name);
                }
                // If name didn't change, keep the slug as edited in the form

                existingProduct.UpdatedAt = DateTime.UtcNow;

                // Preserve SellerId, CreatedAt, Description, and other fields not editable from this form
                // existingProduct.SellerId = existingProduct.SellerId; // already set
                // existingProduct.CreatedAt = existingProduct.CreatedAt; // already set

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

        // Log ModelState errors for debugging (mirror Create behavior)
        if (!ModelState.IsValid)
        {
            foreach (var error in ModelState)
            {
                if (error.Value.Errors.Any())
                {
                    _logger.LogWarning("ModelState error for {Key}: {Errors}", error.Key, string.Join(", ", error.Value.Errors.Select(e => e.ErrorMessage)));
                }
            }
        }

        ViewBag.Categories = await _context.Categories.ToListAsync();
        ViewBag.Brands = await _context.Brands.ToListAsync();
        return View(product);
    }

    // POST: seller/products/{id}/delete
    [HttpPost("{id}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        var currentUserId = User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(currentUserId))
        {
            return RedirectToAction("Login", "Account");
        }
        
        var product = await _context.Products.FindAsync(id);
        if (product == null)
        {
            return NotFound();
        }

        // Sellers are permitted to delete any product; no owner check enforced here.

        // Check if the product is in any shopping carts or past orders
        var isInCart = await _context.ShoppingCartItems.AnyAsync(i => i.ProductId == id);
        var isInOrder = await _context.OrderItems.AnyAsync(i => i.ProductId == id);

        if (isInCart || isInOrder)
        {
            var locations = new List<string>();
            if (isInCart) locations.Add("giỏ hàng của người dùng");
            if (isInOrder) locations.Add("đơn hàng đã đặt");
            
            TempData["Warning"] = $"Không thể xóa sản phẩm này vì nó đang tồn tại trong {string.Join(" và ", locations)}. Vui lòng xem xét ẩn sản phẩm thay vì xóa.";
            return RedirectToAction(nameof(Index));
        }

        // Delete image file if exists
        if (!string.IsNullOrEmpty(product.FeaturedImageUrl))
        {
            var imagePath = product.FeaturedImageUrl.Replace("~/", "");
            var imageFullPath = Path.Combine(_webHostEnvironment.WebRootPath, imagePath);
            if (System.IO.File.Exists(imageFullPath))
            {
                try
                {
                    System.IO.File.Delete(imageFullPath);
                }
                catch(Exception ex)
                {
                    _logger.LogWarning(ex, "Could not delete image file {Path} for product {ProductId}", imageFullPath, id);
                }
            }
        }

        _context.Products.Remove(product);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Xóa sản phẩm thành công!";
        return RedirectToAction(nameof(Index));
    }

    // POST: Update product status (Active/Inactive)
    [HttpPost]
    [Route("update-status")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateProductStatus(Guid productId, string status)
    {
        var currentUserId = User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(currentUserId))
        {
            return RedirectToAction("Login", "Account");
        }

        var product = await _context.Products.FindAsync(productId);
        
        if (product == null)
        {
            TempData["Error"] = "Không tìm thấy sản phẩm!";
            return RedirectToAction(nameof(Index));
        }

        product.Status = status;
        product.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        TempData["Success"] = $"Đã cập nhật trạng thái sản phẩm thành {(status == "Active" ? "Đang bán" : "Tạm ẩn")}!";
        return RedirectToAction(nameof(Index));
    }

    // POST: Toggle featured status
    [HttpPost]
    [Route("update-featured")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateProductFeatured(Guid productId, bool isFeatured)
    {
        var currentUserId = User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(currentUserId))
        {
            return RedirectToAction("Login", "Account");
        }

        var product = await _context.Products.FindAsync(productId);
        
        if (product == null)
        {
            TempData["Error"] = "Không tìm thấy sản phẩm!";
            return RedirectToAction(nameof(Index));
        }

        product.IsFeatured = isFeatured;
        product.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        TempData["Success"] = isFeatured ? "Đã đặt sản phẩm làm nổi bật!" : "Đã bỏ nổi bật sản phẩm!";
        return RedirectToAction(nameof(Index));
    }

    // POST: Delete product
    [HttpPost]
    [Route("delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteProduct(Guid productId)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(currentUserId))
        {
            return RedirectToAction("Login", "Account");
        }

        var product = await _context.Products.FindAsync(productId);
        
        if (product == null)
        {
            TempData["Error"] = "Không tìm thấy sản phẩm!";
            return RedirectToAction(nameof(Index));
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

        TempData["Success"] = "Đã xóa sản phẩm thành công!";
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
