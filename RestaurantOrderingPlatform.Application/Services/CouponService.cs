using Microsoft.EntityFrameworkCore;
using RestaurantOrderingPlatform.Application.Exceptions;
using RestaurantOrderingPlatform.Application.Interfaces;
using RestaurantOrderingPlatform.Application.DTOs.Coupons;
using RestaurantOrderingPlatform.Domain.Entities;
using RestaurantOrderingPlatform.Application.DTOs;

namespace RestaurantOrderingPlatform.Application.Services;

/// <summary>
/// إدارة الكوبونات (Owner بس). الضمانات الحقيقية في الداتابيز:
///  - UNIQUE (RestaurantId, Code) → منع تكرار الكود حتى مع طلبين متزامنين.
///  - FK Restrict على CouponUsage/Order → منع حذف كوبون اتستخدم حتى لو الاستخدام اتسجّل بعد الـ check.
/// الـ pre-checks هنا لرسائل خطأ واضحة بس، مش هي الحَكَم.
/// </summary>
public class CouponService : ICouponService
{
    private const int MaxPageSize = 50;

    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public CouponService(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<CouponResponse> CreateAsync(CreateCouponRequest request, CancellationToken ct = default)
    {
        var restaurantId = await ResolveRestaurantIdAsync(ct);
        var code = NormalizeCode(request.Code);

        if (await _db.Coupons.AnyAsync(c => c.RestaurantId == restaurantId && c.Code == code, ct))
            throw new ConflictException($"Coupon code '{code}' already exists.");

        var coupon = new Coupon
        {
            Id = Guid.NewGuid(),
            RestaurantId = restaurantId,
            Code = code,
            DiscountType = request.DiscountType,
            DiscountValue = request.DiscountValue,
            MaxDiscountAmount = request.MaxDiscountAmount,
            MinimumOrderAmount = request.MinimumOrderAmount,
            ExpiryDate = request.ExpiryDate is { } e ? UtcDateTime.Normalize(e) : null,
            UsageLimitTotal = request.UsageLimitTotal,
            UsageLimitPerCustomer = request.UsageLimitPerCustomer,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _db.Coupons.Add(coupon);

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // طلبين متزامنين بنفس الكود: الـ UNIQUE index رفض التاني.
            if (await _db.Coupons.AsNoTracking().AnyAsync(c => c.RestaurantId == restaurantId && c.Code == code, ct))
                throw new ConflictException($"Coupon code '{code}' already exists.");
            throw;
        }

        return ToResponse(coupon, usageCount: 0, DateTime.UtcNow);
    }

    public async Task<PagedResponse<CouponResponse>> GetAllAsync(bool? isActive, int page, int pageSize, CancellationToken ct = default)
    {
        var restaurantId = await ResolveRestaurantIdAsync(ct);
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        var query = _db.Coupons.AsNoTracking().Where(c => c.RestaurantId == restaurantId);
        if (isActive.HasValue) query = query.Where(c => c.IsActive == isActive.Value);

        var total = await query.CountAsync(ct);

        var rows = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(c => new CouponRow(c, _db.CouponUsages.Count(u => u.CouponId == c.Id)))
            .ToListAsync(ct);

        var now = DateTime.UtcNow;
        return new PagedResponse<CouponResponse>(
            rows.Select(r => ToResponse(r.Coupon, r.UsageCount, now)).ToList(), page, pageSize, total);
    }

    public async Task<CouponResponse> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var restaurantId = await ResolveRestaurantIdAsync(ct);
        var row = await _db.Coupons.AsNoTracking()
            .Where(c => c.Id == id && c.RestaurantId == restaurantId)
            .Select(c => new CouponRow(c, _db.CouponUsages.Count(u => u.CouponId == c.Id)))
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("Coupon not found.");

        return ToResponse(row.Coupon, row.UsageCount, DateTime.UtcNow);
    }

