namespace RestaurantOrderingPlatform.Domain.Entities;

public class MenuItem
{
    public Guid Id { get; set; }

    public Guid MenuCategoryId { get; set; }
    public MenuCategory MenuCategory { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string? ImageUrl { get; set; }

    // Two distinct concepts, deliberately not merged:
    // IsAvailable = temporarily 86'd (owner toggles constantly, e.g. sold out today)
    // IsDeleted   = permanently removed, but FK integrity for historical OrderItems
    //               must survive it, so this is soft-delete, never a hard DELETE.
    public bool IsAvailable { get; set; } = true;
    public bool IsDeleted { get; set; }

    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
