/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License"); 
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/
using System;
using System.Linq;
using QuantConnect.Data.Market;
using QuantConnect.ManagedOrders;
using QuantConnect.Orders;

namespace QuantConnect.Algorithm.Examples
{
    /// <summary>
    /// Basic template algorithm simply initializes the date range and cash
    /// </summary>
    public class BasicManagedOrdersTemplateAlgorithm : QCAlgorithm,
        IExecutionRouter
    {
        private IManagedOrderService managedOrderService;
        private string symbol = "SPY";
        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            managedOrderService = new ManagedOrderService();

            SetStartDate(2013, 10, 07);  //Set Start Date
            SetEndDate(2013, 10, 11);    //Set End Date
            SetCash(100000);             //Set Strategy Cash
            // Find more symbols here: http://quantconnect.com/data
            AddSecurity(SecurityType.Equity, symbol, Resolution.Hour);
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">TradeBars IDictionary object with your stock data</param>
        /// 

        private IManagedOrder[] GenerateManagedOrders(int quantity, TradeBar data)
        {
            if (quantity > 0)
            {
                var entryOrder = new ManagedOrderLimit(this, symbol, quantity, Math.Round(data.Low, 2)) { Tag = "Low Entry" };
                var slOrder = new ManagedOrderStopMarket(this, symbol, -quantity, Math.Round(data.Low + data.Low - data.High, 2)) { Tag = "Low SL" };
                var tpOrder = new ManagedOrderLimit(this, symbol, -quantity, Math.Round(data.High, 2)) { Tag = "Low TP" };

                slOrder.AttachToId(entryOrder.Id);
                tpOrder.AttachToId(entryOrder.Id);
                var ocaGroupid = Guid.NewGuid();
                slOrder.JoinOCAGroup(ocaGroupid);
                tpOrder.JoinOCAGroup(ocaGroupid);

                Log(string.Format("Bracket order Quantity {3} Low Entry {0} SL {1} TP {2}", entryOrder.LimitPrice, slOrder.StopPrice, tpOrder.LimitPrice, quantity));

                return new IManagedOrder[] { entryOrder, slOrder, tpOrder };
            }
            if (quantity < 0)
            {
                var entryOrder = new ManagedOrderLimit(this, symbol, quantity, Math.Round(data.High, 2)) { Tag = "High Entry" };
                var slOrder = new ManagedOrderStopMarket(this, symbol, -quantity, Math.Round(data.High + data.High - data.Low, 2)) { Tag = "High SL" };
                var tpOrder = new ManagedOrderLimit(this, symbol, -quantity, Math.Round(data.Low, 2)) { Tag = "High TP" };

                slOrder.AttachToId(entryOrder.Id);
                tpOrder.AttachToId(entryOrder.Id);
                var ocaGroupid = Guid.NewGuid();
                slOrder.JoinOCAGroup(ocaGroupid);
                tpOrder.JoinOCAGroup(ocaGroupid);

                Log(string.Format("Bracket order Quantity {3} High Entry {0} SL {1} TP {2}", entryOrder.LimitPrice, slOrder.StopPrice, tpOrder.LimitPrice, quantity));

                return new IManagedOrder[] { entryOrder, slOrder, tpOrder };
            }

            return null;
        }

        public void OnData(TradeBars data)
        {
            var highOrders = GenerateManagedOrders(-10, data[symbol]);
            var lowOrders = GenerateManagedOrders(10, data[symbol]);

            var ocaGroupId = Guid.NewGuid();

            highOrders[0].JoinOCAGroup(ocaGroupId);
            lowOrders[0].JoinOCAGroup(ocaGroupId);

            managedOrderService.OnData(data);

            managedOrderService.Submit(highOrders.Concat(lowOrders).ToArray());
        }

        public void OnData(Ticks data)
        {
            managedOrderService.OnData(data);
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            base.OnOrderEvent(orderEvent);

            var copy = OrderStatusChanged;

            if (copy != null)
                copy(this, orderEvent);
        }

        public event EventHandler<OrderEvent> OrderStatusChanged;
    }
}