using OrderFulfillment.Core.Configuration;

namespace OrderFulfillment.OrderProcessor.Configuration
{
    /// <summary>
    ///   The configuration needed for the Order Processor.
    /// </summary>
    /// 
    public class OrderProcessorConfiguration : IConfiguration
    {
        /// <summary>
        ///   The service level agreement value to use when applying rules to the order.
        /// </summary>
        /// 
        public string ServiceLevelAgreementCode { get;  set; }

        /// <summary>
        ///   The partner sub-code value to use when applying rules to the order.
        /// </summary>
        /// 
        public string PartnerSubCode { get; set; }

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