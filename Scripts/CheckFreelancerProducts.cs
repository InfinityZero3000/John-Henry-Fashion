using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using JohnHenryFashionWeb.Data;

namespace JohnHenryFashionWeb.Scripts
{
    public class CheckFreelancerProducts
    {
        // This is a utility script - use as reference only
        // To run: dotnet script CheckFreelancerProducts.cs
        public static async Task CheckProducts(string[] args)
        {
            // Read connection string from appsettings.json
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();

            var connectionString = configuration.GetConnectionString("DefaultConnection");
            
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            optionsBuilder.UseNpgsql(connectionString);

            using var context = new ApplicationDbContext(optionsBuilder.Options);
            
            // Get all Freelancer products
            var products = await context.Products
                .Where(p => p.SKU.StartsWith("FW"))
                .OrderBy(p => p.SKU)
                .Select(p => new { p.SKU, p.Name, p.FeaturedImageUrl })
                .ToListAsync();

            Console.WriteLine($"=== FOUND {products.Count} FREELANCER PRODUCTS ===\n");
            
            var wwwroot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var missingCount = 0;
            var pathMismatchCount = 0;
            var okCount = 0;

            foreach (var product in products)
            {
                var currentPath = product.FeaturedImageUrl;
                
                // Check if current path file exists
                var currentFileExists = false;
                if (!string.IsNullOrEmpty(currentPath))
                {
                    var currentPhysicalPath = Path.Combine(wwwroot, currentPath.TrimStart('/'));
                    currentFileExists = File.Exists(currentPhysicalPath);
                }

                // Search for file in all possible locations
                var possibleLocations = new[]
                {
                    Path.Combine(wwwroot, "images", "ao-nu", $"{product.SKU}.jpg"),
                    Path.Combine(wwwroot, "images", "quan-nu", $"{product.SKU}.jpg"),
                    Path.Combine(wwwroot, "images", "chan-vay-nu", $"{product.SKU}.jpg"),
                    Path.Combine(wwwroot, "images", "dam-nu", $"{product.SKU}.jpg"),
                    Path.Combine(wwwroot, "images", "products", $"{product.SKU}.jpg"),
                };

                string? foundLocation = null;
                foreach (var location in possibleLocations)
                {
                    if (File.Exists(location))
                    {
                        foundLocation = location.Replace(wwwroot, "").Replace("\\", "/");
                        break;
                    }
                }

                if (foundLocation == null)
                {
                    Console.WriteLine($"MISSING: {product.SKU} - {product.Name}");
                    Console.WriteLine($"   Current DB path: {currentPath ?? "NULL"}");
                    Console.WriteLine();
                    missingCount++;
                }
                else if (currentPath != foundLocation)
                {
                    Console.WriteLine($"⚠️  PATH MISMATCH: {product.SKU}");
                    Console.WriteLine($"   DB path:     {currentPath ?? "NULL"}");
                    Console.WriteLine($"   Actual path: {foundLocation}");
                    Console.WriteLine();
                    pathMismatchCount++;
                }
                else
                {
                    okCount++;
                }
            }

            Console.WriteLine("\n=== SUMMARY ===");
            Console.WriteLine($"✅ OK: {okCount}");
            Console.WriteLine($"⚠️  Path Mismatch: {pathMismatchCount}");
            Console.WriteLine($"Missing Files: {missingCount}");
            Console.WriteLine($"📊 Total: {products.Count}");
        }
    }
}
