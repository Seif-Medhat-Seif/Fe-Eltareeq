namespace RestaurantOrderingPlatform.Domain.Entities;

public class MenuCategory
{
    public Guid Id { get; set; }

    public Guid RestaurantId { get; set; }
    public Restaurant Restaurant { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }

    public ICollection<MenuItem> Items { get; set; } = new List<MenuItem>();
}
