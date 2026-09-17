namespace RestaurantOrderingPlatform.Application.DTOs.Restaurant;

public class RestaurantResponse
{
    public Guid Id { get; set; }
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
    public bool ManuallyClosed { get; set; }
    public bool HideUnavailableItems { get; set; }

    // Always computed server-side (ManuallyClosed + WorkingHour vs. current
    // time) — NEVER a stored column, and never left to the frontend to derive.
    public bool IsOpen { get; set; }

    public DateTime CreatedAt { get; set; }
}
