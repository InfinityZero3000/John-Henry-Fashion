using System.ComponentModel.DataAnnotations;
using JohnHenryFashionWeb.Models;

namespace JohnHenryFashionWeb.ViewModels
{
    public class CheckoutViewModel
    {
        public List<CartItemViewModel> CartItems { get; set; } = new();
        public decimal Subtotal { get; set; }
        public decimal ShippingFee { get; set; }
        public decimal Tax { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal Total => Subtotal + ShippingFee + Tax - DiscountAmount;

        [Required(ErrorMessage = "Email là bắt buộc")]
        [EmailAddress(ErrorMessage = "Địa chỉ email không hợp lệ")]
        public string Email { get; set; } = "";

        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string PhoneNumber { get; set; } = "";
        
        public AddressViewModel ShippingAddress { get; set; } = new();
        public AddressViewModel BillingAddress { get; set; } = new();
        public bool UseSameAddressForBilling { get; set; } = true;
        
        public string? ShippingMethod { get; set; }
        public string? CouponCode { get; set; }
        public string? Notes { get; set; }
        
        public List<Address> SavedAddresses { get; set; } = new();
        public List<ShippingMethod> ShippingMethods { get; set; } = new();
        public List<PaymentMethod> PaymentMethods { get; set; } = new();
    }

    public class CheckoutCreateViewModel
    {
        [Required(ErrorMessage = "Email là bắt buộc")]
        [EmailAddress(ErrorMessage = "Địa chỉ email không hợp lệ")]
        public string Email { get; set; } = "";

        public AddressViewModel ShippingAddress { get; set; } = new();
        public AddressViewModel BillingAddress { get; set; } = new();
        public bool UseSameAddressForBilling { get; set; } = true;

        [Required(ErrorMessage = "Phương thức vận chuyển là bắt buộc")]
        public string ShippingMethod { get; set; } = "";

        public string? PaymentMethod { get; set; }

        public string? CouponCode { get; set; }
        public string? Notes { get; set; }
    }

    public class AddressViewModel
    {
        [Required(ErrorMessage = "Họ và tên là bắt buộc")]
        [Display(Name = "Họ và tên")]
        public string FullName { get; set; } = "";

        [Required(ErrorMessage = "Số điện thoại là bắt buộc")]
        [Display(Name = "Số điện thoại")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        public string PhoneNumber { get; set; } = "";

        [Required(ErrorMessage = "Địa chỉ là bắt buộc")]
        [Display(Name = "Địa chỉ")]
        public string Address { get; set; } = "";

        [Required(ErrorMessage = "Phường/Xã là bắt buộc")]
        [Display(Name = "Phường/Xã")]
        public string Ward { get; set; } = "";

        [Required(ErrorMessage = "Quận/Huyện là bắt buộc")]
        [Display(Name = "Quận/Huyện")]
        public string District { get; set; } = "";

        [Required(ErrorMessage = "Tỉnh/Thành phố là bắt buộc")]
        [Display(Name = "Tỉnh/Thành phố")]
        public string City { get; set; } = "";

        [Display(Name = "Mã bưu điện")]
        public string? PostalCode { get; set; }

        [Display(Name = "Ghi chú địa chỉ")]
        public string? AddressNote { get; set; }

        public bool IsDefault { get; set; }
    }

    public class PaymentViewModel
    {
        public string SessionId { get; set; } = "";
        public CheckoutSession Session { get; set; } = new();
        public List<PaymentMethod> PaymentMethods { get; set; } = new();
        public List<ShippingMethod> ShippingMethods { get; set; } = new();
        public decimal TotalAmount { get; set; }
        
        public string SelectedPaymentMethod { get; set; } = "";
        public string? PaymentMethodId { get; set; }
        
        // For Stripe
        public string? StripeCardNumber { get; set; }
        public string? StripeExpiryMonth { get; set; }
        public string? StripeExpiryYear { get; set; }
        public string? StripeCvc { get; set; }
        public string? StripeCardholderName { get; set; }
        
        // For bank transfer
        public string? SelectedBankAccount { get; set; }
    }

    public class ProcessPaymentViewModel
    {
        [Required(ErrorMessage = "Session ID là bắt buộc")]
        public string SessionId { get; set; } = "";

        [Required(ErrorMessage = "Phương thức thanh toán là bắt buộc")]
        public string PaymentMethod { get; set; } = "";

        public string? PaymentMethodId { get; set; }
        
        // Stripe payment details
        public StripePaymentViewModel? StripePayment { get; set; }
        
        // Bank transfer details
        public string? SelectedBankAccount { get; set; }
        public string? TransferNote { get; set; }
    }

    public class StripePaymentViewModel
    {
        [Required(ErrorMessage = "Số thẻ là bắt buộc")]
        [Display(Name = "Số thẻ")]
        [RegularExpression(@"^\d{13,19}$", ErrorMessage = "Số thẻ không hợp lệ")]
        public string CardNumber { get; set; } = "";

