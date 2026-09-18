using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RestaurantOrderingPlatform.Domain.Enums
{
    public enum OrderStatus
    {
        PendingPayment = 0,
        Paid,
        Accepted,
        Preparing,
        Ready,
        DriverAssigned,
        PickedUp,
        OnTheWay,
        Delivered,        // terminal (happy path)

        Rejected,
        RefundPending,
        Refunded,         // terminal (rejection branch)

        PaymentMismatch,  // Admin review required

        Cancelled,        // NEW — العميل ألغى الأوردر قبل الدفع
        Expired           // NEW — الأوردر فضل PendingPayment أكتر من مدة معينة
    }

}
