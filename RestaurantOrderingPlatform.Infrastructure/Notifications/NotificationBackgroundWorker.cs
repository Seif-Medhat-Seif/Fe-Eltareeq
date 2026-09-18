using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RestaurantOrderingPlatform.Application.Interfaces;

namespace RestaurantOrderingPlatform.Infrastructure.Notifications;

public class NotificationBackgroundWorker : BackgroundService
{
    private readonly NotificationQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<NotificationBackgroundWorker> _logger;

    public NotificationBackgroundWorker(
        NotificationQueue queue,
        IServiceScopeFactory scopeFactory,
        ILogger<NotificationBackgroundWorker> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var notification in _queue.ReadAllAsync(stoppingToken))
        {
            try
            {
                // ★ scope جديد لكل رسالة — مبنستخدمش الـ scope بتاع الـ request
                using var scope = _scopeFactory.CreateScope();
                var sender = scope.ServiceProvider.GetRequiredService<IWhatsAppSender>();
                await sender.SendAsync(notification, stoppingToken);
            }
            catch (Exception ex)
            {
                // ★ الـ catch ده مش اختياري. exception غير ممسوك هنا
                //   بيوقف الـ BackgroundService كله وبالتالي كل الإشعارات بعد كده.
                _logger.LogWarning(ex,
                    "Failed to send WhatsApp notification for order {OrderNumber}",
                    notification.OrderNumber);
            }
        }
    }
}