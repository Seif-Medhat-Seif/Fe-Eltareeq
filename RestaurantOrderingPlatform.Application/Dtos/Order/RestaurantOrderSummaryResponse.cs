using System;
using RestaurantOrderingPlatform.Domain.Enums;

namespace RestaurantOrderingPlatform.Application.DTOs.Order;

/// <summary>
/// Restaurant queue response: shows customer information, not restaurant information.
/// </summary>
public record RestaurantOrderSummaryResponse(
    Guid Id,
    string OrderNumber,
    OrderStatus Status,
    string CustomerName,
    string? CustomerPhone,
    int ItemsCount,
    decimal Total,
    DateTime CreatedAt);
