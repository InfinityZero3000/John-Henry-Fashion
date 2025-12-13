using JohnHenryFashionWeb.Data;
using JohnHenryFashionWeb.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JohnHenryFashionWeb.Helpers
{
    /// <summary>
    /// Helper class để validate trạng thái thanh toán của đơn hàng
    /// Đảm bảo chỉ các đơn hàng đã thanh toán mới được xử lý
    /// </summary>
    public class PaymentValidator
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PaymentValidator> _logger;

        public PaymentValidator(ApplicationDbContext context, ILogger<PaymentValidator> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Kiểm tra xem đơn hàng đã được thanh toán chưa
        /// </summary>
        /// <param name="orderId">ID của đơn hàng</param>
        /// <returns>True nếu đơn hàng đã thanh toán hoặc là COD đã giao hàng</returns>
        public async Task<PaymentValidationResult> ValidatePaymentStatusAsync(Guid orderId)
        {
            try
            {
                var order = await _context.Orders
                    .Include(o => o.Payments)
                    .FirstOrDefaultAsync(o => o.Id == orderId);

                if (order == null)
                {
                    return new PaymentValidationResult
                    {
                        IsValid = false,
                        Message = "Không tìm thấy đơn hàng",
                        RequiresPayment = false
                    };
                }

                // Lấy payment method
                var paymentMethod = order.PaymentMethod?.ToLower() ?? "";

                // Kiểm tra theo từng loại payment method
                switch (paymentMethod)
                {
                    case "vnpay":
                    case "momo":
                        // Online payment: Phải có trạng thái "paid"
                        if (order.PaymentStatus == "paid")
                        {
                            return new PaymentValidationResult
                            {
                                IsValid = true,
                                Message = "Đơn hàng đã thanh toán online",
                                RequiresPayment = false
                            };
                        }
                        else
                        {
                            return new PaymentValidationResult
                            {
                                IsValid = false,
                                Message = "Đơn hàng chưa thanh toán qua cổng thanh toán",
                                RequiresPayment = true
                            };
                        }

                    case "cod":
                        // COD: Chỉ cho phép xử lý khi status là "delivered" (đã giao hàng)
                        // hoặc admin đã xác nhận thanh toán
                        if (order.Status == "delivered" && order.PaymentStatus == "paid")
                        {
                            return new PaymentValidationResult
                            {
                                IsValid = true,
                                Message = "Đơn hàng COD đã giao và thanh toán",
                                RequiresPayment = false
                            };
                        }
                        else if (order.Status == "delivered" || order.Status == "shipped")
                        {
                            return new PaymentValidationResult
                            {
                                IsValid = true,
                                Message = "Đơn hàng COD đang trong quá trình giao hàng",
                                RequiresPayment = false,
                                IsCODPending = true
                            };
                        }
                        else if (order.PaymentStatus == "cod_pending" && order.Status == "pending")
                        {
                            return new PaymentValidationResult
                            {
                                IsValid = true,
                                Message = "Đơn hàng COD chờ xác nhận",
                                RequiresPayment = false,
                                IsCODPending = true
                            };
                        }
                        else
                        {
                            return new PaymentValidationResult
                            {
                                IsValid = false,
                                Message = "Đơn hàng COD chưa được giao",
                                RequiresPayment = false,
                                IsCODPending = true
                            };
                        }

                    case "bank_transfer":
                        // Bank Transfer: Phải được admin xác nhận (status = "paid")
                        if (order.PaymentStatus == "paid")
                        {
                            return new PaymentValidationResult
                            {
                                IsValid = true,
                                Message = "Đơn hàng đã được xác nhận chuyển khoản",
                                RequiresPayment = false
                            };
                        }
                        else
                        {
                            return new PaymentValidationResult
                            {
                                IsValid = false,
                                Message = "Đơn hàng chờ xác nhận chuyển khoản",
                                RequiresPayment = true
                            };
                        }

                    default:
                        _logger.LogWarning("Unknown payment method: {PaymentMethod} for order {OrderId}", 
                            paymentMethod, orderId);
                        return new PaymentValidationResult
                        {
                            IsValid = false,
                            Message = "Phương thức thanh toán không hợp lệ",
                            RequiresPayment = true
                        };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating payment for order {OrderId}", orderId);
                return new PaymentValidationResult
                {
                    IsValid = false,
                    Message = "Lỗi khi kiểm tra trạng thái thanh toán",
                    RequiresPayment = false
                };
            }
        }

        /// <summary>
        /// Kiểm tra xem đơn hàng có thể được ship không
        /// </summary>
        public async Task<bool> CanShipOrderAsync(Guid orderId)
        {
            var validation = await ValidatePaymentStatusAsync(orderId);
            
            // Chỉ cho phép ship nếu:
            // 1. Đơn đã thanh toán (online payment hoặc bank transfer đã xác nhận)
            // 2. Hoặc là COD (cho phép ship trước, thu tiền sau)
            return validation.IsValid || validation.IsCODPending;
        }

        /// <summary>
        /// Kiểm tra xem đơn hàng có thể được mark là completed không
        /// </summary>
        public async Task<bool> CanCompleteOrderAsync(Guid orderId)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null) return false;

            var paymentMethod = order.PaymentMethod?.ToLower() ?? "";

            // COD: Có thể complete khi delivered (sẽ tự động mark as paid khi complete)
            if (paymentMethod == "cod")
            {
                return order.Status == "delivered" || order.Status == "shipped";
            }

            // Các phương thức khác: Phải đã thanh toán
            return order.PaymentStatus == "paid";
        }
    }

    public class PaymentValidationResult
    {
        public bool IsValid { get; set; }
        public string Message { get; set; } = "";
        public bool RequiresPayment { get; set; }
        public bool IsCODPending { get; set; }
    }
}