    public async Task<CouponResponse> UpdateAsync(Guid id, UpdateCouponRequest request, CancellationToken ct = default)
    {
        var restaurantId = await ResolveRestaurantIdAsync(ct);
        var coupon = await _db.Coupons.FirstOrDefaultAsync(c => c.Id == id && c.RestaurantId == restaurantId, ct)
            ?? throw new NotFoundException("Coupon not found.");

        // الأوردرات القديمة محمية أصلًا (Order.DiscountAmount snapshot)، فتعديل الكوبون مش بيأثر عليها.
        // بس تغيير النوع (نسبة ↔ مبلغ ثابت) بعد ما اتستخدم بيقلب معنى الكوبون عند اللي معاه الكود → ممنوع.
        var usageCount = await _db.CouponUsages.CountAsync(u => u.CouponId == id, ct);
        var everOrdered = usageCount > 0 || await _db.Orders.AnyAsync(o => o.CouponId == id, ct);

        if (everOrdered && request.DiscountType != coupon.DiscountType)
            throw new ConflictException("DiscountType cannot be changed after the coupon has been used. Create a new coupon instead.");

        if (request.UsageLimitTotal is { } limit && limit < usageCount)
            throw new ConflictException($"UsageLimitTotal cannot be lower than the current usage count ({usageCount}).");

        coupon.DiscountType = request.DiscountType;
        coupon.DiscountValue = request.DiscountValue;
        coupon.MaxDiscountAmount = request.MaxDiscountAmount;
        coupon.MinimumOrderAmount = request.MinimumOrderAmount;
        coupon.ExpiryDate = request.ExpiryDate is { } e ? UtcDateTime.Normalize(e) : null;
        coupon.UsageLimitTotal = request.UsageLimitTotal;
        coupon.UsageLimitPerCustomer = request.UsageLimitPerCustomer;

        await _db.SaveChangesAsync(ct);
        return ToResponse(coupon, usageCount, DateTime.UtcNow);
    }

    public async Task<CouponResponse> SetActiveAsync(Guid id, bool isActive, CancellationToken ct = default)
    {
        var restaurantId = await ResolveRestaurantIdAsync(ct);

        // UPDATE واحد idempotent: مفيش read-modify-write.
        var affected = await _db.Coupons
            .Where(c => c.Id == id && c.RestaurantId == restaurantId)
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.IsActive, isActive), ct);

        if (affected == 0) throw new NotFoundException("Coupon not found.");
        return await GetByIdAsync(id, ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var restaurantId = await ResolveRestaurantIdAsync(ct);

        var exists = await _db.Coupons.AsNoTracking().AnyAsync(c => c.Id == id && c.RestaurantId == restaurantId, ct);
        if (!exists) throw new NotFoundException("Coupon not found.");

        // الـ spec: 409 لو في أي CouponUsage. وكمان أي Order (حتى لسه PendingPayment) شايل الكوبون،
        // لأن Order.CouponId علاقة Restrict وهتفشل الحذف.
        if (await _db.CouponUsages.AnyAsync(u => u.CouponId == id, ct) ||
            await _db.Orders.AnyAsync(o => o.CouponId == id, ct))
            throw new ConflictException("This coupon has been used and cannot be deleted. Deactivate it instead.");

        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            // السلات اللي مطبّقة الكوبون: نشيله منها (Cart.CouponId FK) بدل ما الحذف يفشل.
            await _db.Carts.Where(c => c.CouponId == id)
                .ExecuteUpdateAsync(s => s.SetProperty(c => c.CouponId, (Guid?)null), ct);

            var deleted = await _db.Coupons
                .Where(c => c.Id == id && c.RestaurantId == restaurantId)
                .ExecuteDeleteAsync(ct);

            await tx.CommitAsync(ct);
            if (deleted == 0) throw new NotFoundException("Coupon not found.");
        }
        catch (DbUpdateException)
        {
            // استخدام اتسجّل بعد الـ check (webhook متزامن): الـ FK منع الحذف. ده الضمان الحقيقي.
            await tx.RollbackAsync(ct);
            throw new ConflictException("This coupon has just been used and cannot be deleted. Deactivate it instead.");
        }
    }

    // ───────────── helpers ─────────────

    private static string NormalizeCode(string code) => code.Trim().ToUpperInvariant();

    private async Task<Guid> ResolveRestaurantIdAsync(CancellationToken ct)
    {
        var userId = _currentUser.UserId ?? throw new UnauthorizedAppException("Not authenticated.");

        // V1: صاحب المطعم عنده مطعم واحد. الموظفين مالهمش صلاحية كوبونات، فمفيش resolution للموظف هنا.
        var restaurantId = await _db.Restaurants.AsNoTracking()
            .Where(r => r.RestaurantOwner.UserId == userId)
            .Select(r => (Guid?)r.Id)
            .FirstOrDefaultAsync(ct);

        return restaurantId ?? throw new NotFoundException("Restaurant not found for the current owner.");
    }

    private static CouponResponse ToResponse(Coupon c, int usageCount, DateTime now) => new(
        c.Id, c.Code, c.DiscountType, c.DiscountValue, c.MaxDiscountAmount, c.MinimumOrderAmount,
        c.ExpiryDate, c.UsageLimitTotal, c.UsageLimitPerCustomer, c.IsActive,
        IsExpired: c.ExpiryDate.HasValue && c.ExpiryDate.Value <= now,
        IsExhausted: c.UsageLimitTotal.HasValue && usageCount >= c.UsageLimitTotal.Value,
        UsageCount: usageCount,
        CreatedAt: c.CreatedAt);

    private sealed record CouponRow(Coupon Coupon, int UsageCount);
}
