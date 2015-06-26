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

        public ManagedOrderBase(IExecutionRouter router)
        {
            this.ExecutionRouter = router;
        }

        #region IManagedOrder Members

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

        public Order UnderlyingOrder
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
            throw new NotImplementedException();
        }

        public void Cancel()
        {
            if (UnderlyingOrder != null
                && UnderlyingOrder.Status.AllowsCancel()
                && (currentOrderEvent == null || currentOrderEvent.Status.AllowsCancel()))
            {
                RequestState = ManagedOrderRequestState.Canceling;

                try
                {
                    ExecutionRouter.Transactions.RemoveOrder(UnderlyingOrder.Id);
                }
                catch (Exception ex)
                {
                    RequestState = ManagedOrderRequestState.None;
                }
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

        public void Process(OrderEvent orderEvent)
        {
            currentOrderEvent = orderEvent;

            if (State.IsOpen() == true)
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
                    if(State == ManagedOrderState.Working || State == ManagedOrderState.Submitted)
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
        }

        #endregion

    }
}
