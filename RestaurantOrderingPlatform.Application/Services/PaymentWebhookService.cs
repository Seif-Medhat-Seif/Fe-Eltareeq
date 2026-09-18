// Application/Services/PaymentWebhookService.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RestaurantOrderingPlatform.Application.Common.Interfaces;
using RestaurantOrderingPlatform.Application.Interfaces;
using RestaurantOrderingPlatform.Domain.Entities;
using RestaurantOrderingPlatform.Domain.Enums;

namespace RestaurantOrderingPlatform.Application.Services;

/// <summary>
/// ده أخطر class في المشروع كله. اقرأ القواعد الأربعة قبل ما تلمسه:
///
/// 1) مبنثقش في أي حاجة جاية من الفرونت — إحنا بنثق في التوقيع (HMAC) بس.
/// 2) الـ handler ده لازم يبقى IDEMPOTENT: Paymob ممكن يبعت نفس الحدث 2 أو 5 مرات.
///    نفس الحدث مرتين لازم ينتج نفس النتيجة بالظبط، من غير أوردر يتأكد مرتين
///    ولا واتساب يتبعت مرتين.
/// 3) الـ endpoint بيرجع 200 دايمًا — حتى لو فشلنا داخليًا. لو رجّعنا 500،
///    Paymob هيفضل يعيد المحاولة (retry storm) وهيضاعف المشكلة.
/// 4) مفيش exception بيخرج من هنا للـ Controller.
/// </summary>
public class PaymentWebhookService : IPaymentWebhookService
{
    private const string ProviderName = "Paymob";

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

    public async Task HandlePaymobCallbackAsync(string rawJsonBody, string? hmacFromQuery, CancellationToken ct = default)
    {
        // ══ STEP 1: التوقيع ══════════════════════════════════════════════
        // أي حد يعرف الـ URL بتاعنا يقدر يبعتلنا JSON فيه "success": true.
        // الـ HMAC هو الحاجة الوحيدة اللي بتفرّق بين Paymob وبين مهاجم.
        if (!_gateway.TryParseCallback(rawJsonBody, hmacFromQuery, out var cb))
        {
            _logger.LogWarning("Paymob webhook rejected: invalid or missing HMAC signature.");
            return;   // 200 بره، بس مفيش أي تأثير
        }

        // ══ STEP 2: الـ Idempotency Guard ════════════════════════════════
        // الغلطة الشائعة هنا:
        //     if (await _db.ProcessedWebhooks.AnyAsync(...)) return;
        //     ... process ...
        //     _db.ProcessedWebhooks.Add(...)
        // ده race condition جاهز: لو نسختين من الـ webhook وصلوا في نفس اللحظة،
        // الاتنين هيعدّوا الـ AnyAsync قبل ما أي واحد فيهم يكتب.
        //
        // الحل الصح: نحاول نكتب الأول. الـ UNIQUE INDEX على
        // (Provider, ExternalEventId) هو الحَكَم. لو فشل الـ INSERT →
        // يبقى في نسخة تانية سبقتنا → نخرج بهدوء.
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
            _logger.LogInformation(
                "Duplicate Paymob webhook ignored. TransactionId={TransactionId}", cb.ExternalEventId);
            return;
        }

        // من هنا وطالع: إحنا الوحيدين اللي بنعالج الحدث ده، مضمون.

        // ══ STEP 3: نلاقي الأوردر ════════════════════════════════════════
        if (!Guid.TryParse(cb.MerchantOrderId, out var orderId))
        {
            marker.Outcome = "Unparseable merchant order id";
            await _db.SaveChangesAsync(ct);
            _logger.LogError("Paymob webhook had no usable merchant order id. Tx={Tx}", cb.ExternalEventId);
            return;
        }

