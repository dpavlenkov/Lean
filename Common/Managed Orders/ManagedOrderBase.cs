using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuantConnect.Orders;

namespace QuantConnect.ManagedOrders
{
    public abstract class ManagedOrderBase : IManagedOrder
    {
        protected List<Guid> ocaGroups = new List<Guid>();
        protected OrderEvent currentOrderEvent;
        protected Order underlyingOrder;

        public ManagedOrderBase(IExecutionRouter router, string symbol, int quantity)
        {
            this.Id = Guid.NewGuid();
            this.Symbol = symbol;
            this.Quantity = quantity;
            this.ExecutionRouter = router;
        }

        #region IManagedOrder Members

        public Guid Id
        {
            get;
            private set;
        }

        public string Symbol
        {
            get;
            protected set;
        }

        public string Tag
        {
            get;
            set;
        }

        public int Quantity
        {
            get;
            protected set;
        }

        public ManagedOrderState State
        {
            get;
            protected set;
        }

        public ManagedOrderRequestState RequestState
        {
            get;
            protected set;
        }

        public int UnderlyingOrderId
        {
            get;
            protected set;
        }

        public IExecutionRouter ExecutionRouter
        {
            get;
            protected set;
        }

        public Guid? AttachedToId
        {
            get;
            protected set;
        }

        public Guid[] OCAGroups
        {
            get { return ocaGroups.ToArray(); }
        }

        public void Submit()
        {
            if(this.CanSubmit() == false) 
                return;

            try
            {
                UnderlyingOrderId = SubmitInternal();

                if (UnderlyingOrderId > 0)
                {
                    State = ManagedOrderState.Submitted;
                }
                else
                {
                    State = ManagedOrderState.Error;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("{0}: {1}", ex.Message, ex.StackTrace);
                State = ManagedOrderState.Error;
            }
        }

        protected abstract int SubmitInternal();

        protected void LogException(Exception ex)
        {
            Console.Error.WriteLine("{0} {1}", ex.Message, ex.StackTrace);
        }

        protected void LogStatus()
        {
            Console.WriteLine("{0} {4} {1} {2} {3}", Id, Tag, State, RequestState, UnderlyingOrderId);
        }

        public void Cancel()
        {
            if (State.IsOpen() == true)
            {
                if (UnderlyingOrderId > 0)
                {
                    if (underlyingOrder != null
                        && underlyingOrder.Status.AllowsCancel()
                        && (currentOrderEvent == null || currentOrderEvent.Status.AllowsCancel()))
                    {
                        RequestState = ManagedOrderRequestState.Canceling;

                        try
                        {
                            ExecutionRouter.Transactions.RemoveOrder(underlyingOrder.Id);
                            LogStatus();
                            return;
                        }
                        catch (Exception ex)
                        {
                            LogException(ex);
                        }
                    }
                }

                State = ManagedOrderState.Canceled;
                RequestState = ManagedOrderRequestState.None;

                LogStatus();
            }
        }

        public void AttachToId(Guid? managedOrderId)
        {
            AttachedToId = managedOrderId;
        }

        public void JoinOCAGroup(Guid groupId)
        {
            ocaGroups.Add(groupId);
        }

        public void LeaveOCAGroup(Guid groupId)
        {
            ocaGroups.Remove(groupId);
        }

        private void TryGetUnderlyingOrder()
        {
            if (underlyingOrder == null)
                ExecutionRouter.Transactions.Orders.TryGetValue(UnderlyingOrderId, out underlyingOrder);
        }

        public void Process(OrderEvent orderEvent)
        {
            currentOrderEvent = orderEvent;

            TryGetUnderlyingOrder();

            if (State.IsOpen() == false)
                return;

            switch (orderEvent.Status)
            {
                case OrderStatus.Canceled:
                    State = ManagedOrderState.Canceled;
                    RequestState = ManagedOrderRequestState.None;

                    break;
                case OrderStatus.Filled:
                    State = ManagedOrderState.Filled;
                    RequestState = ManagedOrderRequestState.None;
                    break;
                case OrderStatus.PartiallyFilled:
                    State = ManagedOrderState.PartiallyFilled;
                    RequestState = ManagedOrderRequestState.None;
                    break;
                case OrderStatus.Submitted:
                    if(State == ManagedOrderState.Submitted)
                        State = ManagedOrderState.Working;
                    if (RequestState == ManagedOrderRequestState.Submitting
                        || RequestState == ManagedOrderRequestState.Amending)
                        RequestState = ManagedOrderRequestState.None;
                    break;
                case OrderStatus.Invalid:
                    State = ManagedOrderState.Error;
                    break;
                default:
                    break;
            }

            LogStatus();
        }

        #endregion

    }
}
