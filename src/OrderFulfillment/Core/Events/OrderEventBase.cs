using System;
using OrderFulfillment.Core.Commands;
using OrderFulfillment.Core.Extensions;

namespace OrderFulfillment.Core.Events
{
    /// <summary>
    ///   Serves as a base class for order-related events.
    /// </summary>
    /// 
    /// <seealso cref="OrderFulfillment.Core.Events.EventBase" />
    /// 
    public class OrderEventBase : EventBase
    {
        /// <summary>
        ///   The code of the partner who requested order fulfillment.
        /// </summary>
        /// 
        public string PartnerCode { get;  set; }

        /// <summary>
        ///   The unique identifier of the order.
        /// </summary>
        /// 
        public string OrderId { get;  set; }

        /// <summary>
        ///   Creates a new event based on the base-level and order-related properties of 
        ///   the current instance.
        /// </summary>
        /// 
        /// <typeparam name="TEvent">The type of event to be created</typeparam>
        /// 
        /// <param name="mutator">The mutator to run on creation as a means to populate properties of the new event.</param>
        /// 
        /// <returns>A new event with the basic properties set based on the source <paramref name="instance" /></returns>
        /// 
        public TEvent CreateNewOrderEvent<TEvent>(Action<TEvent> mutator = null) where TEvent : OrderEventBase, new()
        {
            Action<TEvent> orderMutator = target =>
            {
                target.PartnerCode  = this.PartnerCode;
                target.OrderId      = this.OrderId;

                mutator?.Invoke(target);                
            };

            return this.CreateNewEvent(orderMutator);
        }

        /// <summary>
        ///   Creates a new command based on the base-level and order-related properties of 
        ///   the current instance.
        /// </summary>
        /// 
        /// <typeparam name="TCommand">The type of command to be created</typeparam>
        /// 
        /// <param name="mutator">The mutator to run on creation as a means to populate properties of the new command.</param>
        /// 
        /// <returns>A new command with the basic properties set based on the source <paramref name="instance" /></returns>
        /// 
        public TCommand CreateNewOrderCommand<TCommand>(Action<TCommand> mutator = null) where TCommand : OrderCommandBase, new()
        {
            Action<TCommand> orderMutator = target =>
            {
                target.PartnerCode  = this.PartnerCode;
                target.OrderId      = this.OrderId;

                mutator?.Invoke(target);                
            };

            return this.CreateNewCommand(orderMutator);
        }
    }
}
