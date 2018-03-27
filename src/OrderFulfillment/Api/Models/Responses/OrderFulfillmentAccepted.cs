namespace OrderFulfillment.Api.Models.Responses
{
    /// <summary>
    ///   Serves as the content when an order fullfillment request
    ///   has successfully been accepted.
    /// </summary>
    /// 
    public class OrderFulfillmentAccepted
    {
        /// <summary>
        ///   The relevant details of the order that was accepted.
        /// </summary>
        /// 
        public OrderData FulfillerData { get;  set; }

        /// <summary>
        ///   Initializes a new instance of the <see cref="OrderFulfillmentAccepted"/> class.
        /// </summary>
        /// 
        public OrderFulfillmentAccepted()
        {
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref="OrderFulfillmentAccepted"/> class.
        /// </summary>
        /// 
        /// <param name="orderId">The identifier of the order that was accepted.</param>
        /// 
        public OrderFulfillmentAccepted(string orderId)
        {
           this.FulfillerData = new OrderData { OrderId = orderId };
        }
    }
}