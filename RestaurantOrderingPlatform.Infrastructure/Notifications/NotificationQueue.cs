using RestaurantOrderingPlatform.Application.Interfaces;
using System.Threading.Channels;

namespace RestaurantOrderingPlatform.Infrastructure.Notifications;

/// <summary>
/// Fire-and-forget queue في الذاكرة.
///
/// ليه مش Task.Run؟ لأن Task.Run جوه request بياخد الـ scoped services معاه
/// وممكن تلاقي DbContext اتعمله dispose تحت رجلك = ObjectDisposedException عشوائي.
/// وليه مش Hangfire؟ لأنه لسه مش متثبت، والـ Channel ده كفاية لـ V1
/// وبيتشال بسطرين لما Hangfire ييجي.
///
/// القيد الوحيد اللي لازم تعرفه: لو السيرفر وقع، الرسايل اللي في الـ queue بتضيع.
/// وده مقبول عن قصد — الـ spec بتقول الإشعارات fire-and-forget ومش بتتخزن.
/// </summary>
public class NotificationQueue : INotificationService
{
    private readonly Channel<OrderNotification> _channel =
        Channel.CreateBounded<OrderNotification>(new BoundedChannelOptions(1000)
        {
            FullMode = BoundedChannelFullMode.DropOldest   // إشعار ضايع > سيرفر واقف
        });

    public void Enqueue(OrderNotification notification) => _channel.Writer.TryWrite(notification);

    public IAsyncEnumerable<OrderNotification> ReadAllAsync(CancellationToken ct) =>
        _channel.Reader.ReadAllAsync(ct);
}