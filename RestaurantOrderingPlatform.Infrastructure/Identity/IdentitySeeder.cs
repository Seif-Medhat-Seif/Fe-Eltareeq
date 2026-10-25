using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace RestaurantOrderingPlatform.Infrastructure.Identity;

/// <summary>
/// بيشتغل عند كل إقلاع وidempotent: يعمل الـ 4 roles لو مش موجودين، وحساب أدمن واحد من الإعدادات.
/// مفيش endpoint لتسجيل أدمن (Admin accounts are managed separately، زي الـ Vision).
/// </summary>
public class IdentitySeeder
{
    private static readonly string[] Roles = { "Admin", "Customer", "RestaurantOwner", "Driver" };

    private readonly RoleManager<IdentityRole<Guid>> _roles;
    private readonly UserManager<AppUser> _users;
    private readonly AdminSeedSettings _settings;
    private readonly ILogger<IdentitySeeder> _logger;

    public IdentitySeeder(
        RoleManager<IdentityRole<Guid>> roles,
        UserManager<AppUser> users,
        IOptions<AdminSeedSettings> settings,
        ILogger<IdentitySeeder> logger)
    {
        _roles = roles;
        _users = users;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        foreach (var role in Roles)
        {
            if (await _roles.RoleExistsAsync(role)) continue;

            var result = await _roles.CreateAsync(new IdentityRole<Guid> { Id = Guid.NewGuid(), Name = role });
            if (!result.Succeeded)
                throw new InvalidOperationException(
                    $"Could not create role '{role}': {string.Join("; ", result.Errors.Select(e => e.Description))}");
        }

        if (string.IsNullOrWhiteSpace(_settings.Email) || string.IsNullOrWhiteSpace(_settings.Password))
        {
            _logger.LogWarning("AdminSeed:Email / AdminSeed:Password not configured. No admin account was created.");
            return;
        }

        var existing = await _users.FindByEmailAsync(_settings.Email);
        if (existing is not null)
        {
            // عن قصد منرقّيش حساب موجود لأدمن: لو حد سجّل بالإيميل ده قبل أول إقلاع، الترقية التلقائية = ثغرة صلاحيات.
            if (!await _users.IsInRoleAsync(existing, "Admin"))
                _logger.LogError(
                    "A user with the configured admin email already exists but is NOT an Admin. " +
                    "Seeding skipped on purpose. Use a different email or fix the account manually.");
            return;
        }

        var admin = new AppUser
        {
            Id = Guid.NewGuid(),
            UserName = _settings.Email,
            Email = _settings.Email,
            EmailConfirmed = true,
            FullName = _settings.FullName,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _users.CreateAsync(admin, _settings.Password);
        if (!created.Succeeded)
            throw new InvalidOperationException(
                $"Could not create admin: {string.Join("; ", created.Errors.Select(e => e.Description))}");

        await _users.AddToRoleAsync(admin, "Admin");
        _logger.LogInformation("Admin account created for {Email}.", _settings.Email);
    }
}
