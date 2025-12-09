// Payment JavaScript - Redesigned
let stripe;
let elements;
let cardNumber, cardExpiry, cardCvc;
let pollingInterval;
let countdown = 900; // 15 minutes

$(document).ready(function() {
    initializePaymentForm();
    initializeStripe();
    initializeCopyButtons();
    
    // Show appropriate payment section based on selected method
    showPaymentSection();
    
    // Auto-generate QR code for VNPay or MoMo ONLY if payment method is already selected
    // Don't auto-generate if user needs to select first
    const selectedMethod = $('input[name="SelectedPaymentMethod"]').val();
    const hasPaymentMethodSelection = $('.payment-methods-selection').length > 0;
    
    console.log('Payment page loaded - SelectedMethod:', selectedMethod, 'HasSelectionUI:', hasPaymentMethodSelection);
    
    // Only auto-generate if method is selected AND no selection UI is shown
    if (selectedMethod && !hasPaymentMethodSelection && (selectedMethod === 'vnpay' || selectedMethod === 'momo')) {
        console.log('Auto-generating QR code for', selectedMethod);
        // Delay a bit to ensure page is fully loaded
        setTimeout(() => {
        generateAndShowQRCode(selectedMethod);
        }, 500);
    } else {
        console.log('Skipping auto QR generation - Method:', selectedMethod, 'HasSelectionUI:', hasPaymentMethodSelection);
    }
});

function initializePaymentForm() {
    // Handle payment form submission
    $('#paymentForm').submit(function(e) {
        e.preventDefault();
        
        if (validatePaymentForm()) {
            processPayment();
        }
    });
}

function showPaymentSection() {
    // No need to handle payment method changes since it's pre-selected
    // Just show the correct section on page load
    const selectedMethod = $('input[name="SelectedPaymentMethod"]').val();
    
    // Hide all sections first
    $('.payment-details-form, .payment-qr-section').hide();
    
    // Show appropriate section
    switch (selectedMethod) {
        case 'vnpay':
            $('#vnpayPaymentForm').show();
            break;
        case 'momo':
            $('#momoPaymentForm').show();
            break;
        case 'stripe':
            $('#stripePaymentForm').show();
            break;
        case 'bank_transfer':
            $('#bankTransferForm').show();
            break;
        case 'cod':
            $('#codForm').show();
            break;
    }
}

function initializeCopyButtons() {
    // Handle copy bank account number and transfer content
    $('.copy-btn').click(function() {
        const textToCopy = $(this).data('copy');
        copyToClipboard(textToCopy);
        
        // Show feedback
        const originalHtml = $(this).html();
        $(this).html('<i class="fas fa-check"></i> Đã copy!').prop('disabled', true);
        
        setTimeout(() => {
            $(this).html(originalHtml).prop('disabled', false);
        }, 2000);
    });
}

function initializeStripe() {
    // Initialize Stripe if available
    if (typeof Stripe !== 'undefined') {
        // Replace with your publishable key
        stripe = Stripe('pk_test_51SMLP3KDeUHV329keOxjGoZw3WCiC1Th0Qw6ppGktAcPm7MQhUe2rCq6Gcq3pykoIFTmuVWA4KdL1AkuvEekeVlK00axb4qUUb');
        elements = stripe.elements();

        // Create card elements
        const style = {
            base: {
                fontSize: '16px',
                color: '#424770',
                '::placeholder': {
                    color: '#aab7c4',
                },
            },
            invalid: {
                color: '#9e2146',
            },
        };

        cardNumber = elements.create('cardNumber', { style });
        cardExpiry = elements.create('cardExpiry', { style });
        cardCvc = elements.create('cardCvc', { style });

        cardNumber.mount('#stripeCardNumber');
        cardExpiry.mount('#stripeCardExpiry');
        cardCvc.mount('#stripeCardCvc');

        // Handle real-time validation errors from the card Element
        cardNumber.on('change', handleCardChange);
        cardExpiry.on('change', handleCardChange);
        cardCvc.on('change', handleCardChange);
    }
}

function handleCardChange(event) {
    const displayError = document.getElementById('card-errors');
    if (event.error) {
        displayError.textContent = event.error.message;
    } else {
        displayError.textContent = '';
    }
}

