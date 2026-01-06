using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;

namespace JohnHenryFashionWeb.ViewModels;

public class ForgotPasswordViewModel
{
    [Required(ErrorMessage = "Email là bắt buộc")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;
}

public class ResetPasswordViewModel
{
    [Required(ErrorMessage = "Email là bắt buộc")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
    [StringLength(100, ErrorMessage = "Mật khẩu phải có ít nhất {2} ký tự và tối đa {1} ký tự.", MinimumLength = 8)]
    [DataType(DataType.Password)]
    [Display(Name = "Mật khẩu mới")]
    public string Password { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Display(Name = "Xác nhận mật khẩu")]
    [Compare("Password", ErrorMessage = "Mật khẩu và xác nhận mật khẩu không khớp.")]
    public string ConfirmPassword { get; set; } = string.Empty;

    public string Code { get; set; } = string.Empty;
}

public class TwoFactorAuthenticationViewModel
{
    [Required(ErrorMessage = "Mã xác thực là bắt buộc")]
    [StringLength(7, ErrorMessage = "Mã xác thực phải có {2} đến {1} ký tự.", MinimumLength = 6)]
    [DataType(DataType.Text)]
    [Display(Name = "Mã xác thực")]
    public string Code { get; set; } = string.Empty;

    public string? Provider { get; set; }

    [Display(Name = "Ghi nhớ thiết bị này")]
    public bool RememberMachine { get; set; }

    [Display(Name = "Ghi nhớ đăng nhập")]
    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}

public class EmailVerificationViewModel
{
    [Required(ErrorMessage = "Mã xác thực là bắt buộc")]
    [StringLength(6, ErrorMessage = "Mã xác thực phải có 6 ký tự")]
    [DataType(DataType.Text)]
    [Display(Name = "Mã xác thực")]
    public string Code { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }

    public bool IsResent { get; set; } = false;

    public DateTime? CodeSentTime { get; set; }

    [Display(Name = "Mã xác thực đã được gửi đến email của bạn")]
    public string MaskedEmail => MaskEmail(Email);

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

        var domainParts = domain.Split('.');
        var maskedDomain = domainParts.Length > 1
            ? new string('*', domainParts[0].Length) + "." + domainParts[domainParts.Length - 1]
            : new string('*', domain.Length);

        return $"{maskedUsername}@{maskedDomain}";
    }
}

public class EnableAuthenticatorViewModel
{
    [Required(ErrorMessage = "Mã xác thực là bắt buộc")]
    [StringLength(7, ErrorMessage = "Mã xác thực phải có {2} đến {1} ký tự.", MinimumLength = 6)]
    [DataType(DataType.Text)]
    [Display(Name = "Mã xác thực")]
    public string Code { get; set; } = string.Empty;

    public string SharedKey { get; set; } = string.Empty;

    public string AuthenticatorUri { get; set; } = string.Empty;
    
    public string QrCodeDataUrl { get; set; } = string.Empty; // Backend-generated QR code
}

public class ExternalLoginConfirmationViewModel
{
    [Required(ErrorMessage = "Email là bắt buộc")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Họ là bắt buộc")]
    [StringLength(50, ErrorMessage = "Họ không được vượt quá 50 ký tự")]
    [Display(Name = "Họ")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Tên là bắt buộc")]
    [StringLength(50, ErrorMessage = "Tên không được vượt quá 50 ký tự")]
    [Display(Name = "Tên")]
    public string LastName { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
    [Display(Name = "Số điện thoại")]
    public string? PhoneNumber { get; set; }

    [Required(ErrorMessage = "Bạn phải đồng ý với điều khoản sử dụng")]
    [Display(Name = "Tôi đồng ý với điều khoản sử dụng")]
    public bool AgreeToTerms { get; set; }
}

public class ConfirmEmailViewModel
{
    public string UserId { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public bool IsConfirmed { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class ManageLoginsViewModel
{
    public IList<UserLoginInfo> CurrentLogins { get; set; } = new List<UserLoginInfo>();
    public IList<AuthenticationScheme> OtherLogins { get; set; } = new List<AuthenticationScheme>();
}

public class UserSecurityViewModel
{
    public bool TwoFactorEnabled { get; set; }
    public bool PhoneNumberConfirmed { get; set; }
    public bool EmailConfirmed { get; set; }
    public bool HasPassword { get; set; }
    public IList<UserLoginInfo> Logins { get; set; } = new List<UserLoginInfo>();
    public string? PhoneNumber { get; set; }
    public DateTime? LastLoginDate { get; set; }
    public int FailedLoginAttempts { get; set; }
    public bool IsLockedOut { get; set; }
    public DateTime? LockoutEnd { get; set; }
    public IList<string> RecoveryCodes { get; set; } = new List<string>();
    public bool ShowRecoveryCodes { get; set; }
    public string StatusMessage { get; set; } = string.Empty;
}
