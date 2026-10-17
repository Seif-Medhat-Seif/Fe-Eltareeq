// Application/Services/PaymentWebhookService.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RestaurantOrderingPlatform.Application.Interfaces;
using RestaurantOrderingPlatform.Domain.Entities;
using RestaurantOrderingPlatform.Domain.Enums;

namespace RestaurantOrderingPlatform.Application.Services;

/// <summary>
/// القواعد (متوافقة مع إرشادات Paymob الرسمية):
/// 1) التوقيع الأول. توقيع غلط = InvalidSignature (الـ Controller يرد 401).
/// 2) الـ idempotency key هو obj.id (transaction id) بـ UNIQUE index.
/// 3) تسجيل الحدث + تغيير حالة الأوردر + CouponUsage + History = transaction واحدة.
///    لو أي حاجة فيهم فشلت، الـ marker نفسه بيتلغي → إعادة إرسال Paymob بتتعالج طبيعي.
/// 4) تغيير الحالة بـ compare-and-set (UPDATE ... WHERE Status = PendingPayment).
/// 5) 2xx بيتبعت بعد الـ commit بس. فشل تقني = exception = 500 = Paymob يعيد.
/// </summary>
public class PaymentWebhookService : IPaymentWebhookService
{
    private const string ProviderName = "Paymob";
    private const string ExpectedCurrency = "EGP";   // single-currency V1 (Phase 1)

    private readonly IApplicationDbContext _db;
    private readonly IPaymentGateway _gateway;
    private readonly INotificationService _notifications;
    private readonly IIdentityService _identity;
    private readonly ILogger<PaymentWebhookService> _logger;

    public PaymentWebhookService(
        IApplicationDbContext db,
        IPaymentGateway gateway,
        INotificationService notifications,
        IIdentityService identity,
        ILogger<PaymentWebhookService> logger)
    {
        _db = db;
        _gateway = gateway;
        _notifications = notifications;
        _identity = identity;
        _logger = logger;
    }

    public async Task<WebhookResult> HandlePaymobCallbackAsync(
        string rawJsonBody, string? hmacFromQuery, CancellationToken ct = default)
    {
        // ── 1) التوقيع ──
        if (!_gateway.TryParseCallback(rawJsonBody, hmacFromQuery, out var cb))
        {
            _logger.LogWarning("Paymob webhook rejected: invalid HMAC or unreadable payload.");
            return WebhookResult.InvalidSignature;
        }

        // ── 2) callbacks مش محتاجة فعل ──
        // pending: مش نتيجة نهائية. وما بنسجلهاش كـ processed عشان الـ callback النهائي
        // ممكن يجي بنفس الـ transaction id وميتعتبرش duplicate.
        // refund/void: عمليات تانية بتجي بنفس الشكل بـ success=true — لو اتعاملت كدفع تبقى كارثة.
        if (cb.IsPending || cb.IsFollowUpTransaction)
        {
            _logger.LogInformation(
                "Paymob callback ignored (pending={Pending}, followUp={FollowUp}). Tx={Tx}",
                cb.IsPending, cb.IsFollowUpTransaction, cb.ExternalEventId);
            return WebhookResult.Ignored;
        }

        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        // ── 3) تسجيل الحدث جوه نفس الـ transaction ──
        var marker = new ProcessedWebhook
        {
            Id = Guid.NewGuid(),
            Provider = ProviderName,
            ExternalEventId = cb.ExternalEventId,
            ReceivedAt = DateTime.UtcNow
        };
        _db.ProcessedWebhooks.Add(marker);

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            await tx.RollbackAsync(ct);

            // الـ UNIQUE index هو الحَكَم. نفرّق بين duplicate حقيقي وفشل تقني تاني:
            var alreadyProcessed = await _db.ProcessedWebhooks.AsNoTracking()
                .AnyAsync(x => x.Provider == ProviderName && x.ExternalEventId == cb.ExternalEventId, ct);

            if (alreadyProcessed)
            {
                _logger.LogInformation("Duplicate Paymob webhook ignored. Tx={Tx}", cb.ExternalEventId);
                return WebhookResult.Duplicate;
            }

            throw;   // فشل تقني → 500 → Paymob يعيد
        }

        // ── 4) نلاقي الأوردر ──
        var order = await FindOrderAsync(cb, ct);
        if (order is null)
        {
            marker.Outcome = "Order not found";
            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            _logger.LogCritical(
                "Paymob callback for unknown order. Tx={Tx} PaymobOrder={PaymobOrder} Merchant={Merchant} Success={Success}",
                cb.ExternalEventId, cb.GatewayOrderId, cb.MerchantOrderId, cb.Success);
            return WebhookResult.Processed;
        }

        marker.OrderId = order.Id;