// Open MoMo App
function openMoMoApp() {
    const sessionId = $('#SessionId').val();
    const amountText = $('.payment-amount').first().text();
    const amount = amountText.replace(/[^\d]/g, '');
    
    // MoMo deep link format
    const momoUrl = `momo://app?action=payment&amount=${amount}&note=Thanh toan don hang ${sessionId}`;
    
    // Try to open MoMo app
    window.location.href = momoUrl;
    
    // Fallback: If app doesn't open, show instruction
    setTimeout(() => {
        showInfo('Nếu ứng dụng MoMo không tự động mở, vui lòng mở ứng dụng và quét mã QR.');
    }, 1000);
}

// Show info message
function showInfo(message) {
    const alertHtml = `
        <div class="alert alert-info alert-dismissible fade show" role="alert">
            <i class="fas fa-info-circle me-2"></i>
            ${message}
            <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
        </div>
    `;
    
    $('#paymentForm').prepend(alertHtml);
    $('html, body').animate({ scrollTop: 0 }, 300);
    
    setTimeout(() => {
        $('.alert').fadeOut();
    }, 8000);
}

// Start payment status polling (for QR code payments)
function startPaymentPolling(sessionId) {
    // Stop any existing polling
    if (pollingInterval) clearInterval(pollingInterval);
    
    // Reset countdown
    countdown = 900; // 15 minutes
    updateCountdown();
    
    // Poll every 3 seconds
    pollingInterval = setInterval(async () => {
        try {
            const response = await fetch(`/Checkout/CheckPaymentStatus?sessionId=${sessionId}`);
            const data = await response.json();
            
            if (data.status === 'paid') {
                // Payment successful!
                stopPolling();
                showPaymentSuccess(data);
                
                // Redirect after 2 seconds
                setTimeout(() => {
                    window.location.href = data.redirectUrl;
                }, 2000);
            } else if (data.status === 'failed') {
                // Payment failed
                stopPolling();
                showPaymentError(data.message);
            }
            
        } catch (error) {
            console.error('Polling error:', error);
        }
    }, 3000);
}

function updateCountdown() {
    const method = $('input[name="SelectedPaymentMethod"]').val();
    const countdownId = method === 'vnpay' ? '#vnpayCountdown' : '#momoCountdown';
    
    const countdownInterval = setInterval(() => {
        countdown--;
        
        if (countdown <= 0) {
            stopPolling();
            clearInterval(countdownInterval);
            showPaymentTimeout();
            return;
        }
        
        const minutes = Math.floor(countdown / 60);
        const seconds = countdown % 60;
        $(countdownId).text(`${minutes.toString().padStart(2, '0')}:${seconds.toString().padStart(2, '0')}`);
        
        // Update timer class based on time remaining
        const timerElement = $(countdownId).parent();
        timerElement.removeClass('warning danger');
        
        if (countdown < 60) {
            timerElement.addClass('danger');
        } else if (countdown < 120) {
            timerElement.addClass('warning');
        }
    }, 1000);
}

function stopPolling() {
    if (pollingInterval) clearInterval(pollingInterval);
}

function showPaymentSuccess(data) {
    const method = $('input[name="SelectedPaymentMethod"]').val();
    const qrSectionId = method === 'vnpay' ? '#vnpayPaymentForm' : '#momoPaymentForm';
    
    $(qrSectionId + ' .qr-payment-container').html(`
        <div class="payment-success-animation">
            <i class="fas fa-check-circle fa-5x text-success mb-3"></i>
            <h3 class="text-success mb-2">Thanh toán thành công!</h3>
            <p class="text-muted">Đang chuyển hướng đến trang xác nhận...</p>
        </div>
    `);
}

function showPaymentError(message) {
    showError(message || 'Thanh toán thất bại. Vui lòng thử lại.');
}

function showPaymentTimeout() {
    const method = $('input[name="SelectedPaymentMethod"]').val();
    const qrSectionId = method === 'vnpay' ? '#vnpayPaymentForm' : '#momoPaymentForm';
    
    $(qrSectionId + ' .qr-code-display').html(`
        <i class="fas fa-clock fa-4x text-warning mb-3"></i>
        <h5 class="text-warning mb-2">Mã QR đã hết hạn</h5>
        <p class="text-muted">Vui lòng tạo lại mã QR mới</p>
        <button type="button" class="btn btn-primary mt-3" onclick="location.reload()">
            <i class="fas fa-redo me-2"></i>Tạo mã mới
        </button>
    `);
}

