using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using JohnHenryFashionWeb.Data;
using JohnHenryFashionWeb.Models;

namespace JohnHenryFashionWeb.Scripts
{
    /// <summary>
    /// Script to import products from CSV file into database.
    /// Supports two CSV formats:
    ///   - 3-column:  sku,name,price
    ///   - 12-column: SKU,Name,Price,category,brand,StockQuantity,InStock,IsFeatured,IsActive,Rating,ReviewCount,CreatedAt
    /// Usage: Uncomment the call in Program.cs and run: dotnet run
    /// </summary>
    public class ImportProductsFromCsv
    {
        // Maps CSV category names to wwwroot/images/<folder> directory names
        private static readonly System.Collections.Generic.Dictionary<string, string> CategoryFolderMap =
            new System.Collections.Generic.Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "Áo nam",          "ao-nam" },
                { "Thời trang nam",  "ao-nam" },
                { "Áo nữ",           "ao-nu" },
                { "Thời trang nữ",   "ao-nu" },
                { "Quần nam",        "quan-nam" },
                { "Quần nữ",         "quan-nu" },
                { "Đầm nữ",          "dam-nu" },
                { "Chân váy nữ",     "chan-vay-nu" },
                { "Phụ kiện nam",    "phu-kien-nam" },
                { "Phụ kiện nữ",     "phu-kien-nu" },
            };

        public static async Task RunAsync(ApplicationDbContext context, string csvFilePath)
        {
            if (!File.Exists(csvFilePath))
            {
                Console.WriteLine($"File not found: {csvFilePath}");
                return;
            }

            Console.WriteLine($"📂 Reading CSV file: {csvFilePath}");
            Console.WriteLine("⏳ This may take a few minutes...\n");

            var lines = await File.ReadAllLinesAsync(csvFilePath);
            if (lines.Length == 0) { Console.WriteLine("⚠️  CSV file is empty."); return; }

            // Detect header format
            var headerParts = ParseCsvLine(lines[0]);
            bool richFormat = headerParts.Length >= 4 &&
                              headerParts[3].Trim().Equals("category", StringComparison.OrdinalIgnoreCase);

            // Skip header line
            var dataLines = lines.Skip(1).ToList();
            Console.WriteLine($"📊 Found {dataLines.Count} products in CSV (format: {(richFormat ? "rich 12-col" : "simple 3-col")})");

            int imported = 0;
            int updated = 0;
            int skipped = 0;

            foreach (var line in dataLines)
            {
                if (string.IsNullOrWhiteSpace(line)) { skipped++; continue; }

                try
                {
                    var parts = ParseCsvLine(line);
                    if (parts.Length < 3)
                    {
                        Console.WriteLine($"⚠️  Skipping invalid line: {line.Substring(0, Math.Min(50, line.Length))}...");
                        skipped++;
                        continue;
                    }

                    var sku      = parts[0].Trim();
                    var name     = parts[1].Trim();
                    var priceStr = parts[2].Trim();

                    // Extra fields from rich format
                    var categoryName  = richFormat && parts.Length > 3  ? parts[3].Trim()  : string.Empty;
                    var brandName     = richFormat && parts.Length > 4  ? parts[4].Trim()  : "John Henry";
                    var stockStr      = richFormat && parts.Length > 5  ? parts[5].Trim()  : "100";
                    var isFeaturedStr = richFormat && parts.Length > 7  ? parts[7].Trim()  : "f";

                    if (string.IsNullOrEmpty(sku) || string.IsNullOrEmpty(name))
                    {
                        skipped++;
                        continue;
                    }

                    if (!decimal.TryParse(priceStr, System.Globalization.NumberStyles.Any,
                                          System.Globalization.CultureInfo.InvariantCulture, out decimal price))
                    {
                        Console.WriteLine($"⚠️  Invalid price for {sku}: {priceStr}");
                        skipped++;
                        continue;
                    }

                    int stock      = int.TryParse(stockStr, out int s) ? s : 100;
                    bool isFeatured = isFeaturedStr.Equals("t", StringComparison.OrdinalIgnoreCase) ||
                                      isFeaturedStr.Equals("true", StringComparison.OrdinalIgnoreCase);

                    // Check if product already exists by SKU
                    var existingProduct = await context.Products
                        .FirstOrDefaultAsync(p => p.SKU == sku);

                    if (existingProduct != null)
                    {
                        // Update existing product
                        existingProduct.Name = name;
                        existingProduct.Price = price;
                        existingProduct.IsActive = true;
                        existingProduct.UpdatedAt = DateTime.UtcNow;
                        updated++;
                    }
                    else
                    {
                        // Determine image path using category (preferred) or product name fallback
                        var imagePath = GetImagePath(name, sku, categoryName);

                        // Get or create category record
                        var dbCategoryName = string.IsNullOrEmpty(categoryName) ? "Chưa phân loại" : categoryName;
                        var dbCategorySlug = GenerateSlug(dbCategoryName);

                        var dbCategory = await context.Categories
                            .FirstOrDefaultAsync(c => c.Name == dbCategoryName);

                        if (dbCategory == null)
                        {
                            dbCategory = new Category
                            {
                                Name = dbCategoryName,
                                Slug = dbCategorySlug,
                                Description = $"Danh mục {dbCategoryName}",
                                IsActive = true,
                                CreatedAt = DateTime.UtcNow,
                                UpdatedAt = DateTime.UtcNow
                            };
                            await context.Categories.AddAsync(dbCategory);
                            await context.SaveChangesAsync();
                        }

                        // Create new product
                        var product = new Product
                        {
                            SKU = sku,
                            Name = name,
                            Slug = GenerateSlug(name) + "-" + sku.ToLower(),
                            Price = price,
                            Description = $"Sản phẩm {name} - {brandName}",
                            StockQuantity = stock,
                            ManageStock = true,
                            InStock = stock > 0,
                            FeaturedImageUrl = imagePath,
                            IsActive = true,
                            IsFeatured = isFeatured,
                            Status = "active",
                            ViewCount = 0,
                            CategoryId = dbCategory.Id,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };

                        await context.Products.AddAsync(product);
                        imported++;
                    }

                    // Save every 100 products to avoid memory issues
                    if ((imported + updated) % 100 == 0)
                    {
                        await context.SaveChangesAsync();
                        Console.WriteLine($"💾 Progress: {imported} imported, {updated} updated, {skipped} skipped");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️  Error processing line: {ex.Message}");
                    skipped++;
                }
            }

            // Save remaining products
            await context.SaveChangesAsync();

            Console.WriteLine($"\nImport completed!");
            Console.WriteLine($"   ✓ Imported: {imported}");
            Console.WriteLine($"   ✓ Updated:  {updated}");
            Console.WriteLine($"   ✗ Skipped:  {skipped}");
            Console.WriteLine($"   ═ Total:    {dataLines.Count}");
            
            // Show sample of imported products
            var sampleProducts = await context.Products
                .Where(p => p.IsActive)
                .OrderByDescending(p => p.CreatedAt)
                .Take(5)
                .Select(p => new { p.SKU, p.Name, p.Price })
                .ToListAsync();
                
            Console.WriteLine($"\n📦 Sample of recently imported products:");
            foreach (var product in sampleProducts)
            {
                Console.WriteLine($"   - {product.SKU}: {product.Name} ({product.Price:N0} VNĐ)");
            }
        }

        /// <summary>
        /// Parse CSV line handling commas inside quotes
        /// </summary>
        private static string[] ParseCsvLine(string line)
        {
            var values = new System.Collections.Generic.List<string>();
            var currentValue = "";
            bool inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == '"')
                {
                    inQuotes = !inQuotes;
                }
                else if (c == ',' && !inQuotes)
                {
                    values.Add(currentValue.Trim('"'));
                    currentValue = "";
                }
                else
                {
                    currentValue += c;
                }
            }
            
            values.Add(currentValue.Trim('"'));
            return values.ToArray();
        }

        /// <summary>
        /// Determine image path using category (preferred) or product name keywords (fallback).
        /// Image filename = {sku}.jpg inside the wwwroot/images/{folder}/ directory.
        /// </summary>
        private static string GetImagePath(string name, string sku, string? category = null)
        {
            // 1. Try direct category → folder lookup
            if (!string.IsNullOrEmpty(category) && CategoryFolderMap.TryGetValue(category, out var folderFromCategory))
                return $"/images/{folderFromCategory}/{sku}.jpg";

            // 2. Fallback: guess from product name keywords
            bool isFemale = name.Contains("Nữ") || name.Contains("nữ") || name.Contains("Nu") ||
                            name.Contains("Lady") || name.Contains("lady");

            if (name.Contains("Quần") || name.Contains("quần") || name.Contains("Jean") || name.Contains("jean"))
                return isFemale ? $"/images/quan-nu/{sku}.jpg" : $"/images/quan-nam/{sku}.jpg";

            if (name.Contains("Đầm") || name.Contains("đầm"))
                return $"/images/dam-nu/{sku}.jpg";

            if (name.Contains("Chân váy") || name.Contains("chân váy"))
                return $"/images/chan-vay-nu/{sku}.jpg";

            if (name.Contains("Phụ kiện") || name.Contains("phụ kiện"))
                return isFemale ? $"/images/phu-kien-nu/{sku}.jpg" : $"/images/phu-kien-nam/{sku}.jpg";

            // Default: áo (shirt-type)
            return isFemale ? $"/images/ao-nu/{sku}.jpg" : $"/images/ao-nam/{sku}.jpg";
        }

        /// <summary>
        /// Generate URL-friendly slug from product name
        /// </summary>
        private static string GenerateSlug(string name)
        {
            // Convert to lowercase
            var slug = name.ToLower();
            
            // Replace Vietnamese characters
            slug = slug.Replace("á", "a").Replace("à", "a").Replace("ả", "a").Replace("ã", "a").Replace("ạ", "a")
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
            
            // Remove special characters, keep only alphanumeric and spaces
            slug = System.Text.RegularExpressions.Regex.Replace(slug, @"[^a-z0-9\s-]", "");
            
            // Replace multiple spaces/hyphens with single hyphen
            slug = System.Text.RegularExpressions.Regex.Replace(slug, @"[\s-]+", "-");
            
            // Trim hyphens from start and end
            slug = slug.Trim('-');
            
            return slug;
        }
    }
}
