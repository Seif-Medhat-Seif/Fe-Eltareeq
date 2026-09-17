namespace RestaurantOrderingPlatform.Domain.Entities;

public class OrderItem
{
    public Guid Id { get; set; }

    public Guid OrderId { get; set; }
    public Order Order { get; set; } = null!;

    // Kept as an FK for reporting/joins (Module 10's top-items query), but
    // the two Snapshot fields below are the actual source of truth for
    // display — deliberately denormalized so future menu edits/deletes never
    // retroactively change what a historical order shows. This is the
    // Phase 3 domain design principle, now made concrete.
    public Guid MenuItemId { get; set; }
    public MenuItem MenuItem { get; set; } = null!;

    public string ItemNameSnapshot { get; set; } = string.Empty;
    public decimal ItemPriceSnapshot { get; set; }

    public int Quantity { get; set; }
    public decimal LineTotal { get; set; }
}
