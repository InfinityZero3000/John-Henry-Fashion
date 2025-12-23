/**
 * Enhanced Admin Dashboard JavaScript
 * Modern ES6+ implementation with advanced features
 */

class AdminDashboard {
    constructor() {
        this.initializeComponents();
        this.setupEventListeners();
        this.initializeCharts();
        this.setupNotifications();
        this.initializeModals();
        this.setupFileUploads();
        this.initializeDataTables();
        this.setupFormValidation();
        this.initializeTooltips();
        this.setupKeyboardShortcuts();
    }

    initializeComponents() {
        // Initialize sidebar state
        this.sidebar = document.querySelector('#admin-sidebar');
        this.mainContent = document.querySelector('.admin-main-content');
        this.sidebarToggle = document.querySelector('#sidebar-toggle');
        this.darkModeToggle = document.querySelector('#dark-mode-toggle');
        
        // Initialize data refresh interval
        this.dataRefreshInterval = 30000; // 30 seconds
        this.startDataRefresh();
        
        // Initialize responsive breakpoints
        this.breakpoints = {
            mobile: 768,
            tablet: 1024,
            desktop: 1200
        };
        
        this.checkResponsive();
        window.addEventListener('resize', () => this.checkResponsive());
    }

    setupEventListeners() {
        // Sidebar toggle
        if (this.sidebarToggle) {
            this.sidebarToggle.addEventListener('click', () => this.toggleSidebar());
        }

        // Dark mode toggle
        if (this.darkModeToggle) {
            this.darkModeToggle.addEventListener('click', () => this.toggleDarkMode());
        }

        // Navigation menu items
        document.querySelectorAll('.admin-nav-item').forEach(item => {
            item.addEventListener('click', (e) => this.handleNavigation(e));
        });

        // Quick action buttons
        document.querySelectorAll('.quick-action-btn').forEach(btn => {
            btn.addEventListener('click', (e) => this.handleQuickAction(e));
        });

        // Search functionality
        const searchInput = document.querySelector('#admin-search');
        if (searchInput) {
            searchInput.addEventListener('input', (e) => this.handleSearch(e));
        }

        // Notification dismissal
        document.addEventListener('click', (e) => {
            if (e.target.matches('.notification-dismiss')) {
                this.dismissNotification(e.target.closest('.notification'));
            }
        });

        // Form submissions
        document.querySelectorAll('.admin-form').forEach(form => {
            form.addEventListener('submit', (e) => this.handleFormSubmit(e));
        });

        // Bulk actions
        document.querySelectorAll('.bulk-action-checkbox').forEach(checkbox => {
            checkbox.addEventListener('change', () => this.updateBulkActions());
        });
    }

    toggleSidebar() {
        if (this.sidebar && this.mainContent) {
            this.sidebar.classList.toggle('collapsed');
            this.mainContent.classList.toggle('sidebar-collapsed');
            
            // Update toggle icon
            this.updateToggleIcon();
            
            // Save state to localStorage
            const isCollapsed = this.sidebar.classList.contains('collapsed');
            localStorage.setItem('admin_sidebar_collapsed', isCollapsed);
        }
    }

    updateToggleIcon() {
        // Update sidebar toggle button icon if exists
        if (this.sidebarToggle) {
            const isCollapsed = this.sidebar && this.sidebar.classList.contains('collapsed');
            const icon = this.sidebarToggle.querySelector('i');
            if (icon) {
                // Update icon class for collapsed/expanded state
                icon.className = isCollapsed ? 'fas fa-bars' : 'fas fa-times';
            }
        }
    }

    toggleDarkMode() {
        document.body.classList.toggle('dark-mode');
        const isDarkMode = document.body.classList.contains('dark-mode');
        localStorage.setItem('admin_dark_mode', isDarkMode);
        
        // Update chart themes if they exist
        this.updateChartThemes(isDarkMode);
        
        // Show notification
        this.showNotification(
            isDarkMode ? 'Dark mode enabled' : 'Dark mode disabled',
            'success'
        );
    }

