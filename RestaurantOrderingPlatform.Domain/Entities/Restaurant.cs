namespace RestaurantOrderingPlatform.Domain.Entities;

public class Restaurant
{
    public Guid Id { get; set; }

    public Guid RestaurantOwnerId { get; set; }
    public RestaurantOwner RestaurantOwner { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? CuisineType { get; set; }

    public string? LogoUrl { get; set; }
    public string? CoverImageUrl { get; set; }

    public string Street { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }

    public decimal DeliveryFee { get; set; }
    public decimal? MinimumOrderAmount { get; set; }
    public int? EstimatedDeliveryTimeMinutes { get; set; }

    // Renamed from IsActive per the Phase 4 reconciliation — this is the raw
    // manual toggle only. "isOpen" in API responses is ALWAYS computed at
    // read-time (ManuallyClosed + current time vs WorkingHour), never stored.
    public bool ManuallyClosed { get; set; }

    public bool HideUnavailableItems { get; set; }

    public DateTime CreatedAt { get; set; }

    // Navigation
    public ICollection<WorkingHour> WorkingHours { get; set; } = new List<WorkingHour>();
    public ICollection<MenuCategory> MenuCategories { get; set; } = new List<MenuCategory>();
    public ICollection<RestaurantEmployee> Employees { get; set; } = new List<RestaurantEmployee>();
    public ICollection<Coupon> Coupons { get; set; } = new List<Coupon>();
    public ICollection<Order> Orders { get; set; } = new List<Order>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
}
