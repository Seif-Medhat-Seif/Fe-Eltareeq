// Api/Controllers/PaymobWebhookController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantOrderingPlatform.Application.Interfaces;

namespace RestaurantOrderingPlatform.Api.Controllers;

[ApiController]
[Route("api/payments/paymob")]
[AllowAnonymous]   // الـ HMAC هو الـ auth
public class PaymobWebhookController : ControllerBase
{
    private readonly IPaymentWebhookService _webhooks;
    public PaymobWebhookController(IPaymentWebhookService webhooks) => _webhooks = webhooks;

    [HttpPost("webhook")]
    public async Task<IActionResult> Handle([FromQuery] string? hmac, CancellationToken ct)
    {
        // raw body: الـ HMAC بيتحسب على قيم الحقول زي ما وصلت، فمنسيبش الـ model binder يلمس الـ JSON
        string body;
        using (var reader = new StreamReader(Request.Body))
            body = await reader.ReadToEndAsync(ct);

        var result = await _webhooks.HandlePaymobCallbackAsync(body, hmac, ct);

        // توقيع غلط → 401. أي exception تقني بيعدّي للـ ExceptionHandlingMiddleware → 500،
        // وده مقصود: Paymob يعيد الإرسال بدل ما الحدث يضيع.
        return result == WebhookResult.InvalidSignature
            ? Unauthorized(new { received = false })
            : Ok(new { received = true });
    }
}