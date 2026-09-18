using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RestaurantOrderingPlatform.Domain.Enums
{
    public enum PaymentStatus
    {
        Initiated,
        Succeeded,
        Pending,
        Failed
    }
}
