/**
 * CART VALIDATION SCRIPT
 * Purpose: Validate product data before adding to cart
 * Created: 2025-10-22
 */

// Global validation function
function validateProductData(productId, productName, quantity) {
    const errors = [];
    
    // Validate productId (SKU)
    if (!productId || productId === 'null' || productId === 'undefined' || productId.trim() === '') {
        errors.push('Mã sản phẩm không hợp lệ');
        console.error('VALIDATION ERROR: Invalid product SKU', {
            productId: productId,
            productName: productName,
            type: typeof productId,
            isEmpty: !productId,
            isNull: productId === null,
            isUndefined: productId === undefined
        });
    }
    
    // Validate quantity
    if (!quantity || isNaN(quantity) || quantity <= 0) {
        errors.push('Số lượng không hợp lệ');
        console.error('VALIDATION ERROR: Invalid quantity', {
            quantity: quantity,
            type: typeof quantity,
            parsed: parseInt(quantity)
        });
    }
    
    // Log validation result
    if (errors.length > 0) {
        console.error('Product validation failed:', {
            productId: productId,
            productName: productName,
            quantity: quantity,
            errors: errors
        });
    } else {
        console.log('Product validation passed:', {
            productId: productId,
            productName: productName,
            quantity: quantity
        });
    }
    
    return {
        isValid: errors.length === 0,
        errors: errors
    };
}

// Enhanced Add to Cart function with validation
async function addToCartWithValidation(productId, quantity = 1, size = null, color = null) {
    console.group('🛒 Add to Cart Request');
    console.log('Input parameters:', { productId, quantity, size, color });
    
    // Get product name from DOM if available
    const productName = document.querySelector(`[data-product-id="${productId}"]`)?.dataset?.productName || 'Unknown';
    
    // Validate data
    const validation = validateProductData(productId, productName, quantity);
    
    if (!validation.isValid) {
        console.error('Validation failed:', validation.errors);
        console.groupEnd();
        
        alert('Lỗi: ' + validation.errors.join(', ') + '\nVui lòng thử lại hoặc liên hệ hỗ trợ.');
        return {
            success: false,
            message: validation.errors.join(', ')
        };
    }
    
    // Create request data as JSON
    const requestData = {
        productId: productId.trim(),
        quantity: parseInt(quantity)
    };
    
    if (size) requestData.size = size;
    if (color) requestData.color = color;
    
    console.log('Sending request to /Products/AddToCart with data:', requestData);
    
    try {
        const response = await fetch('/Products/AddToCart', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'X-Requested-With': 'XMLHttpRequest'
            },
            body: JSON.stringify(requestData)
        });
        
        console.log('📥 Response status:', response.status);
        
        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }
        
        const result = await response.json();
        console.log('📥 Response data:', result);
        
        if (result.success) {
            console.log('Add to cart successful');
            
            // Update cart count in header
            updateCartCount(result.cartCount);
            
            // Show success message
            showToast(result.message || 'Đã thêm vào giỏ hàng', 'success');
            
            // Trigger cart updated event (for sidebar reload)
            document.dispatchEvent(new CustomEvent('cartUpdated', {
                detail: { 
                    cartCount: result.cartCount,
                    cartTotal: result.cartTotal,
                    productName: result.productName
                }
            }));
            
            // Trigger product added event (for sidebar open)
            document.dispatchEvent(new CustomEvent('productAddedToCart', {
                detail: { 
                    productId: productId,
                    productName: result.productName,
                    cartCount: result.cartCount,
                    cartTotal: result.cartTotal
                }
            }));
        } else {
            console.error('Add to cart failed:', result.message);
            showToast(result.message || 'Không thể thêm vào giỏ hàng', 'error');
        }
        
        console.groupEnd();
        return result;
        
    } catch (error) {
        console.error('Network error:', error);
        console.groupEnd();
        
        showToast('Lỗi kết nối. Vui lòng thử lại.', 'error');
        return {
            success: false,
            message: error.message
        };
    }
}

