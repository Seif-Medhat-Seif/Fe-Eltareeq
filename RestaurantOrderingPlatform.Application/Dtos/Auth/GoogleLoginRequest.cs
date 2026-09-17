using RestaurantOrderingPlatform.Domain.Enums;

namespace RestaurantOrderingPlatform.Application.DTOs.Auth;

public class GoogleLoginRequest
{
    // Google Sign-In ID Token from the client SDK — verified server-side
    // against Google's public keys (IGoogleAuthService), never trusted as-is.
    public string IdToken { get; set; } = string.Empty;

    // Only used if the email is NEW. If the email already has an account,
    // this is deliberately IGNORED — the existing role always wins
    // (locked behavior, Module 1 spec).
    public AccountType AccountType { get; set; }
}
