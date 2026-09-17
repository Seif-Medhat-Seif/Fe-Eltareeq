using Google.Apis.Auth;
using Microsoft.Extensions.Configuration;
using RestaurantOrderingPlatform.Application.Exceptions;
using RestaurantOrderingPlatform.Application.Interfaces;

namespace RestaurantOrderingPlatform.Infrastructure.GoogleAuth;

// Implements the generic IThirdPartyAuthService — the class name stays
// Google-specific (it IS the Google adapter), only the interface it
// implements is generic. This is the one file that would be deleted if
// the provider ever changed.
public class GoogleAuthService : IThirdPartyAuthService
{
    private readonly IConfiguration _configuration;

    public GoogleAuthService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<GoogleUserInfo> ValidateIdTokenAsync(string idToken, CancellationToken ct = default)
    {
        var clientId = _configuration["GoogleAuth:ClientId"]
            ?? throw new InvalidOperationException("GoogleAuth:ClientId is not configured.");

        try
        {
            var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = new[] { clientId }
            });

            return new GoogleUserInfo(payload.Email, payload.Name);
        }
        catch (InvalidJwtException)
        {
            throw new UnauthorizedAppException("Invalid Google token.");
        }
    }
}
