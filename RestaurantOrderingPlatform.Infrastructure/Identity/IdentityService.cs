using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RestaurantOrderingPlatform.Application.Exceptions;
using RestaurantOrderingPlatform.Application.Interfaces;
using static RestaurantOrderingPlatform.Application.Interfaces.IIdentityService;

namespace RestaurantOrderingPlatform.Infrastructure.Identity;

public class IdentityService : IIdentityService
{
    private readonly UserManager<AppUser> _userManager;

    public IdentityService(UserManager<AppUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<Guid> CreateUserAsync(string fullName, string email, string password, string? phoneNumber, CancellationToken ct = default)
    {
        var user = NewAppUser(fullName, email, phoneNumber);
        var result = await _userManager.CreateAsync(user, password);

        if (!result.Succeeded)
            throw new IdentityOperationException(result.Errors.Select(e => e.Description).ToList());

        return user.Id;
    }

    public async Task<Guid> CreateUserWithoutPasswordAsync(string fullName, string email, CancellationToken ct = default)
    {
        var user = NewAppUser(fullName, email, phoneNumber: null);
        var result = await _userManager.CreateAsync(user); // no password — Google-only login until a "set password" feature exists

        if (!result.Succeeded)
            throw new IdentityOperationException(result.Errors.Select(e => e.Description).ToList());

        return user.Id;
    }

    public async Task<bool> EmailExistsAsync(string email, CancellationToken ct = default)
        => await _userManager.FindByEmailAsync(email) is not null;

    public async Task<Guid?> FindUserIdByEmailAsync(string email, CancellationToken ct = default)
    {
        var user = await _userManager.FindByEmailAsync(email);
        return user?.Id;
    }

    public async Task AddToRoleAsync(Guid userId, string role, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString())
            ?? throw new InvalidOperationException($"User {userId} not found when assigning role.");

        var result = await _userManager.AddToRoleAsync(user, role);

        if (!result.Succeeded)
            throw new IdentityOperationException(result.Errors.Select(e => e.Description).ToList());
    }

    public async Task<IReadOnlyList<string>> GetRolesAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString())
            ?? throw new InvalidOperationException($"User {userId} not found when reading roles.");

        return (await _userManager.GetRolesAsync(user)).ToList();
    }

    public async Task<Guid?> CheckPasswordAsync(string email, string password, CancellationToken ct = default)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null) return null;

        var isValid = await _userManager.CheckPasswordAsync(user, password);
        return isValid ? user.Id : null;
    }

    public async Task<(string FullName, string Email)> GetUserBasicInfoAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString())
            ?? throw new InvalidOperationException($"User {userId} not found.");

        return (user.FullName, user.Email ?? string.Empty);
    }

    public async Task<string> GeneratePasswordResetTokenAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString())
            ?? throw new InvalidOperationException($"User {userId} not found.");

        return await _userManager.GeneratePasswordResetTokenAsync(user);
    }

    public async Task ResetPasswordAsync(Guid userId, string token, string newPassword, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString())
            ?? throw new InvalidOperationException($"User {userId} not found.");

        var result = await _userManager.ResetPasswordAsync(user, token, newPassword);

        if (!result.Succeeded)
            throw new IdentityOperationException(result.Errors.Select(e => e.Description).ToList());
    }

    public async Task UpdateProfileAsync(Guid userId, string fullName, string? phoneNumber)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString())
            ?? throw new NotFoundException("User not found.");

        user.FullName = fullName;
        user.PhoneNumber = phoneNumber;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
            throw new IdentityOperationException(result.Errors.Select(e => e.Description).ToList());
    }

    public async Task<(string FullName, string Email, string? PhoneNumber)> GetProfileAsync(Guid userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString())
            ?? throw new NotFoundException("User not found.");

        return (user.FullName, user.Email!, user.PhoneNumber);
    }

    public async Task<UserContact?> GetContactAsync(Guid userId, CancellationToken ct = default)
    {
        var u = await _userManager.Users
            .AsNoTracking()
            .Where(x => x.Id == userId)
            .Select(x => new UserContact(x.Id, x.FullName, x.Email!, x.PhoneNumber))
            .FirstOrDefaultAsync(ct);

        return u;
    }

    // ★ batch — عشان نتجنب N+1 في queue المطعم (20 أوردر = 20 كويري لو عملناها فردي)
    public async Task<IReadOnlyDictionary<Guid, UserContact>> GetContactsAsync(
        IReadOnlyCollection<Guid> userIds, CancellationToken ct = default)
    {
        if (userIds.Count == 0)
            return new Dictionary<Guid, UserContact>();

        var list = await _userManager.Users
            .AsNoTracking()
            .Where(x => userIds.Contains(x.Id))
            .Select(x => new UserContact(x.Id, x.FullName, x.Email!, x.PhoneNumber))
            .ToListAsync(ct);

        return list.ToDictionary(x => x.UserId);
    }

    private static AppUser NewAppUser(string fullName, string email, string? phoneNumber) => new()
    {
        Id = Guid.NewGuid(),
        FullName = fullName,
        Email = email,
        UserName = email,
        PhoneNumber = phoneNumber,
        CreatedAt = DateTime.UtcNow
    };
}
