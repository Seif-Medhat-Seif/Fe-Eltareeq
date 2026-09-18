namespace RestaurantOrderingPlatform.Infrastructure.Paymob;

// Infrastructure/Integrations/Paymob/PaymobSettings.cs
public class PaymobSettings
{
    public string BaseUrl { get; set; } = "https://accept.paymob.com/api";
    public string IframeBaseUrl { get; set; } = "https://accept.paymob.com/api/acceptance/iframes";
    public string ApiKey { get; set; } = default!;        // user-secrets
    public string HmacSecret { get; set; } = default!;    // user-secrets
    public int IntegrationId { get; set; }
    public int IframeId { get; set; }
    public string Currency { get; set; } = "EGP";
}