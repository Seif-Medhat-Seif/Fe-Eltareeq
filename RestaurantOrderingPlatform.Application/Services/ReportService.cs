using Microsoft.EntityFrameworkCore;
using RestaurantOrderingPlatform.Application.Exceptions;
using RestaurantOrderingPlatform.Application.Interfaces;
using RestaurantOrderingPlatform.Application.DTOs.Reports;
using RestaurantOrderingPlatform.Domain.Enums;

namespace RestaurantOrderingPlatform.Application.Services;

/// <summary>
/// Read-only aggregation. كل الحسابات بتتعمل في SQL (GROUP BY / SUM) مش في الذاكرة.
/// "الإيراد" = أوردرات Delivered بس، والتاريخ = لحظة Delivered من OrderStatusHistory
/// (مش CreatedAt: أوردر اتعمل 31/10 ووصل 1/11 بيتحسب في نوفمبر).
/// حدود اليوم بـ UTC: [from 00:00, to+1 00:00).
/// </summary>
public class ReportService : IReportService
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public ReportService(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<SalesSummaryResponse> GetSalesSummaryAsync(ReportDateRangeQuery query, CancellationToken ct = default)
    {
        var restaurantId = await ResolveRestaurantIdAsync(ct);
        var from = query.From!.Value;
        var to = query.To!.Value;
        var (fromUtc, toUtc) = Bounds(from, to);

        var daily = await (
                from h in _db.OrderStatusHistories.AsNoTracking()
                join o in _db.Orders.AsNoTracking() on h.OrderId equals o.Id
                where o.RestaurantId == restaurantId
                      && h.Status == OrderStatus.Delivered
                      && h.ChangedAt >= fromUtc && h.ChangedAt < toUtc
                group o by h.ChangedAt.Date into g
                select new
                {
                    Day = g.Key,
                    Orders = g.Count(),
                    Subtotal = g.Sum(x => x.Subtotal),
                    Fees = g.Sum(x => x.DeliveryFee),
                    Discounts = g.Sum(x => x.DiscountAmount),
                    Total = g.Sum(x => x.Total)
                })
            .ToListAsync(ct);

        var byDay = daily.ToDictionary(d => DateOnly.FromDateTime(d.Day));

        // نملّا الأيام الفاضية بصفر عشان الـ chart في الفرونت يفضل متصل.
        var points = new List<DailySalesPoint>();
        for (var day = from; day <= to; day = day.AddDays(1))
            points.Add(byDay.TryGetValue(day, out var d)
                ? new DailySalesPoint(day, d.Orders, d.Total)
                : new DailySalesPoint(day, 0, 0m));

        var count = daily.Sum(d => d.Orders);
        var total = daily.Sum(d => d.Total);

        return new SalesSummaryResponse(
            from, to, count,
            daily.Sum(d => d.Subtotal), daily.Sum(d => d.Fees), daily.Sum(d => d.Discounts),
            total,
            count == 0 ? 0m : decimal.Round(total / count, 2, MidpointRounding.AwayFromZero),
            points);
    }

    public async Task<IReadOnlyList<TopItemResponse>> GetTopItemsAsync(TopItemsQuery query, CancellationToken ct = default)
    {
        var restaurantId = await ResolveRestaurantIdAsync(ct);
        var (fromUtc, toUtc) = Bounds(query.From!.Value, query.To!.Value);

        // الأصناف بتتجمّع بـ MenuItemId، والاسم من الـ Snapshot (مش من MenuItem الحالي).
        // لو الاسم اتغيّر في النص بنعرض أحدث اسم alphabetically (Max) كاختيار ثابت.
        return await (
                from i in _db.OrderItems.AsNoTracking()
                join o in _db.Orders.AsNoTracking() on i.OrderId equals o.Id
                join h in _db.OrderStatusHistories.AsNoTracking() on o.Id equals h.OrderId
                where o.RestaurantId == restaurantId
                      && h.Status == OrderStatus.Delivered
                      && h.ChangedAt >= fromUtc && h.ChangedAt < toUtc
                group i by i.MenuItemId into g
                orderby g.Sum(x => x.Quantity) descending, g.Sum(x => x.LineTotal) descending
                select new TopItemResponse(
                    g.Key,
                    g.Max(x => x.ItemNameSnapshot),
                    g.Sum(x => x.Quantity),
                    g.Select(x => x.OrderId).Distinct().Count(),
                    g.Sum(x => x.LineTotal)))
            .Take(query.Limit)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<OrderStatusCountResponse>> GetOrderStatusBreakdownAsync(
        ReportDateRangeQuery query, CancellationToken ct = default)
    {
        var restaurantId = await ResolveRestaurantIdAsync(ct);
        var (fromUtc, toUtc) = Bounds(query.From!.Value, query.To!.Value);

        // هنا الأوردرات بتتحسب بتاريخ الإنشاء (كلهم، مش Delivered بس) وبحالتها الحالية.
        var counts = await _db.Orders.AsNoTracking()
            .Where(o => o.RestaurantId == restaurantId && o.CreatedAt >= fromUtc && o.CreatedAt < toUtc)
            .GroupBy(o => o.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var lookup = counts.ToDictionary(c => c.Status, c => c.Count);

        // كل قيم الـ enum بتظهر (حتى صفر) وبترتيب الـ workflow، فالـ response شكله ثابت.
        return Enum.GetValues<OrderStatus>()
            .Select(s => new OrderStatusCountResponse(s.ToString(), lookup.GetValueOrDefault(s)))
            .ToList();
    }

    public async Task<ReviewsSummaryResponse> GetReviewsSummaryAsync(ReviewsDateRangeQuery query, CancellationToken ct = default)
    {
        var restaurantId = await ResolveRestaurantIdAsync(ct);

        var reviews = _db.Reviews.AsNoTracking()
            .Where(r => r.RestaurantId == restaurantId && r.ModerationStatus == ReviewModerationStatus.Visible);

        if (query.From.HasValue && query.To.HasValue)
        {
            var (fromUtc, toUtc) = Bounds(query.From.Value, query.To.Value);
            reviews = reviews.Where(r => r.CreatedAt >= fromUtc && r.CreatedAt < toUtc);
        }

        var groups = await reviews
            .GroupBy(r => r.Rating)
            .Select(g => new { Rating = (int)g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var lookup = groups.ToDictionary(g => g.Rating, g => g.Count);
        var distribution = Enumerable.Range(1, 5)
            .Select(star => new RatingCountResponse(star, lookup.GetValueOrDefault(star)))
            .ToList();

        var total = distribution.Sum(d => d.Count);
        var avg = total == 0
            ? 0m
            : decimal.Round(distribution.Sum(d => (decimal)d.Rating * d.Count) / total, 2, MidpointRounding.AwayFromZero);

        return new ReviewsSummaryResponse(total, avg, distribution);
    }

    // ───────────── helpers ─────────────

    private static (DateTime FromUtc, DateTime ToUtc) Bounds(DateOnly from, DateOnly to) => (
        DateTime.SpecifyKind(from.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc),
        DateTime.SpecifyKind(to.AddDays(1).ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc));

    /// <summary>Owner بس (الـ Controller بيقفل الـ role). الموظفين مستبعدين صراحةً حسب الـ brief.</summary>
    private async Task<Guid> ResolveRestaurantIdAsync(CancellationToken ct)
    {
        var userId = _currentUser.UserId ?? throw new UnauthorizedAppException("Not authenticated.");

        return await _db.Restaurants.AsNoTracking()
            .Where(r => r.RestaurantOwner.UserId == userId)
            .Select(r => (Guid?)r.Id)
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("Restaurant not found for the current owner.");
    }
}
