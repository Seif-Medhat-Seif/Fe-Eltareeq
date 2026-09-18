namespace RestaurantOrderingPlatform.Application.DTOs.Order;

public record OrderAddressResponse(
    string Label,
    string Line,
    string City,
    string? Landmark);
