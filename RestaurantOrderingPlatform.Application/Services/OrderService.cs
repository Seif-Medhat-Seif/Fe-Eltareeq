// Application/Services/OrderService.cs
using Microsoft.EntityFrameworkCore;
using RestaurantOrderingPlatform.Application.DTOs;
using RestaurantOrderingPlatform.Application.DTOs.Orders;
using RestaurantOrderingPlatform.Application.Exceptions;
using RestaurantOrderingPlatform.Application.Interfaces;
using RestaurantOrderingPlatform.Domain.Entities;
using RestaurantOrderingPlatform.Domain.Enums;

namespace RestaurantOrderingPlatform.Application.Services;

public class OrderService : IOrderService
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IIdentityService _identity;
    private readonly IPaymentGateway _gateway;
    private readonly INotificationService _notifications;

    public OrderService(
        IApplicationDbContext db,
        ICurrentUserService currentUser,
        IIdentityService identity,
        IPaymentGateway gateway,
        INotificationService notifications)
    {
        _db = db;
        _currentUser = currentUser;
        _identity = identity;
        _gateway = gateway;
        _notifications = notifications;
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  CHECKOUT  —  POST /api/orders
    // ═══════════════════════════════════════════════════════════════════════
    public async Task<OrderResponse> CheckoutAsync(CreateOrderRequest request, CancellationToken ct = default)
    {
        var customer = await GetCurrentCustomerAsync(ct);

        // ── 1) نجيب السلة كاملة في كويري واحدة ─────────────────────────────
        var cart = await _db.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.CustomerId == customer.Id, ct)
            ?? throw new ConflictException("Your cart is empty.");

        if (cart.Items.Count == 0 || cart.RestaurantId is null)
            throw new ConflictException("Your cart is empty.");

        // ── 2) العنوان لازم يكون بتاع العميل ده ────────────────────────────
        var address = await _db.Addresses
            .FirstOrDefaultAsync(a => a.Id == request.AddressId && a.CustomerId == customer.Id, ct)
            ?? throw new NotFoundException("Address not found.");

        // ── 3) المطعم — لازم يكون شغال دلوقتي ──────────────────────────────
        var restaurant = await _db.Restaurants
            .Include(r => r.WorkingHours)
            .FirstOrDefaultAsync(r => r.Id == cart.RestaurantId, ct)
            ?? throw new NotFoundException("Restaurant not found.");

        if (!RestaurantAvailability.IsOpen(restaurant))
            throw new ConflictException("The restaurant is currently closed and cannot accept orders.");

        // ── 4) نعيد قراءة الأصناف من الداتابيز (مش من الـ cart، ومش من العميل) ──
        var itemIds = cart.Items.Select(i => i.MenuItemId).Distinct().ToList();

        var menuItems = await _db.MenuItems
            .Where(mi => itemIds.Contains(mi.Id))
            .Select(mi => new
            {
                mi.Id,
                mi.Name,
                mi.Price,
                mi.IsAvailable,
                RestaurantId = mi.MenuCategory.RestaurantId
            })
            .ToListAsync(ct);
        // ملاحظة: الـ global query filter بتاع IsDeleted شغال هنا تلقائيًا،
        // يعني أي صنف اتمسح soft-delete مش هيرجع خالص → هيقع في الـ check اللي تحت.

        var unavailable = new List<string>();
        foreach (var cartItem in cart.Items)
        {
            var mi = menuItems.FirstOrDefault(m => m.Id == cartItem.MenuItemId);
            if (mi is null) { unavailable.Add("A removed item"); continue; }
            if (!mi.IsAvailable) unavailable.Add(mi.Name);
            if (mi.RestaurantId != restaurant.Id)
                throw new ConflictException("Cart contains items from more than one restaurant.");
        }

        if (unavailable.Count > 0)
            throw new ConflictException(
                $"These items are no longer available: {string.Join(", ", unavailable)}. Please update your cart.");

        // ── 5) الحسابات — كلها سيرفر-سايد ──────────────────────────────────
        var orderItems = cart.Items.Select(ci =>
        {
            var mi = menuItems.First(m => m.Id == ci.MenuItemId);
            return new OrderItem
            {
                Id = Guid.NewGuid(),
                MenuItemId = mi.Id,
                ItemNameSnapshot = mi.Name,
                ItemPriceSnapshot = mi.Price,
                Quantity = ci.Quantity,
                LineTotal = decimal.Round(mi.Price * ci.Quantity, 2)
            };
        }).ToList();

        var subtotal = decimal.Round(orderItems.Sum(i => i.LineTotal), 2);

        if (restaurant.MinimumOrderAmount is { } min && subtotal < min)
            throw new ConflictException($"Minimum order amount for this restaurant is {min:0.00} EGP.");

        var deliveryFee = restaurant.DeliveryFee;

        // ── 6) الكوبون — الفحص الحقيقي هنا، مش وقت الإضافة للسلة ───────────
        decimal discount = 0m;
        Coupon? coupon = null;

        if (cart.CouponId is { } couponId)
        {
            coupon = await _db.Coupons.FirstOrDefaultAsync(c => c.Id == couponId, ct);
            discount = await ValidateAndCalculateDiscountAsync(coupon, restaurant.Id, customer.Id, subtotal, ct);
        }

        var total = decimal.Round(subtotal + deliveryFee - discount, 2);
        if (total < 0) total = 0m;

        var order = new Order
        {
            Id = Guid.NewGuid(),
            OrderNumber = GenerateOrderNumber(),
            CustomerId = customer.Id,
            RestaurantId = restaurant.Id,
            AddressId = address.Id,
            CouponId = coupon?.Id,
            Status = OrderStatus.PendingPayment,
            Subtotal = subtotal,
            DeliveryFee = deliveryFee,
            DiscountAmount = discount,
            Total = total,
            CreatedAt = DateTime.UtcNow,

            // ★ Address snapshot — بنفس منطق OrderItem snapshot
            AddressLabelSnapshot = address.Label,
            AddressLineSnapshot = BuildAddressLine(address),
            AddressCitySnapshot = address.City,
            AddressLandmarkSnapshot = address.Landmark,

            Items = orderItems
        };

        order.StatusHistory = new List<OrderStatusHistory>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Status = OrderStatus.PendingPayment,
                ChangedAt = DateTime.UtcNow,
                ChangedByUserId = _currentUser.UserId
            }
        };

        // ── 7) حدود الـ Transaction ────────────────────────────────────────
        //  كل ده لازم ينجح أو يفشل مع بعض: الأوردر + الأصناف + التاريخ + تفريغ السلة.
        //  مفيش أي نداء على API خارجي جوه الـ transaction دي (Paymob في endpoint تاني).
        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            _db.Orders.Add(order);

            _db.CartItems.RemoveRange(cart.Items);
            cart.RestaurantId = null;
            cart.CouponId = null;

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }

        return await BuildOrderResponseAsync(order.Id, ct);
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  INITIATE / RETRY PAYMENT  —  POST /api/orders/{id}/payment
    // ═══════════════════════════════════════════════════════════════════════
    public async Task<PaymentIntentResponse> InitiatePaymentAsync(Guid orderId, CancellationToken ct = default)
    {
        var customer = await GetCurrentCustomerAsync(ct);

        var order = await _db.Orders
            .FirstOrDefaultAsync(o => o.Id == orderId && o.CustomerId == customer.Id, ct)
            ?? throw new NotFoundException("Order not found.");

        if (order.Status != OrderStatus.PendingPayment)
            throw new ConflictException($"Order is in '{order.Status}' state and cannot be paid.");

        var contact = await _identity.GetContactAsync(customer.UserId, ct)
            ?? throw new NotFoundException("User account not found.");

        // Paymob بيرفض الـ intention من غير تليفون (400)، فنرد 409 واضح قبل ما ننده الـ gateway
        if (string.IsNullOrWhiteSpace(contact.PhoneNumber))
            throw new ConflictException("Please add a phone number to your profile before paying.");

        var result = await _gateway.CreateIntentAsync(new PaymentIntentRequest(
            order.Id, order.OrderNumber, order.Total,
            contact.FullName, contact.Email, contact.PhoneNumber,
            order.AddressCitySnapshot, order.AddressLineSnapshot), ct);

        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            Status = PaymentStatus.Pending,
            AmountPaid = 0m,
            PaymobOrderId = result.GatewayOrderId,
            CreatedAt = DateTime.UtcNow
        };

        _db.Payments.Add(payment);
        await _db.SaveChangesAsync(ct);

        return new PaymentIntentResponse(
            payment.Id, order.Id, result.CheckoutUrl,
            order.Total, result.ExpiresAt);
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  CUSTOMER READS
    // ═══════════════════════════════════════════════════════════════════════
    public async Task<PagedResponse<OrderSummaryResponse>> GetMyOrdersAsync(int page, int pageSize, CancellationToken ct = default)
    {
        var customer = await GetCurrentCustomerAsync(ct);
        (page, pageSize) = Normalize(page, pageSize);

        var query = _db.Orders.AsNoTracking()
            .Where(o => o.CustomerId == customer.Id)
            .OrderByDescending(o => o.CreatedAt);

        var total = await query.CountAsync(ct);

        var items = await query
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(o => new OrderSummaryResponse(
                o.Id, o.OrderNumber, o.Status, o.RestaurantId,
                o.Restaurant.Name, o.Items.Count, o.Total, o.CreatedAt))
            .ToListAsync(ct);

        return new PagedResponse<OrderSummaryResponse>(items, page, pageSize, total);
    }

    public async Task<OrderResponse> GetMyOrderByIdAsync(Guid orderId, CancellationToken ct = default)
    {
        var customer = await GetCurrentCustomerAsync(ct);

        var exists = await _db.Orders.AnyAsync(o => o.Id == orderId && o.CustomerId == customer.Id, ct);
        if (!exists) throw new NotFoundException("Order not found.");

        return await BuildOrderResponseAsync(orderId, ct);
    }

    public async Task CancelAsync(Guid orderId, CancellationToken ct = default)
    {
        var customer = await GetCurrentCustomerAsync(ct);

        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        var affected = await _db.Orders
            .Where(o => o.Id == orderId
                     && o.CustomerId == customer.Id
                     && o.Status == OrderStatus.PendingPayment)
            .ExecuteUpdateAsync(s => s.SetProperty(o => o.Status, OrderStatus.Cancelled), ct);

        if (affected == 0)
        {
            await tx.RollbackAsync(ct);
            throw new ConflictException("Order cannot be cancelled at this stage.");
        }

        _db.OrderStatusHistories.Add(new OrderStatusHistory
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            Status = OrderStatus.Cancelled,
            ChangedAt = DateTime.UtcNow,
            ChangedByUserId = _currentUser.UserId,
            Note = "Cancelled by customer"
        });

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  RESTAURANT SIDE
    // ═══════════════════════════════════════════════════════════════════════
    public async Task<PagedResponse<RestaurantOrderSummaryResponse>> GetRestaurantOrdersAsync(
        OrderStatus? status, int page, int pageSize, CancellationToken ct = default)
    {
        var restaurantId = await ResolveRestaurantScopeAsync(ct);
        (page, pageSize) = Normalize(page, pageSize);

        var query = _db.Orders.AsNoTracking()
            .Where(o => o.RestaurantId == restaurantId
                     && o.Status != OrderStatus.PendingPayment      // المطعم مبيشوفش أوردر مش مدفوع
                     && o.Status != OrderStatus.Cancelled
                     && o.Status != OrderStatus.Expired
                     && o.Status != OrderStatus.PaymentMismatch);

        if (status.HasValue) query = query.Where(o => o.Status == status.Value);

        var ordered = query.OrderByDescending(o => o.CreatedAt);
        var total = await ordered.CountAsync(ct);

        var rows = await ordered
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(o => new
            {
                o.Id,
                o.OrderNumber,
                o.Status,
                o.Total,
                o.CreatedAt,
                ItemsCount = o.Items.Count,
                CustomerUserId = o.Customer.UserId
            })
            .ToListAsync(ct);

        // اسم وتليفون العميل عايشين في AppUser (Identity) — نجيبهم عن طريق الـ abstraction
        var contacts = await _identity.GetContactsAsync(rows.Select(r => r.CustomerUserId).Distinct().ToList(), ct);

        var items = rows.Select(r =>
        {
            contacts.TryGetValue(r.CustomerUserId, out var c);
            return new RestaurantOrderSummaryResponse(
                r.Id, r.OrderNumber, r.Status,
                c?.FullName ?? "Customer", c?.PhoneNumber,
                r.ItemsCount, r.Total, r.CreatedAt);
        }).ToList();

        return new PagedResponse<RestaurantOrderSummaryResponse>(items, page, pageSize, total);
    }

    public async Task<OrderResponse> GetRestaurantOrderByIdAsync(Guid orderId, CancellationToken ct = default)
    {
        var restaurantId = await ResolveRestaurantScopeAsync(ct);
        var ok = await _db.Orders.AnyAsync(o => o.Id == orderId && o.RestaurantId == restaurantId, ct);
        if (!ok) throw new NotFoundException("Order not found.");
        return await BuildOrderResponseAsync(orderId, ct);
    }

    public Task<OrderResponse> AcceptAsync(Guid orderId, CancellationToken ct = default)
        => TransitionByRestaurantAsync(orderId, OrderStatus.Accepted, null, ct);

    public Task<OrderResponse> RejectAsync(Guid orderId, string reason, CancellationToken ct = default)
        => TransitionByRestaurantAsync(orderId, OrderStatus.Rejected, reason, ct);

    public Task<OrderResponse> UpdateStatusAsync(Guid orderId, OrderStatus newStatus, CancellationToken ct = default)
        => TransitionByRestaurantAsync(orderId, newStatus, null, ct);

    private async Task<OrderResponse> TransitionByRestaurantAsync(
        Guid orderId, OrderStatus target, string? reason, CancellationToken ct)
    {
        var restaurantId = await ResolveRestaurantScopeAsync(ct);

        var order = await _db.Orders
            .FirstOrDefaultAsync(o => o.Id == orderId && o.RestaurantId == restaurantId, ct)
            ?? throw new NotFoundException("Order not found.");

        if (!OrderStateMachine.CanTransition(order.Status, target))
            throw new ConflictException(
                $"Cannot change order from '{order.Status}' to '{target}'. " +
                $"Allowed next: {string.Join(", ", OrderStateMachine.NextStates(order.Status))}.");

        order.Status = target;
        if (target == OrderStatus.Rejected) order.RejectionReason = reason;
        if (target == OrderStatus.Delivered) order.DeliveredAt = DateTime.UtcNow;

        _db.OrderStatusHistories.Add(new OrderStatusHistory
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            Status = target,
            ChangedAt = DateTime.UtcNow,
            ChangedByUserId = _currentUser.UserId,
            Note = reason
        });

        // لو اترفض → نروح فورًا لـ RefundPending (خطوة واحدة، مش محتاجة تدخل بشري)
        if (target == OrderStatus.Rejected)
        {
            order.Status = OrderStatus.RefundPending;
            _db.OrderStatusHistories.Add(new OrderStatusHistory
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                Status = OrderStatus.RefundPending,
                ChangedAt = DateTime.UtcNow,
                Note = "Awaiting manual refund by admin"
            });
        }

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            // ★ RowVersion اشتغل: موظف تاني غيّر نفس الأوردر في نفس اللحظة.
            throw new ConflictException("This order was just updated by someone else. Please refresh and try again.");
        }

        await NotifyAsync(order.Id, MapNotification(target), ct);
        return await BuildOrderResponseAsync(order.Id, ct);
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  ADMIN — PAYMENT MISMATCHES
    // ═══════════════════════════════════════════════════════════════════════
    public async Task<PagedResponse<PaymentMismatchResponse>> GetPaymentMismatchesAsync(
        int page, int pageSize, CancellationToken ct = default)
    {
        (page, pageSize) = Normalize(page, pageSize);

        var query = _db.Orders.AsNoTracking()
            .Where(o => o.Status == OrderStatus.PaymentMismatch)
            .OrderBy(o => o.CreatedAt);

        var total = await query.CountAsync(ct);

        var items = await query
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(o => new PaymentMismatchResponse(
                o.Id, o.OrderNumber, o.Total,
                o.Payments.OrderByDescending(p => p.CreatedAt).Select(p => p.AmountPaid).FirstOrDefault(),
                o.Payments.OrderByDescending(p => p.CreatedAt).Select(p => p.PaymobTransactionReference).FirstOrDefault(),
                o.CreatedAt))
            .ToListAsync(ct);

        return new PagedResponse<PaymentMismatchResponse>(items, page, pageSize, total);
    }

    public async Task<OrderResponse> ResolvePaymentMismatchAsync(
        Guid orderId, ResolvePaymentMismatchRequest request, CancellationToken ct = default)
    {
        var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == orderId, ct)
            ?? throw new NotFoundException("Order not found.");

        if (order.Status != OrderStatus.PaymentMismatch)
            throw new ConflictException("This order is not flagged as a payment mismatch.");

        var target = request.Resolution == PaymentMismatchResolution.ConfirmPaid
            ? OrderStatus.Paid
            : OrderStatus.PendingPayment;

        order.Status = target;
        if (target == OrderStatus.Paid) order.PaidAt = DateTime.UtcNow;

        _db.OrderStatusHistories.Add(new OrderStatusHistory
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            Status = target,
            ChangedAt = DateTime.UtcNow,
            ChangedByUserId = _currentUser.UserId,
            Note = $"Mismatch resolved by admin: {request.Resolution}. {request.AdminNote}".Trim()
        });

        await _db.SaveChangesAsync(ct);

        if (target == OrderStatus.Paid)
            await NotifyAsync(order.Id, OrderNotificationType.PaymentConfirmed, ct);

        return await BuildOrderResponseAsync(order.Id, ct);
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  HELPERS
    // ═══════════════════════════════════════════════════════════════════════

    private async Task<Customer> GetCurrentCustomerAsync(CancellationToken ct)
    {
        var userId = _currentUser.UserId ?? throw new UnauthorizedAppException("Not authenticated.");
        return await _db.Customers.FirstOrDefaultAsync(c => c.UserId == userId, ct)
            ?? throw new NotFoundException("Customer profile not found.");
    }

    /// <summary>
    /// بيرجع الـ RestaurantId اللي المستخدم الحالي مسموح له يشتغل عليه —
    /// سواء كان Owner (مطعمه) أو Employee (المطعم اللي متعيّن فيه).
    /// ده تطبيق BR-9: الموظف مبيشوفش غير مطعمه، والـ check في الـ Service مش الـ Controller.
    /// </summary>
    private async Task<Guid> ResolveRestaurantScopeAsync(CancellationToken ct)
    {
        var userId = _currentUser.UserId ?? throw new UnauthorizedAppException("Not authenticated.");

        var ownerRestaurantId = await _db.Restaurants
            .Where(r => r.RestaurantOwner.UserId == userId)
            .Select(r => (Guid?)r.Id)
            .FirstOrDefaultAsync(ct);

        if (ownerRestaurantId is { } id) return id;

        var employeeRestaurantId = await _db.RestaurantEmployees
            .Where(e => e.IsActive && e.Customer.UserId == userId)
            .Select(e => (Guid?)e.RestaurantId)
            .FirstOrDefaultAsync(ct);

        return employeeRestaurantId
            ?? throw new UnauthorizedAppException("You are not linked to any restaurant.");
    }

    private async Task<decimal> ValidateAndCalculateDiscountAsync(
        Coupon? coupon, Guid restaurantId, Guid customerId, decimal subtotal, CancellationToken ct)
    {
        if (coupon is null)
            throw new ConflictException("The applied coupon no longer exists.");

        if (coupon.RestaurantId != restaurantId)             // BR-7
            throw new ConflictException("Coupon is not valid for this restaurant.");

        if (!coupon.IsActive)
            throw new ConflictException("Coupon is no longer active.");

        if (coupon.ExpiryDate is { } exp && exp < DateTime.UtcNow)
            throw new ConflictException("Coupon has expired.");

        if (coupon.MinimumOrderAmount is { } minAmt && subtotal < minAmt)
            throw new ConflictException($"This coupon requires a minimum order of {minAmt:0.00} EGP.");

        // ★ الفحص اللي مكانش ممكن في Module 5 — usage limits
        if (coupon.UsageLimitTotal is { } limitTotal)
        {
            var used = await _db.CouponUsages.CountAsync(u => u.CouponId == coupon.Id, ct);
            if (used >= limitTotal)
                throw new ConflictException("This coupon has reached its usage limit.");
        }

        var usedByCustomer = await _db.CouponUsages
            .CountAsync(u => u.CouponId == coupon.Id && u.CustomerId == customerId, ct);

        if (usedByCustomer >= coupon.UsageLimitPerCustomer)
            throw new ConflictException("You have already used this coupon the maximum number of times.");

        var discount = coupon.DiscountType == DiscountType.Percentage
            ? decimal.Round(subtotal * (coupon.DiscountValue / 100m), 2)
            : coupon.DiscountValue;

        if (coupon.MaxDiscountAmount is { } cap && discount > cap) discount = cap;
        if (discount > subtotal) discount = subtotal;   // الخصم مياكلش الديليفري

        return decimal.Round(discount, 2);
    }

    private async Task<OrderResponse> BuildOrderResponseAsync(Guid orderId, CancellationToken ct)
    {
        var o = await _db.Orders.AsNoTracking()
            .Where(x => x.Id == orderId)
            .Select(x => new OrderResponse(
                x.Id, x.OrderNumber, x.Status, x.RestaurantId, x.Restaurant.Name,
                new OrderAddressResponse(
                    x.AddressLabelSnapshot, x.AddressLineSnapshot,
                    x.AddressCitySnapshot, x.AddressLandmarkSnapshot),
                x.Items.Select(i => new OrderItemResponse(
                    i.Id, i.MenuItemId, i.ItemNameSnapshot, i.ItemPriceSnapshot, i.Quantity, i.LineTotal)).ToList(),
                x.Subtotal, x.DeliveryFee, x.DiscountAmount, x.Total,
                x.RejectionReason, x.CreatedAt,
                x.StatusHistory.OrderBy(h => h.ChangedAt)
                    .Select(h => new OrderStatusHistoryResponse(h.Status, h.ChangedAt, h.Note)).ToList()))
            .FirstOrDefaultAsync(ct);

        return o ?? throw new NotFoundException("Order not found.");
    }

    private async Task NotifyAsync(Guid orderId, OrderNotificationType? type, CancellationToken ct)
    {
        if (type is null) return;

        var data = await _db.Orders.AsNoTracking()
            .Where(o => o.Id == orderId)
            .Select(o => new { o.OrderNumber, RestaurantName = o.Restaurant.Name, o.Customer.UserId })
            .FirstOrDefaultAsync(ct);

        if (data is null) return;

        var contact = await _identity.GetContactAsync(data.UserId, ct);
        if (contact?.PhoneNumber is null) return;   // مفيش تليفون = مفيش واتساب، وخلاص

        _notifications.Enqueue(new OrderNotification(
            contact.PhoneNumber, contact.FullName, data.OrderNumber,
            data.RestaurantName, type.Value));
    }

    private static OrderNotificationType? MapNotification(OrderStatus s) => s switch
    {
        OrderStatus.Paid => OrderNotificationType.PaymentConfirmed,
        OrderStatus.Accepted => OrderNotificationType.OrderAccepted,
        OrderStatus.Rejected => OrderNotificationType.OrderRejected,
        OrderStatus.Preparing => OrderNotificationType.Preparing,
        OrderStatus.Ready => OrderNotificationType.Ready,
        OrderStatus.PickedUp => OrderNotificationType.PickedUp,
        OrderStatus.OnTheWay => OrderNotificationType.OnTheWay,
        OrderStatus.Delivered => OrderNotificationType.Delivered,
        _ => null
    };

    private static string BuildAddressLine(Address a)
    {
        var parts = new List<string> { a.Street };
        if (!string.IsNullOrWhiteSpace(a.Building)) parts.Add($"Building {a.Building}");
        if (!string.IsNullOrWhiteSpace(a.Floor)) parts.Add($"Floor {a.Floor}");
        if (!string.IsNullOrWhiteSpace(a.Apartment)) parts.Add($"Apt {a.Apartment}");
        return string.Join(", ", parts);
    }

    private static string GenerateOrderNumber()
        => $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";

    private static (int, int) Normalize(int page, int pageSize)
        => (page < 1 ? 1 : page, pageSize is < 1 or > 50 ? 20 : pageSize);
}