function validatePaymentForm() {
    // Check if payment method is selected from radio buttons
    const radioMethod = $('input[name="paymentMethodRadio"]:checked').val();
    const hiddenMethod = $('input[name="SelectedPaymentMethod"]').val();
    const selectedPaymentMethod = radioMethod || hiddenMethod;
    
    if (!selectedPaymentMethod) {
        showError('Vui lòng chọn phương thức thanh toán');
        // Scroll to payment method selection
        $('html, body').animate({
            scrollTop: $('.payment-methods-selection').offset().top - 100
        }, 500);
        return false;
    }
    
    // Update hidden field if radio button is selected
    if (radioMethod && radioMethod !== hiddenMethod) {
        $('input[name="SelectedPaymentMethod"]').val(radioMethod);
    }

    // Validate specific payment methods
    switch (selectedPaymentMethod) {
        case 'stripe':
            return validateStripeForm();
        default:
            return true;
    }
}

function validateStripeForm() {
    const cardholderName = $('#stripeCardholderName').val().trim();
    
    if (!cardholderName) {
        showError('Vui lòng nhập tên chủ thẻ');
        return false;
    }
    
    // Stripe elements validation will be handled by Stripe
    return true;
}

function validateBankTransferForm() {
    // Bank transfer doesn't require additional validation
    return true;
}

function processPayment() {
    const selectedPaymentMethod = $('input[name="SelectedPaymentMethod"]').val();
    const radioMethod = $('input[name="paymentMethodRadio"]:checked').val();
    const finalMethod = radioMethod || selectedPaymentMethod;
    
    // Update hidden field
    if (finalMethod) {
        $('input[name="SelectedPaymentMethod"]').val(finalMethod);
        $('#PaymentMethod').val(finalMethod);
    }
    
    // Show loading modal
    $('#paymentLoadingModal').modal('show');
    
    if (finalMethod === 'stripe') {
        processStripePayment();
    } else {
        // For other methods (including VNPay/MoMo), submit form normally
        // Payment gateway URL will be returned and user will be redirected
        processStandardPayment();
    }
}

function processStripePayment() {
    const cardholderName = $('#stripeCardholderName').val();
    
    stripe.createToken(cardNumber, {
        name: cardholderName
    }).then(function(result) {
        if (result.error) {
            // Show error to your customer
            showError(result.error.message);
            $('#paymentLoadingModal').modal('hide');
        } else {
            // Send the token to your server
            submitPaymentWithToken(result.token);
        }
    });
}

function submitPaymentWithToken(token) {
    const formData = getPaymentFormData();
    formData.stripeToken = token.id;
    
    submitPaymentForm(formData);
}

function processStandardPayment() {
    const formData = getPaymentFormData();
    submitPaymentForm(formData);
}

function getPaymentFormData() {
    // Check both radio button and hidden field
    const radioMethod = $('input[name="paymentMethodRadio"]:checked').val();
    const hiddenMethod = $('input[name="SelectedPaymentMethod"]').val();
    const selectedPaymentMethod = radioMethod || hiddenMethod;
    
    // Update both hidden fields
    if (selectedPaymentMethod) {
        $('input[name="SelectedPaymentMethod"]').val(selectedPaymentMethod);
        $('#PaymentMethod').val(selectedPaymentMethod);
    }
    
    return {
        SessionId: $('#SessionId').val(),
        PaymentMethod: selectedPaymentMethod,
        PaymentMethodId: $('input[name="PaymentMethodId"]').val() || null
    };
}

