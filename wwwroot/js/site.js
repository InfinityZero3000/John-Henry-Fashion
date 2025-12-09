// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Product interaction functionality
document.addEventListener('DOMContentLoaded', function() {
    // Initialize cart count on page load
    updateCartCount();
    
    // Initialize wishlist states for products on the page
    initializeWishlistStates();
});

// Add to Wishlist functionality
function addToWishlist(productId, buttonElement) {
    if (!isUserAuthenticated()) {
        showAuthRequiredModal();
        return;
    }

    const originalIcon = buttonElement.innerHTML;
    buttonElement.innerHTML = '<i class="fas fa-spinner fa-spin"></i>';
    buttonElement.disabled = true;

    // Create FormData to send productId as form parameter
    const formData = new FormData();
    formData.append('productId', productId);

    fetch('/Wishlist/Add', {
        method: 'POST',
        body: formData
    })
    .then(response => response.json())
    .then(data => {
        if (data.success) {
            buttonElement.innerHTML = '<i class="fas fa-heart text-danger"></i>';
            buttonElement.onclick = () => removeFromWishlist(productId, buttonElement);
            showToast('Success', data.message || 'Đã thêm vào danh sách yêu thích', 'success');
            
            // Update wishlist count if available
            if (data.wishlistCount !== undefined) {
                updateWishlistCount(data.wishlistCount);
            }
        } else {
            buttonElement.innerHTML = originalIcon;
            showToast('Error', data.message || 'Có lỗi khi thêm vào wishlist', 'error');
        }
    })
    .catch(error => {
        buttonElement.innerHTML = originalIcon;
        showToast('Error', 'Có lỗi khi thêm vào danh sách yêu thích', 'error');
        console.error('Error:', error);
    })
    .finally(() => {
        buttonElement.disabled = false;
    });
}

// Remove from Wishlist functionality
function removeFromWishlist(productId, buttonElement) {
    const originalIcon = buttonElement.innerHTML;
    buttonElement.innerHTML = '<i class="fas fa-spinner fa-spin"></i>';
    buttonElement.disabled = true;

    // Create FormData to send productId as form parameter
    const formData = new FormData();
    formData.append('productId', productId);

    fetch('/Wishlist/Remove', {
        method: 'POST',
        body: formData
    })
    .then(response => response.json())
    .then(data => {
        if (data.success) {
            buttonElement.innerHTML = '<i class="far fa-heart"></i>';
            buttonElement.onclick = () => addToWishlist(productId, buttonElement);
            showToast('Success', data.message || 'Đã xóa khỏi danh sách yêu thích', 'success');
            
            // Update wishlist count if available
            if (data.wishlistCount !== undefined) {
                updateWishlistCount(data.wishlistCount);
            }
        } else {
            buttonElement.innerHTML = originalIcon;
            showToast('Error', data.message || 'Có lỗi khi xóa khỏi wishlist', 'error');
        }
    })
    .catch(error => {
        buttonElement.innerHTML = originalIcon;
        showToast('Error', 'Có lỗi khi xóa khỏi danh sách yêu thích', 'error');
        console.error('Error:', error);
    })
    .finally(() => {
        buttonElement.disabled = false;
    });
}

// Add to Cart functionality
function addToCart(productId, buttonElement, quantity = 1, size = null, color = null) {
    if (!isUserAuthenticated()) {
        showAuthRequiredModal();
        return;
    }

    const originalText = buttonElement.innerHTML;
    buttonElement.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Adding...';
    buttonElement.disabled = true;

    const requestData = {
        productId: productId,
        quantity: quantity
    };

    if (size) requestData.size = size;
    if (color) requestData.color = color;

    fetch('/Products/AddToCart', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'X-Requested-With': 'XMLHttpRequest'
        },
        body: JSON.stringify(requestData)
    })
    .then(response => response.json())
    .then(data => {
        if (data.success) {
            buttonElement.innerHTML = '<i class="fas fa-check"></i> Added!';
            updateCartCount(data.cartCount);
            showToast('Success', data.message, 'success');
            
            // Reset button after 2 seconds
            setTimeout(() => {
                buttonElement.innerHTML = originalText;
                buttonElement.disabled = false;
            }, 2000);
        } else {
            buttonElement.innerHTML = originalText;
            showToast('Error', data.message, 'error');
            buttonElement.disabled = false;
        }
    })
    .catch(error => {
        buttonElement.innerHTML = originalText;
        showToast('Error', 'An error occurred while adding to cart', 'error');
        console.error('Error:', error);
        buttonElement.disabled = false;
    });
}

