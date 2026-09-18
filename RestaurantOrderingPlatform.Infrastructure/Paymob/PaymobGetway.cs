// Infrastructure/Integrations/Paymob/PaymobGateway.cs
using System.Globalization;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RestaurantOrderingPlatform.Application.Common.Interfaces;

namespace RestaurantOrderingPlatform.Infrastructure.Paymob;

/// <summary>
/// Wrapper رفيع حوالين Paymob. مفيش هنا أي قرار بيزنس —
/// ده "بيقول إيه اللي حصل" بس، والـ OrderService/WebhookService هما اللي بيقرروا.
/// (نفس المبدأ المكتوب في Phase 6: Integrations vs Services)
/// </summary>
public class PaymobGateway : IPaymentGateway
{
    private readonly HttpClient _http;
    private readonly PaymobSettings _settings;
    private readonly ILogger<PaymobGateway> _logger;

    public PaymobGateway(HttpClient http, IOptions<PaymobSettings> settings, ILogger<PaymobGateway> logger)
    {
        _http = http;
        _settings = settings.Value;
        _logger = logger;
    }

    // ───────────────────────── Create intent (3 خطوات) ─────────────────────
    public async Task<PaymentIntentResult> CreateIntentAsync(PaymentIntentRequest request, CancellationToken ct = default)
    {
        // 1) auth token
        var authRes = await _http.PostAsJsonAsync($"{_settings.BaseUrl}/auth/tokens",
            new { api_key = _settings.ApiKey }, ct);
        authRes.EnsureSuccessStatusCode();
        var authToken = (await authRes.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct))
            .GetProperty("token").GetString()!;

        // 2) order registration
        // ★ amount_cents: Paymob بيتعامل بالقروش (integers) — أي حساب بالـ double هنا
        //   بيولّد فروق قرش. بنستخدم decimal ونقرّب صراحة.
        var amountCents = (long)decimal.Round(request.Amount * 100m, 0, MidpointRounding.AwayFromZero);

