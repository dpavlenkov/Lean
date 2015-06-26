using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using QuantConnect;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Orders;

namespace QuantConnect.ManagedOrders
{
    public class ManagedOrderService : IManagedOrderService
    {
        private ConcurrentQueue<IManagedOrderRequest> requestQueue;
        private ConcurrentQueue<Tuple<OrderEvent, IExecutionRouter>> orderEventQueue;
        private ConcurrentQueue<object> dataQueue;
        private ConcurrentDictionary<Guid, HashSet<IManagedOrder>> attachedToLookup;
        private ConcurrentDictionary<Guid, HashSet<IManagedOrder>> ocaGroupLookup;

        private int timerMilliseconds = 1000;
        private System.Timers.Timer timer;
        private long requestCount;
        private object requestCounterLock = new object();
        private Mutex runMutex = new Mutex();

        public ManagedOrderService()
        {
            requestQueue = new ConcurrentQueue<IManagedOrderRequest>();
            dataQueue = new ConcurrentQueue<object>();
            orderEventQueue = new ConcurrentQueue<Tuple<OrderEvent, IExecutionRouter>>();
            attachedToLookup = new ConcurrentDictionary<Guid, HashSet<IManagedOrder>>();
            ocaGroupLookup = new ConcurrentDictionary<Guid, HashSet<IManagedOrder>>();

            ErrorMessages = new List<string>();

            timer = new System.Timers.Timer(timerMilliseconds);
            timer.AutoReset = false;
            timer.Elapsed += Timer_Elapsed;
        }

        public List<string> ErrorMessages
        {
            get;
            private set;
        }

        public void Submit(params IManagedOrder[] managedOrders)
        {
            foreach (var managedOrder in managedOrders)
            {
                TryAttachToId(managedOrder);
                TryJoinOcaGroup(managedOrder);

                requestQueue.Enqueue(new ManagedOrderSubmitRequest(managedOrder));
            }

            Run();
        }

        private void LogException(Exception ex)
        {
            ErrorMessages.Add(String.Format("{0} {1}", ex.Message, ex.StackTrace));
        }

        private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            Run();
        }

        private async void Run()
        {
            bool lockAcquired = false;

            lock(requestCounterLock)
            {
                lockAcquired = runMutex.WaitOne(0);

                if (lockAcquired)
                {
                    Interlocked.Exchange(ref requestCount, 0);
                }
                else
                {
                    if (Interlocked.Read(ref requestCount) > 0)
                    {
                        return;
                    }

                    Interlocked.Increment(ref requestCount);
                }
            }

            if (lockAcquired == false)
                lockAcquired = runMutex.WaitOne();

            if(lockAcquired)
            {
                lock (requestCounterLock)
                {
                    if (Interlocked.Read(ref requestCount) > 0)
                        Interlocked.Decrement(ref requestCount);
                }

                try
                {
                    ProcessOrderEvents();
                    ProcessManagedOrderRequests();
                    ProcessData();
                }
                catch (Exception ex)
                {
                    LogException(ex);
                }
                finally
                {
                    try
                    {
                        timer.Reset();
                    }
                    finally
                    {
                        runMutex.ReleaseMutex();
                    }
                }
            }
        }

        private void ProcessOrderEvents()
        {

        }

        private void ProcessManagedOrderRequests()
        {

        }

        private void ProcessData()
        {

        }

        private void TryJoinOcaGroup(IManagedOrder managedOrder)
        {
            foreach (var groupId in managedOrder.OCAGroups)
            {
                HashSet<IManagedOrder> set;
                if (ocaGroupLookup.TryGetValue(groupId, out set) == false)
                {
                    set = new HashSet<IManagedOrder>();
                    ocaGroupLookup[groupId]=set;
                }

                set.Add(managedOrder);
            }
        }

        private void TryAttachToId(IManagedOrder managedOrder)
        {
            if (managedOrder.AttachedToId.HasValue == false)
                return;

            var id = managedOrder.AttachedToId.Value;

            HashSet<IManagedOrder> set;
            if (attachedToLookup.TryGetValue(id, out set) == false)
            {
                set = new HashSet<IManagedOrder>();
                attachedToLookup[id] = set;
            }

            set.Add(managedOrder);
        }
        
        private void OnOrderEvent(OrderEvent orderEvent, IExecutionRouter executionRouter)
        {
            orderEventQueue.Enqueue(new Tuple<OrderEvent, IExecutionRouter>(orderEvent, executionRouter));

            Run();
        }

        public void OnData(TradeBars data)
        {
            dataQueue.Enqueue(data);

            Run();
        }

        public void OnData(Ticks data)
        {
            dataQueue.Enqueue(data);

            Run();
        }
    }
}
