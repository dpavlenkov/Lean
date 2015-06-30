﻿/*
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

namespace QuantConnect.Orders
{
    /// <summary>
    /// Stop Market Order Type Definition
    /// </summary>
    public class StopLimitOrder : Order
    {
        /// <summary>
        /// Stop price for this stop market order.
        /// </summary>
        public decimal StopPrice;

        /// <summary>
        /// Signal showing the "StopLimitOrder" has been converted into a Limit Order
        /// </summary>
        public bool StopTriggered = false;

        /// <summary>
        /// Limit price for the stop limit order
        /// </summary>
        public decimal LimitPrice;

        /// <summary>
        /// Maximum value of the order at is the stop limit price
        /// </summary>
        public override decimal Value
        {
            get { return Quantity*LimitPrice; }
        }

        /// <summary>
        /// Create update request for pending orders. Null values will be ignored.
        /// </summary>
        public UpdateOrderRequest UpdateRequest(int? quantity = null, decimal? stopPrice = null, decimal? limitPrice = null, string tag = null)
        {
            return new UpdateOrderRequest
            {
                Id = Guid.NewGuid(),
                OrderId = Id,
                Created = DateTime.Now,
                Quantity = quantity ?? Quantity,
                LimitPrice = limitPrice ?? LimitPrice,
                StopPrice = stopPrice ?? StopPrice,
                Tag = tag ?? Tag
            };
        }

        /// <summary>
        /// Apply changes after the update request is processed.
        /// </summary>
        public override void ApplyUpdate(UpdateOrderRequest request)
        {
            base.ApplyUpdate(request);

            LimitPrice = request.LimitPrice;
            StopPrice = request.StopPrice;
        }

        /// <summary>
        /// Create submit request.
        /// </summary>
        public static SubmitOrderRequest SubmitRequest(string symbol, int quantity, decimal stopPrice, decimal limitPrice, string tag, SecurityType securityType, decimal price = 0, DateTime? time = null)
        {
            return new SubmitOrderRequest
            {
                Id = Guid.NewGuid(),
                Symbol = symbol,
                Quantity = quantity,
                Tag = tag,
                SecurityType = securityType,
                Created = time ?? DateTime.Now,
                StopPrice = stopPrice,
                LimitPrice = limitPrice,
                Type = OrderType.StopLimit
            };
        }

        /// <summary>
        /// Copy order before submitting to broker for update.
        /// </summary>
        public override Order Copy()
        {
            var target = new StopLimitOrder();
            CopyTo(target);
            target.StopPrice = StopPrice;
            target.LimitPrice = LimitPrice;
            target.StopTriggered = StopTriggered;

            return target;
        }

        /// <summary>
        /// Default constructor for JSON Deserialization:
        /// </summary>
        public StopLimitOrder()
            : base(OrderType.StopLimit)
        {
        }

        /// <summary>
        /// New Stop Market Order constructor - 
        /// </summary>
        /// <param name="symbol">Symbol asset we're seeking to trade</param>
        /// <param name="type">Type of the security order</param>
        /// <param name="quantity">Quantity of the asset we're seeking to trade</param>
        /// <param name="limitPrice">Maximum price to fill the order</param>
        /// <param name="time">Time the order was placed</param>
        /// <param name="stopPrice">Price the order should be filled at if a limit order</param>
        /// <param name="tag">User defined data tag for this order</param>
        public StopLimitOrder(string symbol, int quantity, decimal stopPrice, decimal limitPrice, DateTime time, string tag = "", SecurityType type = SecurityType.Base) :
            base(symbol, quantity, OrderType.StopLimit, time, limitPrice, tag, type)
        {
            StopPrice = stopPrice;
            LimitPrice = limitPrice;

            if (tag == "")
            {
                //Default tag values to display stop price in GUI.
                Tag = "Stop Price: " + stopPrice.ToString("C") + " Limit Price: " + limitPrice.ToString("C");
            }
        }

        /// <summary>
        /// Intiializes a new instance of the <see cref="StopLimitOrder"/> class.
        /// </summary>
        /// <param name="request">Submit order request.</param>
        public StopLimitOrder(SubmitOrderRequest request) :
            base(request)
        {
            StopPrice = request.StopPrice;
            LimitPrice = request.LimitPrice;

            if (Tag == "")
            {
                //Default tag values to display stop price in GUI.
                Tag = "Stop Price: " + StopPrice.ToString("C") + " Limit Price: " + LimitPrice.ToString("C");
            }
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>
        /// A string that represents the current object.
        /// </returns>
        /// <filterpriority>2</filterpriority>
        public override string ToString()
        {
            return string.Format("{0} order for {1} unit{2} of {3} at stop {4} limit {5}", Type, Quantity, Quantity == 1 ? "" : "s", Symbol, StopPrice, LimitPrice);
        }
    }
}