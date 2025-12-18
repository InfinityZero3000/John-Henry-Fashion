using System.Net.Mail;
using System.Net;
using Microsoft.Extensions.Options;
using JohnHenryFashionWeb.Models;
using JohnHenryFashionWeb.Services;

namespace JohnHenryFashionWeb.Services
{
    public interface IEmailService
    {
        Task<bool> SendEmailAsync(string to, string subject, string body, bool isHtml = true);
        Task<bool> SendEmailAsync(string to, string subject, string body, List<string>? cc = null, List<string>? bcc = null, bool isHtml = true);
        Task<bool> SendWelcomeEmailAsync(string email, string userName);
        Task<bool> SendOrderConfirmationEmailAsync(string email, Order order);
        Task<bool> SendOrderStatusUpdateEmailAsync(string email, Order order);
        Task<bool> SendPasswordResetEmailAsync(string email, string resetLink);
        Task<bool> SendContactConfirmationEmailAsync(string email, ContactMessage message);
        Task<bool> SendContactNotificationToAdminAsync(ContactMessage message);
        Task<bool> SendNewsletterEmailAsync(string email, string subject, string content);
        Task<bool> SendBulkEmailAsync(List<string> recipients, string subject, string content);
        Task<bool> SendProductNotificationEmailAsync(string email, Product product, string notificationType);
        Task<bool> SendTwoFactorCodeEmailAsync(string email, string code);
        Task<bool> SendRefundRequestedEmailAsync(string email, string customerName, string orderNumber, decimal amount);
        Task<bool> SendRefundApprovedEmailAsync(string email, string customerName, string orderNumber, decimal amount);
        Task<bool> SendRefundRejectedEmailAsync(string email, string customerName, string orderNumber, string reason);
    }

    public class EmailService : IEmailService
    {
        private readonly EmailSettings _emailSettings;
        private readonly ILogger<EmailService> _logger;
        private readonly IWebHostEnvironment _environment;
        private readonly ICacheService _cacheService;

        public EmailService(
            IOptions<EmailSettings> emailSettings,
            ILogger<EmailService> logger,
            IWebHostEnvironment environment,
            ICacheService cacheService)
        {
            _emailSettings = emailSettings.Value;
            _logger = logger;
            _environment = environment;
            _cacheService = cacheService;
        }

        public async Task<bool> SendEmailAsync(string to, string subject, string body, bool isHtml = true)
        {
            return await SendEmailAsync(to, subject, body, null, null, isHtml);
        }

        public async Task<bool> SendEmailAsync(string to, string subject, string body, List<string>? cc = null, List<string>? bcc = null, bool isHtml = true)
        {
            try
            {
                using var client = CreateSmtpClient();
                using var message = new MailMessage();

                message.From = new MailAddress(_emailSettings.FromEmail, _emailSettings.FromName);
                message.To.Add(to);
                message.Subject = subject;
                message.Body = body;
                message.IsBodyHtml = isHtml;

                // Add CC recipients
                if (cc != null)
                {
                    foreach (var ccEmail in cc)
                    {
                        message.CC.Add(ccEmail);
                    }
                }

                // Add BCC recipients
                if (bcc != null)
                {
                    foreach (var bccEmail in bcc)
                    {
                        message.Bcc.Add(bccEmail);
                    }
                }

                await client.SendMailAsync(message);
                _logger.LogInformation("Email sent successfully to {To} with subject: {Subject}", to, subject);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {To} with subject: {Subject}", to, subject);
                return false;
            }
        }

        public async Task<bool> SendWelcomeEmailAsync(string email, string userName)
        {
            var template = await GetEmailTemplateAsync("Welcome");
            var body = template.Replace("{{UserName}}", userName)
                              .Replace("{{CompanyName}}", "John Henry Fashion")
                              .Replace("{{LoginUrl}}", $"{_emailSettings.BaseUrl}/Account/Login");

            return await SendEmailAsync(email, "Chào mừng đến với John Henry Fashion!", body, null, null, true);
        }