        var orderRes = await _http.PostAsJsonAsync($"{_settings.BaseUrl}/ecommerce/orders", new
        {
            auth_token = authToken,
            delivery_needed = "false",
            amount_cents = amountCents.ToString(CultureInfo.InvariantCulture),
            currency = _settings.Currency,
            // ★ ده اللي هيرجعلنا في الـ webhook ونعرف بيه الأوردر بتاعنا
            merchant_order_id = request.OrderId.ToString(),
            items = Array.Empty<object>()
        }, ct);
        orderRes.EnsureSuccessStatusCode();
        var paymobOrderId = (await orderRes.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct))
            .GetProperty("id").GetRawText();

        // 3) payment key
        var nameParts = request.CustomerFullName.Split(' ', 2);
        var keyRes = await _http.PostAsJsonAsync($"{_settings.BaseUrl}/acceptance/payment_keys", new
        {
            auth_token = authToken,
            amount_cents = amountCents.ToString(CultureInfo.InvariantCulture),
            expiration = 3600,
            order_id = paymobOrderId,
            currency = _settings.Currency,
            integration_id = _settings.IntegrationId,
            billing_data = new
            {
                first_name = nameParts[0],
                last_name = nameParts.Length > 1 ? nameParts[1] : "-",
                email = request.CustomerEmail,
                phone_number = request.CustomerPhone ?? "+200000000000",
                street = request.Street,
                city = request.City,
                country = "EG",
                apartment = "NA",
                floor = "NA",
                building = "NA",
                shipping_method = "NA",
                postal_code = "NA",
                state = "NA"
            }
        }, ct);
        keyRes.EnsureSuccessStatusCode();
        var paymentToken = (await keyRes.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct))
            .GetProperty("token").GetString()!;

        var checkoutUrl = $"{_settings.IframeBaseUrl}/{_settings.IframeId}?payment_token={paymentToken}";

        return new PaymentIntentResult(paymobOrderId, paymentToken, checkoutUrl);
    }

    // ───────────────────────── Callback + HMAC ─────────────────────────────

    /// <summary>
    /// الحقول اللي Paymob بيحسب عليها الـ HMAC — بالترتيب المعجمي ده بالظبط.
    /// أي حرف زيادة أو ناقص = توقيع غلط. الترتيب مش اختياري.
    /// </summary>
    private static readonly string[] HmacFields =
    {
        "amount_cents", "created_at", "currency", "error_occured", "has_parent_transaction",
        "id", "integration_id", "is_3d_secure", "is_auth", "is_capture", "is_refunded",
        "is_standalone_payment", "is_voided", "order.id", "owner", "pending",
        "source_data.pan", "source_data.sub_type", "source_data.type", "success"
    };

    public bool TryParseCallback(string rawJsonBody, string? hmacFromQuery, out GatewayCallback callback)
    {
        callback = default!;

        if (string.IsNullOrWhiteSpace(hmacFromQuery)) return false;

        try
        {
            using var doc = JsonDocument.Parse(rawJsonBody);
            var root = doc.RootElement;

            // الـ payload بييجي على شكل { "type": "TRANSACTION", "obj": { ... } }
            var obj = root.TryGetProperty("obj", out var o) ? o : root;

            // 1) نبني السلسلة ونقارن التوقيع
            var sb = new StringBuilder();
            foreach (var field in HmacFields)
                sb.Append(ReadPath(obj, field));

            var computed = ComputeHmacSha512(sb.ToString(), _settings.HmacSecret);

            // ★ مقارنة ثابتة الزمن — مش == عادية.
            //   المقارنة العادية بتخرج من أول حرف مختلف، وده بيسرّب معلومة
            //   عن طول البادئة الصحيحة (timing attack).
            if (!CryptographicOperations.FixedTimeEquals(
                    Encoding.UTF8.GetBytes(computed),
                    Encoding.UTF8.GetBytes(hmacFromQuery.Trim().ToLowerInvariant())))
            {
                _logger.LogWarning("Paymob HMAC mismatch.");
                return false;
            }

            // 2) نفك الـ payload
            var transactionId = ReadPath(obj, "id");
            var gatewayOrderId = ReadPath(obj, "order.id");
            var success = string.Equals(ReadPath(obj, "success"), "true", StringComparison.OrdinalIgnoreCase);
            var amountCents = long.TryParse(ReadPath(obj, "amount_cents"), out var ac) ? ac : 0L;
            var currency = ReadPath(obj, "currency");

            string? merchantOrderId = null;
            if (obj.TryGetProperty("order", out var ord) &&
                ord.TryGetProperty("merchant_order_id", out var moid))
                merchantOrderId = moid.ValueKind == JsonValueKind.String ? moid.GetString() : moid.GetRawText();

            string? failure = null;
            if (obj.TryGetProperty("data", out var data) &&
                data.TryGetProperty("message", out var msg))
                failure = msg.GetString();

            callback = new GatewayCallback(
                ExternalEventId: transactionId,
                GatewayOrderId: gatewayOrderId,
                Success: success,
                AmountPaid: amountCents / 100m,     // رجوع للجنيه — decimal مش double
                Currency: string.IsNullOrEmpty(currency) ? "EGP" : currency,
                FailureReason: failure,
                MerchantOrderId: merchantOrderId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse Paymob callback payload.");
            return false;
        }
    }

    private static string ReadPath(JsonElement root, string path)
    {
        var current = root;
        foreach (var segment in path.Split('.'))
        {
            if (current.ValueKind != JsonValueKind.Object || !current.TryGetProperty(segment, out var next))
                return string.Empty;
            current = next;
        }

        return current.ValueKind switch
        {
            JsonValueKind.String => current.GetString() ?? string.Empty,
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Null => string.Empty,
            _ => current.GetRawText()
        };
    }

    private static string ComputeHmacSha512(string payload, string secret)
    {
        using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}