        var payment = await _db.Payments
            .Where(p => p.OrderId == order.Id && p.PaymobOrderId == cb.GatewayOrderId)
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (payment is null)
        {
            payment = new Payment
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                Status = PaymentStatus.Pending,
                PaymobOrderId = cb.GatewayOrderId,
                CreatedAt = DateTime.UtcNow
            };
            _db.Payments.Add(payment);
        }

        payment.PaymobTransactionReference = cb.ExternalEventId;
        payment.AmountPaid = cb.AmountPaid;
        payment.CompletedAt = DateTime.UtcNow;

        // ── 5) الدفع فشل: الأوردر يفضل PendingPayment والعميل يعيد المحاولة ──
        if (!cb.Success)
        {
            payment.Status = PaymentStatus.Failed;
            payment.FailureReason = cb.FailureReason;
            marker.Outcome = "Payment failed";
            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return WebhookResult.Processed;
        }

        // ── 6) النجاح: مطابقة المبلغ والعملة ──
        var amountOk =
            Math.Abs(cb.AmountPaid - order.Total) <= 0.01m &&
            string.Equals(cb.Currency, ExpectedCurrency, StringComparison.OrdinalIgnoreCase);

        var target = amountOk ? OrderStatus.Paid : OrderStatus.PaymentMismatch;
        DateTime? paidAt = amountOk ? DateTime.UtcNow : null;

        // ★ compare-and-set: بيغيّر الحالة بس لو لسه PendingPayment.
        //   بيحمينا من: إلغاء العميل في نفس اللحظة، ومن callbackين لنفس الأوردر.
        var affected = await _db.Orders
            .Where(o => o.Id == order.Id && o.Status == OrderStatus.PendingPayment)
            .ExecuteUpdateAsync(s => s
                .SetProperty(o => o.Status, target)
                .SetProperty(o => o.PaidAt, paidAt), ct);

        payment.Status = PaymentStatus.Succeeded;   // الفلوس اتسحبت فعلًا في الحالتين

        if (affected == 0)
        {
            // فلوس اتسحبت على أوردر مش PendingPayment (اتلغى، أو اتدفع قبل كده بمحاولة تانية).
            // مفيش تغيير حالة، بس لازم حد يرجّع الفلوس.
            marker.Outcome = "Payment received for an order that is no longer PendingPayment — REFUND REQUIRED";
            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            _logger.LogCritical(
                "Money received for non-pending order {OrderNumber}. Tx={Tx} Amount={Amount}. Manual refund required.",
                order.OrderNumber, cb.ExternalEventId, cb.AmountPaid);
            return WebhookResult.Processed;
        }

        _db.OrderStatusHistories.Add(new OrderStatusHistory
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            Status = target,
            ChangedAt = DateTime.UtcNow,
            Note = amountOk
                ? $"Paymob transaction {cb.ExternalEventId}"
                : $"Expected {order.Total:0.00} {ExpectedCurrency} but received {cb.AmountPaid:0.00} {cb.Currency}"
        });

        // استهلاك الكوبون مرتبط بالدفع مش بالـ checkout. UNIQUE على CouponUsage.OrderId.
        if (amountOk && order.CouponId is { } couponId && order.DiscountAmount > 0)
        {
            _db.CouponUsages.Add(new CouponUsage
            {
                Id = Guid.NewGuid(),
                CouponId = couponId,
                CustomerId = order.CustomerId,
                OrderId = order.Id,
                UsedAt = DateTime.UtcNow
            });
        }

        marker.Outcome = amountOk ? "Paid" : "Amount mismatch";
        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        if (!amountOk)
        {
            _logger.LogError(
                "PAYMENT MISMATCH on order {OrderNumber}: expected {Expected}, got {Actual} {Currency}",
                order.OrderNumber, order.Total, cb.AmountPaid, cb.Currency);
            return WebhookResult.Processed;
        }

        // ── 7) الإشعار بعد الـ commit، وفشله ممنوع يأثر على أي حاجة ──
        await TryNotifyPaidAsync(order, ct);
        return WebhookResult.Processed;
    }

    /// <summary>أولًا special_reference (= Order.Id). لو مش موجود نرجع لـ Paymob order id المخزّن عندنا.</summary>
    private async Task<Order?> FindOrderAsync(GatewayCallback cb, CancellationToken ct)
    {
        if (Guid.TryParse(cb.MerchantOrderId, out var orderId))
        {
            var byRef = await _db.Orders.AsNoTracking().FirstOrDefaultAsync(o => o.Id == orderId, ct);
            if (byRef is not null) return byRef;
        }

        if (!string.IsNullOrEmpty(cb.GatewayOrderId))
        {
            return await _db.Orders.AsNoTracking()
                .FirstOrDefaultAsync(o => o.Payments.Any(p => p.PaymobOrderId == cb.GatewayOrderId), ct);
        }

        return null;
    }

    private async Task TryNotifyPaidAsync(Order order, CancellationToken ct)
    {
        try
        {
            var info = await _db.Orders.AsNoTracking()
                .Where(o => o.Id == order.Id)
                .Select(o => new { o.OrderNumber, RestaurantName = o.Restaurant.Name, o.Customer.UserId })
                .FirstAsync(ct);

            var contact = await _identity.GetContactAsync(info.UserId, ct);
            if (contact?.PhoneNumber is null) return;

            _notifications.Enqueue(new OrderNotification(
                contact.PhoneNumber, contact.FullName, info.OrderNumber,
                info.RestaurantName, OrderNotificationType.PaymentConfirmed));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not enqueue payment notification for order {OrderId}", order.Id);
        }
    }
}