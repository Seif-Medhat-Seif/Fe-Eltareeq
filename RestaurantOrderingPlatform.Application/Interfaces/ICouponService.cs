using RestaurantOrderingPlatform.Application.DTOs;
using RestaurantOrderingPlatform.Application.DTOs.Coupons;

namespace RestaurantOrderingPlatform.Application.Interfaces;

public interface ICouponService
{
    Task<CouponResponse> CreateAsync(CreateCouponRequest request, CancellationToken ct = default);
    Task<PagedResponse<CouponResponse>> GetAllAsync(bool? isActive, int page, int pageSize, CancellationToken ct = default);
    Task<CouponResponse> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<CouponResponse> UpdateAsync(Guid id, UpdateCouponRequest request, CancellationToken ct = default);
    Task<CouponResponse> SetActiveAsync(Guid id, bool isActive, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
