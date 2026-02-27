using System.Drawing;
using System.Drawing.Imaging;

namespace JohnHenryFashionWeb.Services
{
    public interface IImageOptimizationService
    {
        Task<string> OptimizeImageAsync(IFormFile file, string uploadPath, int maxWidth = 1200, int quality = 85);
        Task<string> CreateThumbnailAsync(string imagePath, int width = 300, int height = 300);
        Task<string> ConvertToWebPAsync(string imagePath);
        Task<bool> DeleteImageAsync(string imagePath);
        string GetOptimizedImageUrl(string originalPath, int? width = null, int? height = null);
    }

    public class ImageOptimizationService : IImageOptimizationService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<ImageOptimizationService> _logger;
        private readonly string[] _allowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        private const long MaxFileSize = 5 * 1024 * 1024; // 5MB

        public ImageOptimizationService(IWebHostEnvironment environment, ILogger<ImageOptimizationService> logger)
        {
            _environment = environment;
            _logger = logger;
        }

        public async Task<string> OptimizeImageAsync(IFormFile file, string uploadPath, int maxWidth = 1200, int quality = 85)
        {
            try
            {
                if (file == null || file.Length == 0)
                    throw new ArgumentException("Invalid file");

                if (file.Length > MaxFileSize)
                    throw new ArgumentException($"File size exceeds the maximum allowed size of {MaxFileSize / (1024 * 1024)}MB");

                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!_allowedExtensions.Contains(extension))
                    throw new ArgumentException("Unsupported file type");

                var fileName = Guid.NewGuid().ToString() + extension;
                var fullUploadPath = Path.Combine(_environment.WebRootPath, uploadPath.TrimStart('/'));
                
                if (!Directory.Exists(fullUploadPath))
                    Directory.CreateDirectory(fullUploadPath);

                var filePath = Path.Combine(fullUploadPath, fileName);

                // Simple file copy for now (without image processing)
                using var stream = new FileStream(filePath, FileMode.Create);
                await file.CopyToAsync(stream);

                var relativePath = Path.Combine(uploadPath, fileName).Replace('\\', '/');
                return "/" + relativePath.TrimStart('/');
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error optimizing image: {FileName}", file.FileName);
                throw;
            }
        }

        public async Task<string> CreateThumbnailAsync(string imagePath, int width = 300, int height = 300)
        {
            try
            {
                await Task.CompletedTask;
                var fullPath = Path.Combine(_environment.WebRootPath, imagePath.TrimStart('/'));
                if (!File.Exists(fullPath))
                    throw new FileNotFoundException("Image not found");

                var directory = Path.GetDirectoryName(fullPath)!;
                var fileNameWithoutExt = Path.GetFileNameWithoutExtension(fullPath);
                var extension = Path.GetExtension(fullPath);
                
                var thumbnailFileName = $"{fileNameWithoutExt}_thumb_{width}x{height}{extension}";
                var thumbnailPath = Path.Combine(directory, thumbnailFileName);

                if (File.Exists(thumbnailPath))
                {
                    var relativeThumbnailPath = Path.GetRelativePath(_environment.WebRootPath, thumbnailPath);
                    return "/" + relativeThumbnailPath.Replace('\\', '/');
                }

                // Simple copy for now (without resizing)
                File.Copy(fullPath, thumbnailPath);

                var relativeThumbPath = Path.GetRelativePath(_environment.WebRootPath, thumbnailPath);
                return "/" + relativeThumbPath.Replace('\\', '/');
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating thumbnail for: {ImagePath}", imagePath);
                throw;
            }
        }

        public async Task<string> ConvertToWebPAsync(string imagePath)
        {
            try
            {
                await Task.CompletedTask;
                var fullPath = Path.Combine(_environment.WebRootPath, imagePath.TrimStart('/'));
                if (!File.Exists(fullPath))
                    throw new FileNotFoundException("Image not found");

                var webpPath = Path.ChangeExtension(fullPath, ".webp");
                
                if (File.Exists(webpPath))
                {
                    var relativeWebpPath = Path.GetRelativePath(_environment.WebRootPath, webpPath);
                    return "/" + relativeWebpPath.Replace('\\', '/');
                }

                // Simple copy with webp extension for now
                File.Copy(fullPath, webpPath);

                var relativeWebpPathResult = Path.GetRelativePath(_environment.WebRootPath, webpPath);
                return "/" + relativeWebpPathResult.Replace('\\', '/');
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error converting to WebP: {ImagePath}", imagePath);
                throw;
            }
        }

        public async Task<bool> DeleteImageAsync(string imagePath)
        {
            try
            {
                var fullPath = Path.Combine(_environment.WebRootPath, imagePath.TrimStart('/'));
                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                    
                    // Also delete WebP version if exists
                    var webpPath = Path.ChangeExtension(fullPath, ".webp");
                    if (File.Exists(webpPath))
                        File.Delete(webpPath);
                    
                    // Delete thumbnails
                    var directory = Path.GetDirectoryName(fullPath)!;
                    var fileNameWithoutExt = Path.GetFileNameWithoutExtension(fullPath);
                    var thumbnailPattern = $"{fileNameWithoutExt}_thumb_*";
                    
                    var thumbnails = Directory.GetFiles(directory, thumbnailPattern);
                    foreach (var thumbnail in thumbnails)
                    {
                        File.Delete(thumbnail);
                    }
                }
                
                return await Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting image: {ImagePath}", imagePath);
                return false;
            }
        }

        public string GetOptimizedImageUrl(string originalPath, int? width = null, int? height = null)
        {
            if (string.IsNullOrEmpty(originalPath))
                return "/images/placeholder.jpg";

            // Return WebP version if available
            var webpPath = Path.ChangeExtension(originalPath, ".webp");
            var fullWebpPath = Path.Combine(_environment.WebRootPath, webpPath.TrimStart('/'));
            
            if (File.Exists(fullWebpPath))
            {
                return webpPath;
            }

            return originalPath;
        }
    }
}
