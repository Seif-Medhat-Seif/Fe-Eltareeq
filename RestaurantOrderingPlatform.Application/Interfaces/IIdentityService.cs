namespace RestaurantOrderingPlatform.Application.Interfaces;

// Replaces the earlier IIdentityService.cs — this is the complete Module 1 version.
public interface IIdentityService
{
    Task<Guid> CreateUserAsync(string fullName, string email, string password, string? phoneNumber, CancellationToken ct = default);

    // For Google-created accounts: no password set at creation. The user
    // can add one later via a "set password" feature (not in V1 scope).
    Task<Guid> CreateUserWithoutPasswordAsync(string fullName, string email, CancellationToken ct = default);

    Task<bool> EmailExistsAsync(string email, CancellationToken ct = default);
    Task<Guid?> FindUserIdByEmailAsync(string email, CancellationToken ct = default);

    Task AddToRoleAsync(Guid userId, string role, CancellationToken ct = default);
    Task<IReadOnlyList<string>> GetRolesAsync(Guid userId, CancellationToken ct = default);

    Task<Guid?> CheckPasswordAsync(string email, string password, CancellationToken ct = default);

    Task<(string FullName, string Email)> GetUserBasicInfoAsync(Guid userId, CancellationToken ct = default);

    Task<string> GeneratePasswordResetTokenAsync(Guid userId, CancellationToken ct = default);
    Task ResetPasswordAsync(Guid userId, string token, string newPassword, CancellationToken ct = default);

    Task UpdateProfileAsync(Guid userId, string fullName, string? phoneNumber);

    Task<(string FullName, string Email, string? PhoneNumber)> GetProfileAsync(Guid userId);

    public record UserContact(Guid UserId, string FullName, string Email, string? PhoneNumber);

    Task<UserContact?> GetContactAsync(Guid userId, CancellationToken ct = default);

    Task<IReadOnlyDictionary<Guid, UserContact>> GetContactsAsync(
        IReadOnlyCollection<Guid> userIds, CancellationToken ct = default);
}
