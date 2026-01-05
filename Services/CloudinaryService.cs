using CloudinaryDotNet;
using CloudinaryDotNet.Actions;

namespace JohnHenryFashionWeb.Services
{
    public interface ICloudinaryService
    {
        Task<ImageUploadResult> UploadImageAsync(IFormFile file, string folder = "products");
        Task<DeletionResult> DeleteImageAsync(string publicId);
        string GetImageUrl(string publicId, int width = 800, int height = 800);
        Task<List<ImageUploadResult>> UploadMultipleImagesAsync(List<IFormFile> files, string folder = "products");
    }

    public class CloudinaryService : ICloudinaryService
    {
        private readonly Cloudinary _cloudinary;
        private readonly ILogger<CloudinaryService> _logger;

        public CloudinaryService(IConfiguration configuration, ILogger<CloudinaryService> logger)
        {
            _logger = logger;

            var cloudName = configuration["CLOUDINARY_CLOUD_NAME"] 
                ?? configuration["CloudinarySettings:CloudName"];
            var apiKey = configuration["CLOUDINARY_API_KEY"] 
                ?? configuration["CloudinarySettings:ApiKey"];
            var apiSecret = configuration["CLOUDINARY_API_SECRET"] 
                ?? configuration["CloudinarySettings:ApiSecret"];

            if (string.IsNullOrEmpty(cloudName) || string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(apiSecret))
            {
                _logger.LogError("Cloudinary configuration is missing. Please set CLOUDINARY_CLOUD_NAME, CLOUDINARY_API_KEY, and CLOUDINARY_API_SECRET.");
                throw new InvalidOperationException("Cloudinary configuration is incomplete.");
            }

            var account = new Account(cloudName, apiKey, apiSecret);
            _cloudinary = new Cloudinary(account);
            _cloudinary.Api.Secure = true; // Force HTTPS

            _logger.LogInformation("Cloudinary service initialized successfully with cloud: {CloudName}", cloudName);
        }

        public async Task<ImageUploadResult> UploadImageAsync(IFormFile file, string folder = "products")
        {
            if (file == null || file.Length == 0)
            {
                throw new ArgumentException("File is null or empty");
            }

            // Validate file type
            var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/webp", "image/gif" };
            if (!allowedTypes.Contains(file.ContentType.ToLower()))
            {
                throw new ArgumentException($"Invalid file type: {file.ContentType}. Only images are allowed.");
            }

            // Validate file size (max 10MB)
            if (file.Length > 10 * 1024 * 1024)
            {
                throw new ArgumentException("File size exceeds 10MB limit.");
            }

            var uploadResult = new ImageUploadResult();

            try
            {
                using var stream = file.OpenReadStream();
                var fileName = Path.GetFileNameWithoutExtension(file.FileName);
                var sanitizedFileName = SanitizeFileName(fileName);
                var publicId = $"{folder}/{sanitizedFileName}_{Guid.NewGuid().ToString("N").Substring(0, 8)}";

                var uploadParams = new ImageUploadParams
                {
                    File = new FileDescription(file.FileName, stream),
                    PublicId = publicId,
                    Folder = folder,
                    Transformation = new Transformation()
                        .Quality("auto")
                        .FetchFormat("auto"), // Automatic format optimization
                    Overwrite = false,
                    UniqueFilename = true,
                    UseFilename = true
                };

                uploadResult = await _cloudinary.UploadAsync(uploadParams);

                if (uploadResult.Error != null)
                {
                    _logger.LogError("Cloudinary upload error: {Error}", uploadResult.Error.Message);
                    throw new Exception($"Upload failed: {uploadResult.Error.Message}");
                }

                _logger.LogInformation("Image uploaded successfully. Public ID: {PublicId}, URL: {Url}", 
                    uploadResult.PublicId, uploadResult.SecureUrl);

                return uploadResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading image to Cloudinary");
                throw;
            }
        }

        public async Task<List<ImageUploadResult>> UploadMultipleImagesAsync(List<IFormFile> files, string folder = "products")
        {
            var results = new List<ImageUploadResult>();

            foreach (var file in files)
            {
                try
                {
                    var result = await UploadImageAsync(file, folder);
                    results.Add(result);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error uploading file: {FileName}", file.FileName);
                    // Continue with other files even if one fails
                }
            }

            return results;
        }

        public async Task<DeletionResult> DeleteImageAsync(string publicId)
        {
            if (string.IsNullOrEmpty(publicId))
            {
                throw new ArgumentException("Public ID is null or empty");
            }

            try
            {
                var deleteParams = new DeletionParams(publicId);
                var result = await _cloudinary.DestroyAsync(deleteParams);

                if (result.Error != null)
                {
                    _logger.LogError("Cloudinary deletion error: {Error}", result.Error.Message);
                    throw new Exception($"Deletion failed: {result.Error.Message}");
                }

                _logger.LogInformation("Image deleted successfully. Public ID: {PublicId}", publicId);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting image from Cloudinary");
                throw;
            }
        }

        public string GetImageUrl(string publicId, int width = 800, int height = 800)
        {
            if (string.IsNullOrEmpty(publicId))
            {
                return "/images/placeholder.jpg"; // Fallback placeholder
            }

            var transformation = new Transformation()
                .Width(width)
                .Height(height)
                .Crop("fill")
                .Quality("auto")
                .FetchFormat("auto");

            return _cloudinary.Api.UrlImgUp.Transform(transformation).BuildUrl(publicId);
        }

        private string SanitizeFileName(string fileName)
        {
            // Remove special characters and spaces
            var sanitized = System.Text.RegularExpressions.Regex.Replace(fileName, @"[^a-zA-Z0-9_-]", "_");
            return sanitized.ToLower();
        }
    }
}
