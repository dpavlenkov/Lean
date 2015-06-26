using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.ManagedOrders
{
    public class ManagedOrderStopMarket :
        ManagedOrderBase
    {
        public ManagedOrderStopMarket(
            IExecutionRouter router,
            string symbol,
            int quantity,
            decimal stopPrice)
            : base(router, symbol, quantity)
        {
            this.StopPrice = stopPrice;
        }

        public decimal StopPrice
        {
            get;
            private set;
        }

        protected override int SubmitInternal()
        {
            return ExecutionRouter.StopMarketOrder(Symbol, Quantity, StopPrice, Tag);
        }
    }
}
