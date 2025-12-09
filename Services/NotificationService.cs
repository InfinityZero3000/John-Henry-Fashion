using JohnHenryFashionWeb.Models;
using JohnHenryFashionWeb.Data;
using Microsoft.EntityFrameworkCore;

namespace JohnHenryFashionWeb.Services
{
    public interface INotificationService
    {
        Task CreateNotificationAsync(string userId, string title, string message, string type, string? actionUrl = null);
        Task<List<Notification>> GetUserNotificationsAsync(string userId, bool unreadOnly = false);
        Task<int> GetUnreadCountAsync(string userId);
        Task MarkAsReadAsync(int notificationId, string userId);
        Task MarkAllAsReadAsync(string userId);
        Task DeleteNotificationAsync(int notificationId, string userId);
        Task SendOrderNotificationAsync(Order order);
        Task SendWelcomeNotificationAsync(string userId, string userName);
        Task SendProductNotificationAsync(string userId, Product product, string notificationType);
        Task SendSystemNotificationAsync(string userId, string message);
        Task SendBulkNotificationAsync(List<string> userIds, string title, string message, string type);
        Task CleanupOldNotificationsAsync();
        Task SendNotificationAsync(string userId, string title, string message, string type = "info", string? actionUrl = null);
    }

    public class NotificationService : INotificationService
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly ILogger<NotificationService> _logger;
        private readonly ICacheService _cacheService;

        public NotificationService(
            ApplicationDbContext context,
            IEmailService emailService,
            ILogger<NotificationService> logger,
            ICacheService cacheService)
        {
            _context = context;
            _emailService = emailService;
            _logger = logger;
            _cacheService = cacheService;
        }

