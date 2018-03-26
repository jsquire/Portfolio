using System.Collections.Generic;
using OrderFulfillment.Core.Models.Operations;

namespace OrderFulfillment.Api.Models.Requests
{
    /// <summary>
    ///   The request message received when order fultillment is being
    ///   requested.
    /// </summary>
    /// 
    public class OrderFulfillmentMessage
    {
        /// <summary>
        ///   The header for the fulfillment request, containing details
        ///   about the order.
        /// </summary>
        /// 
        public OrderHeader OrderRequestHeader { get;  set; }

        /// <summary>
        ///   The set of items that appear to be coming "out" at us.
        /// </summary>
        /// 
        public List<LineItem> LineItems { get; set; }

        /// <summary>
        ///   A test hook allowing the caller to specify emulated responses for
        ///   external dependencies, allowing order fullfilment to be tested in
        ///   full or partial isolation.
        /// </summary>
        /// 
        /// <remarks>
        ///   This field is considered priviledged and only authorized callers
        ///   will be allowed to include it with the request.  If included with a 
        ///   non-priviledged request, the request will be rejected.
        /// </remarks>
        /// 
        public DependencyEmulation Emulation { get;  set; }
    }
}