using System;
using System.Threading.Tasks;
using OrderFulfillment.Core.Models.Operations;

namespace OrderFulfillment.Core.External
{
    /// <summary>
    ///   Defines the contract for clients facilitating communication with the eCommerce
    ///   service.
    /// </summary>
    /// 
    public interface IEcommerceClient : IDisposable
    {
        /// <summary>
        ///    Queries the details of an order from the eCommerce system.
        /// </summary>
        /// 
        /// <param name="orderId">The unique identifier of the order to query for.</param>
        /// <param name="correlationId">The correlation identifier to associate with the request.  If <c>null</c>, no correlation will be sent.</param>
        /// 
        /// <returns>The result of the operation.</returns>
        /// 
        Task<OperationResult> GetOrderDetailsAsync(string orderId,
                                                   string correlationId = null);
    }
}
