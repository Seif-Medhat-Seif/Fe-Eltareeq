using RestaurantOrderingPlatform.Application.DTOs.Auth;

namespace RestaurantOrderingPlatform.Application.Interfaces;

// Replaces the earlier IAuthService.cs — complete Module 1 version.
// Every method that issues an access token also returns a raw refresh
// token string via the out parameter — the Controller is the ONLY layer
// allowed to put that value somewhere (an HttpOnly cookie), per NFR-9.
public interface IAuthService
{
    Task<(AuthResponse Response, string RawRefreshToken)> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
    Task<(AuthResponse Response, string RawRefreshToken)> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task<(AuthResponse Response, string RawRefreshToken)> GoogleLoginAsync(GoogleLoginRequest request, CancellationToken ct = default);
    Task<(AuthResponse Response, string RawRefreshToken)> RefreshTokenAsync(string rawRefreshToken, CancellationToken ct = default);

    Task ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken ct = default);
    Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct = default);
}
