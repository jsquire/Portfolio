using System;
using Autofac;
using FluentAssertions;
using OrderFulfillment.Core.Commands;
using OrderFulfillment.Core.Configuration;
using OrderFulfillment.Core.Events;
using OrderFulfillment.Notifier.Functions;
using Moq;
using Xunit;

namespace OrderFulfillment.Notifier.Tests
{
    /// <summary>
    ///   The suite of tests for the <see cref="EntryPoint" />
    ///   class.
    /// </summary>
    /// 
    public class EntryPointTests
    {
        /// <summary>
        ///   Verifies behavior of the ConfigureDependencyResolver method.
        ///   this is mucking with static members. It is not parallel safe
        /// </summary>
        /// 
        [Fact]
        public void DependencyResolverDiscoversWebJobFunctions()
        {
            var mockConfigurationFactory = new Mock<IConfigurationFactory>();
            var notifyFailureConfig      = new NotifyOfFatalFailureServiceBusQueueCommandPublisherConfiguration { QueueName = "Three" };

            mockConfigurationFactory
                .Setup(factory => factory.Create(It.IsAny<Type>()))
                .Returns<Type>(type =>
                {
                    if (type == typeof(NotifyOfFatalFailureServiceBusQueueCommandPublisherConfiguration))
                    {
                        return notifyFailureConfig;
                    }

                    return Activator.CreateInstance(type);
                });

            var container = EntryPoint.CreateDependencyResolver( () => mockConfigurationFactory.Object);

            using (var scope = container.BeginLifetimeScope())
            {
                var orderProcessorFunctions = scope.Resolve<NotifierFunctions>();
                orderProcessorFunctions.Should().NotBeNull("because the Notifier WebJob functions should be resolvable");
            }
        }

        /// <summary>
        ///   Verifies behavior of the ConfigureDependencyResolver method.
        ///   this is mucking with static members. It is not parallel safe
        /// </summary>
        /// 
        [Fact]
        public void DependencyResolverDiscoversCommandPublishers()
        {
            var mockConfigurationFactory = new Mock<IConfigurationFactory>();
            var notifyFailureConfig      = new NotifyOfFatalFailureServiceBusQueueCommandPublisherConfiguration { QueueName = "Three" };


            mockConfigurationFactory
                .Setup(factory => factory.Create(It.IsAny<Type>()))
                .Returns<Type>(type =>
                {
                     if (type == typeof(NotifyOfFatalFailureServiceBusQueueCommandPublisherConfiguration))
                    {
                        return notifyFailureConfig;
                    }

                    return Activator.CreateInstance(type);
                });

            var container = EntryPoint.CreateDependencyResolver( () => mockConfigurationFactory.Object);

            using (var scope = container.BeginLifetimeScope())
            {
                var processOrderPublisher = scope.Resolve<ICommandPublisher<NotifyOfFatalFailure>>();
                processOrderPublisher.Should().NotBeNull("because the NotifiyOfFatalFalure publisher should be resolvable");
            }
        }

        /// <summary>
        ///   Verifies behavior of the ConfigureDependencyResolver method.
        ///   this is mucking with static members. It is not parallel safe
        /// </summary>
        /// 
        [Fact]
        public void DependencyResolverDependencyResolverDiscoversEventPublishers()
        {
            var mockConfigurationFactory = new Mock<IConfigurationFactory>();
            var eventBaseConfig          = new EventBaseServiceBusTopicEventPublisherConfiguration { TopicName = "One" };

            mockConfigurationFactory
                .Setup(factory => factory.Create(It.IsAny<Type>()))
                .Returns<Type>(type =>
                {
                    if (type == typeof(ServiceBusTopicEventPublisherConfiguration<EventBase>))
                    {
                        return eventBaseConfig;
                    }

                    return Activator.CreateInstance(type);
                });

            var container = EntryPoint.CreateDependencyResolver( () => mockConfigurationFactory.Object);

            using (var scope = container.BeginLifetimeScope())
            {
                var eventBasePublisher = scope.Resolve<IEventPublisher<EventBase>>();
                eventBasePublisher.Should().NotBeNull("because the EventBase publisher should be resolvable");
            }
        }
    }
}
