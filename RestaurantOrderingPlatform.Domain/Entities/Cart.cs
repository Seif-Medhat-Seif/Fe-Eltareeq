namespace RestaurantOrderingPlatform.Domain.Entities;

public class Cart
{
    public Guid Id { get; set; }

    // Unique — one active cart per customer (Phase 4 constraint).
    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    // Nullable: an empty cart has no restaurant yet. First item added sets this;
    // clearing the cart to empty resets it back to null (Module 5 behavior).
    public Guid? RestaurantId { get; set; }
    public Restaurant? Restaurant { get; set; }

    public Guid? CouponId { get; set; }
    public Coupon? Coupon { get; set; }

    public ICollection<CartItem> Items { get; set; } = new List<CartItem>();
}
