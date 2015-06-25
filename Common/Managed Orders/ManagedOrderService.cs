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

        private int timerMilliseconds = 1000;
        private System.Timers.Timer timer;
        private object syncLock = new object();
        private long runRequests;

        public ManagedOrderService()
        {
            requestQueue = new ConcurrentQueue<IManagedOrderRequest>();
            dataQueue = new ConcurrentQueue<object>();
            orderEventQueue = new ConcurrentQueue<Tuple<OrderEvent, IExecutionRouter>>();

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
            if (Interlocked.Read(ref runRequests) > 1)
                return;

            Interlocked.Increment(ref runRequests);

            lock (syncLock)
            {
                if (Interlocked.Decrement(ref runRequests) > 0)
                    return;
                
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
                    timer.Reset();
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
