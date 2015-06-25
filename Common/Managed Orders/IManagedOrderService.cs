using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuantConnect;
using QuantConnect.Data.Market;

namespace QuantConnect.ManagedOrders
{
    public interface IManagedOrderService
    {
        void OnData(TradeBars data);
        void OnData(Ticks data);
    }
}
