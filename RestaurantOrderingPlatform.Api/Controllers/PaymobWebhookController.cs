using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantOrderingPlatform.Application.Interfaces;

namespace RestaurantOrderingPlatform.Api.Controllers;

[ApiController]
[Route("api/payments/paymob")]
[AllowAnonymous]   // Paymob مش هيبعت JWT — الـ HMAC هو الـ auth
public class PaymobWebhookController : ControllerBase
{
    private readonly IPaymentWebhookService _webhooks;
    private readonly ILogger<PaymobWebhookController> _logger;

    public PaymobWebhookController(IPaymentWebhookService webhooks, ILogger<PaymobWebhookController> logger)
    {
        _webhooks = webhooks;
        _logger = logger;
    }

    [HttpPost("webhook")]
    public async Task<IActionResult> Handle([FromQuery] string? hmac, CancellationToken ct)
    {
        // ★ بنقرا الـ body خام (raw) مش [FromBody] object.
        //   السبب: حساب الـ HMAC لازم يبقى على النص زي ما وصل بالظبط.
        //   لو خليت الـ model binder يـ deserialize ويعيد serialize، أي فرق في
        //   الترتيب أو الـ escaping هيكسر التوقيع.
        string body;
        using (var reader = new StreamReader(Request.Body))
            body = await reader.ReadToEndAsync(ct);

        try
        {
            await _webhooks.HandlePaymobCallbackAsync(body, hmac, ct);
        }
        catch (Exception ex)
        {
            // ★ حتى لو الدنيا اتقلبت جوه — بنرجع 200.
            //   500 معناها Paymob هيعيد المحاولة، وممكن يعملها لأيام (retry storm).
            //   بنسجّل الخطأ ونتعامل معاه يدوي.
            _logger.LogError(ex, "Unhandled error while processing Paymob webhook.");
        }

        return Ok(new { received = true });
    }
}