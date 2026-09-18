using System;

namespace RestaurantOrderingPlatform.Application.DTOs.Order;

/// <summary>
/// Lightweight response for list endpoints without items or status history.
/// </summary>
public record OrderSummaryResponse(
    Guid Id,
    string OrderNumber,
    RestaurantOrderingPlatform.Domain.Enums.OrderStatus Status,
    Guid RestaurantId,
    string RestaurantName,
    int ItemsCount,
    decimal Total,
    DateTime CreatedAt);
