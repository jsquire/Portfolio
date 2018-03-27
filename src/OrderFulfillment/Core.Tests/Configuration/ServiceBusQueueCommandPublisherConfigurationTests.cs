using System;
using FluentAssertions;
using OrderFulfillment.Core.Commands;
using OrderFulfillment.Core.Configuration;
using Xunit;

namespace OrderFulfillment.Core.Tests.Configuration
{
    /// <summary>
    ///   The suite of tests for the <see cref="ServiceBusQueueCommandPublisherConfiguration{T}" />
    ///   class
    /// </summary>
    public class ServiceBusQueueCommandPublisherConfigurationTests
    {
        /// <summary>
        ///   Verifies functionality of the <see cref="ServiceBusQueueCommandPublisherConfiguration{T}.GetSpecificConfigurationType" />
        ///   method.
        /// </summary>
        /// 
        [Fact]
        public void GetSpecificConfigurationTypeProducesTheCorrectType()
        {
            var expectedType = typeof(ProcessOrderServiceBusQueueCommandPublisherConfiguration);
            var genericType  = typeof(ServiceBusQueueCommandPublisherConfiguration<ProcessOrder>);
            var actual       = ServiceBusQueueCommandPublisherConfiguration<object>.GetSpecificConfigurationType(genericType);

            actual.Should().NotBeNull("because the specific type should have been located");
            actual.ShouldBeEquivalentTo(expectedType, "because the correct type should have been located");
        }

        /// <summary>
        ///   Verifies functionality of the <see cref="ServiceBusQueueCommandPublisherConfiguration{T}.GetSpecificConfigurationType" />
        ///   method.
        /// </summary>
        /// 
        [Fact]
        public void GetSpecificConfigurationTypeProducesWithASpecificTypeProducesTheCorrectType()
        {
            var expectedType = typeof(ProcessOrderServiceBusQueueCommandPublisherConfiguration);
            var actual       = ServiceBusQueueCommandPublisherConfiguration<object>.GetSpecificConfigurationType(expectedType);

            actual.Should().NotBeNull("because the specific type should have been located");
            actual.ShouldBeEquivalentTo(expectedType, "because the correct type should have been located");
        }

        /// <summary>
        ///   Verifies functionality of the <see cref="ServiceBusQueueCommandPublisherConfiguration{T}.GetSpecificConfigurationType" />
        ///   method.
        /// </summary>
        /// 
        [Fact]
        public void GetSpecificConfigurationTypeWithIncorrectTypeProducesNothing()
        {
            var genericType = typeof(NonSerializedAttribute);
            var actual      = ServiceBusQueueCommandPublisherConfiguration<object>.GetSpecificConfigurationType(genericType);

            actual.Should().BeNull("because the generic type did not correspond to a specific type");
        }
    }
}