        public async Task<bool> SendOrderConfirmationEmailAsync(string email, Order order)
        {
            var template = await GetEmailTemplateAsync("OrderConfirmation");
            var orderItemsHtml = GenerateOrderItemsHtml(order.OrderItems);
            
            // Calculate reward points (1 point per 10,000 VND)
            var rewardPoints = Math.Floor(order.TotalAmount / 10000);
            
            var body = template.Replace("{{OrderNumber}}", order.OrderNumber)
                              .Replace("{{OrderDate}}", order.CreatedAt.ToString("dd/MM/yyyy HH:mm"))
                              .Replace("{{CustomerName}}", $"{order.User?.FirstName} {order.User?.LastName}")
                              .Replace("{{OrderItems}}", orderItemsHtml)
                              .Replace("{{SubTotal}}", order.TotalAmount.ToString("C"))
                              .Replace("{{ShippingCost}}", 0m.ToString("C"))
                              .Replace("{{TotalAmount}}", order.TotalAmount.ToString("C"))
                              .Replace("{{OrderTrackingUrl}}", $"{_emailSettings.BaseUrl}/Account/Orders/{order.Id}")
                              .Replace("{{RewardPoints}}", rewardPoints.ToString("N0"));

            return await SendEmailAsync(email, $"Xác nhận đơn hàng #{order.OrderNumber}", body, null, null, true);
        }

        public async Task<bool> SendOrderStatusUpdateEmailAsync(string email, Order order)
        {
            var template = await GetEmailTemplateAsync("OrderStatusUpdate");
            var statusMessage = GetOrderStatusMessage(order.Status);
            
            var body = template.Replace("{{OrderNumber}}", order.OrderNumber)
                              .Replace("{{CustomerName}}", $"{order.User?.FirstName} {order.User?.LastName}")
                              .Replace("{{OrderStatus}}", GetOrderStatusDisplayName(order.Status))
                              .Replace("{{StatusMessage}}", statusMessage)
                              .Replace("{{OrderTrackingUrl}}", $"{_emailSettings.BaseUrl}/Account/Orders/{order.Id}");

            return await SendEmailAsync(email, $"Cập nhật đơn hàng #{order.OrderNumber}", body, null, null, true);
        }

        public async Task<bool> SendPasswordResetEmailAsync(string email, string resetLink)
        {
            var template = await GetEmailTemplateAsync("PasswordReset");
            var body = template.Replace("{{ResetLink}}", resetLink)
                              .Replace("{{ExpirationTime}}", "24 giờ");

            return await SendEmailAsync(email, "Đặt lại mật khẩu - John Henry Fashion", body, null, null, true);
        }

        public async Task<bool> SendContactConfirmationEmailAsync(string email, ContactMessage message)
        {
            var template = await GetEmailTemplateAsync("ContactConfirmation");
            var body = template.Replace("{{CustomerName}}", message.Name)
                              .Replace("{{Subject}}", message.Subject)
                              .Replace("{{OriginalMessage}}", message.Message)
                              .Replace("{{SubmissionDate}}", message.CreatedAt.ToString("dd/MM/yyyy HH:mm"));

            return await SendEmailAsync(email, "Cảm ơn bạn đã liên hệ - John Henry Fashion", body, null, null, true);
        }

        public async Task<bool> SendContactNotificationToAdminAsync(ContactMessage message)
        {
            if (string.IsNullOrEmpty(_emailSettings.AdminEmail))
            {
                _logger.LogWarning("Admin email not configured. Skipping admin notification.");
                return false;
            }

            var htmlBody = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; background-color: #f9f9f9;'>
                    <div style='background-color: #8B0000; color: white; padding: 20px; text-align: center;'>
                        <h1 style='margin: 0;'>📧 Tin nhắn liên hệ mới</h1>
                    </div>
                    
                    <div style='background-color: white; padding: 30px; border-radius: 5px; margin-top: 20px;'>
                        <h2 style='color: #8B0000; border-bottom: 2px solid #8B0000; padding-bottom: 10px;'>Thông tin người gửi</h2>
                        
                        <table style='width: 100%; margin: 20px 0;'>
                            <tr>
                                <td style='padding: 10px; font-weight: bold; width: 150px;'>Họ và tên:</td>
                                <td style='padding: 10px;'>{message.Name}</td>
                            </tr>
                            <tr style='background-color: #f5f5f5;'>
                                <td style='padding: 10px; font-weight: bold;'>Email:</td>
                                <td style='padding: 10px;'><a href='mailto:{message.Email}'>{message.Email}</a></td>
                            </tr>
                            <tr>
                                <td style='padding: 10px; font-weight: bold;'>Số điện thoại:</td>
                                <td style='padding: 10px;'>{(string.IsNullOrEmpty(message.Phone) ? "Không cung cấp" : message.Phone)}</td>
                            </tr>
                            <tr style='background-color: #f5f5f5;'>
                                <td style='padding: 10px; font-weight: bold;'>Thời gian:</td>
                                <td style='padding: 10px;'>{message.CreatedAt:dd/MM/yyyy HH:mm:ss}</td>
                            </tr>
                        </table>

                        <h2 style='color: #8B0000; border-bottom: 2px solid #8B0000; padding-bottom: 10px; margin-top: 30px;'>Nội dung tin nhắn</h2>
                        
                        <div style='background-color: #f5f5f5; padding: 20px; border-left: 4px solid #8B0000; margin: 20px 0;'>
                            <p style='margin: 0 0 10px 0; font-weight: bold; color: #8B0000;'>Chủ đề: {message.Subject}</p>
                            <div style='white-space: pre-wrap; line-height: 1.6;'>{message.Message}</div>
                        </div>

                        <div style='margin-top: 30px; padding: 15px; background-color: #fff3cd; border-left: 4px solid #ffc107;'>
                            <p style='margin: 0; color: #856404;'>
                                <strong>⚠️ Lưu ý:</strong> Vui lòng phản hồi khách hàng trong vòng 24 giờ để đảm bảo chất lượng dịch vụ.
                            </p>
                        </div>

                        <div style='text-align: center; margin-top: 30px;'>
                            <a href='mailto:{message.Email}?subject=Re: {message.Subject}' 
                               style='display: inline-block; padding: 12px 30px; background-color: #8B0000; color: white; text-decoration: none; border-radius: 5px; font-weight: bold;'>
                                Trả lời ngay
                            </a>
                        </div>
                    </div>

                    <div style='text-align: center; padding: 20px; color: #666; font-size: 12px;'>
                        <p>Email tự động từ hệ thống John Henry Fashion</p>
                        <p>ID Tin nhắn: {message.Id}</p>
                    </div>
                </div>";

            return await SendEmailAsync(_emailSettings.AdminEmail, 
                $"[Liên hệ mới] {message.Subject}", 
                htmlBody, 
                null, 
                null, 
                true);
        }

