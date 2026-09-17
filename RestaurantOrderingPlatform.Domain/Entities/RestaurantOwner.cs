namespace RestaurantOrderingPlatform.Domain.Entities;

public class RestaurantOwner
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    // Navigation — 1:1, nullable because an owner can exist with zero restaurants
    // (confirmed in Module 2: registration and restaurant creation are separate steps)
    public Restaurant? Restaurant { get; set; }
}
