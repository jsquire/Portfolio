using OrderFulfillment.Core.Configuration;

namespace OrderFulfillment.Api.Configuration
{
    // <summary>
    ///   The set of configuration needed for the operations of the 
    ///   <see cref="OrderSubmissionController" /> endpoints.
    /// </summary>
    /// 
    public class OrderSubmissionControllerConfiguration : IConfiguration
    {
        /// <summary>
        ///   The period of time, in seconds, that callers should be told 
        ///   that they can check on the status of an order fulfillment.
        /// </summary>
        /// 
        public int OrderAcceptedRetryAfterInSeconds { get;  set; }

        /// <summary>
        ///   The period of time, in seconds, that callers should be told 
        ///   that they may retry when confronted with an HTTP 503 (Service Unavailable)
        ///   response.
        /// </summary>
        /// 
        public int ServiceUnavailableeRetryAfterInSeconds { get;  set; }

        /// <summary>
        ///   The period of time, in seconds, that should be used as the
        ///   upper bound when adding random jitter to retry-after periods.
        /// </summary>
        /// 
        public int RetryAfterJitterUpperBoundInSeconds { get;  set; }
    }
}