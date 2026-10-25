using Microsoft.EntityFrameworkCore;
using RestaurantOrderingPlatform.Application;
using RestaurantOrderingPlatform.Application.Interfaces;
using RestaurantOrderingPlatform.Application.DTOs.Drivers;
using RestaurantOrderingPlatform.Application.DTOs.Orders;
using RestaurantOrderingPlatform.Application.Exceptions;
using RestaurantOrderingPlatform.Domain.Entities;
using RestaurantOrderingPlatform.Domain.Enums;
using RestaurantOrderingPlatform.Application.DTOs;

namespace RestaurantOrderingPlatform.Application.Services;

/// <summary>
/// قواعد الـ class ده:
/// 1) أي تغيير حالة = UPDATE شرطي واحد (WHERE الحالة المتوقعة)، مش "اقرا وبعدين اكتب".
/// 2) "سواق واحد = توصيلة نشطة واحدة" (BR-6) مضمونة بـ filtered UNIQUE index في الداتابيز.
///    الـ check اللي في الكود لرسالة خطأ لطيفة بس، مش هو الضمان.
/// 3) تغيير الحالة + سطر الـ history في transaction واحدة. الإشعار بعد الـ commit.
/// 4) التحقق (Approved) بيتطبق على available و accept بس. اللي ماسك توصيلة فعلًا يكمّلها
///    حتى لو الأدمن علّق حسابه في النص.
/// </summary>
public class DriverService : IDriverService
{
    // لازم تطابق الـ HasFilter في OrderConfiguration حرفيًا.
    private static readonly OrderStatus[] ActiveStatuses =
        { OrderStatus.DriverAssigned, OrderStatus.PickedUp, OrderStatus.OnTheWay };

    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IIdentityService _identity;
    private readonly INotificationService _notifications;

