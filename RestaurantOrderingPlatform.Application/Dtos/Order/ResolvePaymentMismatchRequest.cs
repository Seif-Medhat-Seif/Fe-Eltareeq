namespace RestaurantOrderingPlatform.Application.DTOs.Order;

public record ResolvePaymentMismatchRequest(
    PaymentMismatchResolution Resolution,
    string? AdminNote);