        public async Task<bool> SendNewsletterEmailAsync(string email, string subject, string content)
        {
            var template = await GetEmailTemplateAsync("Newsletter");
            var body = template.Replace("{{NewsletterContent}}", content)
                              .Replace("{{UnsubscribeUrl}}", $"{_emailSettings.BaseUrl}/Newsletter/Unsubscribe?email={email}");

            return await SendEmailAsync(email, subject, body, null, null, true);
        }

        public async Task<bool> SendBulkEmailAsync(List<string> recipients, string subject, string content)
        {
            var successCount = 0;
            var batchSize = 50; // Send in batches to avoid overwhelming the server

            for (int i = 0; i < recipients.Count; i += batchSize)
            {
                var batch = recipients.Skip(i).Take(batchSize).ToList();
                var tasks = batch.Select(email => SendNewsletterEmailAsync(email, subject, content));
                var results = await Task.WhenAll(tasks);
                successCount += results.Count(r => r);

                // Add delay between batches to avoid rate limiting
                if (i + batchSize < recipients.Count)
                {
                    await Task.Delay(1000);
                }
            }

            _logger.LogInformation("Bulk email sent to {SuccessCount}/{TotalCount} recipients", successCount, recipients.Count);
            return successCount > 0;
        }

        public async Task<bool> SendProductNotificationEmailAsync(string email, Product product, string notificationType)
        {
            var template = await GetEmailTemplateAsync("ProductNotification");
            var notificationMessage = GetProductNotificationMessage(notificationType);
            
            var body = template.Replace("{{ProductName}}", product.Name)
                              .Replace("{{ProductDescription}}", product.Description ?? "")
                              .Replace("{{ProductPrice}}", product.Price.ToString("C"))
                              .Replace("{{ProductImage}}", product.FeaturedImageUrl ?? "")
                              .Replace("{{ProductUrl}}", $"{_emailSettings.BaseUrl}/Products/Details/{product.Id}")
                              .Replace("{{NotificationMessage}}", notificationMessage);

            var subject = notificationType switch
            {
                "back_in_stock" => $"Sản phẩm {product.Name} đã có hàng trở lại!",
                "price_drop" => $"Giảm giá: {product.Name}",
                "new_product" => $"Sản phẩm mới: {product.Name}",
                _ => $"Thông báo sản phẩm: {product.Name}"
            };

            return await SendEmailAsync(email, subject, body, null, null, true);
        }

        private SmtpClient CreateSmtpClient()
        {
            var client = new SmtpClient(_emailSettings.SmtpServer, _emailSettings.SmtpPort);
            client.EnableSsl = _emailSettings.UseSsl;
            client.UseDefaultCredentials = false;
            client.Credentials = new NetworkCredential(_emailSettings.Username, _emailSettings.Password);
            return client;
        }

