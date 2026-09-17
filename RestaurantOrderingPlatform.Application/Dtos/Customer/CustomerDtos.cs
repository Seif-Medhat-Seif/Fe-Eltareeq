namespace RestaurantOrderingPlatform.Application.DTOs.Customer;

// ---------- Profile ----------

/// <summary>Email is included for display only — no endpoint accepts an Email field to change it.</summary>
public record CustomerProfileResponse(
    Guid CustomerId,
    string FullName,
    string Email,
    string? PhoneNumber);

public record UpdateCustomerProfileRequest(string FullName, string? PhoneNumber);

// ---------- Address ----------

public record CreateAddressRequest(
    string Label,
    string Street,
    string City,
    string? Building,
    string? Floor,
    string? Apartment,
    string? Landmark);

public record UpdateAddressRequest(
    string Label,
    string Street,
    string City,
    string? Building,
    string? Floor,
    string? Apartment,
    string? Landmark);

public record AddressResponse(
    Guid Id,
    string Label,
    string Street,
    string City,
    string? Building,
    string? Floor,
    string? Apartment,
    string? Landmark,
    bool IsDefault);