function submitPaymentForm(formData) {
    console.log('Submitting payment form with data:', formData);
    
    // Ensure PaymentMethod is set
    if (!formData.PaymentMethod) {
        showError('Vui lòng chọn phương thức thanh toán');
        return;
    }
    
    // Convert to form data format for ASP.NET Core model binding
    // ASP.NET Core expects PascalCase property names
    const formDataObj = {
        SessionId: formData.SessionId,
        PaymentMethod: formData.PaymentMethod,
        PaymentMethodId: formData.PaymentMethodId || null
    };
    
    const csrfToken = $('input[name="__RequestVerificationToken"]').val();
    
    console.log('Sending form data:', formDataObj);
    console.log('CSRF Token:', csrfToken ? 'Present' : 'Missing');
    
    $.ajax({
        url: $('#paymentForm').attr('action'),
        method: 'POST',
        data: formDataObj,
        headers: {
            'RequestVerificationToken': csrfToken,
            'X-Requested-With': 'XMLHttpRequest'
        },
        success: function(response) {
            console.log('Payment response:', response);
            $('#paymentLoadingModal').modal('hide');
            
            if (response.success) {
                if (response.redirectUrl) {
                    // Redirect to payment gateway or success page
                    console.log('Redirecting to:', response.redirectUrl);
                    window.location.href = response.redirectUrl;
                } else {
                    showSuccess(response.message || 'Thanh toán thành công');
                }
            } else {
                const errorMsg = response.message || 'Thanh toán thất bại';
                if (response.errors) {
                    console.error('Validation errors:', response.errors);
                }
                showError(errorMsg);
            }
        },
        error: function(xhr, status, error) {
            console.error('Payment error:', {
                status: xhr.status,
                statusText: xhr.statusText,
                responseText: xhr.responseText,
                error: error
            });
            
            $('#paymentLoadingModal').modal('hide');
            
            let errorMessage = 'Có lỗi xảy ra khi xử lý thanh toán';
            
            if (xhr.responseJSON) {
                if (xhr.responseJSON.message) {
                errorMessage = xhr.responseJSON.message;
                }
                if (xhr.responseJSON.errors) {
                    console.error('Model errors:', xhr.responseJSON.errors);
                    const errorList = xhr.responseJSON.errors.map(e => e.Errors.join(', ')).join('; ');
                    errorMessage += ': ' + errorList;
                }
            } else if (xhr.responseText) {
                try {
                    const errorData = JSON.parse(xhr.responseText);
                    if (errorData.message) {
                        errorMessage = errorData.message;
                    }
                } catch (e) {
                    errorMessage = `Lỗi ${xhr.status}: ${xhr.statusText}`;
                }
            }
            
            showError(errorMessage);
        }
    });
}

function updatePaymentButtonText(paymentMethod) {
    const buttonText = getPaymentButtonText(paymentMethod);
    $('#paymentButtonText').text(buttonText);
}

function getPaymentButtonText(paymentMethod) {
    switch (paymentMethod) {
        case 'vnpay':
            return 'Thanh toán với VNPay';
        case 'momo':
            return 'Thanh toán với MoMo';
        case 'stripe':
            return 'Thanh toán bằng thẻ';
        case 'cod':
            return 'Xác nhận đặt hàng';
        case 'bank_transfer':
            return 'Xác nhận chuyển khoản';
        default:
            return 'Hoàn tất thanh toán';
    }
}

function copyToClipboard(text) {
    if (navigator.clipboard && window.isSecureContext) {
        // Use the Clipboard API when available
        navigator.clipboard.writeText(text);
    } else {
        // Fallback for older browsers
        const textArea = document.createElement('textarea');
        textArea.value = text;
        textArea.style.position = 'fixed';
        textArea.style.left = '-999999px';
        textArea.style.top = '-999999px';
        document.body.appendChild(textArea);
        textArea.focus();
        textArea.select();
        
        try {
            document.execCommand('copy');
        } catch (err) {
            console.error('Error copying text: ', err);
        }
        
        document.body.removeChild(textArea);
    }
}

function showError(message) {
    // Create and show error alert
    const alertHtml = `
        <div class="alert alert-danger alert-dismissible fade show" role="alert">
            <i class="fas fa-exclamation-circle me-2"></i>
            ${message}
            <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
        </div>
    `;
    
    // Insert at the top of the form
    $('#paymentForm').prepend(alertHtml);
    
    // Scroll to top
    $('html, body').animate({ scrollTop: 0 }, 300);
    
    // Auto-remove after 10 seconds
    setTimeout(() => {
        $('.alert').fadeOut();
    }, 10000);
}

