namespace RestaurantOrderingPlatform.Application.DTOs.Reports;

public record DailySalesPoint(DateOnly Date, int OrderCount, decimal TotalRevenue);

public interface IDateRangeQuery
{
    DateOnly? From { get; }
    DateOnly? To { get; }
}

public record OrderStatusCountResponse(string Status, int Count);

public record RatingCountResponse(int Rating, int Count);

/// <summary>?from=2026-10-01&to=2026-10-31 — الاتنين إجباريين، والنطاق شامل اليومين (UTC).</summary>
public record ReportDateRangeQuery(DateOnly? From, DateOnly? To) : IDateRangeQuery;

/// <summary>نفس الشكل بس الاتنين اختياريين (reviews-summary).</summary>
public record ReviewsDateRangeQuery(DateOnly? From, DateOnly? To) : IDateRangeQuery;

/// <summary>المراجعات Visible بس (Hidden مستبعدة زي الـ public). Distribution دايمًا 1..5 حتى لو صفر.</summary>
public record ReviewsSummaryResponse(
    int TotalReviews,
    decimal AverageRating,
    IReadOnlyList<RatingCountResponse> Distribution);

/// <summary>Delivered بس. التاريخ = لحظة وصول الأوردر لـ Delivered (من OrderStatusHistory).</summary>
public record SalesSummaryResponse(
    DateOnly From,
    DateOnly To,
    int OrderCount,
    decimal Subtotal,
    decimal DeliveryFees,
    decimal Discounts,
    decimal TotalRevenue,
    decimal AverageOrderValue,
    IReadOnlyList<DailySalesPoint> Daily);

public record TopItemResponse(Guid MenuItemId, string ItemName, int QuantitySold, int OrderCount, decimal Revenue);

public record TopItemsQuery(DateOnly? From, DateOnly? To, int Limit = 10) : IDateRangeQuery;