using RestaurantOrderingPlatform.Domain.Enums;

namespace RestaurantOrderingPlatform.Domain.Entities;

public class OrderStatusHistory
{
    public Guid Id { get; set; }

    public Guid OrderId { get; set; }
    public Order Order { get; set; } = null!;

    public OrderStatus Status { get; set; }
    public DateTime ChangedAt { get; set; }

    // --- NEW: مين اللي غيّر؟ (null = النظام/الـ webhook) ---
    public Guid? ChangedByUserId { get; set; }
    public string? Note { get; set; }          // سبب الرفض / ملاحظة الأدمن
}
