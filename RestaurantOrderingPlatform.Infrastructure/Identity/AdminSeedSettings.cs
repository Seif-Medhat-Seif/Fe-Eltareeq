namespace RestaurantOrderingPlatform.Infrastructure.Identity;

public class AdminSeedSettings
{
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";     // user-secrets / environment variable فقط
    public string FullName { get; set; } = "Platform Admin";
}
