using RestaurantOrderingPlatform.Domain.Enums;

namespace RestaurantOrderingPlatform.Domain.Entities;

public class Payment
{
    public Guid Id { get; set; }

    // 1:many with Order — deliberately no unique constraint on OrderId,
    // since retries create additional rows against the same order.
    public Guid OrderId { get; set; }
    public Order Order { get; set; } = null!;

    public PaymentStatus Status { get; set; }
    public decimal AmountPaid { get; set; }

    // Paymob's transaction reference — this is what the webhook uses to
    // look up which Payment row a callback belongs to.
    public string? PaymobTransactionReference { get; set; }

    public DateTime CreatedAt { get; set; }
}
