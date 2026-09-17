namespace RestaurantOrderingPlatform.Domain.Entities;

public class CartItem
{
    public Guid Id { get; set; }

    public Guid CartId { get; set; }
    public Cart Cart { get; set; } = null!;

    public Guid MenuItemId { get; set; }
    public MenuItem MenuItem { get; set; } = null!;

    public int Quantity { get; set; }

    public decimal PriceAtAdd { get; set; }

    // Deliberately NOT storing a price snapshot here — Module 5 locked live
    // price recalculation on every GET /api/cart. Snapshotting only happens
    // once, at Order creation (below). Storing a price here would be a second,
    // premature source of truth that Order's snapshot would then have to
    // override anyway.
}
