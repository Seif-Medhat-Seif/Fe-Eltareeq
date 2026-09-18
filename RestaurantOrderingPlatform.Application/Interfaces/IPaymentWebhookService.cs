namespace RestaurantOrderingPlatform.Application.Interfaces;

public interface IPaymentWebhookService
{
    /// <summary>
    /// بيرجع دايمًا بدون ما يرمي exception للـ Controller (إلا لو حاجة كارثية).
    /// الـ Controller بيرجع 200 لـ Paymob في كل الحالات.
    /// </summary>
    Task HandlePaymobCallbackAsync(string rawJsonBody, string? hmacFromQuery, CancellationToken ct = default);
}