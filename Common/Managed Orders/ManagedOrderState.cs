using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.ManagedOrders
{
    public enum ManagedOrderState
    {
        New,
        Registered,
        Submitted,
        Working,
        Canceled,
        Filled,
        PartiallyFilled,
        Error
    }
}