// Update cart count in header
function updateCartCount(count) {
    const cartCountElements = document.querySelectorAll('.cart-count, .cart-badge');
    cartCountElements.forEach(element => {
        element.textContent = count;
        
        // Animate the change
        element.classList.add('pulse');
        setTimeout(() => element.classList.remove('pulse'), 300);
    });
    
    console.log('Updated cart count to:', count);
}

// Show toast notification
function showToast(message, type = 'info') {
    // Try to use Bootstrap toast if available
    if (typeof bootstrap !== 'undefined' && bootstrap.Toast) {
        let toastContainer = document.getElementById('toast-container');
        if (!toastContainer) {
            toastContainer = document.createElement('div');
            toastContainer.id = 'toast-container';
            toastContainer.className = 'position-fixed top-0 end-0 p-3';
            toastContainer.style.zIndex = '9999';
            document.body.appendChild(toastContainer);
        }
        
        const toastId = 'toast-' + Date.now();
        const bgClass = type === 'success' ? 'bg-success' : type === 'error' ? 'bg-danger' : 'bg-info';
        
        const toastHTML = `
            <div id="${toastId}" class="toast align-items-center text-white ${bgClass} border-0" role="alert">
                <div class="d-flex">
                    <div class="toast-body">${message}</div>
                    <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast"></button>
                </div>
            </div>
        `;
        
        toastContainer.insertAdjacentHTML('beforeend', toastHTML);
        const toastElement = document.getElementById(toastId);
        const toast = new bootstrap.Toast(toastElement);
        toast.show();
        
        toastElement.addEventListener('hidden.bs.toast', () => toastElement.remove());
    } else {
        // Fallback to alert
        alert(message);
    }
}

// Add pulse animation CSS
if (!document.getElementById('cart-validation-styles')) {
    const style = document.createElement('style');
    style.id = 'cart-validation-styles';
    style.textContent = `
        .pulse {
            animation: pulse 0.3s ease-in-out;
        }
        
        @keyframes pulse {
            0%, 100% { transform: scale(1); }
            50% { transform: scale(1.2); }
        }
        
        .cart-count, .cart-badge {
            transition: all 0.3s ease;
        }
    `;
    document.head.appendChild(style);
}

// Auto-attach to all add-to-cart buttons
document.addEventListener('DOMContentLoaded', function() {
    console.log('Cart validation script initialized');
    
    // Find and attach to all add-to-cart buttons
    const addToCartButtons = document.querySelectorAll('.btn-add-to-cart, .btn-add-cart, [data-action="add-to-cart"]');
    
    console.log(`Found ${addToCartButtons.length} add-to-cart buttons`);
    
    addToCartButtons.forEach(button => {
        button.addEventListener('click', async function(e) {
            e.preventDefault();
            
            const productId = this.dataset.productId || this.dataset.productSku;
            const quantity = parseInt(this.dataset.quantity || document.querySelector('#quantity-input')?.value || 1);
            const size = this.dataset.size || document.querySelector('input[name="size"]:checked')?.value;
            const color = this.dataset.color || document.querySelector('input[name="color"]:checked')?.value;
            
            console.log('Button clicked:', {
                button: this,
                productId: productId,
                quantity: quantity,
                size: size,
                color: color
            });
            
            await addToCartWithValidation(productId, quantity, size, color);
        });
    });
    
    // Debug: Log all product cards with their data attributes
    const productCards = document.querySelectorAll('[data-product-id], [data-product-sku]');
    if (productCards.length > 0) {
        console.log('Product cards found:', productCards.length);
        productCards.forEach((card, index) => {
            const productId = card.dataset.productId || card.dataset.productSku;
            const productName = card.dataset.productName;
            
            if (!productId || productId === 'null' || productId === 'undefined') {
                console.warn(`Product card ${index} has invalid SKU:`, {
                    element: card,
                    productId: productId,
                    productName: productName,
                    allAttributes: card.dataset
                });
            }
        });
    }
});

// Export for use in other scripts
if (typeof window !== 'undefined') {
    window.addToCartWithValidation = addToCartWithValidation;
    window.validateProductData = validateProductData;
}

console.log('Cart validation script loaded');