        private async Task<string> GetEmailTemplateAsync(string templateName)
        {
            var cacheKey = $"email_template_{templateName}";
            
            return await _cacheService.GetOrSetAsync(cacheKey, async () =>
            {
                var templatePath = Path.Combine(_environment.ContentRootPath, "EmailTemplates", $"{templateName}.html");
                
                if (File.Exists(templatePath))
                {
                    return await File.ReadAllTextAsync(templatePath);
                }
                
                _logger.LogWarning("Email template not found: {TemplateName}", templateName);
                return GetDefaultTemplate();
            }, TimeSpan.FromHours(1));
        }

        private string GetDefaultTemplate()
        {
            return @"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>John Henry Fashion</title>
    <style>
        body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; }
        .container { max-width: 600px; margin: 0 auto; padding: 20px; }
        .header { background-color: #dc3545; color: white; padding: 20px; text-align: center; }
        .content { padding: 20px; background-color: #f9f9f9; }
        .footer { background-color: #333; color: white; padding: 10px; text-align: center; font-size: 12px; }
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>John Henry Fashion</h1>
        </div>
        <div class='content'>
            {{Content}}
        </div>
        <div class='footer'>
            <p>&copy; 2025 John Henry Fashion. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
        }

        private string GenerateOrderItemsHtml(ICollection<OrderItem> orderItems)
        {
            var html = "<table style='width: 100%; border-collapse: collapse;'>";
            html += "<tr style='background-color: #f8f9fa;'>";
            html += "<th style='border: 1px solid #ddd; padding: 8px; text-align: left;'>Sản phẩm</th>";
            html += "<th style='border: 1px solid #ddd; padding: 8px; text-align: center;'>SL</th>";
            html += "<th style='border: 1px solid #ddd; padding: 8px; text-align: right;'>Giá</th>";
            html += "<th style='border: 1px solid #ddd; padding: 8px; text-align: right;'>Tổng</th>";
            html += "</tr>";

            foreach (var item in orderItems)
            {
                html += "<tr>";
                html += $"<td style='border: 1px solid #ddd; padding: 8px;'>{item.ProductName}</td>";
                html += $"<td style='border: 1px solid #ddd; padding: 8px; text-align: center;'>{item.Quantity}</td>";
                html += $"<td style='border: 1px solid #ddd; padding: 8px; text-align: right;'>{item.UnitPrice:C}</td>";
                html += $"<td style='border: 1px solid #ddd; padding: 8px; text-align: right;'>{item.TotalPrice:C}</td>";
                html += "</tr>";
            }

            html += "</table>";
            return html;
        }

        private string GetOrderStatusMessage(string status)
        {
            return status switch
            {
                "pending" => "Đơn hàng của bạn đang được xử lý.",
                "confirmed" => "Đơn hàng đã được xác nhận và sẽ sớm được chuẩn bị.",
                "processing" => "Đơn hàng đang được chuẩn bị.",
                "shipped" => "Đơn hàng đã được giao cho đơn vị vận chuyển.",
                "delivered" => "Đơn hàng đã được giao thành công.",
                "cancelled" => "Đơn hàng đã bị hủy.",
                _ => "Trạng thái đơn hàng đã được cập nhật."
            };
        }

        private string GetOrderStatusDisplayName(string status)
        {
            return status switch
            {
                "pending" => "Chờ xử lý",
                "confirmed" => "Đã xác nhận",
                "processing" => "Đang xử lý",
                "shipped" => "Đã giao vận",
                "delivered" => "Đã giao hàng",
                "cancelled" => "Đã hủy",
                _ => status
            };
        }

        private string GetProductNotificationMessage(string notificationType)
        {
            return notificationType switch
            {
                "back_in_stock" => "Sản phẩm bạn quan tâm đã có hàng trở lại!",
                "price_drop" => "Sản phẩm bạn theo dõi đang có giá ưu đãi!",
                "new_product" => "Sản phẩm mới vừa được ra mắt!",
                _ => "Có thông báo mới về sản phẩm này."
            };
        }

        public async Task<bool> SendTwoFactorCodeEmailAsync(string email, string code)
        {
            var template = await GetEmailTemplateAsync("TwoFactorCode");
            var subject = "Mã xác thực đăng nhập - John Henry Fashion";

            var body = template
                .Replace("{{UserName}}", "Khách hàng")
                .Replace("{{VerificationCode}}", code)
                .Replace("{{CompanyName}}", "John Henry Fashion")
                .Replace("{{BaseUrl}}", _emailSettings.BaseUrl);

            return await SendEmailAsync(email, subject, body, isHtml: true);
        }

        public async Task<bool> SendRefundRequestedEmailAsync(string email, string customerName, string orderNumber, decimal amount)
        {
            var subject = $"Yêu Cầu Hoàn Trả Đơn Hàng #{orderNumber}";
            var body = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                    <h2 style='color: #333;'>Yêu Cầu Hoàn Trả Đã Được Tiếp Nhận</h2>
                    <p>Xin chào <strong>{customerName}</strong>,</p>
                    <p>Chúng tôi đã nhận được yêu cầu hoàn trả cho đơn hàng <strong>#{orderNumber}</strong>.</p>
                    <div style='background-color: #f5f5f5; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                        <p><strong>Số tiền hoàn trả:</strong> {amount:N0} VNĐ</p>
                        <p><strong>Thời gian xử lý:</strong> 24-48 giờ</p>
                    </div>
                    <p>Chúng tôi sẽ xem xét và phản hồi trong thời gian sớm nhất.</p>
                    <p>Nếu có thắc mắc, vui lòng liên hệ: <a href='mailto:support@johnhenry.vn'>support@johnhenry.vn</a></p>
                    <hr style='margin-top: 30px; border: none; border-top: 1px solid #ddd;'>
                    <p style='color: #666; font-size: 12px;'>Email tự động từ John Henry Fashion</p>
                </div>";

            return await SendEmailAsync(email, subject, body, isHtml: true);
        }

        public async Task<bool> SendRefundApprovedEmailAsync(string email, string customerName, string orderNumber, decimal amount)
        {
            var subject = $"✅ Yêu Cầu Hoàn Trả Được Chấp Nhận - #{orderNumber}";
            var body = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                    <div style='background-color: #4CAF50; color: white; padding: 20px; border-radius: 5px;'>
                        <h2>Yêu Cầu Hoàn Trả Được Chấp Nhận</h2>
                    </div>
                    <p style='margin-top: 20px;'>Xin chào <strong>{customerName}</strong>,</p>
                    <p>Yêu cầu hoàn trả cho đơn hàng <strong>#{orderNumber}</strong> đã được chấp nhận.</p>
                    <div style='background-color: #e8f5e9; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                        <p><strong>Số tiền hoàn trả:</strong> {amount:N0} VNĐ</p>
                        <p><strong>Phương thức hoàn trả:</strong> Chuyển khoản ngân hàng</p>
                        <p><strong>Thời gian nhận tiền:</strong> 3-5 ngày làm việc</p>
                    </div>
                    <p>Chúng tôi sẽ liên hệ để xác nhận thông tin tài khoản ngân hàng.</p>
                    <p>Cảm ơn bạn đã tin tưởng John Henry Fashion!</p>
                    <hr style='margin-top: 30px; border: none; border-top: 1px solid #ddd;'>
                    <p style='color: #666; font-size: 12px;'>Email tự động từ John Henry Fashion</p>
                </div>";

            return await SendEmailAsync(email, subject, body, isHtml: true);
        }

        public async Task<bool> SendRefundRejectedEmailAsync(string email, string customerName, string orderNumber, string reason)
        {
            var subject = $"Yêu Cầu Hoàn Trả Bị Từ Chối - #{orderNumber}";
            var body = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                    <div style='background-color: #ff9800; color: white; padding: 20px; border-radius: 5px;'>
                        <h2>Yêu Cầu Hoàn Trả Bị Từ Chối</h2>
                    </div>
                    <p style='margin-top: 20px;'>Xin chào <strong>{customerName}</strong>,</p>
                    <p>Rất tiếc, yêu cầu hoàn trả cho đơn hàng <strong>#{orderNumber}</strong> không được chấp nhận.</p>
                    <div style='background-color: #fff3e0; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                        <p><strong>Lý do:</strong></p>
                        <p>{reason}</p>
                    </div>
                    <p>Nếu bạn không đồng ý với quyết định này, vui lòng liên hệ:</p>
                    <ul>
                        <li>Email: <a href='mailto:support@johnhenry.vn'>support@johnhenry.vn</a></li>
                        <li>Hotline: 1900-xxxx</li>
                    </ul>
                    <hr style='margin-top: 30px; border: none; border-top: 1px solid #ddd;'>
                    <p style='color: #666; font-size: 12px;'>Email tự động từ John Henry Fashion</p>
                </div>";

            return await SendEmailAsync(email, subject, body, isHtml: true);
        }
    }

    public class EmailSettings
    {
        public string SmtpServer { get; set; } = string.Empty;
        public int SmtpPort { get; set; }
        public bool UseSsl { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FromEmail { get; set; } = string.Empty;
        public string FromName { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = string.Empty;
        public string AdminEmail { get; set; } = string.Empty;
    }
}
