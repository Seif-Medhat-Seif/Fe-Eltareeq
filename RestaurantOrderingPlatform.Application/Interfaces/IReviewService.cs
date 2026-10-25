using RestaurantOrderingPlatform.Application.DTOs;
using RestaurantOrderingPlatform.Application.DTOs.Reviews;

namespace RestaurantOrderingPlatform.Application.Interfaces;

public interface IReviewService
{
    Task<ReviewResponse> CreateAsync(Guid orderId, CreateReviewRequest request, CancellationToken ct = default);
    Task<PagedResponse<ReviewResponse>> GetByRestaurantAsync(Guid restaurantId, int page, int pageSize, CancellationToken ct = default);
    Task<ReviewResponse> GetByIdAsync(Guid reviewId, CancellationToken ct = default);
    Task<ReviewResponse> ReplyAsync(Guid reviewId, ReplyToReviewRequest request, CancellationToken ct = default);
    Task ReportAsync(Guid reviewId, ReportReviewRequest request, CancellationToken ct = default);
}
