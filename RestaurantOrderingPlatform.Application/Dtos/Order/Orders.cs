// Application/DTOs/Orders/OrderDtos.cs
using RestaurantOrderingPlatform.Domain.Enums;

namespace RestaurantOrderingPlatform.Application.DTOs.Orders;

// ───────────────────────────── Requests ─────────────────────────────

/// <summary>
/// Checkout. لاحظ: مفيش items، مفيش أسعار، مفيش total.
/// العميل بيبعت AddressId بس — كل حاجة تانية بتتقرا من الـ Cart سيرفر-سايد.
/// ده تطبيق حرفي لـ "الباك إند مبيثقش في الفرونت".
/// </summary>
public record CreateOrderRequest(Guid AddressId);

public record InitiatePaymentRequest();   // فاضي عن قصد — كل حاجة من الـ Order

public record RejectOrderRequest(string Reason);

public record UpdateOrderStatusRequest(OrderStatus Status);

public record ResolvePaymentMismatchRequest(
    PaymentMismatchResolution Resolution,
    string? AdminNote);

public enum PaymentMismatchResolution { ConfirmPaid, MarkFailed }

// ───────────────────────────── Responses ─────────────────────────────

public record OrderItemResponse(
    Guid Id,
    Guid MenuItemId,
    string ItemName,
    decimal ItemPrice,
    int Quantity,
    decimal LineTotal);

public record OrderStatusHistoryResponse(
    OrderStatus Status,
    DateTime ChangedAt,
    string? Note);

public record OrderAddressResponse(
    string Label,
    string Line,
    string City,
    string? Landmark);

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

/// <summary>نسخة خفيفة للـ list endpoints — من غير items ولا history.</summary>
public record OrderSummaryResponse(
    Guid Id,
    string OrderNumber,
    OrderStatus Status,
    Guid RestaurantId,
    string RestaurantName,
    int ItemsCount,
    decimal Total,
    DateTime CreatedAt);

/// <summary>الـ queue بتاع المطعم — بيشوف العميل مش المطعم.</summary>
public record RestaurantOrderSummaryResponse(
    Guid Id,
    string OrderNumber,
    OrderStatus Status,
    string CustomerName,
    string? CustomerPhone,
    int ItemsCount,
    decimal Total,
    DateTime CreatedAt);

public record PaymentIntentResponse(
    Guid PaymentId,
    Guid OrderId,
    string CheckoutUrl,
    decimal Amount,
    DateTime ExpiresAt);

public record PaymentMismatchResponse(
    Guid OrderId,
    string OrderNumber,
    decimal OrderTotal,
    decimal AmountPaid,
    string? PaymobTransactionReference,
    DateTime CreatedAt);
