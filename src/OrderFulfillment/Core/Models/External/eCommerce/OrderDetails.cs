using System;
using System.Collections.Generic;

namespace OrderFulfillment.Core.Models.External.Ecommerce
{
    /// <summary>
    ///   The details of an order retrieved from the eCommerce system
    ///   associated with user interactions for creating an order.
    /// </summary>
    /// 
    public class OrderDetails
    {
        /// <summary>
        ///   The unique identifier for the order.
        /// </summary>
        /// 
        public string OrderId { get;  set; }

        /// <summary>
        ///   The unique identifier for the user associated with the order.
        /// </summary>
        /// 
        public string UserId { get;  set; }

        /// <summary>
        ///   The recipients of the order.
        /// </summary>
        /// 
        public List<Recipient> Recipients { get;  set; }

        /// <summary>
        ///   The set of items that were ordered.
        /// </summary>
        public List<LineItem> LineItems { get;  set; }

        /// <summary>
        /// This is the reported order data for the order that was placed.
        /// </summary>
        public DateTime? OrderDate { get; set; }
    }
}