        public async Task CreateNotificationAsync(string userId, string title, string message, string type, string? actionUrl = null)
        {
            try
            {
                var notification = new Notification
                {
                    UserId = userId,
                    Title = title,
                    Message = message,
                    Type = type,
                    ActionUrl = actionUrl,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                // Clear user notifications cache
                await _cacheService.RemoveByPatternAsync($"user_notifications_{userId}*");

                _logger.LogInformation("Notification created for user {UserId}: {Title}", userId, title);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create notification for user {UserId}", userId);
                throw;
            }
        }

        public async Task<List<Notification>> GetUserNotificationsAsync(string userId, bool unreadOnly = false)
        {
            var cacheKey = $"user_notifications_{userId}_{unreadOnly}";
            
            return await _cacheService.GetOrSetAsync(cacheKey, async () =>
            {
                var query = _context.Notifications
                    .Where(n => n.UserId == userId);

                if (unreadOnly)
                {
                    query = query.Where(n => !n.IsRead);
                }

                return await query
                    .OrderByDescending(n => n.CreatedAt)
                    .Take(50) // Limit to recent 50 notifications
                    .ToListAsync();
            }, TimeSpan.FromMinutes(5));
        }

        public async Task<int> GetUnreadCountAsync(string userId)
        {
            var cacheKey = $"user_unread_count_{userId}";
            
            // Try to get from cache first
            var cachedValue = await _cacheService.GetAsync<string>(cacheKey);
            if (cachedValue != null && int.TryParse(cachedValue, out var cachedCount))
            {
                return cachedCount;
            }

            // If not in cache, get from database
            var count = await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .CountAsync();

            // Cache the result
            await _cacheService.SetAsync(cacheKey, count.ToString(), TimeSpan.FromMinutes(2));
            
            return count;
        }

        public async Task MarkAsReadAsync(int notificationId, string userId)
        {
            try
            {
                var notification = await _context.Notifications
                    .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);

                if (notification != null && !notification.IsRead)
                {
                    notification.IsRead = true;
                    notification.ReadAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();

                    // Clear cache
                    await _cacheService.RemoveByPatternAsync($"user_notifications_{userId}*");
                    await _cacheService.RemoveAsync($"user_unread_count_{userId}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to mark notification {NotificationId} as read for user {UserId}", notificationId, userId);
                throw;
            }
        }

        public async Task MarkAllAsReadAsync(string userId)
        {
            try
            {
                var unreadNotifications = await _context.Notifications
                    .Where(n => n.UserId == userId && !n.IsRead)
                    .ToListAsync();

                foreach (var notification in unreadNotifications)
                {
                    notification.IsRead = true;
                    notification.ReadAt = DateTime.UtcNow;
                }

                if (unreadNotifications.Any())
                {
                    await _context.SaveChangesAsync();

                    // Clear cache
                    await _cacheService.RemoveByPatternAsync($"user_notifications_{userId}*");
                    await _cacheService.RemoveAsync($"user_unread_count_{userId}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to mark all notifications as read for user {UserId}", userId);
                throw;
            }
        }

        public async Task DeleteNotificationAsync(int notificationId, string userId)
        {
            try
            {
                var notification = await _context.Notifications
                    .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);

                if (notification != null)
                {
                    _context.Notifications.Remove(notification);
                    await _context.SaveChangesAsync();

                    // Clear cache
                    await _cacheService.RemoveByPatternAsync($"user_notifications_{userId}*");
                    await _cacheService.RemoveAsync($"user_unread_count_{userId}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete notification {NotificationId} for user {UserId}", notificationId, userId);
                throw;
            }
        }

        public async Task SendOrderNotificationAsync(Order order)
        {
            try
            {
                // Send notification to customer
                await CreateNotificationAsync(
                    order.UserId,
                    "Đơn hàng mới",
                    $"Đơn hàng #{order.OrderNumber} đã được tạo thành công với tổng giá trị {order.TotalAmount:C}",
                    "order",
                    $"/Account/Orders/{order.Id}"
                );

                // Send email confirmation
                var user = await _context.Users.FindAsync(order.UserId);
                if (user != null && !string.IsNullOrEmpty(user.Email))
                {
                    await _emailService.SendOrderConfirmationEmailAsync(user.Email, order);
                }

                // Notify admin about new order
                var adminUsers = await _context.Users
                    .Where(u => _context.UserRoles
                        .Any(ur => ur.UserId == u.Id && 
                               _context.Roles.Any(r => r.Id == ur.RoleId && r.Name == "Admin")))
                    .ToListAsync();

                foreach (var admin in adminUsers)
                {
                    await CreateNotificationAsync(
                        admin.Id,
                        "Đơn hàng mới",
                        $"Đơn hàng mới #{order.OrderNumber} từ {order.User?.FirstName} {order.User?.LastName}",
                        "admin_order",
                        $"/admin/orders/{order.Id}"
                    );
                }

                // Notify sellers about new orders containing their products
                var orderItems = await _context.OrderItems
                    .Include(oi => oi.Product)
                    .Where(oi => oi.OrderId == order.Id && oi.Product != null && !string.IsNullOrEmpty(oi.Product.SellerId))
                    .ToListAsync();

                var sellerIds = orderItems
                    .Select(oi => oi.Product.SellerId)
                    .Where(id => !string.IsNullOrEmpty(id))
                    .Distinct()
                    .ToList();

                foreach (var sellerId in sellerIds)
                {
                    if (string.IsNullOrEmpty(sellerId))
                        continue;
                        
                    var sellerProducts = orderItems
                        .Where(oi => oi.Product != null && oi.Product.SellerId == sellerId)
                        .ToList();
                    
                    var productCount = sellerProducts.Sum(oi => oi.Quantity);
                    var totalAmount = sellerProducts.Sum(oi => oi.TotalPrice);

                    await CreateNotificationAsync(
                        sellerId,
                        "Đơn hàng mới",
                        $"Bạn có đơn hàng mới #{order.OrderNumber} với {productCount} sản phẩm (Tổng: {totalAmount:N0} ₫)",
                        "seller_order",
                        $"/seller/orders/{order.Id}"
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send order notification for order {OrderId}", order.Id);
            }
        }

        public async Task SendWelcomeNotificationAsync(string userId, string userName)
        {
            try
            {
                await CreateNotificationAsync(
                    userId,
                    "Chào mừng đến với John Henry Fashion!",
                    $"Xin chào {userName}! Cảm ơn bạn đã đăng ký tài khoản. Khám phá bộ sưu tập thời trang mới nhất của chúng tôi.",
                    "welcome",
                    "/Products"
                );

                // Send welcome email
                var user = await _context.Users.FindAsync(userId);
                if (user != null && !string.IsNullOrEmpty(user.Email))
                {
                    await _emailService.SendWelcomeEmailAsync(user.Email, userName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send welcome notification for user {UserId}", userId);
            }
        }

        public async Task SendProductNotificationAsync(string userId, Product product, string notificationType)
        {
            try
            {
                var title = notificationType switch
                {
                    "back_in_stock" => "Sản phẩm đã có hàng",
                    "price_drop" => "Giảm giá sản phẩm",
                    "new_product" => "Sản phẩm mới",
                    _ => "Thông báo sản phẩm"
                };

                var message = notificationType switch
                {
                    "back_in_stock" => $"Sản phẩm '{product.Name}' đã có hàng trở lại!",
                    "price_drop" => $"Sản phẩm '{product.Name}' đang có giá ưu đãi: {product.Price:C}",
                    "new_product" => $"Sản phẩm mới '{product.Name}' vừa được ra mắt!",
                    _ => $"Có thông báo mới về sản phẩm '{product.Name}'"
                };

                await CreateNotificationAsync(
                    userId,
                    title,
                    message,
                    "product",
                    $"/Products/Details/{product.Id}"
                );

                // Send email notification
                var user = await _context.Users.FindAsync(userId);
                if (user != null && !string.IsNullOrEmpty(user.Email))
                {
                    await _emailService.SendProductNotificationEmailAsync(user.Email, product, notificationType);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send product notification for user {UserId}, product {ProductId}", userId, product.Id);
            }
        }

        public async Task SendSystemNotificationAsync(string userId, string message)
        {
            try
            {
                await CreateNotificationAsync(
                    userId,
                    "Thông báo hệ thống",
                    message,
                    "system"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send system notification for user {UserId}", userId);
            }
        }

        public async Task SendBulkNotificationAsync(List<string> userIds, string title, string message, string type)
        {
            try
            {
                var notifications = userIds.Select(userId => new Notification
                {
                    UserId = userId,
                    Title = title,
                    Message = message,
                    Type = type,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                }).ToList();

                _context.Notifications.AddRange(notifications);
                await _context.SaveChangesAsync();

                // Clear cache for all affected users
                foreach (var userId in userIds)
                {
                    await _cacheService.RemoveByPatternAsync($"user_notifications_{userId}*");
                    await _cacheService.RemoveAsync($"user_unread_count_{userId}");
                }

                _logger.LogInformation("Bulk notification sent to {UserCount} users: {Title}", userIds.Count, title);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send bulk notification to {UserCount} users", userIds.Count);
                throw;
            }
        }

        public async Task CleanupOldNotificationsAsync()
        {
            try
            {
                var cutoffDate = DateTime.UtcNow.AddDays(-30); // Keep notifications for 30 days

                var oldNotifications = await _context.Notifications
                    .Where(n => n.CreatedAt < cutoffDate)
                    .ToListAsync();

                if (oldNotifications.Any())
                {
                    _context.Notifications.RemoveRange(oldNotifications);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Cleaned up {Count} old notifications", oldNotifications.Count);

                    // Clear related cache
                    var affectedUserIds = oldNotifications.Select(n => n.UserId).Distinct();
                    foreach (var userId in affectedUserIds)
                    {
                        await _cacheService.RemoveByPatternAsync($"user_notifications_{userId}*");
                        await _cacheService.RemoveAsync($"user_unread_count_{userId}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to cleanup old notifications");
                throw;
            }
        }

        public async Task SendNotificationAsync(string userId, string title, string message, string type = "info", string? actionUrl = null)
        {
            await CreateNotificationAsync(userId, title, message, type, actionUrl);
        }
    }
}
