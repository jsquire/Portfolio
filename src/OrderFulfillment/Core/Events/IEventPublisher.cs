using System.Threading.Tasks;

namespace OrderFulfillment.Core.Events
{
    /// <summary>
    ///   Serves as a publsiher for a single event, or family of events with 
    ///   a common ancestor.
    /// </summary>
    /// 
    /// <typeparam name="T">The type of event capable of being published</typeparam>
    /// 
    public interface IEventPublisher<T> where T : EventBase
    {
        /// <summary>
        ///   Publishes the specified event.
        /// </summary>
        /// 
        /// <param name="event">The event to publish.</param>
        /// 
        Task PublishAsync(T @event);

        /// <summary>
        ///   Attempts to publish the specified event.
        /// </summary>
        /// 
        /// <param name="event">The event to publish.</param>
        /// 
        /// <returns><c>true</c> if the event was successfully published; otherwise, <c>false</c>.</returns>
        /// 
        /// <remarks>
        ///     The implementor of this interface may or may not log failures; the contract
        ///     makes no demands one way or the other.
        /// </remarks>
        /// 
        Task<bool> TryPublishAsync(T @event);
    }
}
