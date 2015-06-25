using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuantConnect;
using QuantConnect.Orders;
using QuantConnect.Securities;

namespace QuantConnect.ManagedOrders
{
    public interface IExecutionRouter
    {
        SecurityTransactionManager Transactions { get; }

        event EventHandler<OrderEvent> OrderStatusChanged;

        int MarketOrder(string symbol, int quantity, bool asynchronous = false, string tag = "");

        int MarketOnOpenOrder(string symbol, int quantity, string tag = "");

        int MarketOnCloseOrder(string symbol, int quantity, string tag = "");

        int LimitOrder(string symbol, int quantity, decimal limitPrice, string tag = "");

        int StopMarketOrder(string symbol, int quantity, decimal stopPrice, string tag = "");

        int StopLimitOrder(string symbol, int quantity, decimal stopPrice, decimal limitPrice, string tag = "");
    }
}