// Update cart count in navigation
function updateCartCount(count = null) {
    if (count !== null) {
        updateCartCountDisplay(count);
    } else {
        // Fetch current cart count
        fetch('/Products/GetCartCount', {
            method: 'GET',
            headers: {
                'X-Requested-With': 'XMLHttpRequest'
            }
        })
        .then(response => response.json())
        .then(data => {
            updateCartCountDisplay(data.cartCount);
        })
        .catch(error => {
            console.error('Error fetching cart count:', error);
        });
    }
}

function updateCartCountDisplay(count) {
    const cartCountElements = document.querySelectorAll('.cart-count');
    cartCountElements.forEach(element => {
        element.textContent = count;
        element.style.display = count > 0 ? 'inline' : 'none';
    });
}

// Initialize wishlist states for products on current page
function initializeWishlistStates() {
    const wishlistButtons = document.querySelectorAll('[data-wishlist-product-id]');
    
    wishlistButtons.forEach(button => {
        const productId = button.getAttribute('data-wishlist-product-id');
        
        fetch(`/Wishlist/IsInWishlist?productId=${productId}`, {
            method: 'GET',
            headers: {
                'X-Requested-With': 'XMLHttpRequest'
            }
        })
        .then(response => response.json())
        .then(data => {
            if (data.isInWishlist) {
                button.innerHTML = '<i class="fas fa-heart text-danger"></i>';
                button.onclick = () => removeFromWishlist(productId, button);
            } else {
                button.innerHTML = '<i class="far fa-heart"></i>';
                button.onclick = () => addToWishlist(productId, button);
            }
        })
        .catch(error => {
            console.error('Error checking wishlist status:', error);
            // Default to not in wishlist
            button.innerHTML = '<i class="far fa-heart"></i>';
            button.onclick = () => addToWishlist(productId, button);
        });
    });
}

// Check if user is authenticated
function isUserAuthenticated() {
    // Check if there's an authentication indicator in the DOM
    return document.querySelector('.user-authenticated') !== null || 
           document.querySelector('[data-user-authenticated="true"]') !== null;
}

// Show authentication required modal
function showAuthRequiredModal() {
    // Create and show a modal for authentication requirement
    const modalHtml = `
        <div class="modal fade" id="authRequiredModal" tabindex="-1" aria-labelledby="authRequiredModalLabel" aria-hidden="true">
            <div class="modal-dialog">
                <div class="modal-content">
                    <div class="modal-header">
                        <h5 class="modal-title" id="authRequiredModalLabel">Login Required</h5>
                        <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
                    </div>
                    <div class="modal-body">
                        <p>You need to login to add items to your wishlist or cart.</p>
                    </div>
                    <div class="modal-footer">
                        <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Cancel</button>
                        <a href="/Account/Login" class="btn btn-primary">Login</a>
                        <a href="/Account/Register" class="btn btn-outline-primary">Register</a>
                    </div>
                </div>
            </div>
        </div>
    `;
    
    // Remove existing modal if any
    const existingModal = document.getElementById('authRequiredModal');
    if (existingModal) {
        existingModal.remove();
    }
    
    // Add modal to page
    document.body.insertAdjacentHTML('beforeend', modalHtml);
    
    // Show modal
    const modal = new bootstrap.Modal(document.getElementById('authRequiredModal'));
    modal.show();
}

// Toast notification system
function showToast(title, message, type = 'info') {
    // Create toast container if it doesn't exist
    let toastContainer = document.getElementById('toast-container');
    if (!toastContainer) {
        toastContainer = document.createElement('div');
        toastContainer.id = 'toast-container';
        toastContainer.className = 'toast-container position-fixed top-0 end-0 p-3';
        toastContainer.style.zIndex = '9999';
        document.body.appendChild(toastContainer);
    }
    
    const toastId = 'toast-' + Date.now();
    const bgClass = type === 'success' ? 'bg-success' : type === 'error' ? 'bg-danger' : 'bg-info';
    
    const toastHtml = `
        <div id="${toastId}" class="toast ${bgClass} text-white" role="alert" aria-live="assertive" aria-atomic="true">
            <div class="toast-header ${bgClass} text-white border-0">
                <strong class="me-auto">${title}</strong>
                <button type="button" class="btn-close btn-close-white" data-bs-dismiss="toast" aria-label="Close"></button>
            </div>
            <div class="toast-body">
                ${message}
            </div>
        </div>
    `;
    
    toastContainer.insertAdjacentHTML('beforeend', toastHtml);
    
    const toastElement = document.getElementById(toastId);
    const toast = new bootstrap.Toast(toastElement, {
        autohide: true,
        delay: 5000
    });
    
    toast.show();
    
    // Remove toast element after it's hidden
    toastElement.addEventListener('hidden.bs.toast', function() {
        toastElement.remove();
    });
}

