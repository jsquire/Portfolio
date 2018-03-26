using System;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;

namespace OrderFulfillment.Core.Logging
{
    /// <summary>
    ///   Serves as an initializer for telemetry in a WebJob, providing
    ///   baseline configuration.
    /// </summary>
    /// 
    /// <seealso cref="Microsoft.ApplicationInsights.Extensibility.ITelemetryInitializer" />
    /// 
    public sealed class ApplicationInsightsWebJobTelemetryInitializer : ITelemetryInitializer
    {
        /// <summary>
        ///   Initializes a new instance of the <see cref="ApplicationInsightsWebJobTelemetryInitializer"/> class.
        /// </summary>
        /// 
        public ApplicationInsightsWebJobTelemetryInitializer()
        {
        }

        /// <summary>
        ///   Initializes properties of the specified <see cref=ITelemetry" />.
        /// </summary>
        /// 
        /// <param name="telemetry">The telemetry item to initialize.</param>
        /// 
        public void Initialize(ITelemetry telemetry)
        {
            var properties = telemetry.Context.Properties;

            // Initialize is called for all pieces of telemetry in a context so it must be idempotent
            // and not set the same context properties more than once.

            if (properties.ContainsKey("WebJob.Name"))
            {
                return;
            }

            properties["WebJob.Name"]  = Environment.GetEnvironmentVariable("WEBJOBS_NAME");
            properties["WebJob.RunId"] = Environment.GetEnvironmentVariable("WEBJOBS_RUN_ID");
        }
    }
}
