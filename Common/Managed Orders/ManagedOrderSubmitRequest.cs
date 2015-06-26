﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.ManagedOrders
{
    public class ManagedOrderSubmitRequest : ManagedOrderRequestBase
    {
        public ManagedOrderSubmitRequest(IManagedOrder managedOrder)
            : base(managedOrder) { }
    }
}
