using System;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using OrderFulfillment.Core.Logging;
using Moq;
using Xunit;

namespace OrderFulfillment.Core.Tests.Logging
{
    /// <summary>
    ///   The suite of unit tests for the <see cref="OrderFulfillment.Core.Tests.Logging.ApplicationInsightsDependencyTelemetryFilter" />
    ///   class.
    /// </summary>
    /// 
    public class ApplicationInsightsDependencyTelemetryFilterTests
    {
        /// <summary>
        ///   Validates behavior of the <see cref="OrderFulfillment.Core.Logging.ApplicationInsightsDependencyTelemetryFilter.Process" />
        ///   method.
        /// </summary>
        /// 
        [Fact]
        public void ProcessChainsTelemetryWhenNotDependencyTelemetry()
        {
            var telemetry     = new RequestTelemetry() { Duration = TimeSpan.FromMilliseconds(100) };
            var mockProcessor = new Mock<ITelemetryProcessor>();
            var filter        = new ApplicationInsightsDependencyTelemetryFilter(mockProcessor.Object, 500);

            filter.Process(telemetry);
            mockProcessor.Verify(processor => processor.Process(It.Is<ITelemetry>(t => t == telemetry)), Times.Once, "because the telemetry was not a dependency");
        }

        /// <summary>
        ///   Validates behavior of the <see cref="OrderFulfillment.Core.Logging.ApplicationInsightsDependencyTelemetryFilter.Process" />
        ///   method.
        /// </summary>
        /// 
        [Fact]
        public void ProcessChainsTelemetryWhenOverTheThreshold()
        {
            var telemetry     = new DependencyTelemetry("Test Dependency", "there", "Test", "ABC123") { Duration = TimeSpan.FromDays(1) };
            var mockProcessor = new Mock<ITelemetryProcessor>();
            var filter        = new ApplicationInsightsDependencyTelemetryFilter(mockProcessor.Object, 100);

            filter.Process(telemetry);
            mockProcessor.Verify(processor => processor.Process(It.Is<ITelemetry>(t => t == telemetry)), Times.Once, "because the dependency duration was greater than the threshold");
        }

        /// <summary>
        ///   Validates behavior of the <see cref="OrderFulfillment.Core.Logging.ApplicationInsightsDependencyTelemetryFilter.Process" />
        ///   method.
        /// </summary>
        /// 
        [Fact]
        public void ProcessIgnoresTelemetryWhenBelowTheThreshold()
        {
            var telemetry     = new DependencyTelemetry("Test Dependency", "there", "Test", "ABC123") { Duration = TimeSpan.FromMilliseconds(100) };
            var mockProcessor = new Mock<ITelemetryProcessor>();
            var filter        = new ApplicationInsightsDependencyTelemetryFilter(mockProcessor.Object, 500);

            filter.Process(telemetry);
            mockProcessor.Verify(processor => processor.Process(It.Is<ITelemetry>(t => t == telemetry)), Times.Never, "because the dependency duration was beneath than the threshold");
        }
    }
}
