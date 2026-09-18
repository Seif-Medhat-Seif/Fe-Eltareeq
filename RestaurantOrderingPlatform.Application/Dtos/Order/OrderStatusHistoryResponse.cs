using System;
using RestaurantOrderingPlatform.Domain.Enums;

namespace RestaurantOrderingPlatform.Application.DTOs.Order;

public record OrderStatusHistoryResponse(
    OrderStatus Status,
    DateTime ChangedAt,
    string? Note);