    public DriverService(
        IApplicationDbContext db,
        ICurrentUserService currentUser,
        IIdentityService identity,
        INotificationService notifications)
    {
        _db = db;
        _currentUser = currentUser;
        _identity = identity;
        _notifications = notifications;
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  PROFILE / VERIFICATION
    // ═══════════════════════════════════════════════════════════════════════
    public async Task<DriverProfileResponse> GetProfileAsync(CancellationToken ct = default)
    {
        var driver = await GetDriverAsync(ct);
        var contact = await _identity.GetContactAsync(driver.UserId, ct);

        // قبل ما السواق يبعت بياناته، الأعمدة فيها placeholder فارغ → منرجّعهاش.
        var submitted = driver.VerificationStatus != DriverVerificationStatus.NotSubmitted;

        return new DriverProfileResponse(
            driver.Id,
            contact?.FullName ?? "Driver",
            contact?.PhoneNumber,
            submitted ? driver.NationalID : null,
            driver.VehicleType,
            submitted ? driver.VehiclePlateNumber : null,
            submitted ? driver.VehicleImageUrl : null,
            driver.VerificationStatus,
            driver.VerificationRejectionReason);
    }

    public async Task<DriverProfileResponse> SubmitProfileAsync(
        UpdateDriverProfileRequest request, CancellationToken ct = default)
    {
        var userId = _currentUser.UserId ?? throw new UnauthorizedAppException("Not authenticated.");

        var driver = await _db.Drivers.FirstOrDefaultAsync(d => d.UserId == userId, ct)
            ?? throw new NotFoundException("Driver profile not found.");

        // التعديل مسموح في NotSubmitted و Rejected بس. Pending = قيد المراجعة، Approved = مقفول.
        if (driver.VerificationStatus == DriverVerificationStatus.Pending)
            throw new ConflictException("Your details are already under review.");

        if (driver.VerificationStatus == DriverVerificationStatus.Approved)
            throw new ConflictException("Verified details cannot be changed here. Please contact support.");

        driver.NationalID = request.NationalId.Trim();
        driver.VehicleType = request.VehicleType;
        driver.VehiclePlateNumber = request.VehicleType == VehicleType.Bike
            ? null
            : request.VehiclePlateNumber?.Trim().ToUpperInvariant();
        driver.VehicleImageUrl = request.VehicleImageUrl.Trim();

        driver.VerificationStatus = DriverVerificationStatus.Pending;
        driver.VerificationSubmittedAt = DateTime.UtcNow;
        driver.VerificationReviewedAt = null;
        driver.VerificationRejectionReason = null;

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // الـ filtered UNIQUE index على NationalID (بين اللي بعتوا بياناتهم). لو الرقم ده عند حساب تاني → 409.
            // بنتأكد بسؤال الداتابيز بدل ما نعتمد على نوع الـ exception.
            var taken = await _db.Drivers.AsNoTracking().AnyAsync(d =>
                d.Id != driver.Id
                && d.NationalID == driver.NationalID
                && d.VerificationStatus != DriverVerificationStatus.NotSubmitted, ct);

            if (taken) throw new ConflictException("This national ID cannot be used.");
            throw;
        }

        return await GetProfileAsync(ct);
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  GET /api/driver/deliveries/available   (polling)
    // ═══════════════════════════════════════════════════════════════════════
    public async Task<PagedResponse<AvailableDeliveryResponse>> GetAvailableAsync(
        int page, int pageSize, CancellationToken ct = default)
    {
        await GetApprovedDriverAsync(ct);
        (page, pageSize) = Normalize(page, pageSize);

        // BR-6: السواق المشغول بيشوف القايمة عادي (الحظر وقت الـ accept مش وقت العرض، زي الـ spec).
        var query = _db.Orders.AsNoTracking()
            .Where(o => o.Status == OrderStatus.Ready && o.DriverId == null);

        var total = await query.CountAsync(ct);

        // الأقدم "جاهز" الأول. بنستخدم Min (أول مرة بقى Ready) مش Max:
        // أوردر اتحرر (release) بعد ما استنّى كتير لازم يرجع لمكانه القديم مش آخر الطابور.
        var items = await query
            .OrderBy(o => o.StatusHistory
                .Where(h => h.Status == OrderStatus.Ready)
                .Min(h => (DateTime?)h.ChangedAt))
            .ThenBy(o => o.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(o => new AvailableDeliveryResponse(
                o.Id,
                o.OrderNumber,
                o.Restaurant.Name,
                o.Restaurant.Street,
                o.Restaurant.City,
                o.AddressCitySnapshot,
                o.Items.Count,
                o.DeliveryFee,
                o.StatusHistory
                    .Where(h => h.Status == OrderStatus.Ready)
                    .Min(h => (DateTime?)h.ChangedAt) ?? o.CreatedAt))
            .ToListAsync(ct);

        return new PagedResponse<AvailableDeliveryResponse>(items, page, pageSize, total);
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  POST /api/driver/deliveries/{id}/accept
    // ═══════════════════════════════════════════════════════════════════════
    public async Task<DeliveryResponse> AcceptAsync(Guid orderId, CancellationToken ct = default)
    {
        var driver = await GetApprovedDriverAsync(ct);

        // (أ) فحص مبكر لرسالة واضحة. تلميح مش ضمان.
        if (await HasActiveDeliveryAsync(driver.Id, ct))
            throw new ConflictException("You already have an active delivery. Finish it before accepting another.");

        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        int affected;
        try
        {
            // (ب) ★ UPDATE شرطي ذري. سواقين اتنين في نفس الميلي ثانية: SQL Server بيسلسل الـ UPDATEين،
            //   الأول بينجح والتاني بيلاقي الشرط اتكسر ويرجّع 0 صفوف.
            affected = await _db.Orders
                .Where(o => o.Id == orderId
                         && o.Status == OrderStatus.Ready
                         && o.DriverId == null)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(o => o.DriverId, driver.Id)
                    .SetProperty(o => o.Status, OrderStatus.DriverAssigned), ct);
        }
        catch (Exception)
        {
            await tx.RollbackAsync(ct);

            // الـ UNIQUE index للتوصيلة النشطة اتكسر؟ يعني نفس السواق كسب أوردر تاني في نفس اللحظة.
            // بنسأل الداتابيز بدل ما نعتمد على نوع الـ exception (بيختلف حسب نسخة EF والـ provider).
            if (await HasActiveDeliveryAsync(driver.Id, ct))
                throw new ConflictException("You already have an active delivery. Finish it before accepting another.");

            throw;
        }

        if (affected == 0)
        {
            await tx.RollbackAsync(ct);

            var exists = await _db.Orders.AnyAsync(o => o.Id == orderId, ct);
            if (!exists) throw new NotFoundException("Delivery not found.");

            throw new ConflictException("This delivery is no longer available.");
        }

        _db.OrderStatusHistories.Add(new OrderStatusHistory
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            Status = OrderStatus.DriverAssigned,
            ChangedAt = DateTime.UtcNow,
            ChangedByUserId = _currentUser.UserId,
            Note = "Accepted by driver"
        });

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        // مفيش إشعار على DriverAssigned (الـ spec بتحدد PickedUp / OnTheWay / Delivered بس).
        return await BuildDeliveryResponseAsync(orderId, ct);
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  GET /api/driver/deliveries/current   → { "delivery": null } لو مفيش
    // ═══════════════════════════════════════════════════════════════════════
    public async Task<CurrentDeliveryResponse> GetCurrentAsync(CancellationToken ct = default)
    {
        var driver = await GetDriverAsync(ct);

        var orderId = await _db.Orders.AsNoTracking()
            .Where(o => o.DriverId == driver.Id && ActiveStatuses.Contains(o.Status))
            .Select(o => (Guid?)o.Id)
            .FirstOrDefaultAsync(ct);

        if (orderId is null) return new CurrentDeliveryResponse(null);

        return new CurrentDeliveryResponse(await BuildDeliveryResponseAsync(orderId.Value, ct));
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  PATCH /api/driver/deliveries/{id}/status   (PickedUp → OnTheWay → Delivered)
    // ═══════════════════════════════════════════════════════════════════════
    public async Task<DeliveryResponse> UpdateStatusAsync(
        Guid orderId, OrderStatus newStatus, CancellationToken ct = default)
    {
        var driver = await GetDriverAsync(ct);

        // مش بتاعي → 404 (مش 403: منكشفش وجود أوردرات غيري).
        var current = await _db.Orders.AsNoTracking()
            .Where(o => o.Id == orderId && o.DriverId == driver.Id)
            .Select(o => new { o.Status })
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("Delivery not found.");

        if (!OrderStateMachine.CanTransition(current.Status, newStatus))
            throw new ConflictException(
                $"Cannot change delivery from '{current.Status}' to '{newStatus}'. " +
                $"Allowed next: {string.Join(", ", OrderStateMachine.NextStates(current.Status))}.");

        var now = DateTime.UtcNow;
        DateTime? deliveredAt = newStatus == OrderStatus.Delivered ? now : null;

        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        // الشرط على الحالة اللي قريتها بالظبط: request مكرر (double-tap) بيلاقي الحالة اتغيرت → 0 صفوف.
        var affected = await _db.Orders
            .Where(o => o.Id == orderId
                     && o.DriverId == driver.Id
                     && o.Status == current.Status)
            .ExecuteUpdateAsync(s => s
                .SetProperty(o => o.Status, newStatus)
                .SetProperty(o => o.DeliveredAt, deliveredAt), ct);

        if (affected == 0)
        {
            await tx.RollbackAsync(ct);
            throw new ConflictException("This delivery was just updated. Please refresh and try again.");
        }

        _db.OrderStatusHistories.Add(new OrderStatusHistory
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            Status = newStatus,
            ChangedAt = now,
            ChangedByUserId = _currentUser.UserId
        });

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        // Delivered بيحرر السواق تلقائيًا (الحالة خرجت من فلتر الـ index) وبيفتح أهلية الريفيو (Module 9).
        await NotifyAsync(orderId, MapNotification(newStatus), ct);

        return await BuildDeliveryResponseAsync(orderId, ct);
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  GET /api/driver/deliveries/history
    // ═══════════════════════════════════════════════════════════════════════
    public async Task<PagedResponse<DeliverySummaryResponse>> GetHistoryAsync(
        int page, int pageSize, CancellationToken ct = default)
    {
        var driver = await GetDriverAsync(ct);
        (page, pageSize) = Normalize(page, pageSize);

        var query = _db.Orders.AsNoTracking()
            .Where(o => o.DriverId == driver.Id && o.Status == OrderStatus.Delivered);

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(o => o.DeliveredAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(o => new DeliverySummaryResponse(
                o.Id, o.OrderNumber, o.Restaurant.Name,
                o.AddressCitySnapshot, o.DeliveryFee, o.DeliveredAt))
            .ToListAsync(ct);

        return new PagedResponse<DeliverySummaryResponse>(items, page, pageSize, total);
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  HELPERS
    // ═══════════════════════════════════════════════════════════════════════

    private async Task<Driver> GetDriverAsync(CancellationToken ct)
    {
        var userId = _currentUser.UserId ?? throw new UnauthorizedAppException("Not authenticated.");
        return await _db.Drivers.AsNoTracking().FirstOrDefaultAsync(d => d.UserId == userId, ct)
            ?? throw new NotFoundException("Driver profile not found.");
    }

    /// <summary>
    /// بوابة التحقق. 409 مش 403 لأن الـ middleware عندنا ملوش ForbiddenException
    /// (ومنلمسهوش عشان endpoint واحد). لو ضفت 403 بعدين، التعديل هنا في سطر واحد.
    /// </summary>
    private async Task<Driver> GetApprovedDriverAsync(CancellationToken ct)
    {
        var driver = await GetDriverAsync(ct);

        if (driver.VerificationStatus != DriverVerificationStatus.Approved)
            throw new ConflictException(driver.VerificationStatus switch
            {
                DriverVerificationStatus.NotSubmitted => "Submit your verification details before accepting deliveries.",
                DriverVerificationStatus.Pending      => "Your account is still under review.",
                _                                     => "Your verification was rejected. Update your details and resubmit."
            });

        return driver;
    }

    private Task<bool> HasActiveDeliveryAsync(Guid driverId, CancellationToken ct)
        => _db.Orders.AnyAsync(o => o.DriverId == driverId && ActiveStatuses.Contains(o.Status), ct);

    private async Task<DeliveryResponse> BuildDeliveryResponseAsync(Guid orderId, CancellationToken ct)
    {
        var o = await _db.Orders.AsNoTracking()
            .Where(x => x.Id == orderId)
            .Select(x => new
            {
                x.Id,
                x.OrderNumber,
                x.Status,
                RestaurantName = x.Restaurant.Name,
                RestaurantStreet = x.Restaurant.Street,
                RestaurantCity = x.Restaurant.City,
                CustomerUserId = x.Customer.UserId,
                Address = new OrderAddressResponse(
                    x.AddressLabelSnapshot, x.AddressLineSnapshot,
                    x.AddressCitySnapshot, x.AddressLandmarkSnapshot),
                Items = x.Items
                    .Select(i => new DeliveryItemResponse(i.ItemNameSnapshot, i.Quantity))
                    .ToList()
            })
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("Delivery not found.");

        var contact = await _identity.GetContactAsync(o.CustomerUserId, ct);

        return new DeliveryResponse(
            o.Id, o.OrderNumber, o.Status,
            new DeliveryRestaurantResponse(o.RestaurantName, o.RestaurantStreet, o.RestaurantCity),
            new DeliveryCustomerResponse(contact?.FullName ?? "Customer", contact?.PhoneNumber),
            o.Address,
            o.Items);
    }

    private async Task NotifyAsync(Guid orderId, OrderNotificationType? type, CancellationToken ct)
    {
        if (type is null) return;

        try
        {
            var data = await _db.Orders.AsNoTracking()
                .Where(o => o.Id == orderId)
                .Select(o => new { o.OrderNumber, RestaurantName = o.Restaurant.Name, o.Customer.UserId })
                .FirstOrDefaultAsync(ct);
            if (data is null) return;

            var contact = await _identity.GetContactAsync(data.UserId, ct);
            if (contact?.PhoneNumber is null) return;

            _notifications.Enqueue(new OrderNotification(
                contact.PhoneNumber, contact.FullName, data.OrderNumber,
                data.RestaurantName, type.Value));
        }
        catch
        {
            // فشل إشعار ممنوع يرجّع التوصيلة لورا. الحالة اتحفظت خلاص (NFR).
        }
    }

    private static OrderNotificationType? MapNotification(OrderStatus s) => s switch
    {
        OrderStatus.PickedUp  => OrderNotificationType.PickedUp,
        OrderStatus.OnTheWay  => OrderNotificationType.OnTheWay,
        OrderStatus.Delivered => OrderNotificationType.Delivered,
        _ => null
    };

    private static (int, int) Normalize(int page, int pageSize)
        => (page < 1 ? 1 : page, pageSize is < 1 or > 50 ? 20 : pageSize);
}
