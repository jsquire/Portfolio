using OrderFulfillment.Core.Configuration;

namespace OrderFulfillment.OrderProcessor.Configuration
{
    /// <summary>
    ///   The configuration needed for the Order Submitter.
    /// </summary>
    /// 
    public class OrderSubmitterConfiguration : IConfiguration
    {
        /// <summary>
        ///   The maximum number of retry counts for performing a given operation.  When these 
        ///   retries are exhuasted, the operation is considered a failure.
        /// </summary>
        /// 
        public int OperationRetryMaxCount { get;  set; }

        /// <summary>
        ///   The baseline number of seconds to apply when calculating the
        ///   exponential backoff for performing a retry.
        /// </summary>
        /// 

        public double OperationRetryExponentialSeconds { get;  set; }

        /// <summary>
        ///   The baseline number of seconds to combine with a random multiplier
        ///   when calculating the jitter for a retry.
        /// </summary>
        /// 
        public double OperationRetryJitterSeconds { get;  set; }
    }
}