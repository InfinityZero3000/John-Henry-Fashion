/**
 * Shopping Cart Sidebar Functionality
 * Handles cart sidebar interactions: open/close, remove items, update quantities
 */

(function() {
    'use strict';

    // Open/Close cart sidebar
    function initCartSidebarControls() {
        const cartSidebar = document.getElementById('cart-sidebar');
        const cartOverlay = document.getElementById('cart-sidebar-overlay');
        const closeBtn = document.getElementById('close-cart-sidebar');
        
        // Open cart sidebar - trigger from header cart icon
        document.querySelectorAll('[data-cart-trigger]').forEach(trigger => {
            trigger.addEventListener('click', function(e) {
                e.preventDefault();
                openCartSidebar();
            });
        });

        // Close cart sidebar
        if (closeBtn) {
            closeBtn.addEventListener('click', closeCartSidebar);
        }

        if (cartOverlay) {
            cartOverlay.addEventListener('click', closeCartSidebar);
        }

        // ESC key to close
        document.addEventListener('keydown', function(e) {
            if (e.key === 'Escape' && cartSidebar?.classList.contains('open')) {
                closeCartSidebar();
            }
        });
    }

    function openCartSidebar() {
        console.log('🛒 cart-sidebar.js: openCartSidebar called');
        
        // Use the global loadAndOpenCartSidebar function if available
        if (typeof window.loadAndOpenCartSidebar === 'function') {
            console.log('Using global loadAndOpenCartSidebar function');
            window.loadAndOpenCartSidebar();
            return;
        }
        
        // Fallback to basic open (without reload)
        const cartSidebar = document.getElementById('cart-sidebar');
        const cartOverlay = document.getElementById('cart-sidebar-overlay');
        
        if (cartSidebar) {
            cartSidebar.classList.add('open');
        }
        if (cartOverlay) {
            cartOverlay.classList.add('show');
        }
        
        // Prevent body scroll
        document.body.style.overflow = 'hidden';
        
        console.log('🛒 Cart sidebar opened (basic mode)');
    }

    function closeCartSidebar() {
        const cartSidebar = document.getElementById('cart-sidebar');
        const cartOverlay = document.getElementById('cart-sidebar-overlay');
        
        if (cartSidebar) {
            cartSidebar.classList.remove('open');
        }
        if (cartOverlay) {
            cartOverlay.classList.remove('show');
        }
        
        // Restore body scroll
        document.body.style.overflow = '';
        
        console.log('🛒 Cart sidebar closed');
    }

    // Remove item from cart
    function initRemoveButtons() {
        document.querySelectorAll('.remove-item-btn').forEach(btn => {
            btn.addEventListener('click', async function() {
                const cartItemId = this.getAttribute('data-cart-item-id');
                
                if (!confirm('Bạn có chắc muốn xóa sản phẩm này khỏi giỏ hàng?')) {
                    return;
                }
                
                console.log('🗑️ Removing cart item:', cartItemId);
                
                try {
                    const formData = new FormData();
                    formData.append('cartItemId', cartItemId);
                    
                    const response = await fetch('/Cart/RemoveItem', {
                        method: 'POST',
                        body: formData
                    });
                    
                    const result = await response.json();
                    
                    if (result.success) {
                        console.log('Item removed successfully');
                        
                        // Remove from DOM with animation
                        const cartItem = this.closest('.cart-item');
                        cartItem.style.opacity = '0';
                        cartItem.style.transform = 'translateX(100px)';
                        
                        setTimeout(() => {
                            cartItem.remove();
                            
                            // Update counts and totals
                            updateCartCounts(result.cartCount, result.cartTotal);
                            
                            // Show empty state if no items
                            if (result.cartCount === 0) {
                                showEmptyCartState();
                            }
                            
                            // Dispatch event for other components
                            document.dispatchEvent(new CustomEvent('cartUpdated', {
                                detail: {
                                    cartCount: result.cartCount,
                                    cartTotal: result.cartTotal
                                }
                            }));
                        }, 300);
                        
                        // Show success toast
                        showToast('Đã xóa sản phẩm khỏi giỏ hàng', 'success');
                    } else {
                        console.error('Remove failed:', result.message);
                        showToast(result.message || 'Không thể xóa sản phẩm', 'error');
                    }
                } catch (error) {
                    console.error('Remove error:', error);
                    showToast('Có lỗi xảy ra khi xóa sản phẩm', 'error');
                }
            });
        });
    }

    // Update item quantity
    function initQuantityControls() {
        document.querySelectorAll('.quantity-btn').forEach(btn => {
            btn.addEventListener('click', async function() {
                const cartItemId = this.getAttribute('data-cart-item-id');
                const action = this.getAttribute('data-action');
                const quantitySpan = this.closest('.quantity-controls').querySelector('.quantity');
                const currentQty = parseInt(quantitySpan.textContent);
                
                let newQty = action === 'increase' ? currentQty + 1 : currentQty - 1;
                
                if (newQty < 1) {
                    showToast('Số lượng tối thiểu là 1', 'warning');
                    return;
                }
                
                console.log('Updating quantity:', cartItemId, 'from', currentQty, 'to', newQty);
                
                // Disable buttons during update
                const buttons = this.closest('.quantity-controls').querySelectorAll('.quantity-btn');
                buttons.forEach(b => b.disabled = true);
                
                try {
                    const formData = new FormData();
                    formData.append('cartItemId', cartItemId);
                    formData.append('quantity', newQty);
                    
                    const response = await fetch('/Cart/UpdateQuantity', {
                        method: 'POST',
                        body: formData
                    });
                    
                    const result = await response.json();
                    
                    if (result.success) {
                        console.log('Quantity updated successfully');
                        
                        // Update quantity display
                        quantitySpan.textContent = newQty;
                        
                        // Update item price
                        const itemPriceEl = this.closest('.cart-item').querySelector('.item-price');
                        if (itemPriceEl && result.itemTotal) {
                            itemPriceEl.textContent = result.itemTotal.toLocaleString('vi-VN') + ' đ';
                        }
                        
                        // Update totals
                        updateCartCounts(result.cartCount, result.cartTotal);
                        
                        // Dispatch event
                        document.dispatchEvent(new CustomEvent('cartUpdated', {
                            detail: {
                                cartCount: result.cartCount,
                                cartTotal: result.cartTotal
                            }
                        }));
                    } else {
                        console.error('Update failed:', result.message);
                        showToast(result.message || 'Không thể cập nhật số lượng', 'error');
                    }
                } catch (error) {
                    console.error('Update error:', error);
                    showToast('Có lỗi xảy ra khi cập nhật số lượng', 'error');
                } finally {
                    // Re-enable buttons
                    buttons.forEach(b => b.disabled = false);
                }
            });
        });
    }

    // Update cart counts in sidebar and header
    function updateCartCounts(cartCount, cartTotal) {
        // Update sidebar count
        const sidebarCount = document.getElementById('cart-items-count');
        if (sidebarCount) {
            sidebarCount.textContent = cartCount;
        }
        
        // Update sidebar total
        const sidebarTotal = document.getElementById('cart-total-amount');
        if (sidebarTotal) {
            sidebarTotal.textContent = (cartTotal || 0).toLocaleString('vi-VN') + ' đ';
        }
        
        // Update header cart badges
        document.querySelectorAll('.cart-count, .cart-badge').forEach(badge => {
            badge.textContent = cartCount;
            
            // Hide badge if count is 0
            if (cartCount === 0) {
                badge.style.display = 'none';
            } else {
                badge.style.display = '';
            }
        });
        
        console.log('Cart counts updated - Count:', cartCount, 'Total:', cartTotal);
    }

    // Show empty cart state
    function showEmptyCartState() {
        const cartContent = document.querySelector('.cart-sidebar-content');
        if (!cartContent) return;
        
        cartContent.innerHTML = `
            <div class="empty-cart">
                <div class="empty-cart-icon">
                    <i data-lucide="shopping-bag"></i>
                </div>
                <h4>Giỏ hàng trống</h4>
                <p>Bạn chưa có sản phẩm nào trong giỏ hàng</p>
                <a href="/" class="btn btn-primary">Tiếp tục mua sắm</a>
            </div>
        `;
        
        // Re-init Lucide icons
        if (typeof lucide !== 'undefined') {
            lucide.createIcons();
        }
        
        console.log('🛒 Empty cart state displayed');
    }

    // Reload cart sidebar from server
    async function reloadCartSidebar() {
        console.log('Reloading cart sidebar...');
        
        try {
            const response = await fetch('/Cart/GetSidebarData');
            const html = await response.text();
            
            // Replace entire sidebar
            const tempDiv = document.createElement('div');
            tempDiv.innerHTML = html;
            const newSidebar = tempDiv.querySelector('#cart-sidebar');
            
            if (newSidebar) {
                const oldSidebar = document.getElementById('cart-sidebar');
                if (oldSidebar) {
                    // Preserve open state
                    const wasOpen = oldSidebar.classList.contains('open');
                    
                    oldSidebar.replaceWith(newSidebar);
                    
                    if (wasOpen) {
                        newSidebar.classList.add('open');
                    }
                    
                    // Re-init all event listeners
                    initCartSidebarControls();
                    initRemoveButtons();
                    initQuantityControls();
                    
                    // Re-init Lucide icons
                    if (typeof lucide !== 'undefined') {
                        lucide.createIcons();
                    }
                    
                    console.log('Cart sidebar reloaded');
                }
            }
        } catch (error) {
            console.error('Failed to reload cart sidebar:', error);
        }
    }

    // Show toast notification
    function showToast(message, type = 'info') {
        // Try to use global showToast if available
        if (typeof window.showToast === 'function') {
            window.showToast(message, type);
            return;
        }
        
        // Fallback: simple alert
        if (type === 'success') {
            console.log('✅', message);
        } else if (type === 'error') {
            console.error('❌', message);
        } else if (type === 'warning') {
            console.warn('⚠️', message);
        } else {
            console.info('ℹ️', message);
        }
        
        // Simple toast implementation
        const toast = document.createElement('div');
        toast.className = `simple-toast toast-${type}`;
        toast.textContent = message;
        toast.style.cssText = `
            position: fixed;
            top: 20px;
            right: 20px;
            padding: 15px 20px;
            background: ${type === 'success' ? '#4caf50' : type === 'error' ? '#f44336' : type === 'warning' ? '#ff9800' : '#2196f3'};
            color: white;
            border-radius: 8px;
            box-shadow: 0 4px 12px rgba(0,0,0,0.15);
            z-index: 10000;
            animation: slideInRight 0.3s ease;
        `;
        
        document.body.appendChild(toast);
        
        setTimeout(() => {
            toast.style.animation = 'slideOutRight 0.3s ease';
            setTimeout(() => toast.remove(), 300);
        }, 3000);
    }

    // Listen for cart updated events from other components
    document.addEventListener('cartUpdated', function(e) {
        console.log('🔔 Cart updated event received:', e.detail);
        // Reload sidebar to reflect changes
        reloadCartSidebar();
    });

    // Listen for "Add to Cart" success to open sidebar
    document.addEventListener('productAddedToCart', function(e) {
        console.log('🔔 Product added to cart event received:', e.detail);
        // Use the global loadAndOpenCartSidebar to reload with fresh data
        if (typeof window.loadAndOpenCartSidebar === 'function') {
            console.log('Calling loadAndOpenCartSidebar from event listener');
            window.loadAndOpenCartSidebar();
        } else {
            // Fallback: reload then open
            console.log('Fallback: reloading cart sidebar then opening');
            reloadCartSidebar().then(() => {
                setTimeout(() => openCartSidebar(), 100);
            });
        }
    });

    // Initialize on DOM ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', function() {
            initCartSidebarControls();
            initRemoveButtons();
            initQuantityControls();
            console.log('Cart sidebar initialized');
        });
    } else {
        initCartSidebarControls();
        initRemoveButtons();
        initQuantityControls();
        console.log('Cart sidebar initialized');
    }

    // Expose init function for re-initialization after dynamic content load
    window.initCartSidebarEvents = function() {
        console.log('🔄 Re-initializing cart sidebar events...');
        initCartSidebarControls();
        initRemoveButtons();
        initQuantityControls();
    };

    // Export functions for external use
    window.cartSidebar = {
        open: openCartSidebar,
        close: closeCartSidebar,
        reload: reloadCartSidebar
    };

    console.log('Cart sidebar module loaded');
})();

// Add CSS animations
if (!document.getElementById('cart-sidebar-animations')) {
    const style = document.createElement('style');
    style.id = 'cart-sidebar-animations';
    style.textContent = `
        @keyframes slideInRight {
            from {
                transform: translateX(100%);
                opacity: 0;
            }
            to {
                transform: translateX(0);
                opacity: 1;
            }
        }
        
        @keyframes slideOutRight {
            from {
                transform: translateX(0);
                opacity: 1;
            }
            to {
                transform: translateX(100%);
                opacity: 0;
            }
        }
        
        .cart-item {
            transition: opacity 0.3s ease, transform 0.3s ease;
        }
        
        .quantity-btn:disabled {
            opacity: 0.5;
            cursor: not-allowed;
        }
    `;
    document.head.appendChild(style);
}
