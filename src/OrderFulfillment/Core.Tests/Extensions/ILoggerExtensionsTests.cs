using FluentAssertions;
using OrderFulfillment.Core.Extensions;
using OrderFulfillment.Core.Logging;
using Moq;
using Serilog;
using Xunit;

namespace OrderFulfillment.Core.Tests.Extensions
{
    /// <summary>
    ///   The suite of unit tests for the <see cref="OrderFulfillment.Core.Extensions.ILoggerExtensions" />
    ///   class.
    /// </summary>
    /// 
    public class ILoggerExtensionsTests
    {
        /// <summary>
        ///   Verifies functionality of the <see cref="OrderFulfillment.Core.Extensions.ILoggerExtensions.WithCorrelationId" />
        ///   method.
        /// </summary>
        /// 
        [Fact]
        public void WithCorrelationIdWithNullLogger()
        {
            ILoggerExtensions.WithCorrelationId(null, "blue").Should().BeNull("because the logger was null");
        }

        /// <summary>
        ///   Verifies functionality of the <see cref="OrderFulfillment.Core.Extensions.ILoggerExtensions.WithCorrelationId" />
        ///   method.
        /// </summary>
        /// 
        /// <param name="correlationId">The correlation identifier to use for testing.</param>
        /// 
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void WithCorrelationIdWithMissingCorrelationId(string correlationId)
        {
            var logger = new Mock<ILogger>();


            ILoggerExtensions.WithCorrelationId(logger.Object, correlationId).Should().BeSameAs(logger.Object, "because the logger instance should be returned");

            logger.Verify(instance => instance.ForContext(It.Is<string>(property => property == LogPropertyNames.CorrelationId), It.Is<string>(id => id == correlationId), It.IsAny<bool>()),
                Times.Never, 
                "because the correlation identifier was missing");
        }

        /// <summary>
        ///   Verifies functionality of the <see cref="OrderFulfillment.Core.Extensions.ILoggerExtensions.WithCorrelationId" />
        ///   method.
        /// </summary>
        /// 
        [Fact]
        public void WithCorrelationIdWithValidCorrelationId()
        {
            var correlationId = "ABC123";
            var logger        = new Mock<ILogger>();
            
            ILoggerExtensions.WithCorrelationId(logger.Object, correlationId);
            
            logger.Verify(instance => instance.ForContext(It.Is<string>(property => property == LogPropertyNames.CorrelationId), It.Is<string>(id => id == correlationId), It.IsAny<bool>()),
                Times.Once, 
                "because the correlation identifier was provided");
        }
    }
}
