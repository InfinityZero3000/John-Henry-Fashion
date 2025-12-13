using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using JohnHenryFashionWeb.Models;
using JohnHenryFashionWeb.ViewModels;
using JohnHenryFashionWeb.Data;
using JohnHenryFashionWeb.Services;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace JohnHenryFashionWeb.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AccountController> _logger;
        private readonly ISecurityService _securityService;
        private readonly IEmailService _emailService;
        private readonly IAuthService _authService;
        private readonly ICacheService _cacheService;
        private readonly IConfiguration _configuration;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext context,
            ILogger<AccountController> logger,
            ISecurityService securityService,
            IEmailService emailService,
            IAuthService authService,
            ICacheService cacheService,
            IConfiguration configuration)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
            _logger = logger;
            _securityService = securityService;
            _emailService = emailService;
            _authService = authService;
            _cacheService = cacheService;
            _configuration = configuration;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            
            if (ModelState.IsValid)
            {
                // Check if account is locked
                if (await _authService.IsAccountLockedAsync(model.Email))
                {
                    ModelState.AddModelError(string.Empty, "Tài khoản của bạn đã bị khóa do quá nhiều lần đăng nhập sai. Vui lòng thử lại sau.");
                    return View(model);
                }

                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user != null && !user.EmailConfirmed)
                {
                    ModelState.AddModelError(string.Empty, "Bạn phải xác nhận email trước khi đăng nhập.");
                    return View(model);
                }

                var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: true);
                
                if (result.Succeeded)
                {
                    // Track successful login
                    await _authService.TrackLoginAttemptAsync(model.Email, true, HttpContext.Connection.RemoteIpAddress?.ToString());
                    await _authService.ResetFailedLoginAttemptsAsync(model.Email);
                    
                    _logger.LogInformation("User {Email} logged in successfully.", model.Email);
                    
                    // Update last login date
                    if (user != null)
                    {
                        user.LastLoginDate = DateTime.UtcNow;
                        await _userManager.UpdateAsync(user);
                    }
                    
                    return await RedirectToLocal(returnUrl, user);
                }
                
                if (result.RequiresTwoFactor)
                {
                    return RedirectToAction(nameof(LoginWith2fa), new { returnUrl, model.RememberMe });
                }
                
                if (result.IsLockedOut)
                {
                    _logger.LogWarning("User account {Email} locked out.", model.Email);
                    await _authService.TrackLoginAttemptAsync(model.Email, false, HttpContext.Connection.RemoteIpAddress?.ToString());
                    return RedirectToAction(nameof(Lockout));
                }
                else
                {
                    // Track failed login attempt
                    await _authService.TrackLoginAttemptAsync(model.Email, false, HttpContext.Connection.RemoteIpAddress?.ToString());
                    
                    var failedAttempts = await _authService.GetFailedLoginAttemptsAsync(model.Email);
                    
                    if (failedAttempts >= 2)
                    {
                        ModelState.AddModelError(string.Empty, $"Email hoặc mật khẩu không đúng. Bạn còn {3 - failedAttempts} lần thử.");
                    }
                    else
                    {
                        ModelState.AddModelError(string.Empty, "Email hoặc mật khẩu không đúng.");
                    }
                    
                    return View(model);
                }
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult Register(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            
            if (ModelState.IsValid)
            {
                // Check if email already exists
                var existingUser = await _userManager.FindByEmailAsync(model.Email);
                if (existingUser != null)
                {
                    ModelState.AddModelError("Email", "Email này đã được sử dụng.");
                    return View(model);
                }
                
                // Check if email verification is required
                if (model.RequireEmailVerification)
                {
                    _logger.LogInformation("Starting registration with email verification for {Email}", model.Email);
                    
                    // Generate 6-digit verification code
                    var verificationCode = new Random().Next(100000, 999999).ToString();
                    
                    // Store BOTH verification code AND registration data in cache (for 10 minutes)
                    var verificationCacheKey = $"email_verification_{model.Email}";
                    var registrationCacheKey = $"pending_registration_{model.Email}";
                    
                    await _cacheService.SetAsync(verificationCacheKey, verificationCode, TimeSpan.FromMinutes(10));
                    await _cacheService.SetAsync(registrationCacheKey, model, TimeSpan.FromMinutes(10));
                    
                    _logger.LogInformation("Stored registration data in cache for {Email}", model.Email);
                    
                    // Send verification code email with promotional content
                    var baseUrl = $"{Request.Scheme}://{Request.Host}";
                    var emailSent = await _emailService.SendEmailAsync(model.Email, "Xác thực tài khoản John Henry - Chào mừng bạn đến với thời trang hiện đại!",
                        $@"
                        <!DOCTYPE html>
                        <html>
                        <head>
                            <meta charset='utf-8'>
                            <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                            <style>
                                * {{ margin: 0; padding: 0; box-sizing: border-box; }}
                                body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; line-height: 1.6; color: #333; background: linear-gradient(135deg, #f5f7fa 0%, #c3cfe2 100%); padding: 20px; }}
                                .email-wrapper {{ max-width: 1500px; margin: 0 auto; background-color: #ffffff; border-radius: 16px; overflow: hidden; box-shadow: 0 10px 40px rgba(0,0,0,0.1); }}
                                
                                /* Header with brand */
                                .header {{ background: linear-gradient(135deg, #1a1a2e 0%, #16213e 100%); padding: 40px 30px; text-align: center; position: relative; overflow: hidden; }}
                                .header::before {{ content: ''; position: absolute; top: 0; right: -50px; width: 200px; height: 200px; background: rgba(255,255,255,0.05); border-radius: 50%; }}
                                .header::after {{ content: ''; position: absolute; bottom: -30px; left: -30px; width: 150px; height: 150px; background: rgba(255,255,255,0.03); border-radius: 50%; }}
                                .brand-logo {{ font-size: 32px; font-weight: 800; color: #ffffff; letter-spacing: 2px; margin-bottom: 8px; text-transform: uppercase; }}
                                .brand-tagline {{ color: rgba(255,255,255,0.85); font-size: 14px; font-weight: 300; letter-spacing: 1px; }}
                                
                                /* Main content area */
                                .content {{ padding: 40px 30px; }}
                                .greeting {{ font-size: 18px; color: #1a1a2e; margin-bottom: 20px; }}
                                .greeting strong {{ color: #dc3545; }}
                                .intro-text {{ color: #555; font-size: 15px; line-height: 1.8; margin-bottom: 30px; }}
                                
                                /* Verification section */
                                .verification-container {{ background: linear-gradient(135deg, #fff 0%, #f8f9fa 100%); border-radius: 12px; padding: 30px; margin: 30px 0; border: 2px solid #e9ecef; text-align: center; }}
                                .verification-label {{ color: #6c757d; font-size: 14px; font-weight: 600; text-transform: uppercase; letter-spacing: 1px; margin-bottom: 15px; }}
                                .verification-code {{ font-size: 42px; font-weight: 800; color: #dc3545; letter-spacing: 12px; font-family: 'Courier New', monospace; padding: 20px; background: white; border-radius: 8px; box-shadow: 0 4px 12px rgba(220,53,69,0.15); display: inline-block; margin: 10px 0; }}
                                .verification-note {{ color: #999; font-size: 13px; margin-top: 15px; }}
                                .verification-note strong {{ color: #dc3545; }}
                                
                                /* Promotional section with horizontal layout */
                                .promo-section {{ margin: 40px 0; }}
                                .promo-header {{ text-align: center; margin-bottom: 30px; }}
                                .promo-title {{ font-size: 24px; font-weight: 700; color: #1a1a2e; margin-bottom: 8px; letter-spacing: 1px; }}
                                .promo-subtitle {{ color: #6c757d; font-size: 14px; }}
                                
                                /* Horizontal product showcase - Full width image */
                                .product-showcase {{ margin: 35px 0; background: white; border-radius: 20px; overflow: hidden; box-shadow: 0 12px 40px rgba(0,0,0,0.15); }}
                                .showcase-image {{ width: 100%; }}
                                .showcase-image img {{ width: 100%; height: 100%; object-fit: cover; display: block; }}
                                .showcase-content {{ padding: 50px 60px; text-align: center; background: linear-gradient(180deg, #ffffff 0%, #f8f9fa 100%); }}
                                .showcase-heading {{ font-size: 36px; font-weight: 800; color: #1a1a2e; margin-bottom: 25px; line-height: 1.2; letter-spacing: -0.5px; }}
                                .showcase-text {{ color: #555; font-size: 18px; line-height: 1.9; margin: 0 auto 35px; max-width: 700px; }}
                                .cta-button {{ display: inline-block; background: linear-gradient(135deg, #dc3545 0%, #c82333 100%); color: white !important; padding: 20px 60px; text-decoration: none; border-radius: 12px; font-weight: 700; font-size: 19px; box-shadow: 0 8px 30px rgba(220,53,69,0.4); transition: all 0.3s ease; text-align: center; letter-spacing: 1px; text-transform: uppercase; }}
                                .cta-button:hover {{ transform: translateY(-3px); box-shadow: 0 12px 40px rgba(220,53,69,0.5); }}
                                
                                /* Features grid - 2x2 square layout */
                                .features-grid {{ display: grid; grid-template-columns: repeat(2, 1fr); gap: 25px; margin: 40px 0; }}
                                .feature-item {{ background: white; padding: 40px 25px; border-radius: 16px; text-align: center; border: 2px solid #f0f0f0; transition: all 0.3s ease; }}
                                .feature-item:hover {{ transform: translateY(-8px); box-shadow: 0 15px 40px rgba(0,0,0,0.12); border-color: #dc3545; }}
                                .feature-icon {{ width: 75px; height: 75px; margin: 0 auto 18px; background: linear-gradient(135deg, #dc3545 0%, #c82333 100%); border-radius: 50%; display: flex; align-items: center; justify-content: center; color: white; font-size: 34px; font-weight: bold; box-shadow: 0 6px 25px rgba(220,53,69,0.35); }}
                                .feature-title {{ font-weight: 700; color: #1a1a2e; font-size: 18px; margin-bottom: 8px; }}
                                .feature-desc {{ color: #6c757d; font-size: 15px; }}
                                
                                /* Offer banner */
                                .offer-banner {{ background: linear-gradient(135deg, #fff5f5 0%, #ffe5e5 100%); border: 2px dashed #dc3545; border-radius: 12px; padding: 25px; text-align: center; margin: 30px 0; }}
                                .offer-title {{ color: #dc3545; font-size: 20px; font-weight: 700; margin-bottom: 10px; }}
                                .offer-text {{ color: #555; font-size: 15px; line-height: 1.6; }}
                                .offer-text strong {{ color: #dc3545; font-size: 18px; }}
                                .offer-conditions {{ color: #999; font-size: 12px; margin-top: 8px; }}
                                
                                /* Divider */
                                .divider {{ height: 1px; background: linear-gradient(90deg, transparent 0%, #e9ecef 50%, transparent 100%); margin: 35px 0; }}
                                
                                /* Footer */
                                .footer {{ background: #f8f9fa; padding: 30px; text-align: center; border-top: 1px solid #e9ecef; }}
                                .footer-brand {{ font-weight: 700; color: #1a1a2e; font-size: 16px; margin-bottom: 8px; letter-spacing: 1px; }}
                                .footer-desc {{ color: #6c757d; font-size: 13px; margin-bottom: 15px; }}
                                .footer-links {{ margin: 20px 0; }}
                                .footer-links a {{ color: #dc3545; text-decoration: none; font-weight: 600; margin: 0 15px; font-size: 13px; }}
                                .footer-links a:hover {{ text-decoration: underline; }}
                                .footer-note {{ color: #999; font-size: 12px; margin-top: 15px; line-height: 1.6; }}
                                
                                /* Responsive */
                                @media only screen and (max-width: 600px) {{
                                    .email-wrapper {{ margin: 0; border-radius: 0; }}
                                    .content {{ padding: 25px 20px; }}
                                    .showcase-image img {{ height: 320px; }}
                                    .showcase-content {{ padding: 35px 25px; }}
                                    .showcase-heading {{ font-size: 26px; }}
                                    .showcase-text {{ font-size: 16px; }}
                                    .cta-button {{ padding: 16px 40px; font-size: 17px; }}
                                    .features-grid {{ grid-template-columns: 1fr; gap: 15px; }}
                                    .feature-item {{ padding: 30px 20px; }}
                                    .feature-icon {{ width: 65px; height: 65px; font-size: 28px; }}
                                    .feature-title {{ font-size: 16px; }}
                                    .verification-code {{ font-size: 32px; letter-spacing: 8px; }}
                                }}
                            </style>
                        </head>
                        <body>
                            <div class='email-wrapper'>
                                <!-- Header -->
                                <div class='header'>
                                    <div class='brand-logo'>JOHN HENRY</div>
                                    <div class='brand-tagline'>Modern Fashion & Lifestyle</div>
                                </div>

                                <!-- Main Content -->
                                <div class='content'>
                                    <div class='greeting'>Xin chào <strong>{model.FirstName} {model.LastName}</strong>,</div>
                                    <p class='intro-text'>Cảm ơn bạn đã tin tưởng và đăng ký tài khoản tại John Henry. Chúng tôi rất hân hạnh được đồng hành cùng bạn trên hành trình khám phá phong cách thời trang hiện đại.</p>
                                    
                                    <!-- Verification Code -->
                                    <div class='verification-container'>
                                        <div class='verification-label'>Mã xác thực của bạn</div>
                                        <div class='verification-code'>{verificationCode}</div>
                                        <div class='verification-note'>Mã này có hiệu lực trong <strong>10 phút</strong></div>
                                    </div>

                                    <div class='divider'></div>

                                    <!-- Promotional Section -->
                                    <div class='promo-section'>
                                        <div class='promo-header'>
                                            <div class='promo-title'>KHÁM PHÁ BỘ SƯU TẬP MỚI NHẤT</div>
                                            <div class='promo-subtitle'>Thiết kế độc đáo - Chất liệu cao cấp - Phong cách hiện đại</div>
                                        </div>
                                        
                                        <!-- Product Showcase -->
                                        <div class='product-showcase'>
                                            <div class='showcase-image'>
                                                <img src='https://raw.githubusercontent.com/InfinityZero3000/Image-CDN/refs/heads/main/banner_8fa38793.jpg' alt='John Henry Collection' />
                                            </div>
                                            <div class='showcase-content'>
                                                <div class='showcase-heading'>Bộ Sưu Tập Thời Trang Nam Nữ Cao Cấp</div>
                                                <div class='showcase-text'>Từ áo sơ mi lịch lãm đến quần kaki thanh lịch, từ váy đầm sang trọng đến phụ kiện tinh tế. John Henry mang đến cho bạn những sản phẩm hoàn hảo cho mọi dịp, giúp bạn tự tin thể hiện phong cách riêng.</div>
                                                <div>
                                                    <a href='{baseUrl}/products' class='cta-button'>Xem Bộ Sưu Tập</a>
                                                </div>
                                            </div>
                                        </div>

                                        <!-- Features - Flex Grid Layout -->
                                        <table role='presentation' cellpadding='0' cellspacing='0' style='width: 100%; margin: 40px 0;'>
                                            <tr>
                                                <td style='padding: 0;'>
                                                    <table role='presentation' cellpadding='0' cellspacing='0' style='width: 100%; border-collapse: collapse;'>
                                                        <!-- Row 1 -->
                                                        <tr>
                                                            <td style='width: 50%; padding: 12.5px; vertical-align: top;'>
                                                                <div class='feature-item' style='padding: 40px 25px; background: white; border-radius: 16px; text-align: center; border: 2px solid #f0f0f0; height: 100%; box-sizing: border-box;'>
                                                                    <div class='feature-icon' style='width: 75px; height: 75px; margin: 0 auto 18px; background: linear-gradient(135deg, #dc3545 0%, #c82333 100%); border-radius: 50%; color: white; font-size: 34px; font-weight: bold; box-shadow: 0 6px 25px rgba(220,53,69,0.35); line-height: 75px; text-align: center;'>★</div>
                                                                    <div class='feature-title' style='font-weight: 700; color: #1a1a2e; font-size: 18px; margin-bottom: 8px;'>Thiết Kế Độc Đáo</div>
                                                                    <div class='feature-desc' style='color: #6c757d; font-size: 15px;'>Phong cách hiện đại</div>
                                                                </div>
                                                            </td>
                                                            <td style='width: 50%; padding: 12.5px; vertical-align: top;'>
                                                                <div class='feature-item' style='padding: 40px 25px; background: white; border-radius: 16px; text-align: center; border: 2px solid #f0f0f0; height: 100%; box-sizing: border-box;'>
                                                                    <div class='feature-icon' style='width: 75px; height: 75px; margin: 0 auto 18px; background: linear-gradient(135deg, #dc3545 0%, #c82333 100%); border-radius: 50%; color: white; font-size: 34px; font-weight: bold; box-shadow: 0 6px 25px rgba(220,53,69,0.35); line-height: 75px; text-align: center;'>✓</div>
                                                                    <div class='feature-title' style='font-weight: 700; color: #1a1a2e; font-size: 18px; margin-bottom: 8px;'>Chất Lượng Cao</div>
                                                                    <div class='feature-desc' style='color: #6c757d; font-size: 15px;'>Vải cao cấp thoải mái</div>
                                                                </div>
                                                            </td>
                                                        </tr>
                                                        <!-- Row 2 -->
                                                        <tr>
                                                            <td style='width: 50%; padding: 12.5px; vertical-align: top;'>
                                                                <div class='feature-item' style='padding: 40px 25px; background: white; border-radius: 16px; text-align: center; border: 2px solid #f0f0f0; height: 100%; box-sizing: border-box;'>
                                                                    <div class='feature-icon' style='width: 75px; height: 75px; margin: 0 auto 18px; background: linear-gradient(135deg, #dc3545 0%, #c82333 100%); border-radius: 50%; color: white; font-size: 34px; font-weight: bold; box-shadow: 0 6px 25px rgba(220,53,69,0.35); line-height: 75px; text-align: center;'>⚡</div>
                                                                    <div class='feature-title' style='font-weight: 700; color: #1a1a2e; font-size: 18px; margin-bottom: 8px;'>Giao Hàng Nhanh</div>
                                                                    <div class='feature-desc' style='color: #6c757d; font-size: 15px;'>Toàn quốc 24-48h</div>
                                                                </div>
                                                            </td>
                                                            <td style='width: 50%; padding: 12.5px; vertical-align: top;'>
                                                                <div class='feature-item' style='padding: 40px 25px; background: white; border-radius: 16px; text-align: center; border: 2px solid #f0f0f0; height: 100%; box-sizing: border-box;'>
                                                                    <div class='feature-icon' style='width: 75px; height: 75px; margin: 0 auto 18px; background: linear-gradient(135deg, #dc3545 0%, #c82333 100%); border-radius: 50%; color: white; font-size: 34px; font-weight: bold; box-shadow: 0 6px 25px rgba(220,53,69,0.35); line-height: 75px; text-align: center;'>↻</div>
                                                                    <div class='feature-title' style='font-weight: 700; color: #1a1a2e; font-size: 18px; margin-bottom: 8px;'>Đổi Trả Dễ Dàng</div>
                                                                    <div class='feature-desc' style='color: #6c757d; font-size: 15px;'>Trong vòng 7 ngày</div>
                                                                </div>
                                                            </td>
                                                        </tr>
                                                    </table>
                                                </td>
                                            </tr>
                                        </table>

                                        <!-- Offer Banner -->
                                        <div class='offer-banner'>
                                            <div class='offer-title'>ƯU ĐÃI ĐẶC BIỆT CHO THÀNH VIÊN MỚI</div>
                                            <div class='offer-text'>
                                                Nhận ngay <strong>VOUCHER GIẢM 10%</strong> cho đơn hàng đầu tiên
                                            </div>
                                            <div class='offer-conditions'>Áp dụng cho đơn hàng từ 500.000đ</div>
                                        </div>
                                    </div>

                                    <div class='divider'></div>

                                    <p style='color: #999; font-size: 13px; text-align: center;'>
                                        Nếu bạn không thực hiện đăng ký này, vui lòng bỏ qua email này.
                                    </p>
                                </div>

                                <!-- Footer -->
                                <div class='footer'>
                                    <div class='footer-brand'>JOHN HENRY FASHION</div>
                                    <div class='footer-desc'>Thời trang nam nữ cao cấp - Phong cách hiện đại</div>
                                    <div class='footer-links'>
                                        <a href='{baseUrl}'>Trang Chủ</a>
                                        <a href='{baseUrl}/'>Sản Phẩm</a>
                                        <a href='{baseUrl}/contact'>Liên Hệ</a>
                                    </div>
                                    <div class='footer-note'>
                                        Email này được gửi tự động, vui lòng không trả lời trực tiếp.<br>
                                        © 2025 John Henry. All rights reserved.
                                    </div>
                                </div>
                            </div>
                        </body>
                        </html>
                        ", isHtml: true);

                    if (!emailSent)
                    {
                        _logger.LogError("Failed to send verification email to {Email}", model.Email);
                        ModelState.AddModelError("", "Không thể gửi email xác thực. Vui lòng thử lại sau.");
                        return View(model);
                    }
                    
                    _logger.LogInformation("Verification email sent to {Email}", model.Email);

                    // Redirect to email verification page (user NOT created yet)
                    return RedirectToAction("EmailVerification", new { email = model.Email, returnUrl });
                }
                else
                {
                    // Original flow with email confirmation link - create user immediately
                    var user = new ApplicationUser
                    {
                        UserName = model.Email,
                        Email = model.Email,
                        FirstName = model.FirstName,
                        LastName = model.LastName,
                        PhoneNumber = model.PhoneNumber,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    var result = await _userManager.CreateAsync(user, model.Password);
                    
                    if (result.Succeeded)
                    {
                        _logger.LogInformation("User created a new account with password.");
                        
                        var code = await _authService.GenerateEmailConfirmationTokenAsync(user);
                        var callbackUrl = Url.Action("ConfirmEmail", "Account",
                            new { userId = user.Id, code }, Request.Scheme);

                        await _emailService.SendEmailAsync(user.Email, "Xác nhận tài khoản",
                            $"Vui lòng xác nhận tài khoản của bạn bằng cách click vào link: <a href='{callbackUrl}'>Xác nhận email</a>", isHtml: true);

                        await _userManager.AddToRoleAsync(user, "Customer");

                        ViewBag.Message = "Tài khoản đã được tạo thành công. Vui lòng kiểm tra email để xác nhận tài khoản.";
                        return View("RegisterConfirmation");
                    }
                    
                    AddErrors(result);
                }
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            _logger.LogInformation("User logged out.");
            
            // Redirect to login page after logout
            return RedirectToAction(nameof(Login), "Account");
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public IActionResult ExternalLogin(string provider, string? returnUrl = null)
        {
            var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Account", new { returnUrl });
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            return Challenge(properties, provider);
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ExternalLoginCallback(string? returnUrl = null, string? remoteError = null)
        {
            _logger.LogInformation("=== GOOGLE LOGIN START ===");
            
            // Bước 1: Kiểm tra lỗi từ Google
            if (remoteError != null)
            {
                _logger.LogError("Google trả về lỗi: {Error}", remoteError);
                TempData["ErrorMessage"] = $"Lỗi từ Google: {remoteError}";
                return RedirectToAction(nameof(Login));
            }

            // Bước 2: Lấy thông tin từ Google
            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                _logger.LogError("Không lấy được thông tin từ Google (info = null)");
                TempData["ErrorMessage"] = "Không thể lấy thông tin từ Google. Vui lòng thử lại.";
                return RedirectToAction(nameof(Login));
            }

            // Bước 3: Lấy email từ Google
            var email = info.Principal.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrEmpty(email))
            {
                _logger.LogError("Google không trả về email");
                TempData["ErrorMessage"] = "Không thể lấy email từ tài khoản Google.";
                return RedirectToAction(nameof(Login));
            }

            _logger.LogInformation("Email từ Google: {Email}", email);

            // Bước 4: Kiểm tra user đã tồn tại chưa
            var user = await _userManager.FindByEmailAsync(email);
            
            if (user == null)
            {
                // Tạo user mới
                _logger.LogInformation("Tạo user mới cho email: {Email}", email);
                
                var firstName = info.Principal.FindFirstValue(ClaimTypes.GivenName) ?? "";
                var lastName = info.Principal.FindFirstValue(ClaimTypes.Surname) ?? "";
                
                user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    FirstName = firstName,
                    LastName = lastName,
                    EmailConfirmed = true, // Google đã xác thực email rồi
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                var createResult = await _userManager.CreateAsync(user);
                if (!createResult.Succeeded)
                {
                    _logger.LogError("Không tạo được user: {Errors}", string.Join(", ", createResult.Errors.Select(e => e.Description)));
                    TempData["ErrorMessage"] = "Không thể tạo tài khoản. Vui lòng thử lại.";
                    return RedirectToAction(nameof(Login));
                }

                // Thêm role Customer
                await _userManager.AddToRoleAsync(user, "Customer");
                _logger.LogInformation("Đã tạo user và thêm role Customer");
            }
            else
            {
                _logger.LogInformation("User đã tồn tại: {Email}", email);
                
                // Đảm bảo EmailConfirmed = true
                if (!user.EmailConfirmed)
                {
                    user.EmailConfirmed = true;
                    await _userManager.UpdateAsync(user);
                    _logger.LogInformation("Đã cập nhật EmailConfirmed = true");
                }
            }

            // Bước 5: Link Google login với user (nếu chưa link)
            var existingLogins = await _userManager.GetLoginsAsync(user);
            if (!existingLogins.Any(l => l.LoginProvider == info.LoginProvider && l.ProviderKey == info.ProviderKey))
            {
                var addLoginResult = await _userManager.AddLoginAsync(user, info);
                if (!addLoginResult.Succeeded)
                {
                    _logger.LogError("Không thể link Google login: {Errors}", string.Join(", ", addLoginResult.Errors.Select(e => e.Description)));
                }
                else
                {
                    _logger.LogInformation("Đã link Google login với user");
                }
            }

            // Bước 6: Đăng nhập user
            await _signInManager.SignInAsync(user, isPersistent: true);
            _logger.LogInformation("Đã đăng nhập user: {Email}", email);

            // Bước 7: Redirect về trang chủ
            _logger.LogInformation("=== GOOGLE LOGIN SUCCESS ===");
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Lockout()
        {
            return View();
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult UnlockAdmin()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnlockAdmin(UnlockAdminViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Verify secret key
            var adminUnlockSecret = _configuration["Security:AdminUnlockSecret"];
            if (string.IsNullOrEmpty(adminUnlockSecret) || model.SecretKey != adminUnlockSecret)
            {
                ModelState.AddModelError(string.Empty, "Mã bảo mật không đúng.");
                _logger.LogWarning("Failed admin unlock attempt with invalid secret key for email: {Email}", model.Email);
                return View(model);
            }

            // Find user by email
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Không tìm thấy tài khoản với email này.");
                return View(model);
            }

            // Check if user is admin
            var roles = await _userManager.GetRolesAsync(user);
            if (!roles.Contains(UserRoles.Admin))
            {
                ModelState.AddModelError(string.Empty, "Tài khoản này không phải là Admin. Chỉ có thể mở khóa tài khoản Admin.");
                _logger.LogWarning("Attempted to unlock non-admin account: {Email}", model.Email);
                return View(model);
            }

            // Check if account is locked
            var isLockedOut = await _userManager.IsLockedOutAsync(user);
            if (!isLockedOut)
            {
                ModelState.AddModelError(string.Empty, "Tài khoản này không bị khóa.");
                return View(model);
            }

            // Unlock the account
            var unlockResult = await _userManager.SetLockoutEndDateAsync(user, null);
            if (!unlockResult.Succeeded)
            {
                ModelState.AddModelError(string.Empty, "Có lỗi xảy ra khi mở khóa tài khoản.");
                _logger.LogError("Failed to unlock admin account: {Email}, Errors: {Errors}", 
                    model.Email, string.Join(", ", unlockResult.Errors.Select(e => e.Description)));
                return View(model);
            }

            // Reset failed access attempts
            var resetResult = await _userManager.ResetAccessFailedCountAsync(user);
            if (!resetResult.Succeeded)
            {
                _logger.LogWarning("Failed to reset access failed count for user: {Email}", model.Email);
            }

            // Log the unlock action
            _logger.LogInformation("Admin account unlocked via emergency unlock: {Email}, IP: {IpAddress}", 
                model.Email, HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["SuccessMessage"] = "Mở khóa tài khoản Admin thành công! Bạn có thể đăng nhập ngay bây giờ.";
            return RedirectToAction(nameof(Login));
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> LoginWith2fa(bool rememberMe, string? returnUrl = null)
        {
            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();

            if (user == null)
            {
                throw new ApplicationException($"Unable to load two-factor authentication user.");
            }

            var model = new TwoFactorAuthenticationViewModel 
            { 
                RememberMe = rememberMe,
                ReturnUrl = returnUrl,
                Provider = "Email" // Default provider
            };
            ViewData["ReturnUrl"] = returnUrl;

            return View(model);
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyTwoFactorLogin(TwoFactorAuthenticationViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("LoginWith2fa", model);
            }

            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
            {
                ModelState.AddModelError("", "Có lỗi xảy ra khi xác thực.");
                return View("LoginWith2fa", model);
            }

            var result = await _signInManager.TwoFactorSignInAsync(model.Provider ?? "Email", model.Code, 
                model.RememberMe, model.RememberMachine);

            if (result.Succeeded)
            {
                await _securityService.RecordLoginAttemptAsync(user.Id, Request.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "", true);
                
                if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                {
                    return Redirect(model.ReturnUrl);
                }
                return RedirectToAction("Index", "Home");
            }

            if (result.IsLockedOut)
            {
                ModelState.AddModelError("", "Tài khoản đã bị khóa do quá nhiều lần đăng nhập sai.");
            }
            else
            {
                ModelState.AddModelError("", "Mã xác thực không chính xác.");
                await _securityService.RecordLoginAttemptAsync(user.Id, Request.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "", false);
            }

            return View("LoginWith2fa", model);
        }

        // Redirect to UserDashboard Profile (main profile page)
        [HttpGet]
        [Authorize]
        public IActionResult Profile(string? tab = null)
        {
            return RedirectToAction("Profile", "UserDashboard", new { tab });
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public IActionResult Profile(ProfileViewModel model)
        {
            return RedirectToAction("Profile", "UserDashboard");
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> ChangePassword()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            var hasPassword = await _userManager.HasPasswordAsync(user);
            if (!hasPassword)
            {
                return RedirectToAction(nameof(SetPassword));
            }

            // Pass user info to ViewBag for sidebar
            ViewBag.UserFullName = $"{user.FirstName} {user.LastName}".Trim();
            ViewBag.UserAvatar = user.Avatar;

            return View();
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            var changePasswordResult = await _userManager.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);
            if (!changePasswordResult.Succeeded)
            {
                AddErrors(changePasswordResult);
                return View(model);
            }

            await _signInManager.RefreshSignInAsync(user);
            _logger.LogInformation("User changed their password successfully.");
            TempData["StatusMessage"] = "Mật khẩu của bạn đã được thay đổi thành công.";

            return RedirectToAction(nameof(ChangePassword));
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> SetPassword()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            var hasPassword = await _userManager.HasPasswordAsync(user);
            if (hasPassword)
            {
                return RedirectToAction(nameof(ChangePassword));
            }

            return View();
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetPassword(SetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            var addPasswordResult = await _userManager.AddPasswordAsync(user, model.NewPassword);
            if (!addPasswordResult.Succeeded)
            {
                AddErrors(addPasswordResult);
                return View(model);
            }

            await _signInManager.RefreshSignInAsync(user);
            TempData["StatusMessage"] = "Mật khẩu của bạn đã được thiết lập thành công.";

            return RedirectToAction(nameof(SetPassword));
        }

        [HttpGet]
        [Authorize]
        public IActionResult Orders()
        {
            // Redirect to Profile page with orders tab
            return RedirectToAction("Profile", new { tab = "orders" });
        }

        #region Helpers

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        private async Task<IActionResult> RedirectToLocal(string? returnUrl, ApplicationUser? user = null)
        {
            // Check user roles first, regardless of returnUrl for admin/seller users
            var currentUser = user ?? await _userManager.GetUserAsync(User);
            if (currentUser != null)
            {
                var roles = await _userManager.GetRolesAsync(currentUser);
                _logger.LogInformation($"User {currentUser.Email} has roles: {string.Join(", ", roles)}");
                
                // Admin and Seller should always go to their dashboards
                if (roles.Contains(UserRoles.Admin))
                {
                    _logger.LogInformation($"Redirecting admin user {currentUser.Email} to admin dashboard");
                    return RedirectToAction("Dashboard", "Admin", new { area = "" });
                }
                else if (roles.Contains(UserRoles.Seller))
                {
                    _logger.LogInformation($"Redirecting seller user {currentUser.Email} to seller dashboard");
                    return RedirectToAction("Dashboard", "Seller");
                }
                
                // For customers, check returnUrl first before redirecting to home
                if (roles.Contains(UserRoles.Customer) || roles.Count == 0)
                {
                    // If there's a valid returnUrl, use it for customers
                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    {
                        _logger.LogInformation($"Redirecting customer user {currentUser.Email} to returnUrl: {returnUrl}");
                        return Redirect(returnUrl);
                    }
                    
                    // Otherwise redirect to home page
                    _logger.LogInformation($"Redirecting customer user {currentUser.Email} to home page");
                    return RedirectToAction("Index", "Home");
                }
            }
            
            // For other cases, check returnUrl
            if (Url.IsLocalUrl(returnUrl))
            {
                _logger.LogInformation($"Redirecting to returnUrl: {returnUrl}");
                return Redirect(returnUrl);
            }
            
            // Default redirect to home page for authenticated users, home for others
            if (User.Identity?.IsAuthenticated == true)
            {
                _logger.LogInformation("Redirecting authenticated user to home page");
                return RedirectToAction("Index", "Home");
            }
            
            _logger.LogInformation("Redirecting to home page");
            return RedirectToAction(nameof(HomeController.Index), "Home");
        }

        [TempData]
        public string? ErrorMessage { get; set; }

        #region Address Management
        [Authorize]
        public async Task<IActionResult> Addresses()
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null)
            {
                return RedirectToAction("Login");
            }

            var addresses = await _context.Addresses
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.IsDefault)
                .ThenByDescending(a => a.CreatedAt)
                .ToListAsync();

            return View(addresses);
        }

        [Authorize]
        [HttpGet]
        public IActionResult AddAddress(string? returnUrl = null)
        {
            // Lưu lại trang trước đó để sau khi lưu địa chỉ sẽ quay lại đúng nơi
            if (string.IsNullOrEmpty(returnUrl))
            {
                // Ưu tiên query string, nếu không có thì dùng Referer
                returnUrl = Request.Query["returnUrl"].ToString();
                if (string.IsNullOrEmpty(returnUrl))
                {
                    returnUrl = Request.Headers["Referer"].ToString();
                }
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View(new Address());
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddAddress(Address model, string? returnUrl = null)
        {
            // Remove navigation property validation - these are not bound from form
            ModelState.Remove("User");
            ModelState.Remove("UserId");
            
            // Log validation errors for debugging
            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(x => x.Value?.Errors.Count > 0)
                    .Select(x => new { Field = x.Key, Errors = x.Value?.Errors.Select(e => e.ErrorMessage).ToList() })
                    .ToList();
                
                _logger.LogWarning("AddAddress validation failed: {Errors}", 
                    System.Text.Json.JsonSerializer.Serialize(errors));
                
                // Remove validation errors for optional fields
                ModelState.Remove("PostalCode");
                ModelState.Remove("Company");
                ModelState.Remove("Address2");
                ModelState.Remove("Phone");
            }
            
            // Check required fields manually
            if (string.IsNullOrWhiteSpace(model.FirstName) ||
                string.IsNullOrWhiteSpace(model.LastName) ||
                string.IsNullOrWhiteSpace(model.Address1) ||
                string.IsNullOrWhiteSpace(model.City))
            {
                ModelState.AddModelError("", "Vui lòng điền đầy đủ thông tin bắt buộc");
                ViewData["ReturnUrl"] = returnUrl;
                return View(model);
            }
            
            var userId = _userManager.GetUserId(User);
            if (userId == null)
            {
                return RedirectToAction("Login");
            }

            try
            {
                model.UserId = userId;
                model.Id = Guid.NewGuid();
                model.CreatedAt = DateTime.UtcNow;
                model.UpdatedAt = DateTime.UtcNow;
                
                // Set default values for optional fields
                model.PostalCode = model.PostalCode ?? "";
                model.State = model.State ?? "";
                model.Country = model.Country ?? "Vietnam";

                // If this is the first address, make it default
                var existingAddresses = await _context.Addresses.Where(a => a.UserId == userId).CountAsync();
                if (existingAddresses == 0)
                {
                    model.IsDefault = true;
                }

                _context.Addresses.Add(model);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Địa chỉ đã được thêm thành công!";

                // Nếu có returnUrl hợp lệ thì quay về đúng trang trước đó
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }

                // Mặc định quay về danh sách địa chỉ
                return RedirectToAction("Addresses");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving address for user {UserId}", userId);
                ModelState.AddModelError("", "Có lỗi xảy ra khi lưu địa chỉ. Vui lòng thử lại.");
                ViewData["ReturnUrl"] = returnUrl;
                return View(model);
            }
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> SaveAddress([FromBody] SaveAddressRequest request)
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                if (userId == null)
                {
                    return Json(new { success = false, message = "Vui lòng đăng nhập để lưu địa chỉ" });
                }

                // Validate request
                if (string.IsNullOrWhiteSpace(request.FullName) ||
                    string.IsNullOrWhiteSpace(request.PhoneNumber) ||
                    string.IsNullOrWhiteSpace(request.Address) ||
                    string.IsNullOrWhiteSpace(request.City))
                {
                    return Json(new { success = false, message = "Vui lòng điền đầy đủ thông tin địa chỉ" });
                }

                // Parse full name into first and last name
                var nameParts = request.FullName.Trim().Split(' ');
                var firstName = nameParts.Length > 0 ? nameParts[0] : request.FullName;
                var lastName = nameParts.Length > 1 ? string.Join(" ", nameParts.Skip(1)) : "";

                // Check if address already exists (same details)
                var existingAddress = await _context.Addresses
                    .FirstOrDefaultAsync(a => 
                        a.UserId == userId &&
                        a.FirstName == firstName &&
                        a.LastName == lastName &&
                        a.Phone == request.PhoneNumber &&
                        a.Address1 == request.Address &&
                        a.City == request.City &&
                        a.State == request.District);

                if (existingAddress != null)
                {
                    // Update existing address
                    existingAddress.IsDefault = request.IsDefault;
                    existingAddress.UpdatedAt = DateTime.UtcNow;
                    
                    if (request.IsDefault)
                    {
                        // Remove default from other addresses
                        var otherAddresses = await _context.Addresses
                            .Where(a => a.UserId == userId && a.Id != existingAddress.Id)
                            .ToListAsync();
                        
                        foreach (var addr in otherAddresses)
                        {
                            addr.IsDefault = false;
                        }
                    }
                    
                    await _context.SaveChangesAsync();
                    return Json(new { success = true, message = "Địa chỉ đã tồn tại và được cập nhật" });
                }

                // Create new address
                var newAddress = new Address
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    Type = "shipping",
                    FirstName = firstName,
                    LastName = lastName,
                    Phone = request.PhoneNumber,
                    Address1 = request.Address,
                    Address2 = !string.IsNullOrWhiteSpace(request.Ward) ? $"{request.Ward}, {request.District}" : request.District,
                    State = request.District ?? "",
                    City = request.City,
                    PostalCode = request.PostalCode ?? "",
                    Country = "Vietnam",
                    IsDefault = request.IsDefault,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                // If this is the first address, make it default
                var existingAddressesCount = await _context.Addresses
                    .Where(a => a.UserId == userId)
                    .CountAsync();
                
                if (existingAddressesCount == 0)
                {
                    newAddress.IsDefault = true;
                }
                else if (request.IsDefault)
                {
                    // Remove default from other addresses
                    var otherAddresses = await _context.Addresses
                        .Where(a => a.UserId == userId)
                        .ToListAsync();
                    
                    foreach (var addr in otherAddresses)
                    {
                        addr.IsDefault = false;
                    }
                }

                _context.Addresses.Add(newAddress);
                await _context.SaveChangesAsync();

                return Json(new { 
                    success = true, 
                    message = "Địa chỉ đã được lưu thành công",
                    addressId = newAddress.Id
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving address");
                return Json(new { success = false, message = "Có lỗi xảy ra khi lưu địa chỉ" });
            }
        }

        // Request model for SaveAddress
        public class SaveAddressRequest
        {
            public string FullName { get; set; } = string.Empty;
            public string PhoneNumber { get; set; } = string.Empty;
            public string Address { get; set; } = string.Empty;
            public string? Ward { get; set; }
            public string? District { get; set; }
            public string City { get; set; } = string.Empty;
            public string? PostalCode { get; set; }
            public bool IsDefault { get; set; } = false;
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> EditAddress(Guid id, string? returnUrl = null)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null)
            {
                return RedirectToAction("Login");
            }

            var address = await _context.Addresses
                .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);

            if (address == null)
            {
                return NotFound();
            }

            // Lưu lại trang trước đó (ưu tiên query string > Referer)
            if (string.IsNullOrEmpty(returnUrl))
            {
                returnUrl = Request.Query["returnUrl"].ToString();
                if (string.IsNullOrEmpty(returnUrl))
                {
                    returnUrl = Request.Headers["Referer"].ToString();
                }
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View(address);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditAddress(Guid id, Address model, string? returnUrl = null)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null)
            {
                return RedirectToAction("Login");
            }

            // Remove validation for navigation properties that shouldn't be validated
            ModelState.Remove("User");
            ModelState.Remove("UserId");
            
            // Remove validation for optional fields
            ModelState.Remove("PostalCode");
            ModelState.Remove("Company");
            ModelState.Remove("Address2");
            ModelState.Remove("Phone");
            
            // Log ModelState errors for debugging
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Where(x => x.Value?.Errors.Count > 0)
                    .Select(x => new { Field = x.Key, Errors = x.Value?.Errors.Select(e => e.ErrorMessage) })
                    .ToList();
                _logger.LogWarning("EditAddress ModelState invalid: {@Errors}", errors);
                
                // Return view with model to show validation errors
                ViewData["ReturnUrl"] = returnUrl;
                return View(model);
            }
            
            // Manual validation for required fields
            if (string.IsNullOrWhiteSpace(model.City))
            {
                ModelState.AddModelError("City", "Vui lòng chọn tỉnh/thành phố");
            }
            
            if (string.IsNullOrWhiteSpace(model.State))
            {
                ModelState.AddModelError("State", "Vui lòng chọn quận/huyện và phường/xã");
            }
            
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("EditAddress manual validation failed");
                ViewData["ReturnUrl"] = returnUrl;
                return View(model);
            }

            var address = await _context.Addresses
                .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);

            if (address == null)
            {
                return NotFound();
            }

            address.FirstName = model.FirstName;
            address.LastName = model.LastName;
            address.Company = model.Company;
            address.Address1 = model.Address1;
            address.Address2 = model.Address2;
            address.City = model.City;
            address.State = model.State;
            address.PostalCode = string.IsNullOrWhiteSpace(model.PostalCode) ? "00000" : model.PostalCode;
            address.Country = model.Country;
            address.Phone = model.Phone;
            address.UpdatedAt = DateTime.UtcNow;

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Address {AddressId} updated successfully for user {UserId}", id, userId);
                TempData["SuccessMessage"] = "Địa chỉ đã được cập nhật thành công!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating address {AddressId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi cập nhật địa chỉ.";
                ViewData["ReturnUrl"] = returnUrl;
                return View(model);
            }

            // Nếu có returnUrl hợp lệ thì quay về đúng trang trước đó
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            // Mặc định quay về danh sách địa chỉ
            return RedirectToAction("Addresses");
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> DeleteAddress(Guid id)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null)
            {
                return Json(new { success = false, message = "Chưa đăng nhập" });
            }

            var address = await _context.Addresses
                .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);

            if (address == null)
            {
                return Json(new { success = false, message = "Không tìm thấy địa chỉ" });
            }

            if (address.IsDefault)
            {
                return Json(new { success = false, message = "Không thể xóa địa chỉ mặc định" });
            }

            _context.Addresses.Remove(address);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Xóa địa chỉ thành công!" });
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> SetDefaultAddress(Guid id)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null)
            {
                return Json(new { success = false, message = "Chưa đăng nhập" });
            }

            var address = await _context.Addresses
                .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);

            if (address == null)
            {
                return Json(new { success = false, message = "Không tìm thấy địa chỉ" });
            }

            // Reset all addresses to non-default
            var userAddresses = await _context.Addresses
                .Where(a => a.UserId == userId)
                .ToListAsync();

            foreach (var addr in userAddresses)
            {
                addr.IsDefault = false;
            }

            // Set selected address as default
            address.IsDefault = true;
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Đã đặt làm địa chỉ mặc định!" });
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Security()
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null)
            {
                return RedirectToAction("Login");
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return RedirectToAction("Login");
            }

            var securityCheck = await _securityService.CheckAccountSecurityAsync(userId);
            var activeSessions = await _securityService.GetActiveSessionsAsync(userId);
            var securityLogs = await _securityService.GetSecurityLogsAsync(userId, 20);

            var model = new SecurityDashboardViewModel
            {
                SecurityScore = securityCheck.SecurityScore,
                IsTwoFactorEnabled = await _userManager.GetTwoFactorEnabledAsync(user),
                ActiveSessions = activeSessions.Select(s => new ActiveSessionViewModel
                {
                    SessionId = s.SessionId,
                    DeviceType = s.DeviceType ?? "Unknown Device",
                    Location = s.Location ?? "Unknown Location",
                    LastActivity = s.LastActivity,
                    IsCurrentSession = s.SessionId == HttpContext.Session.Id
                }).ToList(),
                SecurityLogs = securityLogs.Select(log => new SecurityLogViewModel
                {
                    Action = log.EventType,
                    Timestamp = log.CreatedAt,
                    IpAddress = log.IpAddress ?? "Unknown",
                    IsSuccessful = log.Description?.Contains("success") == true,
                    Details = log.Description
                }).ToList()
            };

            return View(model);
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> TwoFactorVerification(string userId, string provider, bool rememberMe = false, string? returnUrl = null)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return RedirectToAction("Login");
            }

            var model = new TwoFactorVerificationViewModel
            {
                UserId = userId,
                Provider = provider,
                RememberMe = rememberMe,
                ReturnUrl = returnUrl
            };

            ViewBag.MaskedEmail = MaskEmail(user.Email ?? "");
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyTwoFactor(TwoFactorVerificationViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.MaskedEmail = "***@***.com";
                return View(model);
            }

            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null)
            {
                ModelState.AddModelError("", "Có lỗi xảy ra khi xác thực.");
                return View(model);
            }

            var result = await _signInManager.TwoFactorSignInAsync(model.Provider, model.Code, 
                model.RememberMe, rememberClient: false);

            if (result.Succeeded)
            {
                await _securityService.RecordLoginAttemptAsync(user.Id, Request.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "", true);
                
                if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                {
                    return Redirect(model.ReturnUrl);
                }
                return RedirectToAction("Index", "Home");
            }

            if (result.IsLockedOut)
            {
                ModelState.AddModelError("", "Tài khoản đã bị khóa do quá nhiều lần đăng nhập sai.");
            }
            else
            {
                ModelState.AddModelError("", "Mã xác thực không chính xác.");
                await _securityService.RecordLoginAttemptAsync(user.Id, Request.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "", false);
            }

            ViewBag.MaskedEmail = MaskEmail(user.Email ?? "");
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResendTwoFactorCode(string userId, string provider)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return Json(new { success = false, message = "Người dùng không tồn tại." });
            }

            var code = await _userManager.GenerateTwoFactorTokenAsync(user, provider);
            
            // Send code via email
            await _emailService.SendTwoFactorCodeEmailAsync(user.Email ?? "", code);

            return Json(new { success = true, message = "Mã xác thực đã được gửi lại." });
        }

        // Email Verification with code
        [HttpGet]
        [AllowAnonymous]
        public IActionResult EmailVerification(string email, string? returnUrl = null)
        {
            var model = new EmailVerificationViewModel
            {
                Email = email,
                ReturnUrl = returnUrl,
                CodeSentTime = DateTime.UtcNow
            };

            return View(model);
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EmailVerification(EmailVerificationViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Get stored verification code from cache
            var verificationCacheKey = $"email_verification_{model.Email}";
            var storedCode = await _cacheService.GetAsync<string>(verificationCacheKey);

            if (string.IsNullOrEmpty(storedCode))
            {
                ModelState.AddModelError("", "Mã xác thực đã hết hạn. Vui lòng yêu cầu gửi lại mã mới.");
                return View(model);
            }

            if (storedCode != model.Code)
            {
                ModelState.AddModelError("Code", "Mã xác thực không chính xác.");
                return View(model);
            }

            // Get registration data from cache
            var registrationCacheKey = $"pending_registration_{model.Email}";
            var registrationData = await _cacheService.GetAsync<RegisterViewModel>(registrationCacheKey);

            if (registrationData == null)
            {
                _logger.LogError("Registration data not found in cache for {Email}", model.Email);
                ModelState.AddModelError("", "Thông tin đăng ký đã hết hạn. Vui lòng đăng ký lại.");
                return View(model);
            }

            // NOW create the user after successful verification
            var user = new ApplicationUser
            {
                UserName = registrationData.Email,
                Email = registrationData.Email,
                FirstName = registrationData.FirstName,
                LastName = registrationData.LastName,
                PhoneNumber = registrationData.PhoneNumber,
                EmailConfirmed = true, // Already verified!
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, registrationData.Password);

            if (result.Succeeded)
            {
                _logger.LogInformation("User {Email} created successfully after email verification", model.Email);
                
                // Add user to Customer role
                await _userManager.AddToRoleAsync(user, "Customer");
                
                // Remove both codes from cache
                await _cacheService.RemoveAsync(verificationCacheKey);
                await _cacheService.RemoveAsync(registrationCacheKey);

                // Sign in user
                await _signInManager.SignInAsync(user, isPersistent: false);

                ViewBag.Message = "Email đã được xác thực thành công! Tài khoản của bạn đã được kích hoạt.";
                
                if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                {
                    return Redirect(model.ReturnUrl);
                }
                
                return RedirectToAction("Index", "Home");
            }

            // If user creation failed, log errors
            foreach (var error in result.Errors)
            {
                _logger.LogError("Error creating user {Email}: {Error}", model.Email, error.Description);
                ModelState.AddModelError("", error.Description);
            }
            
            return View(model);
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResendEmailVerificationCode(string email)
        {
            // Check if registration data exists in cache (user not created yet)
            var registrationCacheKey = $"pending_registration_{email}";
            var registrationData = await _cacheService.GetAsync<RegisterViewModel>(registrationCacheKey);
            
            if (registrationData == null)
            {
                // Maybe user already created, try to find in database
                var user = await _userManager.FindByEmailAsync(email);
                if (user == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy thông tin đăng ký. Vui lòng đăng ký lại." });
                }
                
                // User exists but not confirmed - should not happen with new flow
                // Generate code for legacy users
                var legacyCode = new Random().Next(100000, 999999).ToString();
                var cacheKey = $"email_verification_{email}";
                await _cacheService.SetAsync(cacheKey, legacyCode, TimeSpan.FromMinutes(10));
                
                await _emailService.SendEmailAsync(email, "Mã xác thực tài khoản John Henry",
                    $@"
                    <h2>Xác thực tài khoản</h2>
                    <p>Chào {user.FirstName} {user.LastName},</p>
                    <p>Mã xác thực mới của bạn là: <strong style='font-size: 24px; color: #007bff;'>{legacyCode}</strong></p>
                    <p>Mã này sẽ hết hiệu lực sau 10 phút.</p>
                    <p>Trân trọng,<br>Đội ngũ John Henry</p>
                    ", isHtml: true);
                    
                return Json(new { success = true, message = "Mã xác thực đã được gửi lại thành công." });
            }

            // User not created yet - resend code with registration data
            var verificationCode = new Random().Next(100000, 999999).ToString();
            
            // Store new code in cache
            var verificationCacheKey = $"email_verification_{email}";
            await _cacheService.SetAsync(verificationCacheKey, verificationCode, TimeSpan.FromMinutes(10));
            
            // Extend registration data expiration
            await _cacheService.SetAsync(registrationCacheKey, registrationData, TimeSpan.FromMinutes(10));
            
            // Send new code
            await _emailService.SendEmailAsync(email, "Mã xác thực tài khoản John Henry",
                $@"
                <h2>Xác thực tài khoản</h2>
                <p>Chào {registrationData.FirstName} {registrationData.LastName},</p>
                <p>Mã xác thực mới của bạn là: <strong style='font-size: 24px; color: #007bff;'>{verificationCode}</strong></p>
                <p>Mã này sẽ hết hiệu lực sau 10 phút.</p>
                <p>Trân trọng,<br>Đội ngũ John Henry</p>
                ", isHtml: true);

            return Json(new { success = true, message = "Mã xác thực đã được gửi lại thành công." });
        }

        // Confirm Email
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmEmail(string userId, string code)
        {
            if (userId == null || code == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return View("Error");
            }

            var result = await _authService.ValidateEmailConfirmationTokenAsync(user, code);
            
            var viewModel = new ConfirmEmailViewModel
            {
                UserId = userId,
                Code = code,
                IsConfirmed = result,
                Message = result ? "Email của bạn đã được xác nhận thành công!" : "Có lỗi xảy ra khi xác nhận email."
            };

            return View(viewModel);
        }

        // Forgot Password
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user == null || !(await _userManager.IsEmailConfirmedAsync(user)))
                {
                    // Don't reveal that the user does not exist or is not confirmed
                    return RedirectToAction(nameof(ForgotPasswordConfirmation));
                }

                var code = await _authService.GeneratePasswordResetTokenAsync(user);
                var callbackUrl = Url.Action("ResetPassword", "Account",
                    new { code }, Request.Scheme);

                await _emailService.SendEmailAsync(model.Email, "Đặt lại mật khẩu",
                    $"Vui lòng đặt lại mật khẩu của bạn bằng cách click vào link: <a href='{callbackUrl}'>Đặt lại mật khẩu</a>", isHtml: true);

                return RedirectToAction(nameof(ForgotPasswordConfirmation));
            }

            return View(model);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        // Reset Password
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPassword(string? code = null)
        {
            if (code == null)
            {
                return BadRequest("Mã xác thực là bắt buộc để đặt lại mật khẩu.");
            }

            var model = new ResetPasswordViewModel { Code = code };
            return View(model);
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                // Don't reveal that the user does not exist
                return RedirectToAction(nameof(ResetPasswordConfirmation));
            }

            var result = await _userManager.ResetPasswordAsync(user, model.Code, model.Password);
            if (result.Succeeded)
            {
                return RedirectToAction(nameof(ResetPasswordConfirmation));
            }

            AddErrors(result);
            return View();
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPasswordConfirmation()
        {
            return View();
        }

        // Two Factor Authentication Setup
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> EnableAuthenticator()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            var key = await _authService.GetAuthenticatorKeyAsync(user);
            var authenticatorUri = await _authService.GenerateAuthenticatorUriAsync(user, key);

            var model = new EnableAuthenticatorViewModel
            {
                SharedKey = FormatKey(key),
                AuthenticatorUri = authenticatorUri
            };

            return View(model);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EnableAuthenticator(EnableAuthenticatorViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                var key = await _authService.GetAuthenticatorKeyAsync(user);
                model.SharedKey = FormatKey(key);
                model.AuthenticatorUri = await _authService.GenerateAuthenticatorUriAsync(user, key);
                return View(model);
            }

            var verificationCode = model.Code.Replace(" ", string.Empty).Replace("-", string.Empty);
            var is2faTokenValid = await _authService.ValidateTwoFactorTokenAsync(user, _userManager.Options.Tokens.AuthenticatorTokenProvider, verificationCode);

            if (!is2faTokenValid)
            {
                ModelState.AddModelError("Code", "Mã xác thực không chính xác.");
                var key = await _authService.GetAuthenticatorKeyAsync(user);
                model.SharedKey = FormatKey(key);
                model.AuthenticatorUri = await _authService.GenerateAuthenticatorUriAsync(user, key);
                return View(model);
            }

            await _userManager.SetTwoFactorEnabledAsync(user, true);
            var statusMessage = "Ứng dụng xác thực của bạn đã được xác minh.";

            if (await _userManager.CountRecoveryCodesAsync(user) == 0)
            {
                var recoveryCodes = await _authService.GenerateRecoveryCodesAsync(user, 10);
                TempData["RecoveryCodes"] = recoveryCodes.ToArray();
                return RedirectToAction(nameof(ShowRecoveryCodes));
            }

            TempData["StatusMessage"] = statusMessage;
            return RedirectToAction(nameof(TwoFactorAuthentication));
        }

        [HttpGet]
        [Authorize]
        public IActionResult ShowRecoveryCodes()
        {
            var recoveryCodes = (string[]?)TempData["RecoveryCodes"];
            if (recoveryCodes == null)
            {
                return RedirectToAction(nameof(TwoFactorAuthentication));
            }

            var model = new UserSecurityViewModel
            {
                RecoveryCodes = recoveryCodes,
                ShowRecoveryCodes = true
            };

            return View(model);
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> TwoFactorAuthentication()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            var model = new UserSecurityViewModel
            {
                HasPassword = await _userManager.HasPasswordAsync(user),
                PhoneNumber = await _userManager.GetPhoneNumberAsync(user),
                TwoFactorEnabled = await _userManager.GetTwoFactorEnabledAsync(user),
                Logins = await _userManager.GetLoginsAsync(user),
                PhoneNumberConfirmed = await _userManager.IsPhoneNumberConfirmedAsync(user),
                EmailConfirmed = user.EmailConfirmed,
                IsLockedOut = await _userManager.IsLockedOutAsync(user),
                FailedLoginAttempts = await _userManager.GetAccessFailedCountAsync(user)
            };

            return View(model);
        }

        private string MaskEmail(string email)
        {
            if (string.IsNullOrEmpty(email) || !email.Contains('@'))
                return "***@***.com";

            var parts = email.Split('@');
            var username = parts[0];
            var domain = parts[1];

            var maskedUsername = username.Length > 2 
                ? username.Substring(0, 2) + new string('*', username.Length - 2)
                : new string('*', username.Length);

            var maskedDomain = domain.Length > 2
                ? new string('*', domain.Length - 2) + domain.Substring(domain.Length - 2)
                : new string('*', domain.Length);

            return $"{maskedUsername}@{maskedDomain}";
        }

        private string FormatKey(string unformattedKey)
        {
            var result = new System.Text.StringBuilder();
            int currentPosition = 0;
            while (currentPosition + 4 < unformattedKey.Length)
            {
                result.Append(unformattedKey.Substring(currentPosition, 4)).Append(" ");
                currentPosition += 4;
            }
            if (currentPosition < unformattedKey.Length)
            {
                result.Append(unformattedKey.Substring(currentPosition));
            }

            return result.ToString().ToLowerInvariant();
        }

        #endregion

        #region Google OAuth Email Helpers

        private async Task SendEmailConfirmationForGoogleUser(ApplicationUser user)
        {
            try
            {
                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                var confirmationLink = Url.Action(nameof(ConfirmEmail), "Account",
                    new { userId = user.Id, token = token }, Request.Scheme);

                await _emailService.SendEmailAsync(user.Email ?? string.Empty, "Xác nhận tài khoản John Henry Fashion",
                    $@"
                    <h2>Xác nhận tài khoản của bạn</h2>
                    <p>Xin chào {user.FirstName} {user.LastName},</p>
                    <p>Cảm ơn bạn đã đăng ký tài khoản John Henry Fashion thông qua Google.</p>
                    <p>Để hoàn tất việc tạo tài khoản, vui lòng click vào link bên dưới:</p>
                    <p><a href='{confirmationLink}' style='background-color: #951329; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px;'>Xác nhận tài khoản</a></p>
                    <p>Nếu bạn không thể click vào link, vui lòng copy và paste URL sau vào trình duyệt:</p>
                    <p>{confirmationLink}</p>
                    <p>Trân trọng,<br>Đội ngũ John Henry Fashion</p>
                    ", isHtml: true);

                _logger.LogInformation("Email confirmation sent to Google user {Email}", user.Email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email confirmation to Google user {Email}", user.Email);
            }
        }

        private Task SendWelcomeEmailAsync(ApplicationUser user)
        {
            return Task.Run(async () =>
            {
                try
                {
                    await _emailService.SendEmailAsync(user.Email ?? string.Empty, "Chào mừng bạn đến với John Henry Fashion",
                        $@"
                        <h2>Chào mừng {user.FirstName} {user.LastName}!</h2>
                        <p>Tài khoản của bạn đã được tạo thành công thông qua Google.</p>
                        <p>Bạn có thể bắt đầu mua sắm ngay bây giờ!</p>
                        <p>Cảm ơn bạn đã gia nhập cộng đồng John Henry Fashion!</p>
                        <p>Trân trọng,<br>Đội ngũ John Henry</p>
                        ", isHtml: true);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Failed to send welcome email to {Email}: {Error}", user.Email, ex.Message);
                }
            });
        }

        #endregion

        [HttpGet]
        [AllowAnonymous]
        public IActionResult AccessDenied(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            
            _logger.LogWarning("Access denied for user {User} trying to access {ReturnUrl}", 
                User?.Identity?.Name ?? "Anonymous", returnUrl);
            
            return View();
        }

        #endregion
    }
}