    checkResponsive() {
        const width = window.innerWidth;
        
        if (width <= this.breakpoints.mobile) {
            document.body.classList.add('mobile-view');
            document.body.classList.remove('tablet-view', 'desktop-view');
            if (this.sidebar) {
                this.sidebar.classList.add('collapsed');
            }
        } else if (width <= this.breakpoints.tablet) {
            document.body.classList.add('tablet-view');
            document.body.classList.remove('mobile-view', 'desktop-view');
        } else {
            document.body.classList.add('desktop-view');
            document.body.classList.remove('mobile-view', 'tablet-view');
        }
    }

    initializeCharts() {
        // Revenue Chart
        const revenueCtx = document.getElementById('revenueChart');
        if (revenueCtx) {
            this.revenueChart = new Chart(revenueCtx, {
                type: 'line',
                data: {
                    labels: ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun'],
                    datasets: [{
                        label: 'Revenue',
                        data: [12000, 19000, 3000, 5000, 2000, 3000],
                        borderColor: 'rgb(75, 192, 192)',
                        backgroundColor: 'rgba(75, 192, 192, 0.1)',
                        tension: 0.4,
                        fill: true
                    }]
                },
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    plugins: {
                        legend: {
                            display: false
                        }
                    },
                    scales: {
                        y: {
                            beginAtZero: true,
                            grid: {
                                color: 'rgba(0,0,0,0.1)'
                            }
                        },
                        x: {
                            grid: {
                                display: false
                            }
                        }
                    }
                }
            });
        }

