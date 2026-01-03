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
    /// Script to import products from CSV file into database
    /// Usage: Uncomment the call in Program.cs and run: dotnet run
    /// </summary>
    public class ImportProductsFromCsv
    {
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
            
            // Skip header line
            var dataLines = lines.Skip(1).ToList();
            Console.WriteLine($"📊 Found {dataLines.Count} products in CSV");

            int imported = 0;
            int updated = 0;
            int skipped = 0;

            foreach (var line in dataLines)
            {
                try
                {
                    // Parse CSV line: sku,name,price
                    var parts = ParseCsvLine(line);
                    if (parts.Length < 3)
                    {
                        Console.WriteLine($"⚠️  Skipping invalid line: {line.Substring(0, Math.Min(50, line.Length))}...");
                        skipped++;
                        continue;
                    }

                    var sku = parts[0].Trim();
                    var name = parts[1].Trim();
                    var priceStr = parts[2].Trim();

                    if (string.IsNullOrEmpty(sku) || string.IsNullOrEmpty(name))
                    {
                        skipped++;
                        continue;
                    }

                    if (!decimal.TryParse(priceStr, out decimal price))
                    {
                        Console.WriteLine($"⚠️  Invalid price for {sku}: {priceStr}");
                        skipped++;
                        continue;
                    }

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
                        // Determine image path based on product name
                        var imagePath = GetImagePath(name, sku);
                        
                        // Get or create default category
                        var defaultCategory = await context.Categories
                            .FirstOrDefaultAsync(c => c.Name == "Chưa phân loại");
                            
                        if (defaultCategory == null)
                        {
                            defaultCategory = new Category
                            {
                                Name = "Chưa phân loại",
                                Slug = "chua-phan-loai",
                                Description = "Sản phẩm chưa được phân loại",
                                IsActive = true,
                                CreatedAt = DateTime.UtcNow,
                                UpdatedAt = DateTime.UtcNow
                            };
                            await context.Categories.AddAsync(defaultCategory);
                            await context.SaveChangesAsync();
                        }
                        
                        // Create new product
                        var product = new Product
                        {
                            SKU = sku,
                            Name = name,
                            Slug = GenerateSlug(name),
                            Price = price,
                            Description = $"Sản phẩm {name}",
                            StockQuantity = 100,
                            ManageStock = true,
                            InStock = true,
                            FeaturedImageUrl = imagePath,
                            IsActive = true,
                            IsFeatured = false,
                            Status = "active",
                            ViewCount = 0,
                            CategoryId = defaultCategory.Id,
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
        /// Determine image path based on product name and SKU
        /// </summary>
        private static string GetImagePath(string name, string sku)
        {
            // Determine category from name
            if (name.Contains("Áo") || name.Contains("áo"))
            {
                if (name.Contains("Polo") || name.Contains("polo"))
                    return $"/images/ao-polo/{sku}.jpg";
                if (name.Contains("Thun") || name.Contains("thun"))
                    return $"/images/ao-thun/{sku}.jpg";
                if (name.Contains("Sơ mi") || name.Contains("sơ mi"))
                    return $"/images/ao-so-mi/{sku}.jpg";
                if (name.Contains("Blazer") || name.Contains("blazer"))
                    return $"/images/ao-blazer/{sku}.jpg";
                return $"/images/ao/{sku}.jpg";
            }
            else if (name.Contains("Quần") || name.Contains("quần"))
            {
                if (name.Contains("Jean") || name.Contains("jean"))
                    return $"/images/quan-jean/{sku}.jpg";
                return $"/images/quan/{sku}.jpg";
            }
            else if (name.Contains("Đầm") || name.Contains("đầm"))
            {
                return $"/images/dam/{sku}.jpg";
            }
            else if (name.Contains("Váy") || name.Contains("váy"))
            {
                return $"/images/vay/{sku}.jpg";
            }
            
            return $"/images/products/{sku}.jpg";
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
