using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuantConnect.Orders;

namespace QuantConnect.ManagedOrders
{
    public class ManagedOrder
    {
        Guid Id;
        Guid? AttachedToId;

        Guid[] OneCancelsAllGroupIds;

        Order UnderlyingOrder;

    }
}
