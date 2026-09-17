namespace RestaurantOrderingPlatform.Domain.Entities;

public class RestaurantEmployee
{
    public Guid Id { get; set; }

    public Guid RestaurantId { get; set; }
    public Restaurant Restaurant { get; set; } = null!;

    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    // BR-2 (one active restaurant per customer) is enforced by a unique
    // filtered index on CustomerId WHERE IsActive = 1 — set up in the
    // Fluent API configuration, not expressible as a plain C# attribute.
    public bool IsActive { get; set; } = true;

    public DateTime AssignedAt { get; set; }
}
