using RestaurantOrderingPlatform.Application.DTOs.Customer;

namespace RestaurantOrderingPlatform.Application.Interfaces;

public interface ICustomerService
{
    Task<CustomerProfileResponse> GetMyProfileAsync(Guid userId);
    Task<CustomerProfileResponse> UpdateMyProfileAsync(Guid userId, UpdateCustomerProfileRequest request);

    Task<IReadOnlyList<AddressResponse>> GetMyAddressesAsync(Guid userId);
    Task<AddressResponse> AddAddressAsync(Guid userId, CreateAddressRequest request);
    Task<AddressResponse> UpdateAddressAsync(Guid userId, Guid addressId, UpdateAddressRequest request);
    Task DeleteAddressAsync(Guid userId, Guid addressId);
    Task<AddressResponse> SetDefaultAddressAsync(Guid userId, Guid addressId);
}
