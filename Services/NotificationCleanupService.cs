using JohnHenryFashionWeb.Data;
using Microsoft.EntityFrameworkCore;

namespace JohnHenryFashionWeb.Services
{
    /// <summary>
    /// Background service để tự động xóa notifications cũ hơn 3 tháng
    /// Chạy mỗi ngày lúc 2:00 AM
    /// </summary>
    public class NotificationCleanupService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<NotificationCleanupService> _logger;
        private readonly TimeSpan _runInterval = TimeSpan.FromDays(1); // Chạy mỗi ngày
        private readonly int _retentionDays = 90; // 3 tháng = 90 ngày

        public NotificationCleanupService(
            IServiceProvider serviceProvider,
            ILogger<NotificationCleanupService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Notification Cleanup Service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Tính thời gian đến lần chạy tiếp theo (2:00 AM)
                    var now = DateTime.Now;
                    var nextRun = now.Date.AddDays(1).AddHours(2); // 2:00 AM ngày mai
                    
                    if (now.Hour < 2)
                    {
                        // Nếu chưa qua 2:00 AM hôm nay, chạy hôm nay
                        nextRun = now.Date.AddHours(2);
                    }

                    var delay = nextRun - now;
                    _logger.LogInformation($"Next notification cleanup will run at {nextRun}");

                    await Task.Delay(delay, stoppingToken);

                    // Thực hiện cleanup
                    await CleanupOldNotificationsAsync(stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    // Bình thường khi app shutdown, không log error
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in Notification Cleanup Service");
                    // Đợi 1 giờ trước khi thử lại nếu có lỗi
                    try
                    {
                        await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
            }

            _logger.LogInformation("Notification Cleanup Service stopped");
        }

        private async Task CleanupOldNotificationsAsync(CancellationToken cancellationToken)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var cutoffDate = DateTime.UtcNow.AddDays(-_retentionDays);

                var oldNotifications = await context.Notifications
                    .Where(n => n.CreatedAt < cutoffDate)
                    .ToListAsync(cancellationToken);

                if (oldNotifications.Any())
                {
                    _logger.LogInformation($"Deleting {oldNotifications.Count} notifications older than {_retentionDays} days");
                    
                    context.Notifications.RemoveRange(oldNotifications);
                    await context.SaveChangesAsync(cancellationToken);

                    _logger.LogInformation($"Successfully deleted {oldNotifications.Count} old notifications");
                }
                else
                {
                    _logger.LogInformation("No old notifications to delete");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during notification cleanup");
                throw;
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Notification Cleanup Service is stopping");
            await base.StopAsync(cancellationToken);
        }
    }
}
