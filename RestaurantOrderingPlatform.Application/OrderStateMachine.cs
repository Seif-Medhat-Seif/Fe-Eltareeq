// Application/OrderStateMachine.cs
using RestaurantOrderingPlatform.Domain.Enums;

namespace RestaurantOrderingPlatform.Application;

/// <summary>
/// مصدر الحقيقة الوحيد لانتقالات حالة الأوردر.
///
/// ليه class ثابت مش if-statements متناثرة في الـ Service؟
/// لأن الـ transitions بتتلمس من 4 أماكن مختلفة (webhook, restaurant, driver, admin).
/// لو كل مكان عامل الـ check بتاعه، أول ما تضيف حالة جديدة هتنسى مكان منهم.
/// هنا مكان واحد، وModule 7 (Driver) هيستخدم نفس الـ class من غير ما يكرر حاجة.
/// </summary>
public static class OrderStateMachine
{
    private static readonly Dictionary<OrderStatus, OrderStatus[]> Allowed = new()
    {
        [OrderStatus.PendingPayment] = new[]
        {
            OrderStatus.Paid,
            OrderStatus.PaymentMismatch,
            OrderStatus.Cancelled,
            OrderStatus.Expired
        },
        [OrderStatus.PaymentMismatch] = new[]
        {
            OrderStatus.Paid,            // Admin: ConfirmPaid
            OrderStatus.PendingPayment   // Admin: MarkFailed → يسيب العميل يعيد الدفع
        },
        [OrderStatus.Paid]           = new[] { OrderStatus.Accepted, OrderStatus.Rejected },
        [OrderStatus.Accepted]       = new[] { OrderStatus.Preparing },
        [OrderStatus.Preparing]      = new[] { OrderStatus.Ready },
        [OrderStatus.Ready]          = new[] { OrderStatus.DriverAssigned },
        [OrderStatus.DriverAssigned] = new[] { OrderStatus.PickedUp },
        [OrderStatus.PickedUp]       = new[] { OrderStatus.OnTheWay },
        [OrderStatus.OnTheWay]       = new[] { OrderStatus.Delivered },

        [OrderStatus.Rejected]      = new[] { OrderStatus.RefundPending },
        [OrderStatus.RefundPending] = new[] { OrderStatus.Refunded },

        // Terminal
        [OrderStatus.Delivered] = Array.Empty<OrderStatus>(),
        [OrderStatus.Refunded]  = Array.Empty<OrderStatus>(),
        [OrderStatus.Cancelled] = Array.Empty<OrderStatus>(),
        [OrderStatus.Expired]   = Array.Empty<OrderStatus>(),
    };

    public static bool CanTransition(OrderStatus from, OrderStatus to)
        => Allowed.TryGetValue(from, out var next) && next.Contains(to);

    public static bool IsTerminal(OrderStatus status)
        => Allowed.TryGetValue(status, out var next) && next.Length == 0;

    public static IReadOnlyList<OrderStatus> NextStates(OrderStatus from)
        => Allowed.TryGetValue(from, out var next) ? next : Array.Empty<OrderStatus>();
}
