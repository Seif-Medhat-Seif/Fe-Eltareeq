namespace RestaurantOrderingPlatform.Application.DTOs.Auth;

// Replaces the earlier RegisterResponse.cs — delete that file and use this
// one everywhere an auth action returns a token (Register, Login, Google
// Login, Refresh Token all share this exact shape).
public class AuthResponse
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
    public DateTime AccessTokenExpiresAt { get; set; }
}
