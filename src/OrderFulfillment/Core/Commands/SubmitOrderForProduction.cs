using OrderFulfillment.Core.Models.Operations;

namespace OrderFulfillment.Core.Commands
{
    /// <summary>
    ///   Triggers an order to be submitted for production
    /// </summary>
    /// 
    /// <seealso cref="OrderFulfillment.Core.Commands.CommandBase" />
    /// 
    public class SubmitOrderForProduction : OrderCommandBase
    {
        /// <summary>
        ///   The operation results to external depenendies to emulate, in support
        ///   of isolated testing.
        /// </summary>
        /// 
        public DependencyEmulation Emulation { get;  set; }
    }
}