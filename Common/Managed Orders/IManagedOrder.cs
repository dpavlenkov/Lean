using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuantConnect.Orders;

namespace QuantConnect.ManagedOrders
{
    public interface IManagedOrder
    {
        Guid Id { get; }
        ManagedOrderState State { get; }
        ManagedOrderRequestState RequestState { get; }
        string Symbol { get; }
        string Tag { get; }
        int UnderlyingOrderId { get; }
        IExecutionRouter ExecutionRouter { get; }
        Guid? AttachedToId { get; }
        Guid[] OCAGroups { get; }
        void Submit();
        void Cancel();
        void AttachToId(Guid? managedOrderId);
        void JoinOCAGroup(Guid groupId);
        void LeaveOCAGroup(Guid groupId);
        void Process(OrderEvent orderEvent);
    }

    public static class ManagedOrderEx
    {
        public static bool AttachedOrdersNeedCancel(this IManagedOrder parentOrder)
        {
            return parentOrder.State.IsCanceled()
                || parentOrder.RequestState == ManagedOrderRequestState.Canceling;
        }

        public static bool CanSubmit(this IManagedOrder managedOrder)
        {
            return managedOrder.State == ManagedOrderState.New;
        }
    }
}
