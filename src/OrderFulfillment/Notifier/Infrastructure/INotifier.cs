using System.Threading.Tasks;
using OrderFulfillment.Core.Models.Operations;

namespace OrderFulfillment.Notifier.Infrastructure
{
    /// <summary>
    ///   Defines the contract to be enacted by order submitters.
    /// </summary>
    /// 
    public interface INotifier
    {
        /// <summary>
        ///   Performs the actions needed to notify of an order failure.
        /// </summary>
        /// 
        /// <param name="partner">The partner associated with the order.</param>
        /// <param name="orderId">The unique identifier of the order.</param>
        /// <param name="correlationId">An optional identifier used to correlate activities across the disparate parts of processing, including external interations.</param>
        /// 
        /// <returns>The result of the operation.</returns>
        /// 
        Task<OperationResult> NotifyOfOrderFailureAsync(string partner,
                                                        string orderId,                                                       
                                                        string correlationId = null);
                                                        
        /// <summary>
        ///   Performs the actions needed to notify of a message stuck in a dead letter area.
        /// </summary>
        /// 
        /// <param name="location">The location of the dead letter message.</param>
        /// <param name="partner">The partner associated with the order.</param>
        /// <param name="orderId">The unique identifier of the order.</param>
        /// <param name="correlationId">An optional identifier used to correlate activities across the disparate parts of processing, including external interations.</param>
        /// 
        /// <returns>The result of the operation.</returns>
        /// 
        Task<OperationResult> NotifyDeadLetterMessageAsync(string location,
                                                           string partner,
                                                           string orderId,                                                       
                                                           string correlationId = null);
    }
}
