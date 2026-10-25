using RestaurantOrderingPlatform.Application.DTOs.Reports;

namespace RestaurantOrderingPlatform.Application.Interfaces;

public interface IReportService
{
    Task<SalesSummaryResponse> GetSalesSummaryAsync(ReportDateRangeQuery query, CancellationToken ct = default);
    Task<IReadOnlyList<TopItemResponse>> GetTopItemsAsync(TopItemsQuery query, CancellationToken ct = default);
    Task<IReadOnlyList<OrderStatusCountResponse>> GetOrderStatusBreakdownAsync(ReportDateRangeQuery query, CancellationToken ct = default);
    Task<ReviewsSummaryResponse> GetReviewsSummaryAsync(ReviewsDateRangeQuery query, CancellationToken ct = default);
}
