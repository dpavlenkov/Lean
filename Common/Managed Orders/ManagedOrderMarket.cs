using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.ManagedOrders
{
    public class ManagedOrderMarket :
        ManagedOrderBase
    {
        public ManagedOrderMarket(
            IExecutionRouter router,
            string symbol,
            int quantity)
            : base(router, symbol, quantity) { }

        protected override int SubmitInternal()
        {
            return ExecutionRouter.MarketOrder(Symbol, Quantity, true, Tag);
        }
    }
}
