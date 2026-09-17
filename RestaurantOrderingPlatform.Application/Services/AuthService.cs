using System.Security.Cryptography;
using RestaurantOrderingPlatform.Application.DTOs.Auth;
using RestaurantOrderingPlatform.Application.Exceptions;
using RestaurantOrderingPlatform.Application.Interfaces;
using RestaurantOrderingPlatform.Domain.Entities;
using RestaurantOrderingPlatform.Domain.Enums;

namespace RestaurantOrderingPlatform.Application.Services;

public class AuthService : IAuthService
{
    private readonly IApplicationDbContext _context;
    private readonly IIdentityService _identityService;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IThirdPartyAuthService _googleAuthService;
    private readonly IEmailService _emailService;

    private const int RefreshTokenLifetimeDays = 7;
    private const int RefreshTokenRotationThresholdDays = 1;

    public AuthService(
        IApplicationDbContext context,
        IIdentityService identityService,
        IJwtTokenGenerator jwtTokenGenerator,
        IThirdPartyAuthService googleAuthService,
        IEmailService emailService)
    {
        _context = context;
        _identityService = identityService;
        _jwtTokenGenerator = jwtTokenGenerator;
        _googleAuthService = googleAuthService;
        _emailService = emailService;
    }

    public async Task<(AuthResponse Response, string RawRefreshToken)> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        if (await _identityService.EmailExistsAsync(request.Email, ct))
            throw new ConflictException("Email is already registered.");

        var userId = await _identityService.CreateUserAsync(
            request.FullName, request.Email, request.Password, request.PhoneNumber, ct);

        var roleName = request.AccountType.ToString();
        await _identityService.AddToRoleAsync(userId, roleName, ct);

        CreateBusinessRow(request.AccountType, userId);
        await _context.SaveChangesAsync(ct);

