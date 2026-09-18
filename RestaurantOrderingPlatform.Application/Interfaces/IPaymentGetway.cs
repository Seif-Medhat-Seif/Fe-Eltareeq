namespace RestaurantOrderingPlatform.Application.Common.Interfaces;

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
    string GatewayOrderId,
    string PaymentToken,
    string CheckoutUrl);

public interface IPaymentGateway
{
    Task<PaymentIntentResult> CreateIntentAsync(PaymentIntentRequest request,
        CancellationToken ct = default);

    /// <summary>بيتحقق من التوقيع ويحوّل الـ payload لشكل مفهوم للـ Application.</summary>
    bool TryParseCallback(string rawJsonBody, string? hmacFromQuery, out GatewayCallback callback);
}

public record GatewayCallback(
    string ExternalEventId,     // transaction id
    string GatewayOrderId,
    bool Success,
    decimal AmountPaid,          // بالجنيه، مش بالقروش — التحويل مسؤولية الـ Infrastructure
    string Currency,
    string? FailureReason,
    string? MerchantOrderId);    // إحنا بنحط فيه Order.Id