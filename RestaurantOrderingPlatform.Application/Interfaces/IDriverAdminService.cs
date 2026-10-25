using RestaurantOrderingPlatform.Application.DTOs;
using RestaurantOrderingPlatform.Application.DTOs.Drivers;
using RestaurantOrderingPlatform.Application.DTOs.Orders;
using RestaurantOrderingPlatform.Domain.Enums;

namespace RestaurantOrderingPlatform.Application.Interfaces;

public interface IDriverAdminService
{
    Task<PagedResponse<AdminDriverSummaryResponse>> GetDriversAsync(
        DriverVerificationStatus? status, int page, int pageSize, CancellationToken ct = default);

    Task<AdminDriverDetailResponse> GetDriverAsync(Guid driverId, CancellationToken ct = default);

    Task<AdminDriverDetailResponse> ReviewVerificationAsync(
        Guid driverId, ReviewDriverVerificationRequest request, CancellationToken ct = default);

    Task ReleaseDeliveryAsync(Guid orderId, string reason, CancellationToken ct = default);
}
