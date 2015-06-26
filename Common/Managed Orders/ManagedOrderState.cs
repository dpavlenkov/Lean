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
                || source == ManagedOrderState.Submitted
                || source == ManagedOrderState.Working;
        }

        public static bool IsFilled(this ManagedOrderState source)
        {
            return source == ManagedOrderState.Filled
                || source == ManagedOrderState.PartiallyFilled;
        }

        public static bool IsCanceled(this ManagedOrderState source)
        {
            return source == ManagedOrderState.Canceled
                || source == ManagedOrderState.Error;
        }

        public static bool AllowsCancel(this ManagedOrderState source)
        {
            return source == ManagedOrderState.New
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
