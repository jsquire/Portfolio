using System;
using System.Threading.Tasks;
using Autofac;
using FluentAssertions;
using OrderFulfillment.Core.Commands;
using OrderFulfillment.Core.Configuration;
using OrderFulfillment.Core.Events;
using OrderFulfillment.OrderSubmitter.Functions;
using Moq;
using Xunit;

namespace OrderFulfillment.OrderSubmitter.Tests
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
            var processOrderConfig       = new ProcessOrderServiceBusQueueCommandPublisherConfiguration { QueueName = "One" };
            var submitOrderConfig        = new SubmitOrderForProductionServiceBusQueueCommandPublisherConfiguration { QueueName = "Two" };
            var notifyFailureConfig      = new NotifyOfFatalFailureServiceBusQueueCommandPublisherConfiguration { QueueName = "Three" };

            mockConfigurationFactory
                .Setup(factory => factory.Create(It.IsAny<Type>()))
                .Returns<Type>(type =>
                {
                    if (type == typeof(ProcessOrderServiceBusQueueCommandPublisherConfiguration))
                    {
                        return processOrderConfig;
                    }
                    else if (type == typeof(SubmitOrderForProductionServiceBusQueueCommandPublisherConfiguration))
                    {
                        return submitOrderConfig;
                    }
                    else if (type == typeof(NotifyOfFatalFailureServiceBusQueueCommandPublisherConfiguration))
                    {
                        return notifyFailureConfig;
                    }

                    return Activator.CreateInstance(type);
                });

            var container = EntryPoint.CreateDependencyResolver( () => mockConfigurationFactory.Object);

            using (var scope = container.BeginLifetimeScope())
            {
                var orderProcessorFunctions = scope.Resolve<OrderSubmitterFunctions>();
                orderProcessorFunctions.Should().NotBeNull("because the Order Submitter WebJob functions should be resolvable");
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
            var processOrderConfig       = new ProcessOrderServiceBusQueueCommandPublisherConfiguration { QueueName = "One" };
            var submitOrderConfig        = new SubmitOrderForProductionServiceBusQueueCommandPublisherConfiguration { QueueName = "Two" };

            mockConfigurationFactory
                .Setup(factory => factory.Create(It.IsAny<Type>()))
                .Returns<Type>(type =>
                {
                    if (type == typeof(ProcessOrderServiceBusQueueCommandPublisherConfiguration))
                    {
                        return processOrderConfig;
                    }
                    else if (type == typeof(SubmitOrderForProductionServiceBusQueueCommandPublisherConfiguration))
                    {
                        return submitOrderConfig;
                    }

                    return Activator.CreateInstance(type);
                });

            var container = EntryPoint.CreateDependencyResolver( () => mockConfigurationFactory.Object);

            using (var scope = container.BeginLifetimeScope())
            {
                var processOrderPublisher = scope.Resolve<ICommandPublisher<ProcessOrder>>();
                processOrderPublisher.Should().NotBeNull("because the ProcessOrder publisher should be resolvable");

                var submitOrderPublisher = scope.Resolve<ICommandPublisher<SubmitOrderForProduction>>();
                submitOrderPublisher.Should().NotBeNull("because the SubmitOrderForProduction publisher should be resolvable");
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
