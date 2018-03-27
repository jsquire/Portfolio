using OrderFulfillment.Core.Infrastructure;

namespace OrderFulfillment.Core.Commands
{
    /// <summary>
    ///   Serves as the base class for command messages.
    /// </summary>
    /// 
    /// <seealso cref="OrderFulfillment.Core.Infrastructure.MessageBase" />
    /// 
    public class CommandBase : MessageBase
    {
        /// <summary>
        ///   The number of times that the command was attempted to be handled.
        /// </summary>
        /// 
        public int PreviousAttemptsToHandleCount { get;  set; }
    }
}
