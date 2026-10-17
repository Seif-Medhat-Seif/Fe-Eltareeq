// Application/Common/Interfaces/IPaymentGateway.cs
// (يستبدل الـ region القديم في Module6.Interfaces.cs)
namespace RestaurantOrderingPlatform.Application.Interfaces;

public record PaymentIntentRequest(
    Guid OrderId,
    string OrderNumber,
    decimal Amount,
    string CustomerFullName,
    string CustomerEmail,
    string? CustomerPhone,
    string City,
    string Street);

public record PaymentIntentResult(
    string GatewayOrderId,   // = intention_order_id (بيرجع في الـ webhook كـ order.id)
    string CheckoutUrl,
    DateTime ExpiresAt);       // نفس القيمة اللي بعتناها لـ Paymob في expiration

public interface IPaymentGateway
{
    Task<PaymentIntentResult> CreateIntentAsync(PaymentIntentRequest request, CancellationToken ct = default);

    /// <summary>false = التوقيع غلط أو الـ payload مش مفهوم.</summary>
    bool TryParseCallback(string rawJsonBody, string? hmacFromQuery, out GatewayCallback callback);
}

public record GatewayCallback(
    string ExternalEventId,       // obj.id  (مفتاح الـ idempotency)
    string GatewayOrderId,        // obj.order.id
    bool Success,
    bool IsPending,             // callback مبدئي — مش نتيجة نهائية
    bool IsFollowUpTransaction, // refund / void / capture (is_refunded | is_voided | has_parent_transaction)
    decimal AmountPaid,            // بالجنيه
    string Currency,
    string? FailureReason,
    string? MerchantOrderId);      // = special_reference = Order.Id