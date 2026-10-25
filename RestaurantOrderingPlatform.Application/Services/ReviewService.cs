using Microsoft.EntityFrameworkCore;
using RestaurantOrderingPlatform.Application.Exceptions;
using RestaurantOrderingPlatform.Application.Interfaces;
using RestaurantOrderingPlatform.Application.DTOs.Orders;
using RestaurantOrderingPlatform.Application.DTOs.Reviews;
using RestaurantOrderingPlatform.Domain.Entities;
using RestaurantOrderingPlatform.Domain.Enums;
using RestaurantOrderingPlatform.Application.DTOs;

namespace RestaurantOrderingPlatform.Application.Services;

/// <summary>
/// الضمانات الحقيقية في الداتابيز:
///  - UNIQUE (OrderId) على Review → مراجعة واحدة لكل أوردر حتى مع ضغطتين متزامنتين.
///  - filtered UNIQUE (ReviewId, RestaurantOwnerId) WHERE Status='Open' → بلاغ مفتوح واحد لكل owner.
///  - الرد: UPDATE ... WHERE OwnerReply IS NULL → رد واحد بس من غير read-modify-write.
/// </summary>
public class ReviewService : IReviewService
{
    private const int MaxPageSize = 50;

    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IIdentityService _identity;

    public ReviewService(IApplicationDbContext db, ICurrentUserService currentUser, IIdentityService identity)
    {
        _db = db;
        _currentUser = currentUser;
        _identity = identity;
    }

    // ───────────── Customer ─────────────

