using System;

namespace OrderFulfillment.Api.Models.Requests
{
    /// <summary>
    ///   The header-level information for an order submitted for
    ///   fulfillment.
    /// </summary>
    /// 
    public class OrderHeader
    {
        /// <summary>
        ///   The unique identifier for the order.
        /// </summary>
        /// 
        public string OrderId { get;  set; }

        /// <summary>
        ///   The date/time that the order was placed.
        /// </summary>
        /// 
        /// <value>
        ///   The date/time in UTC.
        /// </value>
        public DateTime? OrderDate { get;  set; }
    }
}