// Quick Add to Cart - Shows modal with size/color selection if needed
function quickAddToCart(productId, productName, productSku, hasSize, hasColor) {
    if (!isUserAuthenticated()) {
        showAuthRequiredModal();
        return;
    }

    // If product doesn't have size or color, add directly
    if (!hasSize && !hasColor) {
        quickAddToCartDirect(productSku);
        return;
    }

    // Show modal for size/color selection
    showQuickAddModal(productId, productName, productSku, hasSize, hasColor);
}

// Quick add to cart without selection (for products without size/color)
function quickAddToCartDirect(productSku) {
    const requestData = {
        productId: productSku,
        quantity: 1
    };

    fetch('/Products/AddToCart', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'X-Requested-With': 'XMLHttpRequest'
        },
        body: JSON.stringify(requestData)
    })
    .then(response => {
        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }
        return response.json();
    })
    .then(data => {
        if (data.success) {
            showToast('Thành công', data.message || 'Đã thêm vào giỏ hàng', 'success');
            updateCartCount(data.cartCount);
        } else {
            showToast('Lỗi', data.message || 'Không thể thêm vào giỏ hàng', 'error');
        }
    })
    .catch(error => {
        console.error('Error:', error);
        showToast('Lỗi', 'Có lỗi xảy ra. Vui lòng thử lại.', 'error');
    });
}

// Show quick add modal for size/color selection
function showQuickAddModal(productId, productName, productSku, hasSize, hasColor) {
    const modalId = 'quickAddModal-' + productId;
    
    // Remove existing modal if any
    const existingModal = document.getElementById(modalId);
    if (existingModal) {
        existingModal.remove();
    }

    // Fetch product details
    fetch(`/Products/GetProductDetails?id=${productId}`)
        .then(response => response.json())
        .then(product => {
            let sizeOptionsHtml = '';
            let colorOptionsHtml = '';

            if (hasSize && product.size) {
                const sizes = product.size.split(',');
                sizeOptionsHtml = `
                    <div class="mb-3">
                        <label class="form-label fw-bold">Chọn Size:</label>
                        <div class="d-flex gap-2 flex-wrap">
                            ${sizes.map((size, index) => `
                                <button type="button" class="btn size-btn-quick ${index === 0 ? 'active' : ''}" 
                                        data-size="${size.trim()}" 
                                        onclick="selectQuickSize(this)">
                                    ${size.trim()}
                                </button>
                            `).join('')}
                        </div>
                    </div>
                `;
            }

            if (hasColor && product.color) {
                const colors = product.color.split(',');
                colorOptionsHtml = `
                    <div class="mb-3">
                        <label class="form-label fw-bold">Chọn Màu: <span class="selected-color-quick">${colors[0].trim()}</span></label>
                        <div class="d-flex gap-2 flex-wrap">
                            ${colors.map((color, index) => {
                                const colorName = color.trim().toLowerCase();
                                const colorHex = getColorHex(colorName);
                                return `
                                    <button type="button" 
                                            class="color-swatch-quick ${index === 0 ? 'active' : ''}" 
                                            data-color="${color.trim()}"
                                            style="background-color: ${colorHex}; ${colorName === 'white' ? 'border: 2px solid #ddd;' : ''}"
                                            onclick="selectQuickColor(this)"
                                            title="${color.trim()}">
                                    </button>
                                `;
                            }).join('')}
                        </div>
                    </div>
                `;
            }

            const modalHtml = `
                <div class="modal fade" id="${modalId}" tabindex="-1" aria-hidden="true">
                    <div class="modal-dialog modal-dialog-centered">
                        <div class="modal-content">
                            <div class="modal-header">
                                <h5 class="modal-title">${productName}</h5>
                                <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
                            </div>
                            <div class="modal-body">
                                ${colorOptionsHtml}
                                ${sizeOptionsHtml}
                                <div class="mb-3">
                                    <label class="form-label fw-bold">Số lượng:</label>
                                    <div class="input-group" style="max-width: 150px;">
                                        <button class="btn btn-outline-secondary" type="button" onclick="changeQuickQuantity(-1)">
                                            <i class="fas fa-minus"></i>
                                        </button>
                                        <input type="number" class="form-control text-center" id="quickQty" value="1" min="1" max="${product.stockQuantity}">
                                        <button class="btn btn-outline-secondary" type="button" onclick="changeQuickQuantity(1)">
                                            <i class="fas fa-plus"></i>
                                        </button>
                                    </div>
                                </div>
                            </div>
                            <div class="modal-footer">
                                <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Hủy</button>
                                <button type="button" class="btn btn-danger" onclick="confirmQuickAdd('${productSku}', '${modalId}')">
                                    <i class="fas fa-shopping-cart me-2"></i>Thêm vào giỏ
                                </button>
                            </div>
                        </div>
                    </div>
                </div>
            `;

            document.body.insertAdjacentHTML('beforeend', modalHtml);
            const modal = new bootstrap.Modal(document.getElementById(modalId));
            modal.show();

            // Remove modal from DOM when closed
            document.getElementById(modalId).addEventListener('hidden.bs.modal', function() {
                this.remove();
            });
        })
        .catch(error => {
            console.error('Error:', error);
            showToast('Lỗi', 'Không thể tải thông tin sản phẩm', 'error');
        });
}

