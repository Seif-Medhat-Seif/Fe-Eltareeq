using RestaurantOrderingPlatform.Application.DTOs.Auth;

namespace RestaurantOrderingPlatform.Application.Interfaces;

public interface IRestaurantOwnerService
{
    // ownerUserId comes from the JWT (ClaimTypes.NameIdentifier) — never
    // from the request body, so an owner can only ever act on their own restaurant.
    Task AssignEmployeeAsync(Guid ownerUserId, AssignEmployeeRequest request, CancellationToken ct = default);
    Task RemoveEmployeeAsync(Guid ownerUserId, Guid employeeId, CancellationToken ct = default);
}
