using System;

namespace RestaurantOrderingPlatform.Application.DTOs.Order;

public record PaymentIntentResponse(
    Guid PaymentId,
    Guid OrderId,
    string PaymentToken,
    string CheckoutUrl,
    decimal Amount,
    DateTime ExpiresAt);
