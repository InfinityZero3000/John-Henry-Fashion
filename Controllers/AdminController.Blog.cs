using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using JohnHenryFashionWeb.Models;
using JohnHenryFashionWeb.ViewModels;
using Markdig;

namespace JohnHenryFashionWeb.Controllers
{
    public partial class AdminController
    {
        #region Blog Management

        [HttpGet("blog")]
        public async Task<IActionResult> Blog(int page = 1, string? status = null, string? category = null, string? search = null)
        {
            const int pageSize = 20;
            
            var query = _context.BlogPosts
                .Include(b => b.Category)
                .Include(b => b.Author)
                .AsQueryable();

            // Filter by status
            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(b => b.Status == status);
            }

            // Filter by category
            if (!string.IsNullOrEmpty(category) && Guid.TryParse(category, out var categoryId))
            {
                query = query.Where(b => b.CategoryId == categoryId);
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
                .OrderByDescending(b => b.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var categories = await _context.BlogCategories
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .ToListAsync();

            var viewModel = new AdminBlogViewModel
            {
                Posts = posts,
                Categories = categories,
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling((double)totalPosts / pageSize),
                TotalPosts = totalPosts,
                StatusFilter = status,
                CategoryFilter = category,
                SearchTerm = search
            };

            return View(viewModel);
        }

        [HttpGet("blog/preview/{id}")]
        public async Task<IActionResult> PreviewBlogPost(Guid id)
        {
            var post = await _context.BlogPosts
                .Include(p => p.Category)
                .Include(p => p.Author)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (post == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy bài viết";
                return RedirectToAction("Blog");
            }

            // Check if user is authorized to preview (must be admin or author)
            var userId = _userManager.GetUserId(User);
            if (post.AuthorId != userId && !User.IsInRole(UserRoles.Admin))
            {
                TempData["ErrorMessage"] = "Bạn không có quyền xem bài viết này";
                return RedirectToAction("Blog");
            }

            // Convert Markdown to HTML for preview
            var pipeline = new MarkdownPipelineBuilder()
                .UseAdvancedExtensions()
                .Build();
            post.Content = Markdown.ToHtml(post.Content ?? "", pipeline);

            ViewBag.IsPreview = true;
            ViewBag.PreviewMessage = post.Status == "draft" ? "Đây là bản nháp - chưa được công khai" : "Đang xem trước bài viết";
            
            return View("~/Views/Blog/Details.cshtml", post);
        }

        [HttpGet("blog/create")]
        public async Task<IActionResult> CreateBlogPost()
        {
            var categories = await _context.BlogCategories
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .ToListAsync();

            ViewBag.Categories = new SelectList(categories, "Id", "Name");
            return View(new BlogPost());
        }

        [HttpPost("blog/create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateBlogPost(BlogPost model, IFormFile? featuredImage, string? action, string? TagsString)
        {
            try
            {
                _logger.LogInformation("=== START CREATE BLOG POST ===");
                _logger.LogInformation($"Action: {action}");
                _logger.LogInformation($"Title: {model.Title}");
                _logger.LogInformation($"Status from model: {model.Status}");
                _logger.LogInformation($"CategoryId: {model.CategoryId}");
                _logger.LogInformation($"Content Length: {model.Content?.Length ?? 0}");
                _logger.LogInformation($"Featured Image: {featuredImage?.FileName ?? "None"}");
                _logger.LogInformation($"Tags String: {TagsString}");
                
                // Remove navigation property validation errors
                ModelState.Remove("Author");
                ModelState.Remove("Category");
                
                // Validation
                if (string.IsNullOrWhiteSpace(model.Title))
                {
                    ModelState.AddModelError("Title", "Tiêu đề không được để trống");
                }

                if (string.IsNullOrWhiteSpace(model.Content))
                {
                    ModelState.AddModelError("Content", "Nội dung không được để trống");
                }

                if (ModelState.IsValid)
                {
                    var userId = _userManager.GetUserId(User);
                    _logger.LogInformation($"Current User ID: {userId}");
                    
                    if (string.IsNullOrEmpty(userId))
                    {
                        throw new InvalidOperationException("Không thể xác định người dùng hiện tại");
                    }

                    model.Id = Guid.NewGuid();
                    model.AuthorId = userId;
                    model.CreatedAt = DateTime.UtcNow;
                    model.UpdatedAt = DateTime.UtcNow;
                    model.ViewCount = 0;
                    
                    // Parse tags from string
                    if (!string.IsNullOrEmpty(TagsString))
                    {
                        model.Tags = TagsString.Split(',')
                                              .Select(t => t.Trim())
                                              .Where(t => !string.IsNullOrEmpty(t))
                                              .ToArray();
                        _logger.LogInformation($"Parsed {model.Tags.Length} tags");
                    }
                    
                    // Set status based on action button clicked
                    if (!string.IsNullOrEmpty(action))
                    {
                        if (action == "publish")
                        {
                            model.Status = "published";
                            _logger.LogInformation("Status set to 'published' from action button");
                        }
                        else if (action == "draft")
                        {
                            model.Status = "draft";
                            _logger.LogInformation("Status set to 'draft' from action button");
                        }
                    }
                    
                    // Fallback: Set default status if not provided
                    if (string.IsNullOrEmpty(model.Status))
                    {
                        model.Status = "draft";
                        _logger.LogInformation("Status defaulted to 'draft'");
                    }
                    
                    _logger.LogInformation($"Final Status: {model.Status}");
                    _logger.LogInformation($"Generated Post ID: {model.Id}");
                    
                    // Generate unique slug
                    if (string.IsNullOrEmpty(model.Slug))
                    {
                        model.Slug = await GenerateUniqueSlugAsync(model.Title);
                    }
                    else
                    {
                        model.Slug = await GenerateUniqueSlugAsync(model.Slug);
                    }
                    
                    _logger.LogInformation($"Generated Slug: {model.Slug}");

                    // Handle featured image upload
                    if (featuredImage != null && featuredImage.Length > 0)
                    {
                        try
                        {
                            var fileName = await SaveBlogImage(featuredImage);
                            model.FeaturedImageUrl = $"/images/blog/{fileName}";
                            _logger.LogInformation($"Featured image saved: {model.FeaturedImageUrl}");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"Error saving image: {ex.Message}");
                            ModelState.AddModelError("", $"Lỗi khi lưu ảnh: {ex.Message}");
                            throw;
                        }
                    }

                    // Auto-generate excerpt if not provided
                    if (string.IsNullOrEmpty(model.Excerpt) && !string.IsNullOrEmpty(model.Content))
                    {
                        var plainText = System.Text.RegularExpressions.Regex.Replace(model.Content, "<.*?>", string.Empty);
                        model.Excerpt = plainText.Length > 200 ? plainText.Substring(0, 200) + "..." : plainText;
                    }

                    // Set SEO fields if not provided
                    if (string.IsNullOrEmpty(model.MetaTitle))
                    {
                        model.MetaTitle = model.Title;
                    }

                    if (string.IsNullOrEmpty(model.MetaDescription))
                    {
                        model.MetaDescription = model.Excerpt;
                    }

                    // Set publish date if publishing
                    if (model.Status == "published" && !model.PublishedAt.HasValue)
                    {
                        model.PublishedAt = DateTime.UtcNow;
                        _logger.LogInformation($"Set PublishedAt to: {model.PublishedAt}");
                    }

                    _logger.LogInformation("Adding post to database...");
                    _context.BlogPosts.Add(model);
                    
                    var saved = await _context.SaveChangesAsync();
                    _logger.LogInformation($"SaveChanges result: {saved} rows affected");

                    if (saved > 0)
                    {
                        _logger.LogInformation("=== BLOG POST CREATED SUCCESSFULLY ===");
                        _logger.LogInformation($"Blog slug: {model.Slug}");
                        
                        // Different redirect based on status
                        if (model.Status == "published")
                        {
                            TempData["SuccessMessage"] = $"Bài viết '{model.Title}' đã được xuất bản thành công!";
                            // Redirect to public blog details page using slug
                            return RedirectToAction("Details", "Blog", new { slug = model.Slug });
                        }
                        else
                        {
                            TempData["SuccessMessage"] = $"Bản nháp '{model.Title}' đã được lưu thành công!";
                            // Redirect to preview page for draft
                            return RedirectToAction("PreviewBlogPost", new { id = model.Id });
                        }
                    }
                    else
                    {
                        _logger.LogWarning("SaveChanges returned 0 rows affected");
                        ModelState.AddModelError("", "Không thể lưu bài viết. Vui lòng thử lại.");
                    }
                }
                else
                {
                    _logger.LogWarning("ModelState is invalid:");
                    foreach (var key in ModelState.Keys)
                    {
                        var errors = ModelState[key]?.Errors;
                        if (errors != null && errors.Count > 0)
                        {
                            foreach (var error in errors)
                            {
                                _logger.LogWarning($"  {key}: {error.ErrorMessage}");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"=== ERROR CREATING BLOG POST ===");
                _logger.LogError($"Exception Type: {ex.GetType().Name}");
                _logger.LogError($"Message: {ex.Message}");
                _logger.LogError($"StackTrace: {ex.StackTrace}");
                
                if (ex.InnerException != null)
                {
                    _logger.LogError($"Inner Exception: {ex.InnerException.Message}");
                }
                
                ModelState.AddModelError("", $"Có lỗi xảy ra: {ex.Message}");
            }

            // Reload categories for form
            var categories = await _context.BlogCategories
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .ToListAsync();

            ViewBag.Categories = new SelectList(categories, "Id", "Name");
            
            _logger.LogInformation("Returning to create view with errors");
            return View(model);
        }

        [HttpGet("blog/edit/{id}")]
        public async Task<IActionResult> EditBlogPost(Guid id)
        {
            var post = await _context.BlogPosts
                .Include(b => b.Category)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (post == null)
            {
                return NotFound();
            }

            var categories = await _context.BlogCategories
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .ToListAsync();

            ViewBag.Categories = new SelectList(categories, "Id", "Name");
            return View(post);
        }

        [HttpPost("blog/edit/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditBlogPost(Guid id, BlogPost model, IFormFile? featuredImage)
        {
            if (id != model.Id)
            {
                return BadRequest();
            }

            // Remove navigation properties from validation
            ModelState.Remove("Author");
            ModelState.Remove("Category");

            // Log validation errors if any
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("=== EDIT BLOG POST VALIDATION FAILED ===");
                foreach (var key in ModelState.Keys)
                {
                    var state = ModelState[key];
                    if (state != null && state.Errors.Count > 0)
                    {
                        foreach (var error in state.Errors)
                        {
                            _logger.LogWarning($"{key}: {error.ErrorMessage}");
                        }
                    }
                }
            }

            try
            {
                if (ModelState.IsValid)
                {
                    var existingPost = await _context.BlogPosts.FindAsync(id);
                    if (existingPost == null)
                    {
                        return NotFound();
                    }

                    // Update fields
                    existingPost.Title = model.Title;
                    
                    // Generate unique slug if changed
                    if (string.IsNullOrEmpty(model.Slug))
                    {
                        existingPost.Slug = await GenerateUniqueSlugAsync(model.Title, existingPost.Id);
                    }
                    else if (model.Slug != existingPost.Slug)
                    {
                        existingPost.Slug = await GenerateUniqueSlugAsync(model.Slug, existingPost.Id);
                    }
                    
                    existingPost.Excerpt = model.Excerpt;
                    existingPost.Content = model.Content;
                    existingPost.CategoryId = model.CategoryId;
                    existingPost.Tags = model.Tags;
                    existingPost.MetaTitle = model.MetaTitle;
                    existingPost.MetaDescription = model.MetaDescription;
                    existingPost.IsFeatured = model.IsFeatured;
                    existingPost.Status = model.Status;
                    existingPost.UpdatedAt = DateTime.UtcNow;

                    // Handle featured image upload
                    if (featuredImage != null && featuredImage.Length > 0)
                    {
                        var fileName = await SaveBlogImage(featuredImage);
                        existingPost.FeaturedImageUrl = $"/images/blog/{fileName}";
                    }

                    // Set publish date if publishing for first time
                    if (model.Status == "published" && !existingPost.PublishedAt.HasValue)
                    {
                        existingPost.PublishedAt = DateTime.UtcNow;
                    }

                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Bài viết đã được cập nhật thành công!";
                    return RedirectToAction("Blog");
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Có lỗi xảy ra: {ex.Message}");
            }

            var categories = await _context.BlogCategories
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .ToListAsync();

            ViewBag.Categories = new SelectList(categories, "Id", "Name");
            return View(model);
        }

        [HttpPost("blog/delete/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteBlogPost(Guid id)
        {
            var post = await _context.BlogPosts.FindAsync(id);
            if (post == null)
            {
                return Json(new { success = false, message = "Không tìm thấy bài viết" });
            }

            try
            {
                // Delete featured image if exists
                if (!string.IsNullOrEmpty(post.FeaturedImageUrl))
                {
                    var imagePath = Path.Combine(_webHostEnvironment.WebRootPath, post.FeaturedImageUrl.TrimStart('/'));
                    if (System.IO.File.Exists(imagePath))
                    {
                        System.IO.File.Delete(imagePath);
                    }
                }

                _context.BlogPosts.Remove(post);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Xóa bài viết thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Có lỗi xảy ra: {ex.Message}" });
            }
        }

        [HttpPost("blog/publish/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PublishBlogPost(Guid id)
        {
            var post = await _context.BlogPosts.FindAsync(id);
            if (post == null)
            {
                return Json(new { success = false, message = "Không tìm thấy bài viết" });
            }

            try
            {
                post.Status = "published";
                
                if (!post.PublishedAt.HasValue)
                {
                    post.PublishedAt = DateTime.UtcNow;
                }

                post.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return Json(new { 
                    success = true, 
                    message = "Bài viết đã được xuất bản thành công!",
                    newStatus = "published"
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Có lỗi xảy ra: {ex.Message}" });
            }
        }

        [HttpPost("blog/toggle-status/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleBlogStatus(Guid id)
        {
            var post = await _context.BlogPosts.FindAsync(id);
            if (post == null)
            {
                return Json(new { success = false, message = "Không tìm thấy bài viết" });
            }

            try
            {
                post.Status = post.Status == "published" ? "draft" : "published";
                
                if (post.Status == "published" && !post.PublishedAt.HasValue)
                {
                    post.PublishedAt = DateTime.UtcNow;
                }

                post.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return Json(new { 
                    success = true, 
                    message = $"Trạng thái bài viết đã được thay đổi thành '{(post.Status == "published" ? "Đã xuất bản" : "Bản nháp")}'!",
                    newStatus = post.Status
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Có lỗi xảy ra: {ex.Message}" });
            }
        }

        #region Blog Categories

        [HttpGet("blog/categories")]
        public async Task<IActionResult> BlogCategories()
        {
            var categories = await _context.BlogCategories
                .OrderBy(c => c.SortOrder)
                .ThenBy(c => c.Name)
                .ToListAsync();

            return View(categories);
        }

        [HttpGet("blog/categories/create")]
        public IActionResult CreateBlogCategory()
        {
            return View(new BlogCategory());
        }

        [HttpPost("blog/categories/create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateBlogCategory(BlogCategory model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    model.Id = Guid.NewGuid();
                    model.CreatedAt = DateTime.UtcNow;
                    model.UpdatedAt = DateTime.UtcNow;
                    
                    // Generate unique slug
                    if (string.IsNullOrEmpty(model.Slug))
                    {
                        model.Slug = await GenerateUniqueCategorySlugAsync(model.Name);
                    }
                    else
                    {
                        model.Slug = await GenerateUniqueCategorySlugAsync(model.Slug);
                    }

                    _context.BlogCategories.Add(model);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Danh mục blog đã được tạo thành công!";
                    return RedirectToAction("BlogCategories");
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Có lỗi xảy ra: {ex.Message}");
            }

            return View(model);
        }

        [HttpGet("blog/categories/edit/{id}")]
        public async Task<IActionResult> EditBlogCategory(Guid id)
        {
            var category = await _context.BlogCategories.FindAsync(id);
            if (category == null)
            {
                return NotFound();
            }

            return View(category);
        }

        [HttpPost("blog/categories/edit/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditBlogCategory(Guid id, BlogCategory model)
        {
            if (id != model.Id)
            {
                return BadRequest();
            }

            try
            {
                if (ModelState.IsValid)
                {
                    var existingCategory = await _context.BlogCategories.FindAsync(id);
                    if (existingCategory == null)
                    {
                        return NotFound();
                    }

                    existingCategory.Name = model.Name;
                    
                    // Generate unique slug if changed
                    if (string.IsNullOrEmpty(model.Slug))
                    {
                        existingCategory.Slug = await GenerateUniqueCategorySlugAsync(model.Name, existingCategory.Id);
                    }
                    else if (model.Slug != existingCategory.Slug)
                    {
                        existingCategory.Slug = await GenerateUniqueCategorySlugAsync(model.Slug, existingCategory.Id);
                    }
                    
                    existingCategory.Description = model.Description;
                    existingCategory.IsActive = model.IsActive;
                    existingCategory.SortOrder = model.SortOrder;
                    existingCategory.UpdatedAt = DateTime.UtcNow;

                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Danh mục blog đã được cập nhật thành công!";
                    return RedirectToAction("BlogCategories");
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Có lỗi xảy ra: {ex.Message}");
            }

            return View(model);
        }

        [HttpPost("blog/categories/delete/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteBlogCategory(Guid id)
        {
            var category = await _context.BlogCategories
                .Include(c => c.BlogPosts)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null)
            {
                return Json(new { success = false, message = "Không tìm thấy danh mục" });
            }

            if (category.BlogPosts?.Any() == true)
            {
                return Json(new { success = false, message = "Không thể xóa danh mục đang có bài viết" });
            }

            try
            {
                _context.BlogCategories.Remove(category);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Xóa danh mục thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Có lỗi xảy ra: {ex.Message}" });
            }
        }

        #endregion

        #region Helper Methods

        private async Task<string> SaveBlogImage(IFormFile image)
        {
            // Validate file
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
            var extension = Path.GetExtension(image.FileName).ToLowerInvariant();
            
            if (!allowedExtensions.Contains(extension))
            {
                throw new InvalidOperationException($"Chỉ chấp nhận file ảnh: {string.Join(", ", allowedExtensions)}");
            }

            // Validate file size (max 5MB)
            if (image.Length > 5 * 1024 * 1024)
            {
                throw new InvalidOperationException("Kích thước file không được vượt quá 5MB");
            }

            var uploadsPath = Path.Combine(_webHostEnvironment.WebRootPath, "images", "blog");
            if (!Directory.Exists(uploadsPath))
            {
                Directory.CreateDirectory(uploadsPath);
            }

            var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(image.FileName)}";
            var filePath = Path.Combine(uploadsPath, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await image.CopyToAsync(stream);
            }

            return fileName;
        }

        private async Task<string> GenerateUniqueSlugAsync(string title, Guid? existingPostId = null)
        {
            var baseSlug = GenerateSlug(title);
            var slug = baseSlug;
            var counter = 1;

            // Check if slug already exists (excluding current post if editing)
            while (await _context.BlogPosts.AnyAsync(p => p.Slug == slug && p.Id != existingPostId))
            {
                slug = $"{baseSlug}-{counter}";
                counter++;
            }

            return slug;
        }

        private async Task<string> GenerateUniqueCategorySlugAsync(string name, Guid? existingCategoryId = null)
        {
            var baseSlug = GenerateSlug(name);
            var slug = baseSlug;
            var counter = 1;

            // Check if slug already exists (excluding current category if editing)
            while (await _context.BlogCategories.AnyAsync(c => c.Slug == slug && c.Id != existingCategoryId))
            {
                slug = $"{baseSlug}-{counter}";
                counter++;
            }

            return slug;
        }

        [HttpPost("blog/upload-image")]
        public async Task<IActionResult> UploadBlogImage(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return Json(new { error = "Không có file được chọn" });
                }

                // Validate file type
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(extension))
                {
                    return Json(new { error = "Chỉ chấp nhận file ảnh (jpg, jpeg, png, gif, webp)" });
                }

                // Validate file size (max 5MB)
                if (file.Length > 5 * 1024 * 1024)
                {
                    return Json(new { error = "File ảnh không được vượt quá 5MB" });
                }

                // Generate unique filename
                var fileName = $"{Guid.NewGuid()}{extension}";
                var uploadPath = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "blog-images");
                
                // Create directory if not exists
                if (!Directory.Exists(uploadPath))
                {
                    Directory.CreateDirectory(uploadPath);
                }

                var filePath = Path.Combine(uploadPath, fileName);

                // Save file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Return URL for TinyMCE
                var fileUrl = $"/uploads/blog-images/{fileName}";
                return Json(new { location = fileUrl });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading blog image");
                return Json(new { error = "Có lỗi xảy ra khi upload ảnh" });
            }
        }

        #endregion

        #endregion
    }
}
