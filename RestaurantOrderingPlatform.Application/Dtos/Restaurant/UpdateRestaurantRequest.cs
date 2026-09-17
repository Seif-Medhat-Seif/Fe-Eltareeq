namespace RestaurantOrderingPlatform.Application.DTOs.Restaurant;

// Full replace — same shape as Create. ManuallyClosed is NOT here
// deliberately: it has its own dedicated PATCH endpoint (a toggle-only
// action shouldn't be bundled with a full-profile replace, to avoid a
// client accidentally reopening/closing a restaurant while editing its name).
public class UpdateRestaurantRequest
{
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
    public bool HideUnavailableItems { get; set; }
}
