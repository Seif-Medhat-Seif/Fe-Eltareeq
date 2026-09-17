namespace RestaurantOrderingPlatform.Application.DTOs.Auth;

public class ForgotPasswordRequest
{
    public string Email { get; set; } = string.Empty;
}

public class ResetPasswordRequest
{
    public string Email { get; set; } = string.Empty;

    // The Identity-generated reset token, sent as a link query param and
    // echoed back by the client — never generated or trusted client-side.
    public string Token { get; set; } = string.Empty;

    public string NewPassword { get; set; } = string.Empty;
}
