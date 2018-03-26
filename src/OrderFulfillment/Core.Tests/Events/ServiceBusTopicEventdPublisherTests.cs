using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using OrderFulfillment.Core.Configuration;
using OrderFulfillment.Core.Events;
using OrderFulfillment.Core.Infrastructure;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using Serilog;
using Xunit;

namespace OrderFulfillment.Core.Tests.Events
{
    /// <summary>
    ///   The suite of tests for the <see cref="ServiceBusTopicEventPublisher{T}" />
    ///   class.
    /// </summary>
    public class ServiceBusTopicEventPublisherTests
    {
        /// <summary>
        ///   Verifies behavior of the constructor.
        /// </summary>
        /// 
        [Fact]
        public void ConstructorValidatesTheLogger()
        {
            Action actionUnderTest = () => new ServiceBusTopicEventPublisher<EventBase>(null, new ServiceBusTopicEventPublisherConfiguration<EventBase>());
            actionUnderTest.ShouldThrow<ArgumentNullException>("because the logger must be provided");
        }

        /// <summary>
        ///   Verifies behavior of the constructor.
        /// </summary>
        /// 
        [Fact]
        public void ConstructorValidatesTheConfiguration()
        {
            Action actionUnderTest = () => new ServiceBusTopicEventPublisher<EventBase>(Mock.Of<ILogger>(), null);
            actionUnderTest.ShouldThrow<ArgumentNullException>("because the logger must be provided");
        }

        /// <summary>
        ///   Verifies behavior of the <see cref="ServiceBusTopicEventPublisher{T}.PublishAsync(T)" />
        ///   method.
        /// </summary>
        /// 
        [Fact]
        public async Task PublishAsyncSendsAMessage()
        {
            var called        = false;
            var configuration = new ServiceBusTopicEventPublisherConfiguration<EventBase>();

            Func<BrokeredMessage, Task> sendMessage = message =>
            {
                called = true;
                return Task.CompletedTask;
            };

            var testPublisher = new TestPublisher<EventBase>(Mock.Of<ILogger>(), configuration, sendMessage);

            await testPublisher.PublishAsync(new OrderReceived());
           
            called.Should().BeTrue("because a message should have been sent to the topic");
        }

        /// <summary>
        ///   Verifies functionality of the <see cref="ServiceBusTopicEventPublisher{T}.CreateTopicClient" />
        ///   method.
        /// </summary>
        /// 
        [Fact]
        public void ClientCreationSetsTheRetryPolicy()
        {
            var configuration = new ServiceBusTopicEventPublisherConfiguration<EventBase> 
            { 
                ServiceBusConnectionString      = "Endpoint=sb://someorderfulfillment.servicebus.windows.net/;SharedAccessKeyName=Fulfillment-App;SharedAccessKey=3L/M5xPb7Lh4KSXJAj6h/8egK9EEZdKyYdt0at21mLI=", 
                TopicName                       = "fake-topic",
                RetryMaximumAttempts            = 7, 
                RetryMinimalBackoffTimeSeconds  = 6, 
                RetryMaximumlBackoffTimeSeconds = 8                 
            };

            var publisher = new ServiceBusTopicEventPublisher<EventBase>(Mock.Of<ILogger>(), configuration);
            var client    = (TopicClient)typeof(ServiceBusTopicEventPublisher<EventBase>).GetMethod("CreateTopicClient", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(publisher, new [] { configuration });
            var policy    = client.RetryPolicy as RetryExponential;

            policy.Should().NotBeNull("because the retry policy should have been set");
            policy.MaxRetryCount.Should().Be(configuration.RetryMaximumAttempts, "because the configuration should have been used to set the max attempts");
            policy.MaximumBackoff.TotalSeconds.Should().Be(configuration.RetryMaximumlBackoffTimeSeconds, "because configuration should have been used to set the max backoff time");
            policy.MinimalBackoff.TotalSeconds.Should().Be(configuration.RetryMinimalBackoffTimeSeconds, "because the configuration should have been used to set the min backoff time");
        }

        /// <summary>
        ///   Verifies behavior of the <see cref="ServiceBusTopicEventPublisher{T}.PublishAsync(T)" />
        ///   method.
        /// </summary>
        /// 
        [Fact]
        public async Task PublishAsyncSendsAMessageThatMatchesTheEvents()
        {
            using (var messageBody = new MemoryStream())
            {
                var message       = default(BrokeredMessage);
                var configuration = new ServiceBusTopicEventPublisherConfiguration<EventBase>();

                Func<BrokeredMessage, Task> sendMessage = msg =>
                {
                    msg.GetBody<Stream>().CopyTo(messageBody);
                    messageBody.Seek(0, SeekOrigin.Begin);

                    message = msg.Clone();
                    return Task.CompletedTask;
                };

                var @event = new OrderReceived
                {
                    OrderId         = "ABC123",
                    PartnerCode     = "SQUIRE",
                    Id              = Guid.NewGuid(),
                    CorrelationId   = Guid.NewGuid().ToString(),
                    OccurredTimeUtc = new DateTime(2017, 01, 05, 5, 10, 30, DateTimeKind.Utc),
                    CurrentUser     = null,
                    Sequence        = 65
                };

                var testPublisher = new TestPublisher<EventBase>(Mock.Of<ILogger>(), configuration, sendMessage);

                await testPublisher.PublishAsync(@event);
           
                message.CorrelationId.Should().Be(@event.CorrelationId, "because the correlation id should have been copied to the message");
                message.MessageId.Should().Be(@event.Id.ToString(), "because the event id should have been copied to the message");
                message.ContentType.Should().Be(MimeTypes.Json, "becaue the message should have the correct type");


                var serializer = new JsonSerializer { ContractResolver = new CamelCasePropertyNamesContractResolver() };
                serializer.Converters.Add(new StringEnumConverter());

                var messageEvent = default(EventBase);

                using (var reader = new StreamReader(messageBody))
                using (var jsonReader = new JsonTextReader(reader))
                {
                
                    messageEvent = serializer.Deserialize<EventBase>(jsonReader);
                    reader.Close();
                    jsonReader.Close();
                }

                messageEvent.ShouldBeEquivalentTo(@event, "because the events should match");

                messageBody?.Close();
                message?.Dispose();
            }   
        }
        
        #region NestedClasses

            private class TestPublisher<T> : ServiceBusTopicEventPublisher<T> where T : EventBase
            {
                Func<BrokeredMessage, Task> sendMessageAsyncDelegate;

                public TestPublisher(ILogger                                       logger, 
                                     ServiceBusTopicEventPublisherConfiguration<T> configuration,
                                     Func<BrokeredMessage, Task>                   sendMessageAsyncDelegate) : base(logger, configuration)
                {

                    this.sendMessageAsyncDelegate = sendMessageAsyncDelegate ?? (_ => Task.CompletedTask);
                }

                protected override Task SendMessageAsync(BrokeredMessage message) =>
                    this.sendMessageAsyncDelegate(message);
        }

        #endregion
    }
}