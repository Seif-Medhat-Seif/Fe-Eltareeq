namespace RestaurantOrderingPlatform.Domain.Entities;

public class CouponUsage
{
    public Guid Id { get; set; }

    public Guid CouponId { get; set; }
    public Coupon Coupon { get; set; } = null!;

    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public Guid OrderId { get; set; }
    public Order Order { get; set; } = null!;

    public DateTime UsedAt { get; set; }
}
