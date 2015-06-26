using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.ManagedOrders
{
    public class ManagedOrderLimit :
        ManagedOrderBase
    {
        public ManagedOrderLimit(
            IExecutionRouter router,
            string symbol,
            int quantity,
            decimal limitPrice)
            : base(router, symbol, quantity)
        {
            this.LimitPrice = limitPrice;
        }

        public decimal LimitPrice
        {
            get;
            private set;
        }

        protected override int SubmitInternal()
        {
            return ExecutionRouter.LimitOrder(Symbol, Quantity, LimitPrice, Tag);
        }
    }
}
