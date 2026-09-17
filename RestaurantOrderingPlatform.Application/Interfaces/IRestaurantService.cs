using RestaurantOrderingPlatform.Application.DTOs.Restaurant;

namespace RestaurantOrderingPlatform.Application.Interfaces;

public interface IRestaurantService
{
    Task<RestaurantResponse> CreateAsync(Guid ownerUserId, CreateRestaurantRequest request, CancellationToken ct = default);

    // Nullable — an owner can validly exist with zero restaurants (BR-10).
    // Controller returns { "restaurant": null } for this case, not a bare 404.
    Task<RestaurantResponse?> GetMineAsync(Guid ownerUserId, CancellationToken ct = default);

    Task<RestaurantResponse> GetByIdAsync(Guid restaurantId, CancellationToken ct = default);

    Task<RestaurantResponse> UpdateAsync(Guid ownerUserId, Guid restaurantId, UpdateRestaurantRequest request, CancellationToken ct = default);

    Task<RestaurantResponse> UpdateStatusAsync(Guid ownerUserId, Guid restaurantId, UpdateRestaurantStatusRequest request, CancellationToken ct = default);

    Task<List<WorkingHourDto>> GetWorkingHoursAsync(Guid restaurantId, CancellationToken ct = default);

    Task<List<WorkingHourDto>> UpdateWorkingHoursAsync(Guid ownerUserId, Guid restaurantId, UpdateWorkingHoursRequest request, CancellationToken ct = default);
}
