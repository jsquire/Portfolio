using System.Threading.Tasks;
using OrderFulfillment.Core.Models.External.OrderProduction;

namespace OrderFulfillment.Core.Storage
{
    /// <summary>
    ///   Defines the contract for storage providers of Order data.
    /// </summary>
    /// 
    public interface IOrderStorage
    {
        /// <summary>
        ///   Retrieves the pending order for the partner/orderId combination.
        /// </summary>
        /// 
        /// <param name="partner">The code of the partner assocaited with the order.</param>
        /// <param name="orderId">The unique identifier of the desired order.</param>
        /// 
        /// <returns>A tuple indicating whether or not the order was found in storage and, if so, the order.
        /// 
        Task<(bool Found, CreateOrderMessage Order)> TryRetrievePendingOrderAsync(string partner,
                                                                                  string orderId);

        /// <summary>
        ///   Deletes the pending order for the partner/orderId combination.
        /// </summary>
        /// 
        /// <param name="partner">The code of the partner assocaited with the order.</param>
        /// <param name="orderId">The unique identifier of the desired order.</param>
        ///
        Task DeletePendingOrderAsync(string partner, string orderId);

        /// <summary>
        ///   Saves an order that is pending submission.
        /// </summary>
        /// 
        /// <param name="partner">The code of the partner assocaited with the order.</param>
        /// <param name="orderId">The unique identifier of the desired order.</param>
        /// <param name="order">The order to save.</param>      
        /// 
        /// <returns>The key for the order in storage.</returns>
        /// 
        Task<string> SavePendingOrderAsync(string partner, string orderId, CreateOrderMessage order);

        /// <summary>
        ///   Saves an order that as completed submission.
        /// </summary>
        /// 
        /// <param name="order">The order to save.</param>       
        /// 
        /// <returns>The key for the order in storage.</returns>
        /// 
        Task<string> SaveCompletedOrderAsync(CreateOrderMessage order);
    }
}
