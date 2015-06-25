using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.ManagedOrders
{
    public enum ManagedOrderRequestState
    {
        None,
        SubmitRequested,
        Submitting,
        AmendRequested,
        Amending,
        CancelRequested,
        Canceling
    }
}
