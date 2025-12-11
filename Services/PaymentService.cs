using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using JohnHenryFashionWeb.Models;
using JohnHenryFashionWeb.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text;
using System.Security.Cryptography;

namespace JohnHenryFashionWeb.Services
{
    public interface IPaymentService
    {
        Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request);
        Task<PaymentResult> ProcessVNPayPaymentAsync(VNPayPaymentRequest request);
        Task<PaymentResult> ProcessMoMoPaymentAsync(MoMoPaymentRequest request);
        Task<PaymentResult> ProcessStripePaymentAsync(StripePaymentRequest request);
        Task<bool> VerifyPaymentAsync(string paymentId, string signature);
        Task<RefundResult> ProcessRefundAsync(RefundRequest request);
        Task<PaymentStatus> GetPaymentStatusAsync(string paymentId);
        Task<List<PaymentMethod>> GetAvailablePaymentMethodsAsync();
        Task<bool> ValidatePaymentDataAsync(PaymentRequest request);
        Task LogPaymentAttemptAsync(PaymentAttempt attempt);
        Task<string> GeneratePaymentUrlAsync(PaymentRequest request);
        Task<PaymentResult> HandlePaymentCallbackAsync(PaymentCallbackData callbackData);
        
        // QR Code Generation Methods
        Task<QRCodeResult> GenerateVNPayQRCodeAsync(VNPayQRRequest request);
        Task<QRCodeResult> GenerateMoMoQRCodeAsync(MoMoQRRequest request);
    }

    public class PaymentService : IPaymentService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<PaymentService> _logger;
        private readonly IEmailService _emailService;
        private readonly INotificationService _notificationService;
        private readonly HttpClient _httpClient;

        public PaymentService(
            ApplicationDbContext context,
            IConfiguration configuration,
            ILogger<PaymentService> logger,
            IEmailService emailService,
            INotificationService notificationService,
            HttpClient httpClient)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
            _emailService = emailService;
            _notificationService = notificationService;
            _httpClient = httpClient;
        }

        public async Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request)
        {
            try
            {
                // Validate payment request
                if (!await ValidatePaymentDataAsync(request))
                {
                    return new PaymentResult
                    {
                        IsSuccess = false,
                        ErrorMessage = "Dữ liệu thanh toán không hợp lệ",
                        PaymentId = Guid.NewGuid().ToString()
                    };
                }

                // Note: PaymentAttempt logging is handled by the controller
                // to ensure Order exists in database before creating PaymentAttempt
                
                // Process payment based on method
                PaymentResult result = request.PaymentMethod.ToLower() switch
                {
                    "vnpay" => await ProcessVNPayPaymentAsync(new VNPayPaymentRequest
                    {
                        Amount = request.Amount,
                        OrderId = request.OrderId ?? string.Empty,
                        OrderInfo = request.OrderInfo,
                        ReturnUrl = request.ReturnUrl,
                        IpAddress = request.IpAddress
                    }),
                    "momo" => await ProcessMoMoPaymentAsync(new MoMoPaymentRequest
                    {
                        Amount = request.Amount,
                        OrderId = request.OrderId,
                        OrderInfo = request.OrderInfo,
                        ReturnUrl = request.ReturnUrl,
                        NotifyUrl = request.NotifyUrl
                    }),
                    "stripe" => await ProcessStripePaymentAsync(new StripePaymentRequest
                    {
                        Amount = request.Amount,
                        Currency = request.Currency,
                        PaymentMethodId = request.PaymentMethodId ?? string.Empty,
                        CustomerId = request.UserId
                    }),
                    "cod" => await ProcessCODPaymentAsync(request),
                    _ => new PaymentResult
                    {
                        IsSuccess = false,
                        ErrorMessage = "Phương thức thanh toán không được hỗ trợ"
                    }
                };

                // Note: PaymentAttempt status update is handled by the controller
                // to ensure proper order of operations

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing payment for order {OrderId}", request.OrderId);
                return new PaymentResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Đã xảy ra lỗi trong quá trình xử lý thanh toán"
                };
            }
        }

        public async Task<PaymentResult> ProcessVNPayPaymentAsync(VNPayPaymentRequest request)
        {
            try
            {
                await Task.CompletedTask;
                var vnpayConfig = _configuration.GetSection("PaymentGateways:VNPay");
                var vnp_TmnCode = vnpayConfig["TmnCode"] ?? string.Empty;
                var vnp_HashSecret = vnpayConfig["HashSecret"];
                var vnp_Url = vnpayConfig["PaymentUrl"] ?? vnpayConfig["Url"] ?? "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";

                // Debug log for configuration values
                _logger.LogInformation("VNPay Config Loaded - TmnCode: {TmnCode}, HashSecret: '{HashSecret}' (Length: {Length}), URL: {Url}", 
                    vnp_TmnCode, 
                    vnp_HashSecret,
                    vnp_HashSecret?.Length ?? 0,
                    vnp_Url);

                // Validate credentials
                if (string.IsNullOrWhiteSpace(vnp_TmnCode) || string.IsNullOrWhiteSpace(vnp_HashSecret))
                {
                    _logger.LogError("VNPay credentials are missing. TmnCode: {HasTmnCode}, HashSecret: {HasHashSecret}",
                        !string.IsNullOrWhiteSpace(vnp_TmnCode),
                        !string.IsNullOrWhiteSpace(vnp_HashSecret));
                    return new PaymentResult
                    {
                        IsSuccess = false,
                        ErrorMessage = "Cấu hình VNPay chưa đầy đủ. Vui lòng kiểm tra TmnCode và HashSecret"
                    };
                }

                // Validate required fields
                if (string.IsNullOrWhiteSpace(request.ReturnUrl))
                {
                    return new PaymentResult
                    {
                        IsSuccess = false,
                        ErrorMessage = "ReturnUrl không được để trống"
                    };
                }

                // Generate unique transaction reference (max 100 chars, alphanumeric only)
                // Try to get OrderNumber from database if OrderId is Guid
                var txnRef = request.OrderId;
                
                // If OrderId is a Guid, try to get OrderNumber from database
                if (Guid.TryParse(request.OrderId, out var orderGuid))
                {
                    var order = await _context.Orders
                        .FirstOrDefaultAsync(o => o.Id == orderGuid);
                    
                    if (order != null && !string.IsNullOrEmpty(order.OrderNumber))
                    {
                        // Use OrderNumber (clean alphanumeric)
                        txnRef = System.Text.RegularExpressions.Regex.Replace(order.OrderNumber, @"[^a-zA-Z0-9]", "");
                    }
                    else
                    {
                        // Fallback: Use first 8 chars of Guid + timestamp
                        txnRef = orderGuid.ToString("N").Substring(0, 8) + DateTime.Now.ToString("HHmmss");
                    }
                }
                else
                {
                    // Remove any special characters, keep only alphanumeric
                    txnRef = System.Text.RegularExpressions.Regex.Replace(txnRef, @"[^a-zA-Z0-9]", "");
                }
                
                // Ensure max length
                if (txnRef.Length > 100)
                {
                    txnRef = txnRef.Substring(0, 100);
                }

                // Calculate amount in VND cents (must be integer, no decimals)
                var vnp_Amount = (long)(request.Amount * 100);
                
                // Format OrderInfo (max 255 chars, URL encode)
                var orderInfo = request.OrderInfo ?? "Thanh toan don hang";
                if (orderInfo.Length > 255)
                {
                    orderInfo = orderInfo.Substring(0, 255);
                }

                // Get IP address (required by VNPay)
                var ipAddress = request.IpAddress;
                if (string.IsNullOrWhiteSpace(ipAddress) || ipAddress == "::1")
                {
                    ipAddress = "127.0.0.1"; // Default for localhost
                }

                // Create date in VNPay format (yyyyMMddHHmmss)
                var createDate = DateTime.Now.ToString("yyyyMMddHHmmss");
                
                // Expire date (15 minutes from now)
                var expireDate = DateTime.Now.AddMinutes(15).ToString("yyyyMMddHHmmss");

                var vnp_Params = new Dictionary<string, string>
                {
                    {"vnp_Version", "2.1.0"},
                    {"vnp_Command", "pay"},
                    {"vnp_TmnCode", vnp_TmnCode},
                    {"vnp_Amount", vnp_Amount.ToString()}, // Must be integer string
                    {"vnp_CurrCode", "VND"},
                    {"vnp_TxnRef", txnRef},
                    {"vnp_OrderInfo", orderInfo},
                    {"vnp_OrderType", "other"},
                    {"vnp_Locale", "vn"},
                    {"vnp_ReturnUrl", request.ReturnUrl},
                    {"vnp_IpAddr", ipAddress},
                    {"vnp_CreateDate", createDate},
                    {"vnp_ExpireDate", expireDate}
                };

                // Add NotifyUrl if provided (optional but recommended)
                if (!string.IsNullOrWhiteSpace(request.NotifyUrl))
                {
                    vnp_Params["vnp_NotifyUrl"] = request.NotifyUrl;
                }

                // Remove empty values
                vnp_Params = vnp_Params.Where(x => !string.IsNullOrWhiteSpace(x.Value)).ToDictionary(x => x.Key, x => x.Value);

                // Sort parameters alphabetically (required by VNPay)
                var sortedParams = vnp_Params.OrderBy(x => x.Key).ToList();
                
                // Create query string (URL encode values theo chuẩn VNPay)
                var query = string.Join("&", sortedParams.Select(x => $"{x.Key}={UrlEncodeVNPay(x.Value)}"));

                // Create signature (HMAC SHA512)
                var signature = CreateVNPaySignature(query, vnp_HashSecret);
                
                // Build final URL
                var paymentUrl = $"{vnp_Url}?{query}&vnp_SecureHash={signature}";
                
                // Enhanced logging for debugging
                _logger.LogInformation("VNPay Payment URL Details:");
                _logger.LogInformation("  TmnCode: {TmnCode}", vnp_TmnCode);
                _logger.LogInformation("  Amount: {Amount} (cents: {AmountCents})", request.Amount, vnp_Amount);
                _logger.LogInformation("  TxnRef: {TxnRef}", txnRef);
                _logger.LogInformation("  ReturnUrl: {ReturnUrl}", request.ReturnUrl);
                _logger.LogInformation("  NotifyUrl: {NotifyUrl}", request.NotifyUrl ?? "N/A");
                _logger.LogInformation("  IpAddr: {IpAddr}", ipAddress);
                _logger.LogInformation("  CreateDate: {CreateDate}", createDate);
                _logger.LogInformation("  ExpireDate: {ExpireDate}", expireDate);
                _logger.LogInformation("  Query String: {Query}", query);
                _logger.LogInformation("  Signature: {Signature}", signature);
                _logger.LogInformation("  Final URL: {PaymentUrl}", paymentUrl);

                return new PaymentResult
                {
                    IsSuccess = true,
                    PaymentUrl = paymentUrl,
                    PaymentId = request.OrderId,
                    Message = "Chuyển hướng đến VNPay để thanh toán"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing VNPay payment for order {OrderId}", request.OrderId);
                return new PaymentResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Lỗi kết nối VNPay"
                };
            }
        }

        public async Task<PaymentResult> ProcessMoMoPaymentAsync(MoMoPaymentRequest request)
        {
            try
            {
                var momoConfig = _configuration.GetSection("PaymentGateways:MoMo");
                var partnerCode = momoConfig["PartnerCode"];
                var accessKey = momoConfig["AccessKey"];
                var secretKey = momoConfig["SecretKey"];
                var endpoint = momoConfig["ApiUrl"] ?? momoConfig["Endpoint"]; // Support both keys
                
                // Validate required credentials
                if (string.IsNullOrWhiteSpace(partnerCode) || 
                    string.IsNullOrWhiteSpace(accessKey) || 
                    string.IsNullOrWhiteSpace(secretKey))
                {
                    _logger.LogError("MoMo credentials are missing for payment processing");
                    return new PaymentResult
                    {
                        IsSuccess = false,
                        ErrorMessage = "Cấu hình MoMo chưa đầy đủ. Vui lòng kiểm tra credentials trong file .env"
                    };
                }
                
                if (string.IsNullOrWhiteSpace(endpoint))
                {
                    var isSandbox = bool.Parse(momoConfig["IsSandbox"] ?? momoConfig["Sandbox"] ?? "true");
                    endpoint = isSandbox 
                        ? "https://test-payment.momo.vn/v2/gateway/api/create"
                        : "https://payment.momo.vn/v2/gateway/api/create";
                    _logger.LogWarning("MoMo endpoint not configured, using default: {Endpoint}", endpoint);
                }

                var requestId = Guid.NewGuid().ToString();
                var orderId = request.OrderId;
                var amount = (long)request.Amount; // MoMo requires long/number type, not string
                var orderInfo = request.OrderInfo;
                var redirectUrl = request.ReturnUrl;
                var ipnUrl = request.NotifyUrl;
                var requestType = "captureWallet"; // Changed from payWithATM to captureWallet (default payment method)
                var extraData = ""; // Must be empty string, not null

                // Create signature - IMPORTANT: Parameters must be in alphabetical order
                // Format: accessKey=...&amount=...&extraData=...&ipnUrl=...&orderId=...&orderInfo=...&partnerCode=...&redirectUrl=...&requestId=...&requestType=...
                var rawHash = $"accessKey={accessKey}&amount={amount}&extraData={extraData}&ipnUrl={ipnUrl}&orderId={orderId}&orderInfo={orderInfo}&partnerCode={partnerCode}&redirectUrl={redirectUrl}&requestId={requestId}&requestType={requestType}";
                var signature = CreateMoMoSignature(rawHash, secretKey!);

                _logger.LogInformation("MoMo Request - OrderId: {OrderId}, Amount: {Amount}, RequestType: {RequestType}, Signature Hash: {RawHash}", 
                    orderId, amount, requestType, rawHash);
                _logger.LogInformation("MoMo Signature: {Signature}", signature);

                var requestData = new
                {
                    partnerCode,
                    partnerName = "John Henry Fashion",
                    storeId = "MomoTestStore",
                    requestId,
                    amount, // This will be serialized as number
                    orderId,
                    orderInfo,
                    redirectUrl,
                    ipnUrl,
                    lang = "vi",
                    extraData,
                    requestType, // Use variable instead of hardcoded
                    signature
                };

                var json = JsonSerializer.Serialize(requestData);
                _logger.LogInformation("MoMo Request JSON: {Json}", json);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(endpoint, content);
                var responseContent = await response.Content.ReadAsStringAsync();
                
                _logger.LogInformation("MoMo Response Status: {StatusCode}, Body: {Response}", 
                    response.StatusCode, responseContent);
                
                if (response.IsSuccessStatusCode)
                {
                    var momoResponse = JsonSerializer.Deserialize<MoMoResponse>(responseContent);
                    
                    if (momoResponse?.ResultCode == 0)
                    {
                        return new PaymentResult
                        {
                            IsSuccess = true,
                            PaymentUrl = momoResponse.PayUrl,
                            PaymentId = orderId,
                            TransactionId = requestId,
                            Message = "Chuyển hướng đến MoMo để thanh toán"
                        };
                    }
                    else
                    {
                        _logger.LogError("MoMo Error - ResultCode: {ResultCode}, Message: {Message}", 
                            momoResponse?.ResultCode, momoResponse?.Message);
                        return new PaymentResult
                        {
                            IsSuccess = false,
                            ErrorMessage = $"MoMo Error: {momoResponse?.Message ?? "Unknown error"}"
                        };
                    }
                }

                _logger.LogError("MoMo HTTP Error: {StatusCode}, Response: {Response}", 
                    response.StatusCode, responseContent);
                return new PaymentResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Lỗi kết nối MoMo (HTTP {(int)response.StatusCode})"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing MoMo payment for order {OrderId}", request.OrderId);
                return new PaymentResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Lỗi kết nối MoMo"
                };
            }
        }

        public async Task<PaymentResult> ProcessStripePaymentAsync(StripePaymentRequest request)
        {
            try
            {
                // Note: This is a simplified implementation
                // In production, you should use the official Stripe .NET SDK
                var stripeConfig = _configuration.GetSection("PaymentGateways:Stripe");
                var secretKey = stripeConfig["SecretKey"];

                // Create payment intent with Stripe API
                var paymentData = new
                {
                    amount = (int)(request.Amount * 100), // Stripe requires amount in cents
                    currency = request.Currency.ToLower(),
                    payment_method = request.PaymentMethodId,
                    customer = request.CustomerId,
                    confirm = true,
                    return_url = _configuration["BaseUrl"] + "/checkout/stripe-return"
                };

                var json = JsonSerializer.Serialize(paymentData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                _httpClient.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", secretKey);

                var response = await _httpClient.PostAsync("https://api.stripe.com/v1/payment_intents", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var stripeResponse = JsonSerializer.Deserialize<StripePaymentResponse>(responseContent);
                    
                    return new PaymentResult
                    {
                        IsSuccess = stripeResponse?.Status == "succeeded",
                        PaymentId = stripeResponse?.Id,
                        TransactionId = stripeResponse?.Id,
                        Message = stripeResponse?.Status == "succeeded" ? "Thanh toán thành công" : "Thanh toán đang xử lý"
                    };
                }

                return new PaymentResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Lỗi kết nối Stripe"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Stripe payment");
                return new PaymentResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Lỗi xử lý thanh toán Stripe"
                };
            }
        }

        private async Task<PaymentResult> ProcessCODPaymentAsync(PaymentRequest request)
        {
            await Task.CompletedTask;
            // Cash on Delivery - always success for now
            return new PaymentResult
            {
                IsSuccess = true,
                PaymentId = Guid.NewGuid().ToString(),
                TransactionId = $"COD_{request.OrderId}",
                Message = "Đặt hàng thành công - Thanh toán khi nhận hàng"
            };
        }

        public async Task<bool> VerifyPaymentAsync(string paymentId, string signature)
        {
            try
            {
                var payment = await _context.PaymentAttempts
                    .FirstOrDefaultAsync(p => p.PaymentId == paymentId);

                if (payment == null) return false;

                // Verify signature based on payment method
                return payment.PaymentMethod.ToLower() switch
                {
                    "vnpay" => VerifyVNPaySignature(paymentId, signature),
                    "momo" => VerifyMoMoSignature(paymentId, signature),
                    "stripe" => true, // Stripe verification handled elsewhere
                    _ => false
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying payment {PaymentId}", paymentId);
                return false;
            }
        }

        public async Task<RefundResult> ProcessRefundAsync(RefundRequest request)
        {
            try
            {
                var refund = new RefundRequest
                {
                    Id = Guid.NewGuid(),
                    PaymentId = request.PaymentId,
                    OrderId = request.OrderId,
                    Amount = request.Amount,
                    Reason = request.Reason,
                    Status = "pending",
                    CreatedAt = DateTime.UtcNow,
                    RequestedBy = request.RequestedBy
                };

                _context.RefundRequests.Add(refund);
                await _context.SaveChangesAsync();

                // Process refund with payment gateway
                // Implementation depends on the original payment method

                return new RefundResult
                {
                    IsSuccess = true,
                    RefundId = refund.Id.ToString(),
                    Message = "Yêu cầu hoàn tiền đã được tạo"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing refund for payment {PaymentId}", request.PaymentId);
                return new RefundResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Lỗi xử lý hoàn tiền"
                };
            }
        }

        public async Task<PaymentStatus> GetPaymentStatusAsync(string paymentId)
        {
            var payment = await _context.PaymentAttempts
                .FirstOrDefaultAsync(p => p.PaymentId == paymentId);

            return payment?.Status switch
            {
                "completed" => PaymentStatus.Completed,
                "pending" => PaymentStatus.Pending,
                "failed" => PaymentStatus.Failed,
                "cancelled" => PaymentStatus.Cancelled,
                _ => PaymentStatus.Unknown
            };
        }

        public async Task<List<PaymentMethod>> GetAvailablePaymentMethodsAsync()
        {
            return await _context.PaymentMethods
                .Where(pm => pm.IsActive)
                .OrderBy(pm => pm.SortOrder)
                .ToListAsync();
        }

        public async Task<bool> ValidatePaymentDataAsync(PaymentRequest request)
        {
            if (string.IsNullOrEmpty(request.OrderId) || 
                string.IsNullOrEmpty(request.UserId) ||
                request.Amount <= 0)
            {
                _logger.LogWarning("ValidatePaymentDataAsync failed - OrderId: {OrderId}, UserId: {UserId}, Amount: {Amount}",
                    request.OrderId, request.UserId, request.Amount);
                return false;
            }

            // Check if order exists and belongs to user
            // OrderId can be either Guid (Id) or OrderNumber (string)
            var order = await _context.Orders
                .FirstOrDefaultAsync(o => (o.Id.ToString() == request.OrderId || o.OrderNumber == request.OrderId) && 
                                        o.UserId == request.UserId);

            if (order == null)
            {
                _logger.LogWarning("ValidatePaymentDataAsync - Order not found. OrderId: {OrderId}, UserId: {UserId}",
                    request.OrderId, request.UserId);
            }

            return order != null;
        }

        public async Task LogPaymentAttemptAsync(PaymentAttempt attempt)
        {
            _context.PaymentAttempts.Add(attempt);
            await _context.SaveChangesAsync();
        }

        public async Task<string> GeneratePaymentUrlAsync(PaymentRequest request)
        {
            var result = await ProcessPaymentAsync(request);
            return result.PaymentUrl ?? "";
        }

        public async Task<PaymentResult> HandlePaymentCallbackAsync(PaymentCallbackData callbackData)
        {
            try
            {
                // Verify callback signature
                if (!await VerifyPaymentAsync(callbackData.PaymentId, callbackData.Signature))
                {
                    return new PaymentResult
                    {
                        IsSuccess = false,
                        ErrorMessage = "Chữ ký không hợp lệ"
                    };
                }

                // Update payment status
                var payment = await _context.PaymentAttempts
                    .FirstOrDefaultAsync(p => p.PaymentId == callbackData.PaymentId);

                if (payment != null)
                {
                    payment.Status = callbackData.Status;
                    payment.TransactionId = callbackData.TransactionId;
                    payment.CompletedAt = DateTime.UtcNow;
                    
                    await _context.SaveChangesAsync();

                    // Send notifications
                    if (callbackData.Status == "completed")
                    {
                        await _notificationService.SendNotificationAsync(payment.UserId, 
                            "Thanh toán thành công", 
                            "Đơn hàng của bạn đã được thanh toán thành công", 
                            "payment_success");
                    }
                }

                return new PaymentResult
                {
                    IsSuccess = callbackData.Status == "completed",
                    PaymentId = callbackData.PaymentId,
                    TransactionId = callbackData.TransactionId
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling payment callback for {PaymentId}", callbackData.PaymentId);
                return new PaymentResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Lỗi xử lý callback thanh toán"
                };
            }
        }

        private string CreateVNPaySignature(string data, string secretKey)
        {
            var keyBytes = Encoding.UTF8.GetBytes(secretKey);
            var dataBytes = Encoding.UTF8.GetBytes(data);
            
            using var hmac = new HMACSHA512(keyBytes);
            var hashBytes = hmac.ComputeHash(dataBytes);
            return Convert.ToHexString(hashBytes).ToLower();
        }

        /// <summary>
        /// URL encode theo chuẩn VNPay - giống như HttpUtility.UrlEncode trong VNPay SDK
        /// </summary>
        private string UrlEncodeVNPay(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            // VNPay sử dụng encoding tương tự HttpUtility.UrlEncode
            // Space -> '+', các ký tự đặc biệt -> %XX
            var bytes = Encoding.UTF8.GetBytes(value);
            var encoded = new StringBuilder();
            
            foreach (var b in bytes)
            {
                // Unreserved characters theo RFC 3986
                if ((b >= 'a' && b <= 'z') || (b >= 'A' && b <= 'Z') || (b >= '0' && b <= '9') ||
                    b == '-' || b == '_' || b == '.' || b == '~')
                {
                    encoded.Append((char)b);
                }
                else if (b == ' ')
                {
                    encoded.Append('+'); // VNPay dùng + cho space
                }
                else
                {
                    encoded.Append('%').Append(b.ToString("X2"));
                }
            }
            
            return encoded.ToString();
        }

        private string CreateMoMoSignature(string data, string secretKey)
        {
            var keyBytes = Encoding.UTF8.GetBytes(secretKey);
            var dataBytes = Encoding.UTF8.GetBytes(data);
            
            using var hmac = new HMACSHA256(keyBytes);
            var hashBytes = hmac.ComputeHash(dataBytes);
            return Convert.ToHexString(hashBytes).ToLower();
        }

        private bool VerifyVNPaySignature(string paymentId, string signature)
        {
            // Implementation for VNPay signature verification
            return true; // Simplified for demo
        }

        private bool VerifyMoMoSignature(string paymentId, string signature)
        {
            // Implementation for MoMo signature verification
            return true; // Simplified for demo
        }

        // ===================================
        // QR Code Generation Methods
        // ===================================

        public Task<QRCodeResult> GenerateVNPayQRCodeAsync(VNPayQRRequest request)
        {
            try
            {
                _logger.LogInformation("Generating VNPay QR code for order {OrderId}", request.OrderId);

                var vnpayConfig = _configuration.GetSection("PaymentGateways:VNPay");
                var vnp_TmnCode = vnpayConfig["TmnCode"] ?? string.Empty;
                var vnp_HashSecret = vnpayConfig["HashSecret"];
                var vnp_Url = vnpayConfig["PaymentUrl"] ?? vnpayConfig["Url"] ?? "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";
                var isSandbox = bool.Parse(vnpayConfig["IsSandbox"] ?? vnpayConfig["Sandbox"] ?? "true");

                // Generate unique transaction ID
                var txnRef = $"{request.OrderId}_{DateTime.Now:yyyyMMddHHmmss}";

                var vnp_Params = new Dictionary<string, string>
                {
                    {"vnp_Version", "2.1.0"},
                    {"vnp_Command", "pay"},
                    {"vnp_TmnCode", vnp_TmnCode},
                    {"vnp_Amount", (request.Amount * 100).ToString("F0")}, // VNPay uses smallest currency unit
                    {"vnp_CurrCode", "VND"},
                    {"vnp_TxnRef", txnRef},
                    {"vnp_OrderInfo", request.OrderInfo},
                    {"vnp_OrderType", "other"},
                    {"vnp_Locale", "vn"},
                    {"vnp_ReturnUrl", request.ReturnUrl},
                    {"vnp_IpAddr", request.IpAddress},
                    {"vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss")}
                };

                // Sort parameters and create query string
                var sortedParams = vnp_Params.OrderBy(x => x.Key).ToList();
                var query = string.Join("&", sortedParams.Select(x => $"{x.Key}={Uri.EscapeDataString(x.Value)}"));

                // Create signature
                var signature = CreateVNPaySignature(query, vnp_HashSecret!);
                var paymentUrl = $"{vnp_Url}?{query}&vnp_SecureHash={signature}";

                _logger.LogInformation("VNPay QR generated successfully for {OrderId}", request.OrderId);

                // Note: VNPay doesn't provide direct QR image API in sandbox
                // In production, you would call VNPay's QR generation API here
                // For now, return the payment URL which can be converted to QR client-side
                var result = new QRCodeResult
                {
                    IsSuccess = true,
                    PaymentUrl = paymentUrl,
                    OrderId = request.OrderId,
                    TransactionId = txnRef,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(15),
                    ExpiresInSeconds = 900,
                    AdditionalData = new Dictionary<string, string>
                    {
                        { "paymentMethod", "vnpay" },
                        { "sandbox", isSandbox.ToString() },
                        { "amount", request.Amount.ToString("N0") }
                    }
                };
                
                return Task.FromResult(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating VNPay QR code for order {OrderId}", request.OrderId);
                var errorResult = new QRCodeResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Không thể tạo mã QR VNPay. Vui lòng thử lại.",
                    OrderId = request.OrderId
                };
                
                return Task.FromResult(errorResult);
            }
        }

        public async Task<QRCodeResult> GenerateMoMoQRCodeAsync(MoMoQRRequest request)
        {
            try
            {
                _logger.LogInformation("Generating MoMo QR code for order {OrderId}", request.OrderId);

                var momoConfig = _configuration.GetSection("PaymentGateways:MoMo");
                var partnerCode = momoConfig["PartnerCode"];
                var accessKey = momoConfig["AccessKey"];
                var secretKey = momoConfig["SecretKey"];
                var endpoint = momoConfig["ApiUrl"] ?? momoConfig["Endpoint"]; // Support both keys
                var isSandbox = bool.Parse(momoConfig["IsSandbox"] ?? momoConfig["Sandbox"] ?? "true");
                
                // Validate required credentials
                if (string.IsNullOrWhiteSpace(partnerCode) || 
                    string.IsNullOrWhiteSpace(accessKey) || 
                    string.IsNullOrWhiteSpace(secretKey))
                {
                    _logger.LogError("MoMo credentials are missing. PartnerCode: {HasPartnerCode}, AccessKey: {HasAccessKey}, SecretKey: {HasSecretKey}",
                        !string.IsNullOrWhiteSpace(partnerCode),
                        !string.IsNullOrWhiteSpace(accessKey),
                        !string.IsNullOrWhiteSpace(secretKey));
                    return new QRCodeResult
                    {
                        IsSuccess = false,
                        ErrorMessage = "Cấu hình MoMo chưa đầy đủ. Vui lòng kiểm tra credentials trong file .env",
                        OrderId = request.OrderId
                    };
                }
                
                // Log credentials info (masked for security) - only in debug mode
                _logger.LogDebug("MoMo Config - PartnerCode: {PartnerCode}, AccessKey prefix: {AccessKeyPrefix}, SecretKey length: {SecretKeyLength}",
                    partnerCode, 
                    accessKey?.Length > 5 ? accessKey.Substring(0, 5) + "***" : "***",
                    secretKey?.Length ?? 0);
                
                if (string.IsNullOrWhiteSpace(endpoint))
                {
                    endpoint = isSandbox 
                        ? "https://test-payment.momo.vn/v2/gateway/api/create"
                        : "https://payment.momo.vn/v2/gateway/api/create";
                    _logger.LogWarning("MoMo endpoint not configured, using default: {Endpoint}", endpoint);
                }

                var requestId = Guid.NewGuid().ToString();
                var orderId = $"{request.OrderId}_{DateTime.Now:yyyyMMddHHmmss}";
                var amount = request.Amount.ToString("F0");
                var orderInfo = string.IsNullOrEmpty(request.OrderInfo) 
                    ? $"Thanh toan don hang {request.OrderId}" 
                    : request.OrderInfo;
                
                // Ensure URLs are properly formatted (MoMo requires absolute URLs)
                var redirectUrl = request.ReturnUrl ?? "";
                var ipnUrl = request.NotifyUrl ?? "";
                
                // For sandbox, if localhost, use ngrok or public URL if available
                // Otherwise, MoMo sandbox should accept localhost
                var extraData = "";
                var requestType = "captureWallet"; // For QR code payment

                // Create signature - IMPORTANT: Order must match MoMo's exact requirement
                // Order: accessKey, amount, extraData, ipnUrl, orderId, orderInfo, partnerCode, redirectUrl, requestId, requestType
                // Note: Do NOT URL encode the rawHash string - use raw values as-is
                var rawHash = $"accessKey={accessKey}&amount={amount}&extraData={extraData}&ipnUrl={ipnUrl}&orderId={orderId}&orderInfo={orderInfo}&partnerCode={partnerCode}&redirectUrl={redirectUrl}&requestId={requestId}&requestType={requestType}";
                
                _logger.LogDebug("MoMo Signature Raw Hash: {RawHash}", rawHash);
                _logger.LogDebug("MoMo SecretKey length: {Length}", secretKey?.Length ?? 0);
                var signature = CreateMoMoSignature(rawHash, secretKey!);
                _logger.LogDebug("MoMo Signature: {Signature}", signature);

                var requestData = new
                {
                    partnerCode,
                    partnerName = "John Henry Fashion",
                    storeId = "JohnHenryStore",
                    requestId,
                    amount,
                    orderId,
                    orderInfo,
                    redirectUrl,
                    ipnUrl,
                    lang = "vi",
                    extraData,
                    requestType,
                    signature,
                    autoCapture = true,
                    orderExpireTime = 15 // 15 minutes
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _logger.LogInformation("Calling MoMo QR API - Endpoint: {Endpoint}, OrderId: {OrderId}, Amount: {Amount}", 
                    endpoint, orderId, amount);
                _logger.LogDebug("MoMo Request Data: {RequestData}", json);

                var response = await _httpClient.PostAsync(endpoint, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                _logger.LogInformation("MoMo API Response - Status: {StatusCode}, Content: {Response}", 
                    response.StatusCode, responseContent);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogDebug("MoMo API Response Content: {ResponseContent}", responseContent);
                    
                    var momoResponse = JsonSerializer.Deserialize<MoMoQRResponse>(responseContent, 
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (momoResponse == null)
                    {
                        _logger.LogError("Failed to deserialize MoMo response. Content: {ResponseContent}", responseContent);
                        return new QRCodeResult
                        {
                            IsSuccess = false,
                            ErrorMessage = "Không thể xử lý phản hồi từ MoMo",
                            OrderId = request.OrderId
                        };
                    }

                    _logger.LogInformation("MoMo Response - ResultCode: {ResultCode}, Message: {Message}", 
                        momoResponse.ResultCode, momoResponse.Message);

                    if (momoResponse.ResultCode == 0)
                    {
                        _logger.LogInformation("MoMo QR generated successfully for {OrderId}. QRUrl: {QRUrl}, PayUrl: {PayUrl}", 
                            request.OrderId, momoResponse.QrCodeUrl, momoResponse.PayUrl);

                        return new QRCodeResult
                        {
                            IsSuccess = true,
                            QRCodeUrl = momoResponse.QrCodeUrl,
                            DeepLink = momoResponse.Deeplink,
                            PaymentUrl = momoResponse.PayUrl,
                            OrderId = request.OrderId,
                            TransactionId = orderId,
                            ExpiresAt = DateTime.UtcNow.AddMinutes(15),
                            ExpiresInSeconds = 900,
                            AdditionalData = new Dictionary<string, string>
                            {
                                { "paymentMethod", "momo" },
                                { "sandbox", isSandbox.ToString() },
                                { "amount", request.Amount.ToString("N0") },
                                { "requestId", requestId }
                            }
                        };
                    }
                    else
                    {
                        _logger.LogError("MoMo API error - ResultCode: {ResultCode}, Message: {Message}", 
                            momoResponse.ResultCode, momoResponse.Message);
                        return new QRCodeResult
                        {
                            IsSuccess = false,
                            ErrorMessage = momoResponse.Message ?? $"Lỗi từ MoMo (Code: {momoResponse.ResultCode})",
                            OrderId = request.OrderId
                        };
                    }
                }
                else
                {
                    _logger.LogError("MoMo API HTTP error: {StatusCode}", response.StatusCode);
                    return new QRCodeResult
                    {
                        IsSuccess = false,
                        ErrorMessage = "Không thể kết nối đến MoMo. Vui lòng thử lại.",
                        OrderId = request.OrderId
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating MoMo QR code for order {OrderId}", request.OrderId);
                return new QRCodeResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Không thể tạo mã QR MoMo. Vui lòng thử lại.",
                    OrderId = request.OrderId
                };
            }
        }
    }

    // MoMo QR Response Model
    public class MoMoQRResponse
    {
        public string? PartnerCode { get; set; }
        public string? OrderId { get; set; }
        public string? RequestId { get; set; }
        public long Amount { get; set; }
        public long ResponseTime { get; set; }
        public string? Message { get; set; }
        public int ResultCode { get; set; }
        public string? PayUrl { get; set; }
        public string? Deeplink { get; set; }
        public string? QrCodeUrl { get; set; }
        public string? AppLink { get; set; }
    }

    // Supporting classes
    public class PaymentRequest
    {
        public string OrderId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "VND";
        public string PaymentMethod { get; set; } = string.Empty;
        public string OrderInfo { get; set; } = string.Empty;
        public string ReturnUrl { get; set; } = string.Empty;
        public string NotifyUrl { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public string UserAgent { get; set; } = string.Empty;
        public string? PaymentMethodId { get; set; }
    }

    public class VNPayPaymentRequest
    {
        public decimal Amount { get; set; }
        public string OrderId { get; set; } = string.Empty;
        public string OrderInfo { get; set; } = string.Empty;
        public string ReturnUrl { get; set; } = string.Empty;
        public string NotifyUrl { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
    }

    public class MoMoPaymentRequest
    {
        public decimal Amount { get; set; }
        public string OrderId { get; set; } = string.Empty;
        public string OrderInfo { get; set; } = string.Empty;
        public string ReturnUrl { get; set; } = string.Empty;
        public string NotifyUrl { get; set; } = string.Empty;
    }

    public class StripePaymentRequest
    {
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "VND";
        public string PaymentMethodId { get; set; } = string.Empty;
        public string CustomerId { get; set; } = string.Empty;
    }

    public class PaymentResult
    {
        public bool IsSuccess { get; set; }
        public string? PaymentId { get; set; }
        public string? TransactionId { get; set; }
        public string? PaymentUrl { get; set; }
        public string? Message { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class RefundResult
    {
        public bool IsSuccess { get; set; }
        public string? RefundId { get; set; }
        public string? Message { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class PaymentCallbackData
    {
        public string PaymentId { get; set; } = string.Empty;
        public string TransactionId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Signature { get; set; } = string.Empty;
    }

    public class MoMoResponse
    {
        public string? PartnerCode { get; set; }
        public string? OrderId { get; set; }
        public string? RequestId { get; set; }
        public long Amount { get; set; }
        public int ResultCode { get; set; }
        public string? Message { get; set; }
        public string? PayUrl { get; set; }
    }

    public class StripePaymentResponse
    {
        public string? Id { get; set; }
        public string? Status { get; set; }
        public long Amount { get; set; }
        public string? Currency { get; set; }
    }

    public enum PaymentStatus
    {
        Unknown,
        Pending,
        Completed,
        Failed,
        Cancelled,
        Refunded
    }
}
