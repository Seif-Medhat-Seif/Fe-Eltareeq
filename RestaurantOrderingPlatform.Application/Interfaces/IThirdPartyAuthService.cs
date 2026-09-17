namespace RestaurantOrderingPlatform.Application.Interfaces;

public record GoogleUserInfo(string Email, string FullName);

// Implemented in Infrastructure/GoogleAuth using Google.Apis.Auth.
// Throws UnauthorizedAppException (Application.Exceptions) if the token
// is invalid, expired, or its audience doesn't match our Client ID.
public interface IThirdPartyAuthService
{
    Task<GoogleUserInfo> ValidateIdTokenAsync(string idToken, CancellationToken ct = default);
}