        var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == orderId, ct);
        if (order is null)
        {
            marker.Outcome = "Order not found";
            await _db.SaveChangesAsync(ct);
            _logger.LogError("Paymob webhook for unknown order {OrderId}", orderId);
            return;
        }

        marker.OrderId = order.Id;

        // نلاقي الـ Payment row المناسبة (أو نعمل واحدة لو الـ webhook سبق الـ intent)
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

        // ══ STEP 4: الدفع فشل ════════════════════════════════════════════
        if (!cb.Success)
        {
            payment.Status = PaymentStatus.Failed;
            payment.FailureReason = cb.FailureReason;
            marker.Outcome = "Payment failed";
            await _db.SaveChangesAsync(ct);
            // الأوردر بيفضل PendingPayment — العميل يقدر يحاول تاني. مفيش تغيير حالة.
            return;
        }

        // ══ STEP 5: الأوردر ممكن يكون اتعالج خلاص (نفس الحدث بمعرّف مختلف) ══
        if (order.Status != OrderStatus.PendingPayment)
        {
            payment.Status = PaymentStatus.Succeeded;
            marker.Outcome = $"Order already in {order.Status}";
            await _db.SaveChangesAsync(ct);
            _logger.LogWarning(
                "Paymob success webhook for order {OrderId} already in status {Status}", order.Id, order.Status);
            return;
        }

        // ══ STEP 6: مطابقة المبلغ ════════════════════════════════════════
        // ده مش paranoia. لو حد عدّل المبلغ في الـ iframe، أو لو بق عندنا في
        // حساب الـ total، المفروض مانأكدش الأوردر تلقائيًا أبدًا.
        // بنسمح بفرق 1 قرش عشان تقريب القروش↔الجنيه.
        var difference = Math.Abs(cb.AmountPaid - order.Total);

        if (difference > 0.01m)
        {
            payment.Status = PaymentStatus.Succeeded;   // الفلوس اتدفعت فعلًا
            order.Status = OrderStatus.PaymentMismatch;

            _db.OrderStatusHistories.Add(new OrderStatusHistory
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                Status = OrderStatus.PaymentMismatch,
                ChangedAt = DateTime.UtcNow,
                Note = $"Expected {order.Total:0.00} but received {cb.AmountPaid:0.00} {cb.Currency}"
            });

            marker.Outcome = "Amount mismatch";
            await _db.SaveChangesAsync(ct);

            _logger.LogError(
                "PAYMENT MISMATCH on order {OrderNumber}: expected {Expected}, got {Actual}",
                order.OrderNumber, order.Total, cb.AmountPaid);
            return;
        }

        // ══ STEP 7: النجاح — كل حاجة في transaction واحدة ════════════════
        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            payment.Status = PaymentStatus.Succeeded;
            order.Status = OrderStatus.Paid;
            order.PaidAt = DateTime.UtcNow;

            _db.OrderStatusHistories.Add(new OrderStatusHistory
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                Status = OrderStatus.Paid,
                ChangedAt = DateTime.UtcNow,
                Note = $"Paymob transaction {cb.ExternalEventId}"
            });

            // ★ استهلاك الكوبون بيحصل هنا، مش وقت الـ Checkout.
            //   ليه؟ لأن أوردر اتعمل ومتدفعش (abandoned) مالوش الحق يستهلك
            //   من حصة الكوبون. الاستهلاك مرتبط بالدفع، مش بالنية.
            //   والـ UNIQUE INDEX على CouponUsage.OrderId بيضمن إن نفس الأوردر
            //   مستحيل يسجّل استخدامين حتى لو الكود ده اتنفذ مرتين بأي طريقة.
            if (order.CouponId is { } couponId && order.DiscountAmount > 0)
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

            marker.Outcome = "Paid";
            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(ct);
            _logger.LogError(ex, "Failed to finalize payment for order {OrderId}", order.Id);
            return;   // الـ marker اتكتب في transaction منفصلة قبل كده — مقصود:
                      // أحسن نفقد webhook مرة واحدة من إننا نعالجه مرتين.
        }

        // ══ STEP 8: الإشعار — بره الـ transaction، بعد الـ commit ═════════
        // لو حطيته جوه الـ transaction وفشل، هيرجّع أوردر مدفوع لحالته الأولى.
        await SendPaymentConfirmedAsync(order, ct);
    }

    private async Task SendPaymentConfirmedAsync(Order order, CancellationToken ct)
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
            // فشل إشعار ممنوع يأثر على حاجة — NFR.
            _logger.LogWarning(ex, "Could not enqueue payment notification for order {OrderId}", order.Id);
        }
    }
}