// Helper function to get color hex code
function getColorHex(colorName) {
    const colorMap = {
        'grey st': '#5a5a5a',
        'gray': '#808080',
        'grey': '#808080',
        'black': '#000000',
        'navy': '#000080',
        'blue': '#0000ff',
        'red': '#ff0000',
        'white': '#ffffff',
        'yellow': '#ffff00',
        'green': '#008000',
        'pink': '#ffc0cb',
        'purple': '#800080',
        'orange': '#ffa500',
        'brown': '#a52a2a'
    };
    return colorMap[colorName] || '#cccccc';
}

// Select size in quick add modal
function selectQuickSize(button) {
    document.querySelectorAll('.size-btn-quick').forEach(btn => btn.classList.remove('active'));
    button.classList.add('active');
}

// Select color in quick add modal
function selectQuickColor(button) {
    document.querySelectorAll('.color-swatch-quick').forEach(btn => btn.classList.remove('active'));
    button.classList.add('active');
    
    const colorText = button.dataset.color;
    const colorTextElement = document.querySelector('.selected-color-quick');
    if (colorTextElement) {
        colorTextElement.textContent = colorText;
    }
}

// Change quantity in quick add modal
function changeQuickQuantity(change) {
    const qtyInput = document.getElementById('quickQty');
    const newValue = parseInt(qtyInput.value) + change;
    const max = parseInt(qtyInput.max);
    
    if (newValue >= 1 && newValue <= max) {
        qtyInput.value = newValue;
    }
}

// Confirm quick add to cart
function confirmQuickAdd(productSku, modalId) {
    const quantity = parseInt(document.getElementById('quickQty').value) || 1;
    const selectedSizeBtn = document.querySelector('.size-btn-quick.active');
    const selectedColorBtn = document.querySelector('.color-swatch-quick.active');
    
    const requestData = {
        productId: productSku,
        quantity: quantity
    };
    
    if (selectedSizeBtn) {
        requestData.size = selectedSizeBtn.dataset.size;
    }
    
    if (selectedColorBtn) {
        requestData.color = selectedColorBtn.dataset.color;
    }

    fetch('/Products/AddToCart', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'X-Requested-With': 'XMLHttpRequest'
        },
        body: JSON.stringify(requestData)
    })
    .then(response => {
        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }
        return response.json();
    })
    .then(data => {
        if (data.success) {
            // Close modal
            const modal = bootstrap.Modal.getInstance(document.getElementById(modalId));
            modal.hide();
            
            showToast('Thành công', data.message || 'Đã thêm vào giỏ hàng', 'success');
            updateCartCount(data.cartCount);
        } else {
            showToast('Lỗi', data.message || 'Không thể thêm vào giỏ hàng', 'error');
        }
    })
    .catch(error => {
        console.error('Error:', error);
        showToast('Lỗi', 'Có lỗi xảy ra. Vui lòng thử lại.', 'error');
    });
}

// Buy Now - Navigate to product detail or checkout
function buyNow(productId) {
    window.location.href = `/Products/ProductDetail/${productId}`;
}

// View store availability
function viewStoreAvailability(productId) {
    // This can be implemented later with actual store locations
    showToast('Thông báo', 'Tính năng đang được phát triển', 'info');
}
