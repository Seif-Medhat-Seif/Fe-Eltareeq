using System;

namespace RestaurantOrderingPlatform.Application.DTOs.Order;

public record OrderItemResponse(
    Guid Id,
    Guid MenuItemId,
    string ItemName,
    decimal ItemPrice,
    int Quantity,
    decimal LineTotal);
