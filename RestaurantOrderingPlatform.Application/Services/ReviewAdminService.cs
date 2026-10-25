using Microsoft.EntityFrameworkCore;
using RestaurantOrderingPlatform.Application.Exceptions;
using RestaurantOrderingPlatform.Application.Interfaces;
using RestaurantOrderingPlatform.Application.DTOs.Reviews;
using RestaurantOrderingPlatform.Domain.Enums;
using RestaurantOrderingPlatform.Application.DTOs;

namespace RestaurantOrderingPlatform.Application.Services;

/// <summary>
/// إجراء الأدمن بيقفل كل البلاغات المفتوحة على المراجعة (Resolved) في نفس الـ transaction.
/// ملحوظة: الـ spec قال إن Delete "no FK dependents" بس ReviewReport بيشاور على Review،
/// فالحذف بيمسح البلاغات الأول ثم المراجعة (أو خلّي الـ FK Cascade).
/// </summary>
public class ReviewAdminService : IReviewAdminService
{
    private const int MaxPageSize = 50;

    private readonly IApplicationDbContext _db;
    private readonly IIdentityService _identity;

    public ReviewAdminService(IApplicationDbContext db, IIdentityService identity)
    {
        _db = db;
        _identity = identity;
    }

    public async Task<PagedResponse<ReportedReviewResponse>> GetReportedAsync(int page, int pageSize, CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        var query = _db.Reviews.AsNoTracking()
            .Where(r => _db.ReviewReports.Any(x => x.ReviewId == r.Id && x.Status == ReviewReportStatus.Open));

        var total = await query.CountAsync(ct);

        // الأقدم بلاغًا الأول (FIFO) عشان مفيش بلاغ يتنسي.
        var rows = await query
            .OrderBy(r => _db.ReviewReports
                .Where(x => x.ReviewId == r.Id && x.Status == ReviewReportStatus.Open)
                .Min(x => x.CreatedAt))
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(r => new
            {
                r.Id, r.RestaurantId,
                RestaurantName = _db.Restaurants.Where(x => x.Id == r.RestaurantId).Select(x => x.Name).First(),
                r.Rating, r.Comment, r.OwnerReply, r.ModerationStatus, r.CreatedAt,
                UserId = _db.Customers.Where(c => c.Id == r.CustomerId).Select(c => c.UserId).First(),
                Reports = _db.ReviewReports
                    .Where(x => x.ReviewId == r.Id && x.Status == ReviewReportStatus.Open)
                    .OrderBy(x => x.CreatedAt)
                    .Select(x => new ReviewReportResponse(x.Id, x.Reason, x.CreatedAt))
                    .ToList()
            })
            .ToListAsync(ct);

        var contacts = await _identity.GetContactsAsync(rows.Select(r => r.UserId).Distinct().ToList(), ct);

        var items = rows.Select(r => new ReportedReviewResponse(
            r.Id, r.RestaurantId, r.RestaurantName, r.Rating, r.Comment, r.OwnerReply,
            contacts.TryGetValue(r.UserId, out var c) ? c.FullName : "Unknown",
            r.ModerationStatus, r.CreatedAt, r.Reports)).ToList();

        return new PagedResponse<ReportedReviewResponse>(items, page, pageSize, total);
    }

    public async Task ModerateAsync(Guid reviewId, ModerateReviewRequest request, CancellationToken ct = default)
    {
        if (!await _db.Reviews.AsNoTracking().AnyAsync(r => r.Id == reviewId, ct))
            throw new NotFoundException("Review not found.");

        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        // ★ compare-and-set: بنقفل البلاغات المفتوحة الأول. لو أدمنين اتنين ضغطوا مع بعض، التاني affected = 0.
        var resolved = await _db.ReviewReports
            .Where(x => x.ReviewId == reviewId && x.Status == ReviewReportStatus.Open)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, ReviewReportStatus.Resolved), ct);

        if (resolved == 0)
            throw new ConflictException("This review has no open reports (already moderated?).");

        switch (request.Action)
        {
            case ReviewModerationAction.Hide:
                await _db.Reviews.Where(r => r.Id == reviewId)
                    .ExecuteUpdateAsync(s => s.SetProperty(r => r.ModerationStatus, ReviewModerationStatus.Hidden), ct);
                break;

            case ReviewModerationAction.Delete:
                await _db.ReviewReports.Where(x => x.ReviewId == reviewId).ExecuteDeleteAsync(ct);
                await _db.Reviews.Where(r => r.Id == reviewId).ExecuteDeleteAsync(ct);
                break;

            case ReviewModerationAction.Dismiss:
                break;   // البلاغات اتقفلت والمراجعة زي ما هي
        }

        await tx.CommitAsync(ct);
    }
}
