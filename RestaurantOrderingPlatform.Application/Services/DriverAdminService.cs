using Microsoft.EntityFrameworkCore;
using RestaurantOrderingPlatform.Application.Interfaces;
using RestaurantOrderingPlatform.Application.DTOs.Drivers;
using RestaurantOrderingPlatform.Application.DTOs.Orders;
using RestaurantOrderingPlatform.Application.Exceptions;
using RestaurantOrderingPlatform.Domain.Entities;
using RestaurantOrderingPlatform.Domain.Enums;
using RestaurantOrderingPlatform.Application.DTOs;

namespace RestaurantOrderingPlatform.Application.Services;

public class DriverAdminService : IDriverAdminService
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IIdentityService _identity;

    public DriverAdminService(
        IApplicationDbContext db,
        ICurrentUserService currentUser,
        IIdentityService identity)
    {
        _db = db;
        _currentUser = currentUser;
        _identity = identity;
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  GET /api/admin/drivers?status=Pending
    // ═══════════════════════════════════════════════════════════════════════
    public async Task<PagedResponse<AdminDriverSummaryResponse>> GetDriversAsync(
        DriverVerificationStatus? status, int page, int pageSize, CancellationToken ct = default)
    {
        (page, pageSize) = Normalize(page, pageSize);

        var query = _db.Drivers.AsNoTracking().AsQueryable();
        if (status.HasValue) query = query.Where(d => d.VerificationStatus == status.Value);

        var total = await query.CountAsync(ct);

        // طابور المراجعة: الأقدم تقديمًا الأول (first come, first reviewed).
        var rows = await query
            .OrderBy(d => d.VerificationSubmittedAt)
            .ThenBy(d => d.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(d => new
            {
                d.Id, d.UserId, d.VehicleType, d.VerificationStatus, d.VerificationSubmittedAt
            })
            .ToListAsync(ct);

        // الاسم والإيميل والتليفون في AppUser → batch واحد (مش N+1).
        var contacts = await _identity.GetContactsAsync(rows.Select(r => r.UserId).Distinct().ToList(), ct);

        var items = rows.Select(r =>
        {
            contacts.TryGetValue(r.UserId, out var c);
            return new AdminDriverSummaryResponse(
                r.Id, c?.FullName ?? "Driver", c?.Email ?? "", c?.PhoneNumber,
                r.VehicleType, r.VerificationStatus, r.VerificationSubmittedAt);
        }).ToList();

        return new PagedResponse<AdminDriverSummaryResponse>(items, page, pageSize, total);
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  GET /api/admin/drivers/{id}
    // ═══════════════════════════════════════════════════════════════════════
    public async Task<AdminDriverDetailResponse> GetDriverAsync(Guid driverId, CancellationToken ct = default)
    {
        var d = await _db.Drivers.AsNoTracking().FirstOrDefaultAsync(x => x.Id == driverId, ct)
            ?? throw new NotFoundException("Driver not found.");

        var contact = await _identity.GetContactAsync(d.UserId, ct);
        var submitted = d.VerificationStatus != DriverVerificationStatus.NotSubmitted;

        return new AdminDriverDetailResponse(
            d.Id,
            contact?.FullName ?? "Driver",
            contact?.Email ?? "",
            contact?.PhoneNumber,
            submitted ? d.NationalID : null,
            d.VehicleType,
            submitted ? d.VehiclePlateNumber : null,
            submitted ? d.VehicleImageUrl : null,
            d.VerificationStatus,
            d.VerificationSubmittedAt,
            d.VerificationReviewedAt,
            d.VerificationRejectionReason);
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  PATCH /api/admin/drivers/{id}/verification
    // ═══════════════════════════════════════════════════════════════════════
    public async Task<AdminDriverDetailResponse> ReviewVerificationAsync(
        Guid driverId, ReviewDriverVerificationRequest request, CancellationToken ct = default)
    {
        var approve = request.Decision == DriverVerificationDecision.Approve;
        var target = approve ? DriverVerificationStatus.Approved : DriverVerificationStatus.Rejected;

        // Approve: من Pending بس. Reject: من Pending أو من Approved (إيقاف سواق ثبت إنه مخالف).
        var allowedFrom = approve
            ? new[] { DriverVerificationStatus.Pending }
            : new[] { DriverVerificationStatus.Pending, DriverVerificationStatus.Approved };

        var now = DateTime.UtcNow;
        var reason = approve ? null : request.Reason?.Trim();

        // ★ UPDATE شرطي: أدمنين اتنين على نفس السواق → واحد بس بينجح، التاني 409. مفيش "آخر واحد يكسب" بصمت.
        var affected = await _db.Drivers
            .Where(d => d.Id == driverId && allowedFrom.Contains(d.VerificationStatus))
            .ExecuteUpdateAsync(s => s
                .SetProperty(d => d.VerificationStatus, target)
                .SetProperty(d => d.VerificationReviewedAt, now)
                .SetProperty(d => d.VerificationRejectionReason, reason), ct);

        if (affected == 0)
        {
            var exists = await _db.Drivers.AnyAsync(d => d.Id == driverId, ct);
            if (!exists) throw new NotFoundException("Driver not found.");
            throw new ConflictException("This driver is not in a state that allows this decision.");
        }

        return await GetDriverAsync(driverId, ct);
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  POST /api/admin/deliveries/{orderId}/release
    // ═══════════════════════════════════════════════════════════════════════
    /// <summary>
    /// بيرجّع أوردر من DriverAssigned إلى Ready ويحرر السواق (موبايل فصل، السواق اختفى...).
    /// عن قصد مش جوه OrderStateMachine: ده rollback إداري مش انتقال "لقدّام". لو اتضاف كحافة في الجدول
    /// المطعم نفسه (اللي بيستخدم نفس الجدول) كان هيقدر يسحب السواق بـ status=Ready.
    /// مسموح قبل PickedUp بس: بعدها الأكل مع السواق وده قرار بشري برّه النظام.
    /// </summary>
    public async Task ReleaseDeliveryAsync(Guid orderId, string reason, CancellationToken ct = default)
    {
        var info = await _db.Orders.AsNoTracking()
            .Where(o => o.Id == orderId)
            .Select(o => new { o.Status, o.DriverId })
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("Order not found.");

        if (info.Status != OrderStatus.DriverAssigned || info.DriverId is null)
            throw new ConflictException("Only a delivery that has not been picked up yet can be released.");

        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        // الشرط على السواق والحالة بالظبط: لو السواق ضغط PickedUp في نفس اللحظة، واحد بس بيكسب.
        var affected = await _db.Orders
            .Where(o => o.Id == orderId
                     && o.Status == OrderStatus.DriverAssigned
                     && o.DriverId == info.DriverId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(o => o.DriverId, (Guid?)null)
                .SetProperty(o => o.Status, OrderStatus.Ready), ct);

        if (affected == 0)
        {
            await tx.RollbackAsync(ct);
            throw new ConflictException("This delivery was just updated. Please refresh and try again.");
        }

        _db.OrderStatusHistories.Add(new OrderStatusHistory
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            Status = OrderStatus.Ready,
            ChangedAt = DateTime.UtcNow,
            ChangedByUserId = _currentUser.UserId,
            Note = $"Released by admin from driver {info.DriverId}: {reason.Trim()}"
        });

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    private static (int, int) Normalize(int page, int pageSize)
        => (page < 1 ? 1 : page, pageSize is < 1 or > 50 ? 20 : pageSize);
}
