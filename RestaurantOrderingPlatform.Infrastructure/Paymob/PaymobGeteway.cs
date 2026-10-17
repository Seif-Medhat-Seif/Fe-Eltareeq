// Infrastructure/Integrations/Paymob/PaymobSettings.cs
// Infrastructure/Integrations/Paymob/PaymobGateway.cs
// (الاتنين في ملف واحد هنا للمقارنة، فصّلهم زي ما كانوا)
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RestaurantOrderingPlatform.Application.Interfaces;
using RestaurantOrderingPlatform.Infrastructure.Paymob;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace RestaurantOrderingPlatform.Infrastructure.Integrations.Paymob;

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

    // ───────────────────── Create intention (Request واحد) ─────────────────────
    public async Task<PaymentIntentResult> CreateIntentAsync(PaymentIntentRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.CustomerPhone))
            throw new InvalidOperationException("Customer phone number is required by Paymob.");

        // المبلغ بالقروش (integer) — decimal + تقريب صريح
        var cents = (long)decimal.Round(request.Amount * 100m, 0, MidpointRounding.AwayFromZero);
        var nameParts = request.CustomerFullName.Split(' ', 2);

        var payload = new Dictionary<string, object?>
        {
            ["amount"] = cents,
            ["currency"] = _settings.Currency,
            ["payment_methods"] = new[] { _settings.IntegrationId },

            // مجموع الـ items لازم يساوي amount → صنف واحد بإجمالي الأوردر
            ["items"] = new[]
            {
                new { name = request.OrderNumber, amount = cents, quantity = 1, description = "Restaurant order" }
            },

            ["billing_data"] = new
            {
                first_name = nameParts[0],
                last_name = nameParts.Length > 1 ? nameParts[1] : "-",
                email = request.CustomerEmail,
                phone_number = NormalizePhone(request.CustomerPhone),
                street = request.Street,
                city = request.City,
                country = "EGY",
                apartment = "NA",
                floor = "NA",
                building = "NA",
                state = "NA",
                postal_code = "NA",
                shipping_method = "NA"
            },

            // بيرجع في الـ callback كـ obj.order.merchant_order_id → Guid.ToString() بس، من غير بادئة
            ["special_reference"] = request.OrderId.ToString(),

            // مدة صلاحية الـ intention. نفس الرقم بنرجّعه للفرونت كـ ExpiresAt
            ["expiration"] = _settings.IntentionExpirySeconds,

            ["notification_url"] = _settings.NotificationUrl
        };

        // redirection_url اختياري: بيرجّع العميل لصفحتك بعد الدفع (كارت ومحفظة بس). للعرض، مش للتأكيد.
        if (!string.IsNullOrWhiteSpace(_settings.RedirectionUrl))
            payload["redirection_url"] = _settings.RedirectionUrl;

        using var req = new HttpRequestMessage(HttpMethod.Post, $"{_settings.BaseUrl.TrimEnd('/')}/v1/intention/")
        {
            Content = JsonContent.Create(payload)
        };
        // الـ scheme هو كلمة "Token" مش "Bearer"
        req.Headers.Authorization = new AuthenticationHeaderValue("Token", _settings.SecretKey);

        using var res = await _http.SendAsync(req, ct);
        var body = await res.Content.ReadAsStringAsync(ct);

        if (!res.IsSuccessStatusCode)
        {
            // التفاصيل في اللوج بس، مش في الـ exception اللي ممكن توصل للعميل
            _logger.LogError("Paymob intention failed: {Status} {Body}", (int)res.StatusCode, body);
            throw new InvalidOperationException("Payment provider rejected the request.");
        }

        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        var clientSecret = root.GetProperty("client_secret").GetString()!;
        // ★ ده اللي بيرجع في الـ webhook كـ order.id — مش حقل "id"
        var paymobOrderId = root.GetProperty("intention_order_id").GetInt64().ToString();

        var checkoutUrl =
            $"{_settings.BaseUrl.TrimEnd('/')}/unifiedcheckout/" +
            $"?publicKey={Uri.EscapeDataString(_settings.PublicKey)}" +
            $"&clientSecret={Uri.EscapeDataString(clientSecret)}";

        return new PaymentIntentResult(
            paymobOrderId, checkoutUrl, DateTime.UtcNow.AddSeconds(_settings.IntentionExpirySeconds));
    }

    /// <summary>أرقام مصر (افتراض موثّق): 010... → +2010...</summary>
    private static string NormalizePhone(string phone)
    {
        var digits = new string(phone.Where(char.IsDigit).ToArray());
        if (digits.StartsWith("20")) return "+" + digits;
        if (digits.StartsWith("0")) return "+20" + digits[1..];
        return "+" + digits;
    }

    // ───────────────────── Callback + HMAC ─────────────────────

    // الترتيب مأخوذ من توثيق Paymob الرسمي (Transaction Processed callback)
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
            if (!root.TryGetProperty("obj", out var obj)) return false;   // مفيش fallback للـ root: التوقيع بيتحسب على obj

            var sb = new StringBuilder();
            foreach (var field in HmacFields)
                sb.Append(ReadPath(obj, field));

            var computed = ComputeHmacSha512(sb.ToString(), _settings.HmacSecret);

            // مقارنة ثابتة الزمن. (FixedTimeEquals بترجع false لو الطولين مختلفين، من غير exception)
            if (!CryptographicOperations.FixedTimeEquals(
                    Encoding.UTF8.GetBytes(computed),
                    Encoding.UTF8.GetBytes(hmacFromQuery.Trim().ToLowerInvariant())))
            {
                _logger.LogWarning("Paymob HMAC mismatch.");
                return false;
            }

            string? merchantOrderId = null;
            if (obj.TryGetProperty("order", out var ord) &&
                ord.ValueKind == JsonValueKind.Object &&
                ord.TryGetProperty("merchant_order_id", out var moid) &&
                moid.ValueKind != JsonValueKind.Null)
                merchantOrderId = moid.ValueKind == JsonValueKind.String ? moid.GetString() : moid.GetRawText();

            string? failure = null;
            if (obj.TryGetProperty("data", out var data) &&
                data.ValueKind == JsonValueKind.Object &&
                data.TryGetProperty("message", out var msg) &&
                msg.ValueKind == JsonValueKind.String)
                failure = msg.GetString();

            var amountCents = long.TryParse(ReadPath(obj, "amount_cents"), out var ac) ? ac : 0L;
            var currency = ReadPath(obj, "currency");

            callback = new GatewayCallback(
                ExternalEventId: ReadPath(obj, "id"),
                GatewayOrderId: ReadPath(obj, "order.id"),
                Success: IsTrue(obj, "success"),
                IsPending: IsTrue(obj, "pending"),
                IsFollowUpTransaction: IsTrue(obj, "is_refunded") || IsTrue(obj, "is_voided") || IsTrue(obj, "has_parent_transaction"),
                AmountPaid: amountCents / 100m,
                Currency: currency,
                FailureReason: failure,
                MerchantOrderId: merchantOrderId);

            return !string.IsNullOrEmpty(callback.ExternalEventId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse Paymob callback payload.");
            return false;
        }
    }

    private static bool IsTrue(JsonElement obj, string path)
        => string.Equals(ReadPath(obj, path), "true", StringComparison.OrdinalIgnoreCase);

    /// <summary>القيمة بالظبط زي ما وصلت (true/false صغيرة، أرقام من غير تنسيق). null → "" وده لسه محتاج تأكيد من payload حقيقي.</summary>
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