        // Orders Chart
        const ordersCtx = document.getElementById('ordersChart');
        if (ordersCtx) {
            this.ordersChart = new Chart(ordersCtx, {
                type: 'bar',
                data: {
                    labels: ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'],
                    datasets: [{
                        label: 'Orders',
                        data: [12, 19, 3, 5, 2, 3, 10],
                        backgroundColor: [
                            'rgba(255, 99, 132, 0.8)',
                            'rgba(54, 162, 235, 0.8)',
                            'rgba(255, 205, 86, 0.8)',
                            'rgba(75, 192, 192, 0.8)',
                            'rgba(153, 102, 255, 0.8)',
                            'rgba(255, 159, 64, 0.8)',
                            'rgba(199, 199, 199, 0.8)'
                        ],
                        borderWidth: 0,
                        borderRadius: 4
                    }]
                },
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    plugins: {
                        legend: {
                            display: false
                        }
                    },
                    scales: {
                        y: {
                            beginAtZero: true,
                            grid: {
                                color: 'rgba(0,0,0,0.1)'
                            }
                        },
                        x: {
                            grid: {
                                display: false
                            }
                        }
                    }
                }
            });
        }

        // Category Distribution Chart
        const categoryCtx = document.getElementById('categoryChart');
        if (categoryCtx) {
            this.categoryChart = new Chart(categoryCtx, {
                type: 'doughnut',
                data: {
                    labels: ['Electronics', 'Clothing', 'Books', 'Home'],
                    datasets: [{
                        data: [300, 150, 100, 80],
                        backgroundColor: [
                            '#FF6384',
                            '#36A2EB',
                            '#FFCE56',
                            '#4BC0C0'
                        ],
                        borderWidth: 0
                    }]
                },
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    plugins: {
                        legend: {
                            position: 'bottom',
                            labels: {
                                padding: 20,
                                usePointStyle: true
                            }
                        }
                    }
                }
            });
        }
    }

    updateChartThemes(isDarkMode) {
        const textColor = isDarkMode ? '#ffffff' : '#333333';
        const gridColor = isDarkMode ? 'rgba(255,255,255,0.1)' : 'rgba(0,0,0,0.1)';

        [this.revenueChart, this.ordersChart, this.categoryChart].forEach(chart => {
            if (chart) {
                chart.options.plugins.legend.labels.color = textColor;
                if (chart.options.scales) {
                    Object.keys(chart.options.scales).forEach(scaleKey => {
                        chart.options.scales[scaleKey].ticks.color = textColor;
                        if (chart.options.scales[scaleKey].grid) {
                            chart.options.scales[scaleKey].grid.color = gridColor;
                        }
                    });
                }
                chart.update();
            }
        });
    }

    setupNotifications() {
        this.notificationContainer = document.querySelector('.notification-container');
        if (!this.notificationContainer) {
            this.notificationContainer = document.createElement('div');
            this.notificationContainer.className = 'notification-container';
            document.body.appendChild(this.notificationContainer);
        }
    }

    showNotification(message, type = 'info', duration = 5000) {
        const notification = document.createElement('div');
        notification.className = `notification notification-${type}`;
        notification.innerHTML = `
            <div class="notification-content">
                <i class="fas fa-${this.getNotificationIcon(type)}"></i>
                <span>${message}</span>
            </div>
            <button class="notification-dismiss" aria-label="Dismiss">
                <i class="fas fa-times"></i>
            </button>
        `;

        this.notificationContainer.appendChild(notification);

        // Animate in
        setTimeout(() => notification.classList.add('show'), 100);

        // Auto dismiss
        if (duration > 0) {
            setTimeout(() => this.dismissNotification(notification), duration);
        }
    }

    getNotificationIcon(type) {
        const icons = {
            success: 'check-circle',
            error: 'exclamation-circle',
            warning: 'exclamation-triangle',
            info: 'info-circle'
        };
        return icons[type] || 'info-circle';
    }

    dismissNotification(notification) {
        if (notification) {
            notification.classList.add('fade-out');
            setTimeout(() => {
                if (notification.parentNode) {
                    notification.parentNode.removeChild(notification);
                }
            }, 300);
        }
    }

    initializeModals() {
        // Modal functionality
        document.addEventListener('click', (e) => {
            if (e.target.matches('[data-bs-toggle="modal"]')) {
                const targetModal = document.querySelector(e.target.getAttribute('data-bs-target'));
                if (targetModal) {
                    this.showModal(targetModal);
                }
            }
        });

        // Close modal on backdrop click
        document.addEventListener('click', (e) => {
            if (e.target.matches('.modal')) {
                this.hideModal(e.target);
            }
        });

        // Close modal on close button click
        document.addEventListener('click', (e) => {
            if (e.target.matches('.modal-close, [data-bs-dismiss="modal"]')) {
                const modal = e.target.closest('.modal');
                if (modal) {
                    this.hideModal(modal);
                }
            }
        });

        // Close modal on escape key
        document.addEventListener('keydown', (e) => {
            if (e.key === 'Escape') {
                const openModal = document.querySelector('.modal.show');
                if (openModal) {
                    this.hideModal(openModal);
                }
            }
        });
    }

    showModal(modal) {
        modal.classList.add('show');
        document.body.classList.add('modal-open');
        modal.style.display = 'block';
    }

    hideModal(modal) {
        modal.classList.remove('show');
        document.body.classList.remove('modal-open');
        setTimeout(() => {
            modal.style.display = 'none';
        }, 150);
    }

    setupFileUploads() {
        document.querySelectorAll('.file-upload-area').forEach(area => {
            const input = area.querySelector('input[type="file"]');
            const preview = area.querySelector('.file-preview');

            if (input) {
                // Drag and drop
                area.addEventListener('dragover', (e) => {
                    e.preventDefault();
                    area.classList.add('dragover');
                });

                area.addEventListener('dragleave', () => {
                    area.classList.remove('dragover');
                });

                area.addEventListener('drop', (e) => {
                    e.preventDefault();
                    area.classList.remove('dragover');
                    const files = e.dataTransfer.files;
                    this.handleFileUpload(files, preview);
                });

                // Click to upload
                area.addEventListener('click', () => input.click());

                // File input change
                input.addEventListener('change', (e) => {
                    this.handleFileUpload(e.target.files, preview);
                });
            }
        });
    }

    handleFileUpload(files, preview) {
        Array.from(files).forEach(file => {
            if (file.type.startsWith('image/')) {
                const reader = new FileReader();
                reader.onload = (e) => {
                    const img = document.createElement('img');
                    img.src = e.target.result;
                    img.className = 'preview-image';
                    if (preview) {
                        preview.appendChild(img);
                    }
                };
                reader.readAsDataURL(file);
            }
        });
    }

    initializeDataTables() {
        document.querySelectorAll('.data-table').forEach(table => {
            if (typeof DataTable !== 'undefined') {
                new DataTable(table, {
                    pageLength: 25,
                    responsive: true,
                    searching: true,
                    ordering: true,
                    language: {
                        search: 'Search:',
                        lengthMenu: 'Show _MENU_ entries',
                        info: 'Showing _START_ to _END_ of _TOTAL_ entries',
                        paginate: {
                            first: 'First',
                            last: 'Last',
                            next: 'Next',
                            previous: 'Previous'
                        }
                    }
                });
            }
        });
    }

    setupFormValidation() {
        document.querySelectorAll('.admin-form').forEach(form => {
            form.addEventListener('submit', (e) => {
                if (!this.validateForm(form)) {
                    e.preventDefault();
                    this.showNotification('Please fix the errors below', 'error');
                }
            });

            // Real-time validation
            form.querySelectorAll('input, select, textarea').forEach(field => {
                field.addEventListener('blur', () => this.validateField(field));
                field.addEventListener('input', () => this.clearFieldError(field));
            });
        });
    }

    validateForm(form) {
        let isValid = true;
        const fields = form.querySelectorAll('input, select, textarea');

        fields.forEach(field => {
            if (!this.validateField(field)) {
                isValid = false;
            }
        });

        return isValid;
    }

    validateField(field) {
        const value = field.value.trim();
        const fieldName = field.getAttribute('name') || field.getAttribute('id');
        let isValid = true;
        let errorMessage = '';

        // Required validation
        if (field.hasAttribute('required') && !value) {
            errorMessage = `${fieldName} is required`;
            isValid = false;
        }

        // Email validation
        if (field.type === 'email' && value && !this.isValidEmail(value)) {
            errorMessage = 'Please enter a valid email address';
            isValid = false;
        }

        // Number validation
        if (field.type === 'number' && value) {
            const min = field.getAttribute('min');
            const max = field.getAttribute('max');
            const numValue = parseFloat(value);

            if (isNaN(numValue)) {
                errorMessage = 'Please enter a valid number';
                isValid = false;
            } else if (min && numValue < parseFloat(min)) {
                errorMessage = `Value must be at least ${min}`;
                isValid = false;
            } else if (max && numValue > parseFloat(max)) {
                errorMessage = `Value must be no more than ${max}`;
                isValid = false;
            }
        }

        this.showFieldError(field, errorMessage);
        return isValid;
    }

    showFieldError(field, message) {
        this.clearFieldError(field);

        if (message) {
            field.classList.add('is-invalid');
            const errorDiv = document.createElement('div');
            errorDiv.className = 'invalid-feedback';
            errorDiv.textContent = message;
            field.parentNode.appendChild(errorDiv);
        } else {
            field.classList.remove('is-invalid');
            field.classList.add('is-valid');
        }
    }

    clearFieldError(field) {
        field.classList.remove('is-invalid', 'is-valid');
        const existingError = field.parentNode.querySelector('.invalid-feedback');
        if (existingError) {
            existingError.remove();
        }
    }

    isValidEmail(email) {
        const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
        return emailRegex.test(email);
    }

    initializeTooltips() {
        document.querySelectorAll('[data-bs-toggle="tooltip"]').forEach(element => {
            if (typeof bootstrap !== 'undefined' && bootstrap.Tooltip) {
                new bootstrap.Tooltip(element);
            }
        });
    }

    setupKeyboardShortcuts() {
        document.addEventListener('keydown', (e) => {
            // Ctrl/Cmd + S: Save current form
            if ((e.ctrlKey || e.metaKey) && e.key === 's') {
                e.preventDefault();
                const activeForm = document.querySelector('form:focus-within');
                if (activeForm) {
                    activeForm.dispatchEvent(new Event('submit'));
                }
            }

            // Ctrl/Cmd + K: Focus search
            if ((e.ctrlKey || e.metaKey) && e.key === 'k') {
                e.preventDefault();
                const searchInput = document.querySelector('#admin-search');
                if (searchInput) {
                    searchInput.focus();
                }
            }

            // Alt + M: Toggle sidebar
            if (e.altKey && e.key === 'm') {
                e.preventDefault();
                this.toggleSidebar();
            }
        });
    }

    handleNavigation(e) {
        e.preventDefault();
        const target = e.currentTarget;
        const href = target.getAttribute('href');

        // Update active state
        document.querySelectorAll('.admin-nav-item').forEach(item => {
            item.classList.remove('active');
        });
        target.classList.add('active');

        // Show loading state
        this.showPageLoading();

        // Navigate (you might want to use a router here)
        if (href && href !== '#') {
            window.location.href = href;
        }
    }

    handleQuickAction(e) {
        const action = e.currentTarget.dataset.action;
        
        switch (action) {
            case 'add-product':
                window.location.href = '/admin/products/create';
                break;
            case 'add-category':
                window.location.href = '/admin/categories/create';
                break;
            case 'export-data':
                this.exportData();
                break;
            case 'refresh-data':
                this.refreshDashboardData();
                break;
            default:
                console.log(`Unknown action: ${action}`);
        }
    }

    handleSearch(e) {
        const query = e.target.value.toLowerCase();
        const searchResults = document.querySelector('.search-results');
        
        if (query.length >= 2) {
            // Perform search (mock implementation)
            this.performSearch(query);
        } else if (searchResults) {
            searchResults.style.display = 'none';
        }
    }

    performSearch(query) {
        // Mock search results
        const results = [
            { type: 'Product', name: 'Sample Product', url: '/admin/products/1' },
            { type: 'Category', name: 'Sample Category', url: '/admin/categories/1' },
            { type: 'Order', name: '#ORD-001', url: '/admin/orders/1' }
        ].filter(item => item.name.toLowerCase().includes(query));

        this.displaySearchResults(results);
    }

    displaySearchResults(results) {
        let searchResults = document.querySelector('.search-results');
        if (!searchResults) {
            searchResults = document.createElement('div');
            searchResults.className = 'search-results';
            document.querySelector('#admin-search').parentNode.appendChild(searchResults);
        }

        if (results.length > 0) {
            searchResults.innerHTML = results.map(result => `
                <div class="search-result-item">
                    <span class="result-type">${result.type}</span>
                    <a href="${result.url}" class="result-name">${result.name}</a>
                </div>
            `).join('');
            searchResults.style.display = 'block';
        } else {
            searchResults.innerHTML = '<div class="no-results">No results found</div>';
            searchResults.style.display = 'block';
        }
    }

    handleFormSubmit(e) {
        const form = e.target;
        const submitBtn = form.querySelector('[type="submit"]');
        
        if (submitBtn) {
            submitBtn.disabled = true;
            submitBtn.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Submitting...';
        }

        // Re-enable button after a timeout (in case of errors)
        setTimeout(() => {
            if (submitBtn) {
                submitBtn.disabled = false;
                submitBtn.innerHTML = submitBtn.dataset.originalText || 'Submit';
            }
        }, 5000);
    }

    updateBulkActions() {
        const checkboxes = document.querySelectorAll('.bulk-action-checkbox:checked');
        const bulkActionBar = document.querySelector('.bulk-action-bar');
        
        if (checkboxes.length > 0) {
            if (bulkActionBar) {
                bulkActionBar.style.display = 'flex';
                bulkActionBar.querySelector('.selected-count').textContent = checkboxes.length;
            }
        } else if (bulkActionBar) {
            bulkActionBar.style.display = 'none';
        }
    }

    showPageLoading() {
        // Create or show loading overlay
        let loader = document.querySelector('.page-loader');
        if (!loader) {
            loader = document.createElement('div');
            loader.className = 'page-loader';
            loader.innerHTML = '<div class="loader-spinner"></div>';
            document.body.appendChild(loader);
        }
        loader.style.display = 'flex';
    }

    hidePageLoading() {
        const loader = document.querySelector('.page-loader');
        if (loader) {
            loader.style.display = 'none';
        }
    }

    exportData() {
        this.showNotification('Preparing data export...', 'info');
        
        // Mock export functionality
        setTimeout(() => {
            this.showNotification('Data exported successfully!', 'success');
            
            // Create and trigger download
            const link = document.createElement('a');
            link.href = 'data:text/csv;charset=utf-8,Sample,Data,Export\n1,2,3';
            link.download = 'admin-data-export.csv';
            link.click();
        }, 2000);
    }

    refreshDashboardData() {
        // Notification disabled to reduce UI clutter
        // this.showNotification('Refreshing dashboard data...', 'info');
        
        // Mock data refresh
        setTimeout(() => {
            // Update stats
            document.querySelectorAll('.stat-value').forEach(element => {
                const currentValue = parseInt(element.textContent.replace(/[^0-9]/g, ''));
                const newValue = currentValue + Math.floor(Math.random() * 10);
                element.textContent = newValue.toLocaleString();
            });
            
            // Update charts with new data
            if (this.revenueChart) {
                this.revenueChart.data.datasets[0].data = this.generateRandomData(6);
                this.revenueChart.update();
            }
            
            if (this.ordersChart) {
                this.ordersChart.data.datasets[0].data = this.generateRandomData(7);
                this.ordersChart.update();
            }
            
            // Notification disabled to reduce UI clutter
            // this.showNotification('Dashboard data refreshed!', 'success');
        }, 1500);
    }

    generateRandomData(length) {
        return Array.from({ length }, () => Math.floor(Math.random() * 100));
    }

    startDataRefresh() {
        // Auto-refresh disabled to reduce unnecessary notifications and CPU usage
        // setInterval(() => {
        //     if (document.visibilityState === 'visible') {
        //         this.refreshDashboardData();
        //     }
        // }, this.dataRefreshInterval);
    }

    // Restore saved preferences
    restorePreferences() {
        // Restore sidebar state
        const sidebarCollapsed = localStorage.getItem('admin_sidebar_collapsed') === 'true';
        if (sidebarCollapsed && this.sidebar) {
            this.sidebar.classList.add('collapsed');
            if (this.mainContent) {
                this.mainContent.classList.add('sidebar-collapsed');
            }
        }

        // Restore dark mode
        const darkMode = localStorage.getItem('admin_dark_mode') === 'true';
        if (darkMode) {
            document.body.classList.add('dark-mode');
        }
    }
}

// Initialize dashboard when DOM is loaded
document.addEventListener('DOMContentLoaded', () => {
    window.adminDashboard = new AdminDashboard();
    window.adminDashboard.restorePreferences();
});

// Expose class on window for browser usage (safe no-op in non-browser envs)
if (typeof window !== 'undefined') {
    window.AdminDashboard = AdminDashboard;
}

// Export for CommonJS-like environments if available
if (typeof module !== 'undefined' && module.exports) {
    module.exports = AdminDashboard;
}