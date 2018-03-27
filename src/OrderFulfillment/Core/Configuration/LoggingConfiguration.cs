namespace OrderFulfillment.Core.Configuration
{
    /// <summary>
    ///    The set of configuration that influences the behavior of logging-related
    ///    operations.
    /// </summary>
    /// 
    public class LoggingConfiguration : IConfiguration
    {
        /// <summary>
        ///   The key associated with the Application Insights instance to which telemetry
        ///   should be submitted.
        /// </summary>
        /// 
        public string ApplicationInsightsKey { get; set; }

        /// <summary>
        ///   The threshold, in milliseconds, for responses from external dependenies to be considered slow.
        /// </summary>
        /// 
        public int DependencySlowResponseThresholdMilliseconds { get; set; }
    }
}
