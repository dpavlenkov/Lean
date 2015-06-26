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
        private ConcurrentDictionary<IExecutionRouter, ConcurrentDictionary<int, IManagedOrder>> orderManagedOrderLookup;
        private ConcurrentDictionary<Guid, IManagedOrder> managedOrderIdLookup;

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
            orderManagedOrderLookup = new ConcurrentDictionary<IExecutionRouter, ConcurrentDictionary<int, IManagedOrder>>();
            managedOrderIdLookup = new ConcurrentDictionary<Guid, IManagedOrder>();

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
            foreach (var managedOrder in managedOrders.OrderBy(o => o.AttachedToId.HasValue ? 1 : 0))
            {
                TryAttachToId(managedOrder);
                TryJoinOcaGroup(managedOrder);
                managedOrderIdLookup[managedOrder.Id] = managedOrder;

                requestQueue.Enqueue(new ManagedOrderSubmitRequest(managedOrder));
            }

            RunSync();
        }

        public void OnData(TradeBars data)
        {
            dataQueue.Enqueue(data);

            RunSync();
        }

        public void OnData(Ticks data)
        {
            dataQueue.Enqueue(data);

            RunSync();
        }

        private void LogException(Exception ex)
        {
            ErrorMessages.Add(String.Format("{0} {1}", ex.Message, ex.StackTrace));
        }

        private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            RunSync();
        }

        private void RunSync()
        {
            Task.Run(() => Run()).Wait();
        }

        private void Run()
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
                    ProcessData();
                    ProcessManagedOrderRequests();
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
            var remainingCount = orderEventQueue.Count;

            while (remainingCount-- > 0)
            {
                try
                {
                    Tuple<OrderEvent, IExecutionRouter> orderEventPair;

                    if (orderEventQueue.TryDequeue(out orderEventPair) == false)
                        break;

                    ProcessOrderEvent(orderEventPair);
                }
                catch(Exception ex)
                {
                    LogException(ex);
                }
            }
        }

        private void ProcessOrderEvent(Tuple<OrderEvent, IExecutionRouter> orderEventPair)
        {
            var orderEvent = orderEventPair.Item1;
            var executionRouter = orderEventPair.Item2;
            var managedOrder = orderManagedOrderLookup[executionRouter][orderEvent.OrderId];

            managedOrder.Process(orderEvent);

            CheckOcaCondition(managedOrder);
            CheckAttachedOrdersCondition(managedOrder);
        }

        private void CheckOcaCondition(IManagedOrder managedOrder)
        {
            foreach (var ocaGroupId in managedOrder.OCAGroups)
            {
                HashSet<IManagedOrder> set;
                if (ocaGroupLookup.TryGetValue(ocaGroupId, out set) == true)
                {
                    if (managedOrder.State.IsFilled())
                    {
                        foreach (var orderToCancel in set.Where(o => o.State.AllowsCancel()))
                        {
                            orderToCancel.Cancel();
                        }

                        ocaGroupLookup.TryRemove(ocaGroupId, out set);
                    }
                    else if (managedOrder.State.IsCanceled())
                    {
                        set.Remove(managedOrder);
                    }
                }
                else
                {
                    managedOrder.Cancel();
                }
            }
        }

        private void CheckAttachedOrdersCondition(IManagedOrder parentOrder, bool submitAsRequest = true)
        {
            if (parentOrder.State.IsFilled() || parentOrder.AttachedOrdersNeedCancel())
            {
                HashSet<IManagedOrder> attachedOrders;
                if (attachedToLookup.TryRemove(parentOrder.Id, out attachedOrders) == true)
                {
                    foreach (var attachedOrder in attachedOrders)
                    {
                        if (parentOrder.State.IsFilled())
                        {
                            if (submitAsRequest == true)
                            {
                                requestQueue.Enqueue(new ManagedOrderSubmitRequest(attachedOrder));
                            }
                            else
                            {
                                attachedOrder.Submit();
                            }
                        }
                        else if (parentOrder.AttachedOrdersNeedCancel())
                        {
                            attachedOrder.Cancel();
                        }
                    }
                }
            }
        }

        private void CheckAttachedOrderNeedCancelCondition(IManagedOrder attachedOrder)
        {
            if (attachedOrder.AttachedToId.HasValue == false)
                return;

            var parentOrderId = attachedOrder.AttachedToId.Value;

            IManagedOrder parentOrder;

            if (managedOrderIdLookup.TryGetValue(parentOrderId, out parentOrder) == true)
            {
                if (parentOrder.AttachedOrdersNeedCancel() == true)
                {
                    attachedOrder.Cancel();
                }
            }
        }

        private bool CanSubmitNew(IManagedOrder managedOrder)
        {
            if (managedOrder.AttachedToId.HasValue == false)
                return true;

            var parentOrderId = managedOrder.AttachedToId.Value;
            IManagedOrder parentOrder;

            if (managedOrderIdLookup.TryGetValue(parentOrderId, out parentOrder) == true)
            {
                return (parentOrder.State.IsFilled());
            }

            return false;
        }

        private void ProcessManagedOrderRequests()
        {
            var remainingCount = requestQueue.Count;

            while (remainingCount-- > 0)
            {
                try
                {
                    IManagedOrderRequest request;

                    if (requestQueue.TryDequeue(out request) == false)
                        break;

                    ProcessManagedOrderRequest(request);
                }
                catch (Exception ex)
                {
                    LogException(ex);
                }
            }
        }

        private void ProcessManagedOrderRequest(IManagedOrderRequest request)
        {
            var managedOrder = request.ManagedOrder;

            if (managedOrder == null)
                return;

            var submitRequest = request as ManagedOrderSubmitRequest;

            if (submitRequest != null)
            {
                CheckOcaCondition(managedOrder);
                CheckAttachedOrderNeedCancelCondition(managedOrder);

                if (managedOrder.State == ManagedOrderState.New 
                    && CanSubmitNew(managedOrder))
                {
                    managedOrder.Submit();

                    if (managedOrder.UnderlyingOrderId > 0)
                    {
                        ConcurrentDictionary<int, IManagedOrder> managedOrderLookup;

                        if (orderManagedOrderLookup.TryGetValue(managedOrder.ExecutionRouter, out managedOrderLookup) == false)
                        {
                            managedOrderLookup = new ConcurrentDictionary<int, IManagedOrder>();
                            orderManagedOrderLookup[managedOrder.ExecutionRouter] = managedOrderLookup;

                            managedOrder.ExecutionRouter.OrderStatusChanged += (s, e) => OnOrderEvent(e, managedOrder.ExecutionRouter);
                        }

                        managedOrderLookup[managedOrder.UnderlyingOrderId] = managedOrder;
                    }
                }

                if (managedOrder.State.IsFilled() || managedOrder.State.IsCanceled())
                {
                    CheckOcaCondition(managedOrder);
                    CheckAttachedOrdersCondition(managedOrder, false);
                }
            }
        }

        private void ProcessData()
        {
            var remainingCount = dataQueue.Count;

            while (remainingCount-- > 0)
            {
                try
                {
                    object data;

                    if (dataQueue.TryDequeue(out data) == false)
                        break;

                    ProcessData(data);
                }
                catch (Exception ex)
                {
                    LogException(ex);
                }
            }
        }

        private void ProcessData(object data)
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

            RunSync();
        }
    }
}
