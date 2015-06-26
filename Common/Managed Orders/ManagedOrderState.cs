using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuantConnect.Orders;

namespace QuantConnect.ManagedOrders
{
    public enum ManagedOrderState
    {
        New,
        Submitted,
        Working,
        Canceled,
        Filled,
        PartiallyFilled,
        Error
    }

    public static class ManagedOrderStateEx
    {
        public static bool IsOpen(this ManagedOrderState source)
        {
            return source == ManagedOrderState.New
                || source == ManagedOrderState.PartiallyFilled
                || source == ManagedOrderState.Submitted
                || source == ManagedOrderState.Working;
        }

        public static bool AllowsCancel(this OrderStatus source)
        {
            return source == OrderStatus.New
                || source == OrderStatus.None
                || source == OrderStatus.Submitted
                || source == OrderStatus.Update;
        }
    }
}
