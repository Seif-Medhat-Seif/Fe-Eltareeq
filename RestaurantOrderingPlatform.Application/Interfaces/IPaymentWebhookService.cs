// Application/Common/Interfaces/IPaymentWebhookService.cs
namespace RestaurantOrderingPlatform.Application.Interfaces;

public enum WebhookResult
{
    Processed,          // اتعالج دلوقتي (نجاح، فشل دفع، mismatch... أي نتيجة حتمية)
    Duplicate,          // نفس الـ transaction id اتعالج قبل كده
    Ignored,            // pending / refund / void — مش محتاج فعل
    InvalidSignature    // الـ HMAC غلط → الـ Controller يرد 401
}

public interface IPaymentWebhookService
{
    /// <summary>
    /// بيرمي exception لو فشل تقني (DB وقع مثلًا) — ده مقصود: الـ Controller يرد 500
    /// فـ Paymob يعيد الإرسال. مفيش حدث بيضيع بصمت.
    /// </summary>
    Task<WebhookResult> HandlePaymobCallbackAsync(string rawJsonBody, string? hmacFromQuery, CancellationToken ct = default);
}