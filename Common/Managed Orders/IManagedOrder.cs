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
        ManagedOrderState State { get; }
        ManagedOrderRequestState RequestState { get; }
        Order UnderlyingOrder { get; }
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
}
