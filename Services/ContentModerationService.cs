using System.Text.RegularExpressions;

namespace JohnHenryFashionWeb.Services
{
    public interface IContentModerationService
    {
        /// <summary>
        /// Kiểm tra nội dung có chứa từ ngữ xúc phạm không
        /// </summary>
        Task<ModerationResult> ModerateContentAsync(string content);
        
        /// <summary>
        /// Kiểm tra và đề xuất tự động duyệt review
        /// </summary>
        Task<bool> ShouldAutoApproveReviewAsync(string? title, string? comment);
    }

    public class ContentModerationService : IContentModerationService
    {
        private readonly ILogger<ContentModerationService> _logger;
        
        // Danh sách từ ngữ xúc phạm tiếng Việt
        private static readonly HashSet<string> OffensiveWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            // Từ ngữ thô tục
            "đm", "dm", "địt", "dit", "đĩ", "di~", "đéo", "deo", "dcm", "dcmm",
            "lồn", "lon", "buồi", "buoi", "cặc", "cac", "đụ", "du", "đcm",
            "vãi", "vai", "vcl", "vl", "cc", "clgt", "cmm", "cmn", "cmnr",
            "đmm", "dmm", "đmn", "dmn", "đĩ mẹ", "đĩ má", "con đĩ", "con di~",
            "má mày", "mẹ mày", "bố mày", "thằng ngu", "thg ngu", "con ngu",
            "ngu ngốc", "ngu xuẩn", "đồ ngu", "do~ ngu", "óc chó", "oc cho",
            "não cá vàng", "não cá voi", "não tôm", "não tép",
            
            // Từ xúc phạm tiếng Anh
            "fuck", "shit", "bitch", "asshole", "bastard", "damn", "crap",
            "dick", "pussy", "cock", "motherfucker", "son of a bitch",
            
            // Từ spam/lừa đảo
            "lừa đảo", "lua dao", "scam", "fake", "giả mạo", "gia mao",
            "đồ giả", "do gia", "hàng giả", "hang gia", "hàng nhái",
            "lừa tiền", "lua tien", "ăn cắp", "an cap", "lừa gạt",
            
            // Từ ngữ phân biệt
            "quê mùa", "que mua", "dân quê", "dan que", "thấp kém", "thap kem",
            "hạ đẳng", "ha dang", "không có văn hóa", "khong co van hoa"
        };

        // Pattern để phát hiện spam (nhiều ký tự lặp, link...)
        private static readonly Regex SpamPatterns = new Regex(
            @"((.)\2{4,})|" +                           // Ký tự lặp quá 4 lần
            @"(https?://[^\s]+)|" +                     // URL links
            @"(\b\d{10,}\b)|" +                         // Số điện thoại
            @"(@[a-zA-Z0-9_]+)|" +                      // Mentions
            @"(mua\s+ngay|mua\s+liền|inbox)|" +         // Spam keywords
            @"(add\s+zalo|add\s+fb|add\s+facebook)",
            RegexOptions.IgnoreCase
        );

        public ContentModerationService(ILogger<ContentModerationService> logger)
        {
            _logger = logger;
        }

        public async Task<ModerationResult> ModerateContentAsync(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return new ModerationResult
                {
                    IsClean = true,
                    ShouldAutoApprove = true,
                    Reason = "Empty content"
                };
            }

            var result = new ModerationResult { IsClean = true, ShouldAutoApprove = true };
            var flaggedWords = new List<string>();

            // 1. Kiểm tra từ ngữ xúc phạm
            var normalizedContent = NormalizeVietnamese(content.ToLower());
            
            foreach (var offensiveWord in OffensiveWords)
            {
                var normalizedWord = NormalizeVietnamese(offensiveWord.ToLower());
                if (normalizedContent.Contains(normalizedWord))
                {
                    flaggedWords.Add(offensiveWord);
                    result.IsClean = false;
                    result.ShouldAutoApprove = false;
                }
            }

            // 2. Kiểm tra spam patterns
            if (SpamPatterns.IsMatch(content))
            {
                result.ContainsSpam = true;
                result.ShouldAutoApprove = false;
                _logger.LogWarning("Content contains spam patterns");
            }

            // 3. Kiểm tra độ dài bất thường (quá ngắn hoặc quá dài)
            if (content.Length < 5)
            {
                result.TooShort = true;
                result.ShouldAutoApprove = false;
            }
            else if (content.Length > 5000)
            {
                result.TooLong = true;
                result.ShouldAutoApprove = false;
            }

            // 4. Kiểm tra toàn chữ hoa (có thể là spam)
            var upperCaseRatio = content.Count(char.IsUpper) / (double)content.Length;
            if (upperCaseRatio > 0.7 && content.Length > 20)
            {
                result.ExcessiveCapitals = true;
                result.ShouldAutoApprove = false;
            }

            if (flaggedWords.Any())
            {
                result.FlaggedWords = flaggedWords;
                result.Reason = $"Chứa từ ngữ không phù hợp: {string.Join(", ", flaggedWords)}";
                _logger.LogWarning($"Content flagged for offensive words: {string.Join(", ", flaggedWords)}");
            }
            else if (!result.ShouldAutoApprove)
            {
                result.Reason = "Content requires manual review";
            }

            return await Task.FromResult(result);
        }

        public async Task<bool> ShouldAutoApproveReviewAsync(string? title, string? comment)
        {
            // Nếu không có nội dung nào, không tự động duyệt
            if (string.IsNullOrWhiteSpace(title) && string.IsNullOrWhiteSpace(comment))
            {
                return false;
            }

            // Kiểm tra title
            if (!string.IsNullOrWhiteSpace(title))
            {
                var titleResult = await ModerateContentAsync(title);
                if (!titleResult.ShouldAutoApprove)
                {
                    _logger.LogInformation($"Review title requires manual review: {titleResult.Reason}");
                    return false;
                }
            }

            // Kiểm tra comment
            if (!string.IsNullOrWhiteSpace(comment))
            {
                var commentResult = await ModerateContentAsync(comment);
                if (!commentResult.ShouldAutoApprove)
                {
                    _logger.LogInformation($"Review comment requires manual review: {commentResult.Reason}");
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Chuẩn hóa tiếng Việt để dễ so sánh (bỏ dấu, convert về dạng cơ bản)
        /// </summary>
        private string NormalizeVietnamese(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            // Remove Vietnamese accents
            var normalized = text.Normalize(System.Text.NormalizationForm.FormD);
            var result = new System.Text.StringBuilder();

            foreach (var c in normalized)
            {
                var category = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
                if (category != System.Globalization.UnicodeCategory.NonSpacingMark)
                {
                    result.Append(c);
                }
            }

            // Replace Vietnamese-specific characters
            var output = result.ToString()
                .Replace("đ", "d")
                .Replace("Đ", "D")
                .Replace("~", "")
                .Replace(" ", "");

            return output;
        }
    }

    /// <summary>
    /// Kết quả kiểm duyệt nội dung
    /// </summary>
    public class ModerationResult
    {
        public bool IsClean { get; set; }
        public bool ShouldAutoApprove { get; set; }
        public bool ContainsSpam { get; set; }
        public bool TooShort { get; set; }
        public bool TooLong { get; set; }
        public bool ExcessiveCapitals { get; set; }
        public List<string> FlaggedWords { get; set; } = new List<string>();
        public string? Reason { get; set; }
    }
}
