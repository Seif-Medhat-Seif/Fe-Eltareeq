namespace RestaurantOrderingPlatform.Infrastructure.Paymob;

// Infrastructure/Integrations/Paymob/PaymobSettings.cs
public class PaymobSettings
{
    public string BaseUrl { get; set; } = "https://accept.paymob.com";
    public string SecretKey { get; set; } = default!;       // user-secrets (sk_test_... / sk_live_...)
    public string PublicKey { get; set; } = default!;       // user-secrets أو env (pk_test_... / pk_live_...)
    public string HmacSecret { get; set; } = default!;      // user-secrets
    public int IntegrationId { get; set; }
    public string Currency { get; set; } = "EGP";
    public string NotificationUrl { get; set; } = default!; // https عام. مش localhost
    public string? RedirectionUrl { get; set; }             // (صفحة نتيجة الدفع في الفرونت (للعرض بس، مش دليل دفع
    public int IntentionExpirySeconds { get; set; } = 3600;
}