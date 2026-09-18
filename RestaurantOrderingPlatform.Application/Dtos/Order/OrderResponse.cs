using System;
using System.Collections.Generic;
using RestaurantOrderingPlatform.Domain.Enums;

namespace RestaurantOrderingPlatform.Application.DTOs.Order;

public record OrderResponse(
    Guid Id,
    string OrderNumber,
    OrderStatus Status,
    Guid RestaurantId,
    string RestaurantName,
    OrderAddressResponse Address,
    IReadOnlyList<OrderItemResponse> Items,
    decimal Subtotal,
    decimal DeliveryFee,
    decimal DiscountAmount,
    decimal Total,
    string? RejectionReason,
    DateTime CreatedAt,
    IReadOnlyList<OrderStatusHistoryResponse> StatusHistory);