    public async Task<ReviewResponse> CreateAsync(Guid orderId, CreateReviewRequest request, CancellationToken ct = default)
    {
        var userId = RequireUserId();
        var customerId = await _db.Customers.AsNoTracking()
            .Where(c => c.UserId == userId).Select(c => (Guid?)c.Id).FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("Customer profile not found.");

        // أوردر حد تاني = 404 (مش 403) عشان منكشفش إن الأوردر موجود.
        var order = await _db.Orders.AsNoTracking()
            .Where(o => o.Id == orderId && o.CustomerId == customerId)
            .Select(o => new { o.RestaurantId, o.Status })
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("Order not found.");

        // BR-8: بعد Delivered بس.
        if (order.Status != OrderStatus.Delivered)
            throw new ConflictException("You can review an order only after it has been delivered.");

        if (await _db.Reviews.AnyAsync(r => r.OrderId == orderId, ct))
            throw new ConflictException("This order has already been reviewed.");

        var review = new Review
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            RestaurantId = order.RestaurantId,
            CustomerId = customerId,
            Rating = request.Rating,
            Comment = string.IsNullOrWhiteSpace(request.Comment) ? null : request.Comment.Trim(),
            ModerationStatus = ReviewModerationStatus.Visible,
            CreatedAt = DateTime.UtcNow
        };
        _db.Reviews.Add(review);

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            if (await _db.Reviews.AsNoTracking().AnyAsync(r => r.OrderId == orderId, ct))
                throw new ConflictException("This order has already been reviewed.");
            throw;
        }

        var name = await FirstNameAsync(userId, ct);
        return new ReviewResponse(review.Id, review.RestaurantId, review.Rating, review.Comment,
            review.OwnerReply, name, review.CreatedAt);
    }

    // ───────────── Public ─────────────

    public async Task<PagedResponse<ReviewResponse>> GetByRestaurantAsync(
        Guid restaurantId, int page, int pageSize, CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        if (!await _db.Restaurants.AsNoTracking().AnyAsync(r => r.Id == restaurantId, ct))
            throw new NotFoundException("Restaurant not found.");

        // المبلّغ عنها لسه ظاهرة. Hidden بس هي اللي بتتشال.
        var query = _db.Reviews.AsNoTracking()
            .Where(r => r.RestaurantId == restaurantId && r.ModerationStatus == ReviewModerationStatus.Visible);

        var total = await query.CountAsync(ct);

        var rows = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(r => new Row(r.Id, r.RestaurantId, r.Rating, r.Comment, r.OwnerReply, r.CreatedAt,
                _db.Customers.Where(c => c.Id == r.CustomerId).Select(c => c.UserId).First()))
            .ToListAsync(ct);

        var names = await FirstNamesAsync(rows.Select(r => r.UserId).Distinct().ToList(), ct);
        var items = rows.Select(r => ToResponse(r, names)).ToList();
        return new PagedResponse<ReviewResponse>(items, page, pageSize, total);
    }

    public async Task<ReviewResponse> GetByIdAsync(Guid reviewId, CancellationToken ct = default)
    {
        var row = await _db.Reviews.AsNoTracking()
            .Where(r => r.Id == reviewId && r.ModerationStatus == ReviewModerationStatus.Visible)
            .Select(r => new Row(r.Id, r.RestaurantId, r.Rating, r.Comment, r.OwnerReply, r.CreatedAt,
                _db.Customers.Where(c => c.Id == r.CustomerId).Select(c => c.UserId).First()))
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("Review not found.");

        var names = await FirstNamesAsync(new[] { row.UserId }, ct);
        return ToResponse(row, names);
    }

    // ───────────── Owner ─────────────

    public async Task<ReviewResponse> ReplyAsync(Guid reviewId, ReplyToReviewRequest request, CancellationToken ct = default)
    {
        var review = await GetOwnedReviewAsync(reviewId, ct);

        if (review.ModerationStatus == ReviewModerationStatus.Hidden)
            throw new ConflictException("This review has been hidden by moderation and cannot be replied to.");

        // رد واحد بس: شرط OwnerReply IS NULL جوه الـ UPDATE نفسه. التاني بيطلع affected = 0.
        var reply = request.Reply.Trim();
        var affected = await _db.Reviews
            .Where(r => r.Id == reviewId && r.OwnerReply == null)
            .ExecuteUpdateAsync(s => s.SetProperty(r => r.OwnerReply, reply), ct);

        if (affected == 0)
            throw new ConflictException("This review already has a reply.");

        return await GetByIdAsync(reviewId, ct);
    }

    public async Task ReportAsync(Guid reviewId, ReportReviewRequest request, CancellationToken ct = default)
    {
        var review = await GetOwnedReviewAsync(reviewId, ct);
        var ownerId = await GetOwnerIdAsync(ct);

        if (await _db.ReviewReports.AnyAsync(
                x => x.ReviewId == review.Id && x.RestaurantOwnerId == ownerId && x.Status == ReviewReportStatus.Open, ct))
            throw new ConflictException("You already have an open report on this review.");

        _db.ReviewReports.Add(new ReviewReport
        {
            Id = Guid.NewGuid(),
            ReviewId = review.Id,
            RestaurantOwnerId = ownerId,
            Reason = request.Reason.Trim(),
            Status = ReviewReportStatus.Open,
            CreatedAt = DateTime.UtcNow
        });

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            if (await _db.ReviewReports.AsNoTracking().AnyAsync(
                    x => x.ReviewId == review.Id && x.RestaurantOwnerId == ownerId && x.Status == ReviewReportStatus.Open, ct))
                throw new ConflictException("You already have an open report on this review.");
            throw;
        }
    }

    // ───────────── helpers ─────────────

    private Guid RequireUserId() => _currentUser.UserId ?? throw new UnauthorizedAppException("Not authenticated.");

    private async Task<Guid> GetOwnerIdAsync(CancellationToken ct)
    {
        var userId = RequireUserId();
        return await _db.RestaurantOwners.AsNoTracking()
            .Where(o => o.UserId == userId).Select(o => (Guid?)o.Id).FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("Owner profile not found.");
    }

    /// <summary>بيرجع المراجعة لو تابعة لمطعم الـ owner الحالي، وإلا 404 (مش بنكشف وجود مراجعات مطاعم تانية).</summary>
    private async Task<ReviewHead> GetOwnedReviewAsync(Guid reviewId, CancellationToken ct)
    {
        var userId = RequireUserId();
        return await _db.Reviews.AsNoTracking()
            .Where(r => r.Id == reviewId && _db.Restaurants.Any(x => x.Id == r.RestaurantId && x.RestaurantOwner.UserId == userId))
            .Select(r => new ReviewHead(r.Id, r.ModerationStatus))
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("Review not found.");
    }

    private async Task<string> FirstNameAsync(Guid userId, CancellationToken ct)
    {
        var names = await FirstNamesAsync(new[] { userId }, ct);
        return names.GetValueOrDefault(userId, "Customer");
    }

    private async Task<Dictionary<Guid, string>> FirstNamesAsync(IReadOnlyCollection<Guid> userIds, CancellationToken ct)
    {
        var contacts = await _identity.GetContactsAsync(userIds, ct);
        return contacts.ToDictionary(
            kv => kv.Key,
            kv => kv.Value.FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "Customer");
    }

    private static ReviewResponse ToResponse(Row r, Dictionary<Guid, string> names) => new(
        r.Id, r.RestaurantId, r.Rating, r.Comment, r.OwnerReply,
        names.GetValueOrDefault(r.UserId, "Customer"), r.CreatedAt);

    private sealed record Row(Guid Id, Guid RestaurantId, int Rating, string? Comment, string? OwnerReply, DateTime CreatedAt, Guid UserId);
    private sealed record ReviewHead(Guid Id, ReviewModerationStatus ModerationStatus);
}
