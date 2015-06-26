using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.ManagedOrders
{
    public class ManagedOrderRequestBase
        : IManagedOrderRequest
    {
        public ManagedOrderRequestBase(IManagedOrder managedOrder)
        {
            this.ManagedOrder = managedOrder;
        }

        public IManagedOrder ManagedOrder { get; private set; }
    }
}
