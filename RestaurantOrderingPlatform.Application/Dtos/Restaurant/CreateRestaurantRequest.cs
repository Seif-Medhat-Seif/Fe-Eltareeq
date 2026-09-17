namespace RestaurantOrderingPlatform.Application.DTOs.Restaurant;

public class CreateRestaurantRequest
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
