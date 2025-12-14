using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using JohnHenryFashionWeb.Services;

namespace JohnHenryFashionWeb.Pages
{
    public class TestEmailModel : PageModel
    {
        private readonly IEmailService _emailService;
        private readonly EmailSettings _emailSettings;
        private readonly ILogger<TestEmailModel> _logger;

        public string SmtpServer { get; set; } = "";
        public int SmtpPort { get; set; }
        public bool UseSsl { get; set; }
        public string FromEmail { get; set; } = "";
        public string FromName { get; set; } = "";
        public string TestEmail { get; set; } = "";
        public TestResult? TestResult { get; set; }

        public TestEmailModel(IEmailService emailService, IOptions<EmailSettings> emailSettings, ILogger<TestEmailModel> logger)
        {
            _emailService = emailService;
            _emailSettings = emailSettings.Value;
            _logger = logger;
        }

        public void OnGet()
        {
            LoadEmailSettings();
        }

        public async Task<IActionResult> OnPostAsync(string testEmail, string templateType)
        {
            LoadEmailSettings();
            TestEmail = testEmail;

            try
            {
                bool result = false;
                string subject = "Test Email";
                string body = "";

                switch (templateType)
                {
                    case "simple":
                        subject = "🔧 Test Email - John Henry Fashion";
                        body = @"
                        <html>
                        <body style='font-family: Arial, sans-serif; padding: 20px;'>
                            <h2 style='color: #dc3545;'>Email Configuration Test</h2>
                            <p>Congratulations! Your email configuration is working correctly.</p>
                            <p><strong>Sent at:</strong> " + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + @"</p>
                            <hr style='margin: 20px 0;'>
                            <p style='color: #666; font-size: 12px;'>
                                This is an automated test email from John Henry Fashion.<br>
                                If you received this email, your SMTP settings are configured properly.
                            </p>
                        </body>
                        </html>";
                        result = await _emailService.SendEmailAsync(testEmail, subject, body, isHtml: true);
                        break;

                    case "order":
                        // Create a mock order for testing
                        var mockOrder = new JohnHenryFashionWeb.Models.Order
                        {
                            Id = Guid.NewGuid(),
                            OrderNumber = "TEST" + DateTime.Now.Ticks.ToString().Substring(8),
                            CreatedAt = DateTime.Now,
                            TotalAmount = 1500000,
                            User = new JohnHenryFashionWeb.Models.ApplicationUser
                            {
                                FirstName = "Test",
                                LastName = "User",
                                Email = testEmail
                            },
                            OrderItems = new List<JohnHenryFashionWeb.Models.OrderItem>
                            {
                                new JohnHenryFashionWeb.Models.OrderItem
                                {
                                    ProductName = "Test Product",
                                    Quantity = 2,
                                    UnitPrice = 750000,
                                    TotalPrice = 1500000
                                }
                            }
                        };
                        result = await _emailService.SendOrderConfirmationEmailAsync(testEmail, mockOrder);
                        break;

                    case "contact":
                        var mockMessage = new JohnHenryFashionWeb.Models.ContactMessage
                        {
                            Name = "Test User",
                            Subject = "Test Contact Message",
                            Message = "This is a test message to verify the contact confirmation email template.",
                            CreatedAt = DateTime.Now
                        };
                        result = await _emailService.SendContactConfirmationEmailAsync(testEmail, mockMessage);
                        break;
                }

                TestResult = new TestResult
                {
                    Success = result,
                    Message = result 
                        ? $"Email sent successfully to {testEmail}! Check your inbox (and spam folder)." 
                        : "Failed to send email. Check the application logs for details."
                };

                _logger.LogInformation("Test email result: {Result} to {Email}", result, testEmail);
            }
            catch (Exception ex)
            {
                TestResult = new TestResult
                {
                    Success = false,
                    Message = $"Error: {ex.Message}"
                };
                _logger.LogError(ex, "Error sending test email to {Email}", testEmail);
            }

            return Page();
        }

        private void LoadEmailSettings()
        {
            SmtpServer = _emailSettings.SmtpServer;
            SmtpPort = _emailSettings.SmtpPort;
            UseSsl = _emailSettings.UseSsl;
            FromEmail = _emailSettings.FromEmail;
            FromName = _emailSettings.FromName;
        }
    }

    public class TestResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
    }
}