        return await BuildAuthResultAsync(userId, request.FullName, request.Email, ct);
    }

    public async Task<(AuthResponse Response, string RawRefreshToken)> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var userId = await _identityService.CheckPasswordAsync(request.Email, request.Password, ct);

        // Deliberately generic message — never reveal whether the email
        // exists or the password was wrong (no user enumeration).
        if (userId is null)
            throw new UnauthorizedAppException("Invalid email or password.");

        var (fullName, email) = await _identityService.GetUserBasicInfoAsync(userId.Value, ct);
        return await BuildAuthResultAsync(userId.Value, fullName, email, ct);
    }

    public async Task<(AuthResponse Response, string RawRefreshToken)> GoogleLoginAsync(GoogleLoginRequest request, CancellationToken ct = default)
    {
        var googleUser = await _googleAuthService.ValidateIdTokenAsync(request.IdToken, ct);

        var existingUserId = await _identityService.FindUserIdByEmailAsync(googleUser.Email, ct);

        Guid userId;
        if (existingUserId is not null)
        {
            // Existing email -> log into the ACTUAL role, ignoring request.AccountType (locked behavior).
            userId = existingUserId.Value;
        }
        else
        {
            // New email -> create using the submitted AccountType.
            userId = await _identityService.CreateUserWithoutPasswordAsync(googleUser.FullName, googleUser.Email, ct);
            await _identityService.AddToRoleAsync(userId, request.AccountType.ToString(), ct);
            CreateBusinessRow(request.AccountType, userId);
            await _context.SaveChangesAsync(ct);
        }

        var (fullName, email) = await _identityService.GetUserBasicInfoAsync(userId, ct);
        return await BuildAuthResultAsync(userId, fullName, email, ct);
    }

    public async Task<(AuthResponse Response, string RawRefreshToken)> RefreshTokenAsync(string rawRefreshToken, CancellationToken ct = default)
    {
        var tokenHash = HashToken(rawRefreshToken);

        var existing = _context.RefreshTokens.FirstOrDefault(t => t.TokenHash == tokenHash);

        if (existing is null || existing.IsRevoked || existing.ExpiresAt <= DateTime.UtcNow)
            throw new UnauthorizedAppException("Invalid or expired refresh token.");

        var (fullName, email) = await _identityService.GetUserBasicInfoAsync(existing.UserId, ct);
        var roles = await _identityService.GetRolesAsync(existing.UserId, ct);
        var (accessToken, expiresAt) = _jwtTokenGenerator.GenerateAccessToken(existing.UserId, email, roles);

        string rawTokenToReturn;

        // Rotate only when close to expiry — otherwise keep reusing the same
        // refresh token (avoids invalidating every other open tab/device on
        // every single API call that happens to trigger a refresh).
        if (existing.ExpiresAt - DateTime.UtcNow <= TimeSpan.FromDays(RefreshTokenRotationThresholdDays))
        {
            existing.IsRevoked = true;
            rawTokenToReturn = await IssueRefreshTokenAsync(existing.UserId, ct);
        }
        else
        {
            rawTokenToReturn = rawRefreshToken;
        }

        await _context.SaveChangesAsync(ct);

        var response = new AuthResponse
        {
            UserId = existing.UserId,
            FullName = fullName,
            Email = email,
            Role = roles.FirstOrDefault() ?? string.Empty,
            AccessToken = accessToken,
            AccessTokenExpiresAt = expiresAt
        };

        return (response, rawTokenToReturn);
    }

    public async Task ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken ct = default)
    {
        var userId = await _identityService.FindUserIdByEmailAsync(request.Email, ct);

        // Deliberately do NOT throw NotFoundException here — revealing
        // "this email doesn't exist" is a user-enumeration vector. Always
        // return success to the caller; only send the email if the account
        // is real.
        if (userId is null) return;

        var (fullName, email) = await _identityService.GetUserBasicInfoAsync(userId.Value, ct);
        var token = await _identityService.GeneratePasswordResetTokenAsync(userId.Value, ct);

        // The frontend URL/route is configuration, not hardcoded here —
        // wired via IEmailService's caller (Infrastructure) or app settings.
        // Kept as a placeholder pattern the Infrastructure implementation builds.
        var resetLink = $"RESET_LINK_BASE?email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(token)}";

        await _emailService.SendPasswordResetEmailAsync(email, fullName, resetLink, ct);
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct = default)
    {
        var userId = await _identityService.FindUserIdByEmailAsync(request.Email, ct)
            ?? throw new UnauthorizedAppException("Invalid reset request.");  // generic on purpose — no enumeration

        await _identityService.ResetPasswordAsync(userId, request.Token, request.NewPassword, ct);
    }

    // ---------- private helpers ----------

    private void CreateBusinessRow(AccountType accountType, Guid userId)
    {
        switch (accountType)
        {
            case AccountType.Customer:
                _context.Customers.Add(new Customer { Id = Guid.NewGuid(), UserId = userId });
                break;

            case AccountType.RestaurantOwner:
                _context.RestaurantOwners.Add(new RestaurantOwner { Id = Guid.NewGuid(), UserId = userId });
                break;

            case AccountType.Driver:
                // KNOWN GAP (flagged, not forgotten): NationalID/VehicleType/
                // VehicleImageUrl are required columns but not collected at
                // registration in V1. Placeholder values here until the
                // Driver Verification/KYC flow is designed in Module 7.
                _context.Drivers.Add(new Driver
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    NationalID = string.Empty,
                    VehicleType = VehicleType.Bike,
                    VehicleImageUrl = string.Empty
                });
                break;
        }
    }

    private async Task<(AuthResponse Response, string RawRefreshToken)> BuildAuthResultAsync(Guid userId, string fullName, string email, CancellationToken ct)
    {
        var roles = await _identityService.GetRolesAsync(userId, ct);
        var (accessToken, expiresAt) = _jwtTokenGenerator.GenerateAccessToken(userId, email, roles);
        var rawRefreshToken = await IssueRefreshTokenAsync(userId, ct);

        await _context.SaveChangesAsync(ct); // persists the new RefreshToken row

        var response = new AuthResponse
        {
            UserId = userId,
            FullName = fullName,
            Email = email,
            Role = roles.FirstOrDefault() ?? string.Empty,
            AccessToken = accessToken,
            AccessTokenExpiresAt = expiresAt
        };

        return (response, rawRefreshToken);
    }

    private async Task<string> IssueRefreshTokenAsync(Guid userId, CancellationToken ct)
    {
        var rawToken = GenerateSecureRandomToken();

        _context.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = HashToken(rawToken),
            ExpiresAt = DateTime.UtcNow.AddDays(RefreshTokenLifetimeDays),
            CreatedAt = DateTime.UtcNow,
            IsRevoked = false
        });

        return await Task.FromResult(rawToken);
    }

    private static string GenerateSecureRandomToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes);
    }

    private static string HashToken(string rawToken)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(rawToken);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash); // 64-char hex string — matches RefreshToken.TokenHash's HasMaxLength(64)
    }
}
