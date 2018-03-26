using OrderFulfillment.Core.Commands;
using OrderFulfillment.Core.Configuration;

namespace OrderFulfillment.OrderProcessor.Configuration
{
    /// <summary>
    ///   The configuration needed for the Order Processor WebJob Host.
    /// </summary>
    /// 
    public class OrderProcessorJobHostConfiguration : IConfiguration
    {
        /// <summary>
        ///   The connection string to use for accessing the Web Job dashboard.
        /// </summary>
        /// 
        public string DashboardConnectionString { get;  set; }

        /// <summary>
        ///   The connection string needed for the storage account to use for Web Job-related
        ///   automatic tracking.
        /// </summary>
        /// 
        public string StorageConnectionString { get;  set; }

        /// <summary>
        ///   The connection string instance to use for the Service Bus instance
        ///   where the assocaited command queue is located.
        /// </summary>
        /// 
        public string ServiceBusConnectionString { get;  set; }

        /// <summary>
        ///   The maximum number of retry counts for handling a given command.  When these 
        ///   retries are exhuasted, the handler will give up and consider it a fatal
        ///   failure.
        /// </summary>
        /// 
        public int CommandRetryMaxCount { get;  set; }

        /// <summary>
        ///   The baseline number of seconds to apply when calculating the
        ///   exponential backoff for performing a retry.
        /// </summary>
        /// 

        public double CommandRetryExponentialSeconds { get;  set; }

        /// <summary>
        ///   The baseline number of seconds to combine with a random multiplier
        ///   when calculating the jitter for a retry.
        /// </summary>
        /// 
        public double CommandRetryJitterSeconds { get;  set; }

        /// <summary>
        ///   Creates an instance of the <see cref="CommandRetryThresholds" /> based on the
        ///   current set of configuration values.
        /// </summary>
        /// 
        /// <returns>A <see cref="CommandRetryThresholds" /> instance populated from configuration values.</returns>
        /// 
        public CommandRetryThresholds CreateCommandRetryThresholdsFromConfiguration() => 
            new CommandRetryThresholds(this.CommandRetryMaxCount, this.CommandRetryExponentialSeconds, this.CommandRetryJitterSeconds);
    }
}
