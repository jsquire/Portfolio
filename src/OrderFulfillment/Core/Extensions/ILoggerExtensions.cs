using System;
using OrderFulfillment.Core.Logging;
using Serilog;

namespace OrderFulfillment.Core.Extensions
{
    /// <summary>
    ///   The set of extension methods for the <see cref="Serilog.ILogger" /> interface.
    /// </summary>
    /// 
    public static class ILoggerExtensions
    {

        /// <summary>
        ///   Adds a correlation identifier to the logging context.
        /// </summary>
        /// 
        /// <param name="instance">The ILogger instance that this method was invoked on</param>
        /// <param name="correlationId">The correlation identifier to add to the logging context</param>
        /// 
        /// <returns>The <paramref name="instance" /> with the requested correlation identifier added to the context</returns>
        /// 
        public static ILogger WithCorrelationId(this ILogger instance, 
                                                     string  correlationId) =>
            String.IsNullOrEmpty(correlationId)
                ? instance
                : instance?.ForContext(LogPropertyNames.CorrelationId, correlationId);
    }
}
