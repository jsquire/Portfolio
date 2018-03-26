using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using Moq;
using OrderFulfillment.Core.WebJobs;
using System.Diagnostics;
using Serilog;
using Microsoft.Azure.WebJobs.Host;

namespace OrderFulfillment.Core.Tests.WebJobs
{
    /// <summary>
    ///   The suite of tests for the <see cref="SerilogWebJobTraceWriter" /> 
    ///   class.
    /// </summary>
    /// 
    public class SerilogWebJobTraceWriterTests
    {
        /// <summary>
        ///   Verifies functionality of the constructor.
        /// </summary>
        /// 
        [Fact]
        public void ConstructorSetsTheRequestedLogLevel()
        {
            var expected = TraceLevel.Off;
            var writer   = new SerilogWebJobTraceWriter(Mock.Of<ILogger>(), expected);
            
            writer.Level.Should().Be(expected, "because the log level should have been set");
        }

        /// <summary>
        ///   Verifies functionality of the constructor.
        /// </summary>
        /// 
        [Fact]
        public void ConstructorAllowsWarningsByDefault()
        {
            var expected = TraceLevel.Warning;
            var writer   = new SerilogWebJobTraceWriter(Mock.Of<ILogger>());
            
            writer.Level.Should().Be(expected, "because the log level should have been set to allow Warnings");
        }

        /// <summary>
        ///   Verifies functionality of the <see cref="SerilogWebJobTraceWriter.Trace" /> 
        ///   method.
        /// </summary>
        /// 
        [Fact]
        public void TraceWithExceptionLogsAsAnError()
        {
            var loggerMock = new Mock<ILogger>();
            var writer     = new SerilogWebJobTraceWriter(loggerMock.Object);
            var expected   = new Exception();
            var trace      = new TraceEvent(TraceLevel.Verbose, "Hello");

            trace.Exception = expected;
            
            writer.Trace(trace);
            loggerMock.Verify(logger => logger.Error(It.Is<Exception>(ex => ex == expected), It.IsAny<string>(), It.IsAny<string>()), Times.Once, "because an exception should be error logged, regardless of level");
        }

        /// <summary>
        ///   Verifies functionality of the <see cref="SerilogWebJobTraceWriter.Trace" /> 
        ///   method.
        /// </summary>
        /// 
        [Fact]
        public void TraceLogsVerboseAsVerbose()
        {
            var loggerMock = new Mock<ILogger>();
            var writer     = new SerilogWebJobTraceWriter(loggerMock.Object);
            var trace      = new TraceEvent(TraceLevel.Verbose, "Hello");
            
            writer.Trace(trace);
            loggerMock.Verify(logger => logger.Verbose(It.IsAny<string>()), Times.Once, "because the correct log method should be called for the level");
        }

        /// <summary>
        ///   Verifies functionality of the <see cref="SerilogWebJobTraceWriter.Trace" /> 
        ///   method.
        /// </summary>
        /// 
        [Fact]
        public void TraceLogsInformationAsInformation()
        {
            var loggerMock = new Mock<ILogger>();
            var writer     = new SerilogWebJobTraceWriter(loggerMock.Object);
            var trace      = new TraceEvent(TraceLevel.Info, "Hello");
            
            writer.Trace(trace);
            loggerMock.Verify(logger => logger.Information(It.IsAny<string>()), Times.Once, "because the correct log method should be called for the level");
        }

        /// <summary>
        ///   Verifies functionality of the <see cref="SerilogWebJobTraceWriter.Trace" /> 
        ///   method.
        /// </summary>
        /// 
        [Fact]
        public void TraceLogsWarningAsWarning()
        {
            var loggerMock = new Mock<ILogger>();
            var writer     = new SerilogWebJobTraceWriter(loggerMock.Object);
            var trace      = new TraceEvent(TraceLevel.Warning, "Hello");
            
            writer.Trace(trace);
            loggerMock.Verify(logger => logger.Warning(It.IsAny<string>()), Times.Once, "because the correct log method should be called for the level");
        }

        /// <summary>
        ///   Verifies functionality of the <see cref="SerilogWebJobTraceWriter.Trace" /> 
        ///   method.
        /// </summary>
        /// 
        [Fact]
        public void TraceLogsErrorAsError()
        {
            var loggerMock = new Mock<ILogger>();
            var writer     = new SerilogWebJobTraceWriter(loggerMock.Object);
            var trace      = new TraceEvent(TraceLevel.Error, "Hello");
            
            writer.Trace(trace);
            loggerMock.Verify(logger => logger.Error(It.IsAny<string>()), Times.Once, "because the correct log method should be called for the level");
        }
    }
}
