using Microsoft.AspNetCore.Mvc;
using RestaurantOrderingPlatform.Application.DTOs.Auth;
using RestaurantOrderingPlatform.Application.Interfaces;

namespace RestaurantOrderingPlatform.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private const string RefreshCookieName = "refreshToken";

    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request, CancellationToken ct)
    {
        var (response, rawRefreshToken) = await _authService.RegisterAsync(request, ct);
        SetRefreshCookie(rawRefreshToken);
        return Ok(response);
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var (response, rawRefreshToken) = await _authService.LoginAsync(request, ct);
        SetRefreshCookie(rawRefreshToken);
        return Ok(response);
    }

    [HttpPost("google-login")]
    public async Task<ActionResult<AuthResponse>> GoogleLogin([FromBody] GoogleLoginRequest request, CancellationToken ct)
    {
        var (response, rawRefreshToken) = await _authService.GoogleLoginAsync(request, ct);
        SetRefreshCookie(rawRefreshToken);
        return Ok(response);
    }

    [HttpPost("refresh-token")]
    public async Task<ActionResult<AuthResponse>> RefreshToken(CancellationToken ct)
    {
        if (!Request.Cookies.TryGetValue(RefreshCookieName, out var rawRefreshToken) || string.IsNullOrEmpty(rawRefreshToken))
        {
            Response.Cookies.Delete(RefreshCookieName);
            return Unauthorized();
        }

        try
        {
            var (response, newRawRefreshToken) = await _authService.RefreshTokenAsync(rawRefreshToken, ct);
            SetRefreshCookie(newRawRefreshToken);
            return Ok(response);
        }
        catch (Application.Exceptions.UnauthorizedAppException)
        {
            Response.Cookies.Delete(RefreshCookieName); // clear the stale cookie, per the locked spec
            return Unauthorized();
        }
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken ct)
    {
        await _authService.ForgotPasswordAsync(request, ct);
        // Always 200 — never reveals whether the email exists (see AuthService).
        return Ok(new { message = "If that email is registered, a reset link has been sent." });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken ct)
    {
        await _authService.ResetPasswordAsync(request, ct);
        return Ok(new { message = "Password has been reset." });
    }

    private void SetRefreshCookie(string rawRefreshToken)
    {
        Response.Cookies.Append(RefreshCookieName, rawRefreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddDays(7)
        });
    }
}
