using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;

namespace OrderFulfillment.Core.Logging
{
    /// <summary>
    ///   Serves as a filter for dependency-related telemetry collected for Appliction Insights.
    /// </summary>
    ///
    public class ApplicationInsightsDependencyTelemetryFilter : ITelemetryProcessor
    {
        /// <summary>
        ///   The next telemetry processor in the chain;  if the telemetry is not filtered, the next processor
        ///   should be invoked.
        /// </summary>
        /// 
        private ITelemetryProcessor Next { get; set; }

        /// <summary>
        ///   The threshold for response time of a dependency, in milliseconds.  This value is read from the Application Insights
        ///   configuration in raw (string) form.
        /// </summary>
        /// 
        public int FilterThresholdMilliseconds { get;  set; }

        /// <summary>
        ///   Initializes a new instance of the <see cref="ApplicationInsightsDependencyTelemetryFilter" /> 
        ///   class.
        /// </summary>
        /// 
        /// <param name="next">The next telemetry processor in the chain, to which control should be passed if the item is not to be filtered out.</param>
        /// <param name="filterThresholdMilliseconds">The threshold for response time of a dependency, in milliseconds, under which the telemetry should be filtered.</param>
        /// 
        public ApplicationInsightsDependencyTelemetryFilter(ITelemetryProcessor next,
                                                            int                 filterThresholdMilliseconds)
        {
            this.Next = next;
            this.FilterThresholdMilliseconds = filterThresholdMilliseconds;
        }

        /// <summary>
        ///   Performs the tasks needed to process a telemetry item, potentially filtering it out if the item should
        ///   not be sent to Application Insights.
        /// </summary>
        /// 
        /// <param name="telemetryItem">The telemetry item to consider.</param>
        /// 
        public void Process(ITelemetry telemetryItem)
        {
            // If the telemetry item is not a dependency trace, there is no configured threshold, or the request duration
            // was within the ignore threshold, then take no action.

            var dependencyTelemetry = telemetryItem as DependencyTelemetry;

            if ((dependencyTelemetry != null) && (dependencyTelemetry.Duration.TotalMilliseconds < this.FilterThresholdMilliseconds))
            {
                return;
            }
            
            this.Next?.Process(telemetryItem);
        }
    }
}
