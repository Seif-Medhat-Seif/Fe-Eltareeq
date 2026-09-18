namespace RestaurantOrderingPlatform.Infrastructure.Integrations.WhatsApp;

public class WhatsAppSettings
{
    public string BaseUrl { get; set; } = "https://graph.facebook.com/v20.0";
    public string PhoneNumberId { get; set; } = default!;
    public string AccessToken { get; set; } = default!;   // user-secrets
    public bool Enabled { get; set; } = true;             // false في الـ Development
}