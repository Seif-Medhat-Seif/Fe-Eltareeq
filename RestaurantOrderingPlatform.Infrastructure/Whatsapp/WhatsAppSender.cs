using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RestaurantOrderingPlatform.Application.Interfaces;
using RestaurantOrderingPlatform.Infrastructure.Integrations.WhatsApp;
using System.Net.Http.Json;

public class WhatsAppSender : IWhatsAppSender
{
    private readonly HttpClient _http;
    private readonly WhatsAppSettings _settings;
    private readonly ILogger<WhatsAppSender> _logger;

    public WhatsAppSender(HttpClient http, IOptions<WhatsAppSettings> settings, ILogger<WhatsAppSender> logger)
    {
        _http = http;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task SendAsync(OrderNotification n, CancellationToken ct = default)
    {
        var text = BuildMessage(n);

        if (!_settings.Enabled)
        {
            _logger.LogInformation("[WhatsApp disabled] To {Phone}: {Text}", n.ToPhoneNumber, text);
            return;
        }

        var url = $"{_settings.BaseUrl}/{_settings.PhoneNumberId}/messages";

        using var req = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(new
            {
                messaging_product = "whatsapp",
                to = Normalize(n.ToPhoneNumber),
                type = "text",
                text = new { body = text }
            })
        };
        req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _settings.AccessToken);

        var res = await _http.SendAsync(req, ct);
        if (!res.IsSuccessStatusCode)
        {
            var body = await res.Content.ReadAsStringAsync(ct);
            _logger.LogWarning("WhatsApp API returned {Status}: {Body}", res.StatusCode, body);
        }
    }

    private static string BuildMessage(OrderNotification n) => n.Type switch
    {
        OrderNotificationType.PaymentConfirmed =>
            $"Hi {n.CustomerName}, we received your payment for order {n.OrderNumber}. {n.RestaurantName} will confirm shortly.",
        OrderNotificationType.OrderAccepted =>
            $"Good news! {n.RestaurantName} accepted your order {n.OrderNumber}.",
        OrderNotificationType.OrderRejected =>
            $"We're sorry — {n.RestaurantName} could not accept order {n.OrderNumber}. Your refund is being processed.",
        OrderNotificationType.Preparing =>
            $"{n.RestaurantName} is preparing your order {n.OrderNumber}.",
        OrderNotificationType.Ready =>
            $"Your order {n.OrderNumber} is ready and waiting for a driver.",
        OrderNotificationType.PickedUp =>
            $"A driver picked up your order {n.OrderNumber}.",
        OrderNotificationType.OnTheWay =>
            $"Your order {n.OrderNumber} is on the way!",
        OrderNotificationType.Delivered =>
            $"Order {n.OrderNumber} delivered. Enjoy your meal — we'd love your review!",
        OrderNotificationType.PaymentFailed =>
            $"Payment for order {n.OrderNumber} did not go through. You can try again from the app.",
        _ => $"Update on your order {n.OrderNumber}."
    };

    /// <summary>WhatsApp عايز الرقم بصيغة دولية من غير + ولا مسافات.</summary>
    private static string Normalize(string phone)
    {
        var digits = new string(phone.Where(char.IsDigit).ToArray());
        if (digits.StartsWith("0")) digits = "20" + digits[1..];   // افتراض مصر (موثّق)
        return digits;
    }
}