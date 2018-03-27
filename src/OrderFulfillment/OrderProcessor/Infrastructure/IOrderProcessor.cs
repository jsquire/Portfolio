using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OrderFulfillment.Core.Models.Operations;

namespace OrderFulfillment.OrderProcessor.Infrastructure
{
    /// <summary>
    ///   Defines the contract to be enacted by order processors.
    /// </summary>
    /// 
    public interface IOrderProcessor : IDisposable
    {
        /// <summary>
        ///   Performs the actions needed to process an order in preparation for submission.
        /// </summary>
        /// 
        /// <param name="partner">The partner associated with the order.</param>
        /// <param name="orderId">The unique identifier of the order.</param>
        /// <param name="orderAssets">The set of assets associated with the order.</param>
        /// <param name="emulation">The set of emulation requirements for processing; this will override the associated external communication an, instead, use the emulated result.</param>
        /// <param name="correlationId">An optional identifier used to correlate activities across the disparate parts of processing, including external interations.</param>
        /// 
        /// <returns>The result of the operation.</returns>
        /// 
        Task<OperationResult> ProcessOrderAsync(string                              partner,
                                                string                              orderId, 
                                                IReadOnlyDictionary<string, string> orderAssets,
                                                DependencyEmulation                 emulation,
                                                string                              correlationId = null);
    }
}
