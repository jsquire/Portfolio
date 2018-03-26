namespace OrderFulfillment.Api.Models.Responses
{
    /// <summary>
    ///   Provides the data associated with an order when it has been
    ///   accepted.
    /// </summary>
    /// 
    public class OrderData
    {
        /// <summary>
        ///   The unique identifier of the order that was accepted.
        /// </summary>
        /// 
        public string OrderId { get;  set; }
    }
}