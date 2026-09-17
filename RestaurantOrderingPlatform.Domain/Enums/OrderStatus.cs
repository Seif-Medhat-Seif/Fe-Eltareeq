using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RestaurantOrderingPlatform.Domain.Enums
{
    public enum OrderStatus
    {
        PendingPayment,
        Paid,
        PaymentMismatch,
        Accepted,
        Rejected,
        RefundPending,
        Refunded,
        Preparing,
        Ready,
        DriverAssigned,
        PickedUp,
        OnTheWay,
        Delivered
    }

}
