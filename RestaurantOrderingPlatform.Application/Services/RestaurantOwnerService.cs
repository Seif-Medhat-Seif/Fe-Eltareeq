using RestaurantOrderingPlatform.Application.DTOs.Auth;
using RestaurantOrderingPlatform.Application.Exceptions;
using RestaurantOrderingPlatform.Application.Interfaces;
using RestaurantOrderingPlatform.Domain.Entities;

namespace RestaurantOrderingPlatform.Application.Services;

public class RestaurantOwnerService : IRestaurantOwnerService
{
    private readonly IApplicationDbContext _context;
    private readonly IIdentityService _identityService;

    public RestaurantOwnerService(IApplicationDbContext context, IIdentityService identityService)
    {
        _context = context;
        _identityService = identityService;
    }

    public async Task AssignEmployeeAsync(Guid ownerUserId, AssignEmployeeRequest request, CancellationToken ct = default)
    {
        var restaurant = GetOwnedRestaurantOrThrow(ownerUserId);

        var customerUserId = await _identityService.FindUserIdByEmailAsync(request.Email, ct);
        if (customerUserId is null)
            throw new NotFoundException("No customer account exists with that email.");

        var customer = _context.Customers.FirstOrDefault(c => c.UserId == customerUserId.Value)
            ?? throw new NotFoundException("No customer account exists with that email.");

        // Pre-check for a clean 409 — the filtered unique index (BR-2) is
        // still the real backstop at the DB level.
        var alreadyActiveElsewhere = _context.RestaurantEmployees
            .Any(e => e.CustomerId == customer.Id && e.IsActive);

        if (alreadyActiveElsewhere)
            throw new ConflictException("This customer already has active employee permissions at a restaurant.");

        _context.RestaurantEmployees.Add(new RestaurantEmployee
        {
            Id = Guid.NewGuid(),
            RestaurantId = restaurant.Id,
            CustomerId = customer.Id,
            IsActive = true,
            AssignedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync(ct);
    }

    public async Task RemoveEmployeeAsync(Guid ownerUserId, Guid employeeId, CancellationToken ct = default)
    {
        var restaurant = GetOwnedRestaurantOrThrow(ownerUserId);

        var employee = _context.RestaurantEmployees
            .FirstOrDefault(e => e.Id == employeeId && e.RestaurantId == restaurant.Id)
            ?? throw new NotFoundException("Employee link not found for this restaurant.");

        // Soft removal only — the Customer account and ordering capability
        // are untouched (locked Module 1 behavior).
        employee.IsActive = false;

        await _context.SaveChangesAsync(ct);
    }

    private Restaurant GetOwnedRestaurantOrThrow(Guid ownerUserId)
    {
        var owner = _context.RestaurantOwners.FirstOrDefault(o => o.UserId == ownerUserId)
            ?? throw new NotFoundException("Restaurant owner account not found.");

        var restaurant = _context.Restaurants.FirstOrDefault(r => r.RestaurantOwnerId == owner.Id)
            ?? throw new NotFoundException("You haven't created a restaurant yet.");

        return restaurant;
    }
}
