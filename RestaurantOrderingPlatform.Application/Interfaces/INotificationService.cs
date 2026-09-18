namespace RestaurantOrderingPlatform.Application.Interfaces;

public enum OrderNotificationType
{
    PaymentConfirmed, OrderAccepted, OrderRejected, Preparing,
    Ready, PickedUp, OnTheWay, Delivered, PaymentFailed
}

public record OrderNotification(
    string ToPhoneNumber,
    string CustomerName,
    string OrderNumber,
    string RestaurantName,
    OrderNotificationType Type,
    string? Extra = null);

/// <summary>
/// Fire-and-forget. الـ implementation بيحط الرسالة في queue في الذاكرة
/// وبيرجع فورًا — فشل واتساب ممنوع منعًا باتًا يوقف تغيير حالة الأوردر (NFR).
/// </summary>
public interface INotificationService
{
    void Enqueue(OrderNotification notification);
}

/// <summary>الـ transport الفعلي — بينفّذ في الـ background worker.</summary>
public interface IWhatsAppSender
{
    Task SendAsync(OrderNotification notification, CancellationToken ct = default);
}