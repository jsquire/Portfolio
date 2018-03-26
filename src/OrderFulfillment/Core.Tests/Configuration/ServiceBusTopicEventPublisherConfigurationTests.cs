using System;
using FluentAssertions;
using OrderFulfillment.Core.Events;
using OrderFulfillment.Core.Configuration;
using Xunit;

namespace OrderFulfillment.Core.Tests.Configuration
{
    /// <summary>
    ///   The suite of tests for the <see cref="ServiceBusTopicEventPublisherConfigurationConfiguration{T}" />
    ///   class
    /// </summary>
    public class ServiceBusTopicEventPublisherConfigurationConfigurationTests
    {
        /// <summary>
        ///   Verifies functionality of the <see cref="ServiceBusTopicEventPublisherConfigurationConfiguration{T}.GetSpecificConfigurationType" />
        ///   method.
        /// </summary>
        /// 
        [Fact]
        public void GetSpecificConfigurationTypeProducesTheCorrectType()
        {
            var expectedType = typeof(EventBaseServiceBusTopicEventPublisherConfiguration);
            var genericType  = typeof(ServiceBusTopicEventPublisherConfiguration<EventBase>);
            var actual       = ServiceBusTopicEventPublisherConfiguration<object>.GetSpecificConfigurationType(genericType);

            actual.Should().NotBeNull("because the specific type should have been located");
            actual.ShouldBeEquivalentTo(expectedType, "because the correct type should have been located");
        }

        /// <summary>
        ///   Verifies functionality of the <see cref="ServiceBusTopicEventPublisherConfigurationConfiguration{T}.GetSpecificConfigurationType" />
        ///   method.
        /// </summary>
        /// 
        [Fact]
        public void GetSpecificConfigurationTypeProducesWithASpecificTypeProducesTheCorrectType()
        {
            var expectedType = typeof(EventBaseServiceBusTopicEventPublisherConfiguration);
            var actual       = ServiceBusTopicEventPublisherConfiguration<object>.GetSpecificConfigurationType(expectedType);

            actual.Should().NotBeNull("because the specific type should have been located");
            actual.ShouldBeEquivalentTo(expectedType, "because the correct type should have been located");
        }

        /// <summary>
        ///   Verifies functionality of the <see cref="ServiceBusTopicEventPublisherConfigurationConfiguration{T}.GetSpecificConfigurationType" />
        ///   method.
        /// </summary>
        /// 
        [Fact]
        public void GetSpecificConfigurationTypeWithIncorrectTypeProducesNothing()
        {
            var genericType = typeof(NonSerializedAttribute);
            var actual      = ServiceBusTopicEventPublisherConfiguration<object>.GetSpecificConfigurationType(genericType);

            actual.Should().BeNull("because the generic type did not correspond to a specific type");
        }
    }
}
