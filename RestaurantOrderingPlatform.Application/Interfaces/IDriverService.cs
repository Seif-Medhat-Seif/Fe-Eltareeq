using RestaurantOrderingPlatform.Application.DTOs;
using RestaurantOrderingPlatform.Application.DTOs.Drivers;
using RestaurantOrderingPlatform.Application.DTOs.Orders;
using RestaurantOrderingPlatform.Domain.Enums;

namespace RestaurantOrderingPlatform.Application.Interfaces;

public interface IDriverService
{
    Task<DriverProfileResponse> GetProfileAsync(CancellationToken ct = default);
    Task<DriverProfileResponse> SubmitProfileAsync(UpdateDriverProfileRequest request, CancellationToken ct = default);

    Task<PagedResponse<AvailableDeliveryResponse>> GetAvailableAsync(int page, int pageSize, CancellationToken ct = default);
    Task<DeliveryResponse> AcceptAsync(Guid orderId, CancellationToken ct = default);
    Task<CurrentDeliveryResponse> GetCurrentAsync(CancellationToken ct = default);
    Task<DeliveryResponse> UpdateStatusAsync(Guid orderId, OrderStatus newStatus, CancellationToken ct = default);
    Task<PagedResponse<DeliverySummaryResponse>> GetHistoryAsync(int page, int pageSize, CancellationToken ct = default);
}
