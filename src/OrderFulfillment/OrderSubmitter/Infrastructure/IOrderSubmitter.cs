using System.Threading.Tasks;
using OrderFulfillment.Core.Models.Operations;

namespace OrderFulfillment.OrderSubmitter.Infrastructure
{
    /// <summary>
    ///   Defines the contract to be enacted by order submitters.
    /// </summary>
    /// 
    public interface IOrderSubmitter
    {
        /// <summary>
        ///   Performs the actions needed to submit an order for production.
        /// </summary>
        /// 
        /// <param name="partner">The partner associated with the order.</param>
        /// <param name="orderId">The unique identifier of the order.</param>
        /// <param name="emulation">The set of emulation requirements for processing; this will override the associated external communication an, instead, use the emulated result.</param>
        /// <param name="correlationId">An optional identifier used to correlate activities across the disparate parts of processing, including external interations.</param>
        /// 
        /// <returns>The result of the operation.</returns>
        /// 
        Task<OperationResult> SubmitOrderForProductionAsync(string              partner,
                                                            string              orderId, 
                                                            DependencyEmulation emulation     = null,
                                                            string              correlationId = null);
    }
}
