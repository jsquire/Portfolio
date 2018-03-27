using System.Collections.Generic;

namespace OrderFulfillment.Api.Models.Requests
{
    /// <summary>
    ///   Represents an item included as part of a fullfillment request.
    /// </summary>
    /// 
    public class LineItem
    {
        /// <summary>
        ///   The line item number that was ordered.
        /// </summary>
        /// 
        public int LineNumber { get; set; }

        /// <summary>
        ///   The quantity of the line item that was ordered.
        /// </summary>
        /// 
        public int Quantity { get; set; }

        /// <summary>
        ///   The assets associated with this ordred item.
        /// </summary>
        /// 
        public List<ItemAsset> Assets { get; set; }
    }
}