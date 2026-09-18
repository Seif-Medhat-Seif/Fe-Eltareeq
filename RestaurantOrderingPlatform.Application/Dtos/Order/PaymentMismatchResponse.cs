using System;

namespace RestaurantOrderingPlatform.Application.DTOs.Order;

public record PaymentMismatchResponse(
    Guid OrderId,
    string OrderNumber,
    decimal OrderTotal,
    decimal AmountPaid,
    string? PaymobTransactionReference,
    DateTime CreatedAt);
