namespace JohnHenryFashionWeb.Helpers
{
    /// <summary>
    /// Helper để chuyển đổi đường dẫn ảnh cục bộ sang jsDelivr CDN URL.
    /// jsDelivr format: https://cdn.jsdelivr.net/gh/{user}/{repo}@{version}/{file}
    /// </summary>
    public static class CdnHelper
    {
        private const string GitHubUser = "InfinityZero3000";
        private const string GitHubRepo = "John-Henry-Fashion";
        private const string GitHubBranch = "main";

        /// <summary>
        /// Base CDN URL prefix for all static assets
        /// </summary>
        public static readonly string CdnBase =
            $"https://cdn.jsdelivr.net/gh/{GitHubUser}/{GitHubRepo}@{GitHubBranch}/wwwroot";

        /// <summary>
        /// Chuyển đổi đường dẫn ảnh cục bộ (/images/...) sang jsDelivr CDN URL.
        /// Nếu URL đã là absolute (http/https) thì giữ nguyên.
        /// Nếu null/rỗng thì trả về ảnh placeholder từ CDN.
        /// </summary>
        /// <param name="localPath">Đường dẫn cục bộ, ví dụ: /images/ao-nam/sp1.jpg</param>
        /// <param name="fallback">Đường dẫn fallback nếu localPath null/rỗng</param>
        public static string GetCdnUrl(string? localPath, string fallback = "/images/placeholder.jpg")
        {
            // Dùng fallback nếu path rỗng
            var path = string.IsNullOrWhiteSpace(localPath) ? fallback : localPath;

            // Nếu đã là URL tuyệt đối (http/https) thì giữ nguyên
            if (path.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                return path;
            }

            // Chuẩn hoá: bỏ tiền tố ~/ nếu có
            if (path.StartsWith("~/"))
                path = path[1..]; // giữ lại phần /images/...

            // Đảm bảo bắt đầu bằng /
            if (!path.StartsWith("/"))
                path = "/" + path;

            return CdnBase + path;
        }

        /// <summary>
        /// Trả về CDN URL cho ảnh sản phẩm với fallback mặc định là placeholder.
        /// </summary>
        public static string GetProductImageUrl(string? imageUrl)
            => GetCdnUrl(imageUrl, "/images/placeholder.jpg");

        /// <summary>
        /// Trả về CDN URL cho ảnh banner.
        /// </summary>
        public static string GetBannerUrl(string? imageUrl)
            => GetCdnUrl(imageUrl, "/images/Banner/default-banner.jpg");
    }
}