        [Required(ErrorMessage = "Tháng hết hạn là bắt buộc")]
        [Display(Name = "Tháng")]
        [Range(1, 12, ErrorMessage = "Tháng phải từ 1 đến 12")]
        public int ExpiryMonth { get; set; }

        [Required(ErrorMessage = "Năm hết hạn là bắt buộc")]
        [Display(Name = "Năm")]
        [Range(2024, 2050, ErrorMessage = "Năm không hợp lệ")]
        public int ExpiryYear { get; set; }

        [Required(ErrorMessage = "Mã CVC là bắt buộc")]
        [Display(Name = "CVC")]
        [RegularExpression(@"^\d{3,4}$", ErrorMessage = "Mã CVC không hợp lệ")]
        public string Cvc { get; set; } = "";

        [Required(ErrorMessage = "Tên chủ thẻ là bắt buộc")]
        [Display(Name = "Tên chủ thẻ")]
        public string CardholderName { get; set; } = "";
    }

    public class CartItemViewModel
    {
        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = "";
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public string? Size { get; set; }
        public string? Color { get; set; }
        public string? ImageUrl { get; set; }
        public decimal Total => Price * Quantity;
    }

    public class ShippingCalculationViewModel
    {
        [Required(ErrorMessage = "Địa chỉ giao hàng là bắt buộc")]
        public AddressViewModel Address { get; set; } = new();

        [Required(ErrorMessage = "Phương thức vận chuyển là bắt buộc")]
        public string ShippingMethod { get; set; } = "";

        public decimal OrderValue { get; set; }
        public decimal Weight { get; set; }
        public List<CartItemViewModel> Items { get; set; } = new();
    }

    public class CouponValidationViewModel
    {
        [Required(ErrorMessage = "Mã giảm giá là bắt buộc")]
        public string CouponCode { get; set; } = "";

        public decimal OrderValue { get; set; }
        public List<Guid> ProductIds { get; set; } = new();
        public List<string> CategoryIds { get; set; } = new();
    }

    public class CheckoutOrderSummaryViewModel
    {
        public List<CartItemViewModel> Items { get; set; } = new();
        public decimal Subtotal { get; set; }
        public decimal ShippingFee { get; set; }
        public decimal Tax { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal Total { get; set; }
        
        public string? CouponCode { get; set; }
        public string? ShippingMethod { get; set; }
        public string ShippingMethodName { get; set; } = "";
        
        public AddressViewModel ShippingAddress { get; set; } = new();
        public AddressViewModel BillingAddress { get; set; } = new();
        
        public string PaymentMethod { get; set; } = "";
        public string PaymentMethodName { get; set; } = "";
        
        public string? Notes { get; set; }
    }

    public class PaymentResultViewModel
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = "";
        public string? OrderId { get; set; }
        public string? OrderNumber { get; set; }
        public string? TransactionId { get; set; }
        public string? PaymentMethod { get; set; }
        public decimal Amount { get; set; }
        public DateTime PaymentDate { get; set; }
        public string? ErrorCode { get; set; }
        public string? RedirectUrl { get; set; }
    }

    public class CheckoutStepViewModel
    {
        public int CurrentStep { get; set; }
        public int TotalSteps { get; set; } = 4;
        public List<CheckoutStep> Steps { get; set; } = new();
        public bool CanProceedToNext { get; set; }
        public bool CanGoToPrevious { get; set; }
    }

    public class CheckoutStep
    {
        public int StepNumber { get; set; }
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public bool IsCompleted { get; set; }
        public bool IsActive { get; set; }
        public bool IsAccessible { get; set; }
        public string Icon { get; set; } = "";
        public string Url { get; set; } = "";
    }

    public class GuestCheckoutViewModel
    {
        [Required(ErrorMessage = "Email là bắt buộc")]
        [EmailAddress(ErrorMessage = "Địa chỉ email không hợp lệ")]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "Họ là bắt buộc")]
        public string FirstName { get; set; } = "";

        [Required(ErrorMessage = "Tên là bắt buộc")]
        public string LastName { get; set; } = "";

        [Required(ErrorMessage = "Số điện thoại là bắt buộc")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        public string PhoneNumber { get; set; } = "";

        public bool CreateAccount { get; set; }

        [Display(Name = "Mật khẩu")]
        [DataType(DataType.Password)]
        public string? Password { get; set; }

        [Display(Name = "Xác nhận mật khẩu")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Mật khẩu xác nhận không khớp")]
        public string? ConfirmPassword { get; set; }

        public bool AcceptTerms { get; set; }
        public bool SubscribeNewsletter { get; set; }
    }

    public class ExpressCheckoutViewModel
    {
        public string PaymentMethod { get; set; } = "";
        public List<CartItemViewModel> Items { get; set; } = new();
        public decimal TotalAmount { get; set; }
        public AddressViewModel ShippingAddress { get; set; } = new();
        public string ShippingMethod { get; set; } = "";
        public string Email { get; set; } = "";
        public string PhoneNumber { get; set; } = "";
    }
}