function showSuccess(message) {
    // Create and show success alert
    const alertHtml = `
        <div class="alert alert-success alert-dismissible fade show" role="alert">
            <i class="fas fa-check-circle me-2"></i>
            ${message}
            <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
        </div>
    `;
    
    // Insert at the top of the form
    $('#paymentForm').prepend(alertHtml);
    
    // Scroll to top
    $('html, body').animate({ scrollTop: 0 }, 300);
}

// Handle page unload warning for payment in progress
window.addEventListener('beforeunload', function(e) {
    if ($('#paymentLoadingModal').hasClass('show')) {
        e.preventDefault();
        e.returnValue = 'Thanh toán đang được xử lý. Bạn có chắc chắn muốn rời khỏi trang?';
        return e.returnValue;
    }
});

// Prevent double submission
let isSubmitting = false;

$('#paymentForm').submit(function(e) {
    if (isSubmitting) {
        e.preventDefault();
        return false;
    }
    
    isSubmitting = true;
    
    // Reset flag after 30 seconds to allow retry if needed
    setTimeout(() => {
        isSubmitting = false;
    }, 30000);
});

// Auto-focus on submit button
$(document).ready(function() {
    $('#submitPaymentBtn').focus();
});

// ===================================
// QR Code Generation Functions
// ===================================

async function generateAndShowQRCode(paymentMethod) {
    const sessionId = $('#SessionId').val();
    
    if (!sessionId) {
        console.error('Session ID not found');
        return;
    }

    const qrDisplayId = paymentMethod === 'vnpay' ? '#vnpayQRCode' : '#momoQRCode';
    const countdownSectionId = paymentMethod === 'vnpay' ? '#vnpayPaymentForm .countdown-section' : '#momoPaymentForm .countdown-section';
    
    // Show loading
    $(qrDisplayId).html(`
        <div class="qr-code-loading">
            <div class="spinner-qr"></div>
            <p class="text-muted mt-3">Đang tạo mã QR thanh toán...</p>
        </div>
    `);

    try {
        const response = await fetch('/Checkout/GeneratePaymentQR', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val()
            },
            body: JSON.stringify({
                sessionId: sessionId,
                paymentMethod: paymentMethod
            })
        });

        const data = await response.json();

        if (data.success) {
            // Display QR code
            if (paymentMethod === 'momo' && data.qrCodeUrl) {
                // MoMo provides QR image URL
                $(qrDisplayId).html(`
                    <img src="${data.qrCodeUrl}" alt="MoMo QR Code" class="img-fluid" style="max-width: 300px;" />
                `);
                
                // Show MoMo app button if deep link available
                if (data.deepLink && $('#openMoMoBtn').length) {
                    $('#openMoMoBtn').show();
                    $('#openMoMoBtn').attr('onclick', `window.location.href='${data.deepLink}'`);
                }
            } else if (paymentMethod === 'vnpay' && data.paymentUrl) {
                // VNPay - Generate QR code from URL using QRCode.js library
                $(qrDisplayId).html('<div id="vnpayQRCodeImage"></div>');
                
                // Check if QRCode library is loaded
                if (typeof QRCode !== 'undefined') {
                    new QRCode(document.getElementById('vnpayQRCodeImage'), {
                        text: data.paymentUrl,
                        width: 280,
                        height: 280,
                        colorDark: '#000000',
                        colorLight: '#ffffff',
                        correctLevel: QRCode.CorrectLevel.H
                    });
                } else {
                    // Fallback if library not loaded
                    $(qrDisplayId).html(`
                        <div class="alert alert-info">
                            <i class="fas fa-info-circle me-2"></i>
                            <p class="mb-2">Vui lòng click vào nút bên dưới để thanh toán</p>
                            <a href="${data.paymentUrl}" class="btn btn-primary" target="_blank">
                                <i class="fas fa-external-link-alt me-2"></i>
                                Thanh toán với VNPay
                            </a>
                        </div>
                    `);
                }
            } else if (data.paymentUrl) {
                // Generic fallback for other payment methods
                $(qrDisplayId).html(`
                    <div class="alert alert-info">
                        <a href="${data.paymentUrl}" class="btn btn-primary" target="_blank">
                            <i class="fas fa-external-link-alt me-2"></i>
                            Tiếp tục thanh toán
                        </a>
                    </div>
                `);
            }
            
            // Show countdown
            $(countdownSectionId).show();
            
            // Start countdown and polling
            countdown = data.expiresInSeconds || 900;
            updateCountdown();
            startPaymentPolling(sessionId);
            
        } else {
            // Show error
            $(qrDisplayId).html(`
                <div class="alert alert-danger">
                    <i class="fas fa-exclamation-triangle me-2"></i>
                    <p class="mb-0">${data.message || 'Không thể tạo mã QR'}</p>
                </div>
            `);
        }
    } catch (error) {
        console.error('Error generating QR code:', error);
        $(qrDisplayId).html(`
            <div class="alert alert-danger">
                <i class="fas fa-exclamation-triangle me-2"></i>
                <p class="mb-0">Lỗi kết nối. Vui lòng thử lại.</p>
            </div>
        `);
    }
}

