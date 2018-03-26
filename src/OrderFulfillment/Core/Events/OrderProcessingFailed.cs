namespace OrderFulfillment.Core.Events
{
    /// <summary>
    ///   An event fired when an order could not be processed and cannot be
    ///   fulfilled.
    /// </summary>
    /// 
    /// <seealso cref="OrderFulfillment.Core.Events.OrderEventBase" />
    /// 
    public class OrderProcessingFailed : OrderEventBase 
    {
    }
}
