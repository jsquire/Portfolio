using System.Collections;
using System.Collections.Generic;
using OrderFulfillment.Core.Models.Operations;

namespace OrderFulfillment.Core.Commands
{
    /// <summary>
    ///   Triggers an order to be processed
    /// </summary>
    /// 
    /// <seealso cref="OrderFulfillment.Core.Commands.CommandBase" />
    /// 
    public class ProcessOrder : OrderCommandBase
    {
        /// <summary>
        ///   The set of assets associated with the order.
        /// </summary>
        /// 
        public IDictionary<string, string> Assets { get;  set; }

        /// <summary>
        ///   The operation results to external depenendies to emulate, in support
        ///   of isolated testing.
        /// </summary>
        /// 
        public DependencyEmulation Emulation { get;  set; }
    }
}
