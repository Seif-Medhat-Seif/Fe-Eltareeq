using RestaurantOrderingPlatform.Application.DTOs;
using RestaurantOrderingPlatform.Application.DTOs.Reviews;

namespace RestaurantOrderingPlatform.Application.Interfaces;

public interface IReviewAdminService
{
    Task<PagedResponse<ReportedReviewResponse>> GetReportedAsync(int page, int pageSize, CancellationToken ct = default);
    Task ModerateAsync(Guid reviewId, ModerateReviewRequest request, CancellationToken ct = default);
}
