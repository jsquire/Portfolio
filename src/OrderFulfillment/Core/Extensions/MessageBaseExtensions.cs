using System;
using OrderFulfillment.Core.Commands;
using OrderFulfillment.Core.Events;
using OrderFulfillment.Core.Infrastructure;

namespace OrderFulfillment.Core.Extensions
{
    /// <summary>
    ///   The set of extension methods or the <see cref="MessageBase" />
    ///   class.
    /// </summary>
    /// 
    public static class MessageBaseExtensions
    {
        /// <summary>
        ///   Creates a new event based on the base-level properties of the target
        ///   <paramref name="instance" />.
        /// </summary>
        /// 
        /// <typeparam name="TEvent">The type of event to be created</typeparam>
        /// 
        /// <param name="instance">The instance that this method was invoked on.</param>
        /// <param name="mutator">The mutator to run on creation as a means to populate properties of the new event.</param>
        /// 
        /// <returns>A new event with the basic properties set based on the source <paramref name="instance" /></returns>
        /// 
        /// <remarks>
        ///   When the new event is created, only the base properties will be populated.  Any extended
        ///   property values must be explicitly set either in the <paramref name="mutator" /> or after
        ///   creation.
        ///     
        ///   The set of properties that will be populated is:
        ///   <list type="-">
        ///     <item><code>Id</code> (new value)</item>
        ///     <item><code>CorrelationId</code> (copied value)</item>
        ///     <item><code>CurrentUser</code> (copied value)</item>
        ///     <item><code>OccurredTimeUtc</code> (new value)</item>
        ///   </list>
        /// </remarks>
        /// 
        public static TEvent CreateNewEvent<TEvent>(this MessageBase    instance,
                                                         Action<TEvent> mutator = null) where TEvent : EventBase, new()
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            var result = new TEvent
            {
                Id              = Guid.NewGuid(),
                CorrelationId   = instance.CorrelationId,
                CurrentUser     = instance.CurrentUser,
                OccurredTimeUtc = DateTime.UtcNow
            };

            mutator?.Invoke(result);
            
            return result;
        }


        /// <summary>
        ///   Creates a new command based on the base-level properties of the target
        ///   <paramref name="instance" />.
        /// </summary>
        /// 
        /// <typeparam name="TCommand">The type of command to be created</typeparam>
        /// 
        /// <param name="instance">The instance that this method was invoked on.</param>
        /// <param name="mutator">The mutator to run on creation as a means to populate properties of the new event.</param>
        /// 
        /// <returns>A new command with the basic properties set based on the source <paramref name="instance" /></returns>
        /// 
        /// <remarks>
        ///   When the new command is created, only the base properties will be populated.  Any extended
        ///   property values must be explicitly set either in the <paramref name="mutator" /> or after
        ///   creation.
        ///     
        ///   The set of properties that will be populated is:
        ///   <list type="-">
        ///     <item><code>Id</code> (new value)</item>
        ///     <item><code>CorrelationId</code> (copied value)</item>
        ///     <item><code>CurrentUser</code> (copied value)</item>
        ///     <item><code>OccurredTimeUtc</code> (new value)</item>
        ///   </list>
        /// </remarks>
        /// 
        public static TCommand CreateNewCommand<TCommand>(this MessageBase    instance,
                                                               Action<TCommand> mutator = null) where TCommand : CommandBase, new()
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            var result = new TCommand
            {
                Id              = Guid.NewGuid(),
                CorrelationId   = instance.CorrelationId,
                CurrentUser     = instance.CurrentUser,
                OccurredTimeUtc = DateTime.UtcNow
            };

            mutator?.Invoke(result);
            
            return result;
        }
    }
}
