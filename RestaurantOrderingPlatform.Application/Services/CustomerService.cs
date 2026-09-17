using Microsoft.EntityFrameworkCore;
using RestaurantOrderingPlatform.Application.DTOs.Customer;
using RestaurantOrderingPlatform.Application.Exceptions;
using RestaurantOrderingPlatform.Application.Interfaces;
using RestaurantOrderingPlatform.Domain.Entities;

namespace RestaurantOrderingPlatform.Application.Services;

public class CustomerService : ICustomerService
{
    private readonly IApplicationDbContext _context;
    private readonly IIdentityService _identityService;

    public CustomerService(IApplicationDbContext context, IIdentityService identityService)
    {
        _context = context;
        _identityService = identityService;
    }

    // ---------------- Profile ----------------

    public async Task<CustomerProfileResponse> GetMyProfileAsync(Guid userId)
    {
        var customer = await GetCustomerByUserIdAsync(userId);
        var (fullName, email, phone) = await _identityService.GetProfileAsync(userId);

        return new CustomerProfileResponse(customer.Id, fullName, email, phone);
    }

    public async Task<CustomerProfileResponse> UpdateMyProfileAsync(Guid userId, UpdateCustomerProfileRequest request)
    {
        var customer = await GetCustomerByUserIdAsync(userId);

        // Identity-layer write goes through IIdentityService — CustomerService never
        // touches UserManager/AppUser directly (Application must stay Identity-blind).
        await _identityService.UpdateProfileAsync(userId, request.FullName, request.PhoneNumber);

        var (fullName, email, phone) = await _identityService.GetProfileAsync(userId);
        return new CustomerProfileResponse(customer.Id, fullName, email, phone);
    }

    // ---------------- Addresses ----------------

    public async Task<IReadOnlyList<AddressResponse>> GetMyAddressesAsync(Guid userId)
    {
        var customer = await GetCustomerByUserIdAsync(userId);

        return await _context.Addresses
            .Where(a => a.CustomerId == customer.Id)
            .OrderByDescending(a => a.IsDefault)
            .ThenBy(a => a.CreatedAt)
            .Select(a => ToResponse(a))
            .ToListAsync();
    }

    public async Task<AddressResponse> AddAddressAsync(Guid userId, CreateAddressRequest request)
    {
        var customer = await GetCustomerByUserIdAsync(userId);

        // BR: the first address a customer ever adds is automatically the default —
        // checked by whether any address rows exist yet, not by any flag on Customer.
        var isFirstAddress = !await _context.Addresses.AnyAsync(a => a.CustomerId == customer.Id);

        var address = new Address
        {
            Id = Guid.NewGuid(),
            CustomerId = customer.Id,
            Label = request.Label,
            Street = request.Street,
            City = request.City,
            Building = request.Building,
            Floor = request.Floor,
            Apartment = request.Apartment,
            Landmark = request.Landmark,
            IsDefault = isFirstAddress,
            CreatedAt = DateTime.UtcNow
        };

        _context.Addresses.Add(address);
        await _context.SaveChangesAsync();

        return ToResponse(address);
    }

    public async Task<AddressResponse> UpdateAddressAsync(Guid userId, Guid addressId, UpdateAddressRequest request)
    {
        var address = await GetOwnedAddressAsync(userId, addressId);

        address.Label = request.Label;
        address.Street = request.Street;
        address.City = request.City;
        address.Building = request.Building;
        address.Floor = request.Floor;
        address.Apartment = request.Apartment;
        address.Landmark = request.Landmark;

        await _context.SaveChangesAsync();

        return ToResponse(address);
    }

    public async Task DeleteAddressAsync(Guid userId, Guid addressId)
    {
        var address = await GetOwnedAddressAsync(userId, addressId);
        var wasDefault = address.IsDefault;

        _context.Addresses.Remove(address);

        if (wasDefault)
        {
            // Auto-promote the oldest remaining address, if any. A customer with
            // saved addresses left is never left with zero default (per spec).
            // "Oldest remaining" is a deliberate, documented choice (see Module 4
            // handoff note) since Address had no chronological field before this
            // module — CreatedAt was added specifically to make this deterministic.
            var nextDefault = await _context.Addresses
                .Where(a => a.CustomerId == address.CustomerId && a.Id != address.Id)
                .OrderBy(a => a.CreatedAt)
                .FirstOrDefaultAsync();

            if (nextDefault is not null)
                nextDefault.IsDefault = true;
        }

        // Single SaveChangesAsync call -> one transaction covers both the delete
        // and the promotion write, satisfying NFR-6 for this multi-row change.
        await _context.SaveChangesAsync();
    }

    public async Task<AddressResponse> SetDefaultAddressAsync(Guid userId, Guid addressId)
    {
        var customer = await GetCustomerByUserIdAsync(userId);
        var target = await GetOwnedAddressAsync(userId, addressId);

        if (target.IsDefault)
            return ToResponse(target);

        var currentDefault = await _context.Addresses
            .FirstOrDefaultAsync(a => a.CustomerId == customer.Id && a.IsDefault);

        if (currentDefault is not null)
            currentDefault.IsDefault = false;

        target.IsDefault = true;

        // Clear-then-set inside one SaveChangesAsync call -> atomic; the DB-level
        // filtered unique index (AddressConfiguration) is the backstop if this
        // logic is ever bypassed by a future bug.
        await _context.SaveChangesAsync();

        return ToResponse(target);
    }

    // ---------------- Helpers ----------------

    private async Task<Customer> GetCustomerByUserIdAsync(Guid userId)
    {
        return await _context.Customers.FirstOrDefaultAsync(c => c.UserId == userId)
            ?? throw new NotFoundException("Customer profile not found for this account.");
    }

    private async Task<Address> GetOwnedAddressAsync(Guid userId, Guid addressId)
    {
        var customer = await GetCustomerByUserIdAsync(userId);

        return await _context.Addresses
            .FirstOrDefaultAsync(a => a.Id == addressId && a.CustomerId == customer.Id)
            ?? throw new NotFoundException("Address not found.");
    }

    private static AddressResponse ToResponse(Address a) => new(
        a.Id, a.Label, a.Street, a.City, a.Building, a.Floor, a.Apartment, a.Landmark, a.IsDefault);
}