// Payment method specific validations
function validatePaymentMethodSpecific(gateway) {
    switch (gateway) {
        case 'vnpay':
            return validateVNPayPayment();
        case 'momo':
            return validateMoMoPayment();
        case 'stripe':
            return validateStripePayment();
        default:
            return true;
    }
}

function validateVNPayPayment() {
    // VNPay specific validation if needed
    return true;
}

function validateMoMoPayment() {
    // MoMo specific validation if needed
    return true;
}

function validateStripePayment() {
    // This will be handled by Stripe Elements
    return true;
}

// Auto-focus on first payment method
$(document).ready(function() {
    const firstPaymentMethod = $('input[name="SelectedPaymentMethod"]:first');
    if (firstPaymentMethod.length) {
        firstPaymentMethod.focus();
    }
});

// Open MoMo App
function openMoMoApp() {
    const sessionId = $('#SessionId').val();
    const amount = $('.payment-amount-display h4').first().text().replace(/[^\d]/g, '');
    
    // MoMo deep link format
    const momoUrl = `momo://app?action=payment&amount=${amount}&note=Thanh toan don hang ${sessionId}`;
    
    // Try to open MoMo app
    window.location.href = momoUrl;
    
    // Fallback: If app doesn't open, show instruction
    setTimeout(() => {
        showInfo('Nếu ứng dụng MoMo không tự động mở, vui lòng mở ứng dụng và quét mã QR.');
    }, 1000);
}

// Show info message
function showInfo(message) {
    const alertHtml = `
        <div class="alert alert-info alert-dismissible fade show" role="alert">
            <i class="fas fa-info-circle me-2"></i>
            ${message}
            <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
        </div>
    `;
    
    $('#paymentForm').prepend(alertHtml);
    $('html, body').animate({ scrollTop: 0 }, 300);
    
    setTimeout(() => {
        $('.alert').fadeOut();
    }, 8000);
}

// Generate QR Code (placeholder - will be replaced with actual QR generation)
function generatePaymentQR(paymentMethod, amount, orderId) {
    // This is a placeholder. In production, you would:
    // 1. Call your backend API to generate QR code
    // 2. Backend calls VNPay/MoMo API
    // 3. Return QR code image URL or base64
    
    const qrContainer = $(`#${paymentMethod}QRCode`);
    
    // Show loading
    qrContainer.html(`
        <div class="spinner-border text-primary" role="status">
            <span class="visually-hidden">Đang tạo mã QR...</span>
        </div>
        <p class="text-muted mt-3">Đang tạo mã QR thanh toán...</p>
    `);
    
    // Simulate API call
    setTimeout(() => {
        // In production, replace this with actual QR image from API
        qrContainer.html(`
            <div class="alert alert-warning">
                <i class="fas fa-exclamation-triangle me-2"></i>
                Mã QR sẽ được hiển thị sau khi bạn xác nhận thanh toán
            </div>
        `);
    }, 1500);
}

// Auto-select first payment method if only one is available
$(document).ready(function() {
    const paymentMethods = $('input[name="SelectedPaymentMethod"]');
    if (paymentMethods.length === 1) {
        paymentMethods.first().prop('checked', true).trigger('change');
    }
});
