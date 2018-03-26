using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using OrderFulfillment.Core.Commands;
using OrderFulfillment.Core.Configuration;
using OrderFulfillment.Core.Infrastructure;
using Moq;
using Moq.Protected;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using NodaTime;
using Serilog;
using Xunit;

namespace OrderFulfillment.Core.Tests.Commands
{
    /// <summary>
    ///   The suite of tests for the <see cref="ServiceBusQueueCommandPublisher{T}" />
    ///   class.
    /// </summary>
    public class ServiceBusQueueCommandPublisherTests
    {
        /// <summary>
        ///   Verifies behavior of the constructor.
        /// </summary>
        /// 
        [Fact]
        public void ConstructorValidatesTheLogger()
        {
            Action actionUnderTest = () => new ServiceBusQueueCommandPublisher<ProcessOrder>(null, new ServiceBusQueueCommandPublisherConfiguration<ProcessOrder>());
            actionUnderTest.ShouldThrow<ArgumentNullException>("because the logger must be provided");
        }

        /// <summary>
        ///   Verifies behavior of the constructor.
        /// </summary>
        /// 
        [Fact]
        public void ConstructorValidatesTheConfiguration()
        {
            Action actionUnderTest = () => new ServiceBusQueueCommandPublisher<ProcessOrder>(Mock.Of<ILogger>(), null);
            actionUnderTest.ShouldThrow<ArgumentNullException>("because the logger must be provided");
        }

        /// <summary>
        ///   Verifies behavior of the <see cref="ServiceBusQueueCommandPublisher{T}.PublishAsync(T)" />
        ///   method.
        /// </summary>
        /// 
        [Fact]
        public async Task PublishAsyncSendsAMessage()
        {
            var called        = false;
            var configuration = new ServiceBusQueueCommandPublisherConfiguration<ProcessOrder>();

            Func<BrokeredMessage, Instant?, Task> sendMessage = (message, publishTime) =>
            {
                called = true;
                return Task.CompletedTask;
            };

            var testPublisher = new TestPublisher<ProcessOrder>(Mock.Of<ILogger>(), configuration, sendMessage);

            await testPublisher.PublishAsync(new ProcessOrder());
           
            called.Should().BeTrue("because a message should have been sent to the queue");
        }

        /// <summary>
        ///   Verifies behavior of the <see cref="ServiceBusQueueCommandPublisher{T}.PublishAsync(T)" />
        ///   method.
        /// </summary>
        /// 
        [Fact]
        public async Task PublishAsynPublishesAtTheRequestedTime()
        {
            var scheduledTime = default(Instant?);
            var configuration = new ServiceBusQueueCommandPublisherConfiguration<ProcessOrder>();

            Func<BrokeredMessage, Instant?, Task> sendMessage = (message, publishTime) =>
            {
                scheduledTime = publishTime;
                return Task.CompletedTask;
            };

            var testPublisher = new TestPublisher<ProcessOrder>(Mock.Of<ILogger>(), configuration, sendMessage);
            var expectedTime  = Instant.FromDateTimeUtc(new DateTime(2012, 08, 24, 05, 15, 44, DateTimeKind.Utc));

            await testPublisher.PublishAsync(new ProcessOrder(), expectedTime);
           
            scheduledTime.HasValue.Should().BeTrue("because the scheduled time should be set");
            scheduledTime.Value.ShouldBeEquivalentTo(expectedTime, "because the scheduled time should honor the requested publish time");
        }

        /// <summary>
        ///   Verifies functionality of the <see cref="ServiceBusQueueCommandPublisher{T}.CreateQueueClient" />
        ///   method.
        /// </summary>
        /// 
        [Fact]
        public void ClientCreationSetsTheRetryPolicy()
        {
            var configuration = new ServiceBusQueueCommandPublisherConfiguration<ProcessOrder> 
            { 
                ServiceBusConnectionString      = "Endpoint=sb://someorderfulfillment.servicebus.windows.net/;SharedAccessKeyName=Fulfillment-App;SharedAccessKey=3L/M5xPb7Lh4KSXJAj6h/8egK9EEZdKyYdt0at21mLI=", 
                QueueName                       = "fake-queue",
                RetryMaximumAttempts            = 2, 
                RetryMinimalBackoffTimeSeconds  = 3, 
                RetryMaximumlBackoffTimeSeconds = 4                 
            };

            var publisher = new ServiceBusQueueCommandPublisher<ProcessOrder>(Mock.Of<ILogger>(), configuration);
            var client    = (QueueClient)typeof(ServiceBusQueueCommandPublisher<ProcessOrder>).GetMethod("CreateQueueClient", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(publisher, new [] { configuration });
            var policy    = client.RetryPolicy as RetryExponential;

            policy.Should().NotBeNull("because the retry policy should have been set");
            policy.MaxRetryCount.Should().Be(configuration.RetryMaximumAttempts, "because the configuration should have been used to set the max attempts");
            policy.MaximumBackoff.TotalSeconds.Should().Be(configuration.RetryMaximumlBackoffTimeSeconds, "because configuration should have been used to set the max backoff time");
            policy.MinimalBackoff.TotalSeconds.Should().Be(configuration.RetryMinimalBackoffTimeSeconds, "because the configuration should have been used to set the min backoff time");
        }

        /// <summary>
        ///   Verifies behavior of the <see cref="ServiceBusQueueCommandPublisher{T}.PublishAsync(T)" />
        ///   method.
        /// </summary>
        /// 
        [Fact]
        public async Task PublishAsyncSendsAMessageThatMatchesTheCommand()
        {
            using (var messageBody = new MemoryStream())
            {
                var message       = default(BrokeredMessage);
                var configuration = new ServiceBusQueueCommandPublisherConfiguration<ProcessOrder>();

                Func<BrokeredMessage, Instant?, Task> sendMessage = (msg, time) =>
                {
                    msg.GetBody<Stream>().CopyTo(messageBody);
                    messageBody.Seek(0, SeekOrigin.Begin);

                    message = msg.Clone();
                    return Task.CompletedTask;
                };

                 var command = new ProcessOrder
                {
                    OrderId         = "ABC123",
                    PartnerCode     = "SQUIRE",
                    Assets          = new Dictionary<string, string> {{"one", "val1"}, {"two", "val2" }},
                    Emulation       = null,
                    Id              = Guid.NewGuid(),
                    CorrelationId   = Guid.NewGuid().ToString(),
                    OccurredTimeUtc = new DateTime(2017, 01, 05, 5, 10, 30, DateTimeKind.Utc),
                    CurrentUser     = null,
                    Sequence        = 65
                };

                var testPublisher = new TestPublisher<ProcessOrder>(Mock.Of<ILogger>(), configuration, sendMessage);

                await testPublisher.PublishAsync(command);
           
                message.CorrelationId.Should().Be(command.CorrelationId, "because the correlation id should have been copied to the message");
                message.MessageId.Should().Be(command.Id.ToString(), "because the command id should have been copied to the message");
                message.ContentType.Should().Be(MimeTypes.Json, "becaue the message should have the correct type");


                var serializer = new JsonSerializer { ContractResolver = new CamelCasePropertyNamesContractResolver() };
                serializer.Converters.Add(new StringEnumConverter());

                var messageCommand = default(ProcessOrder);

                using (var reader = new StreamReader(messageBody))
                using (var jsonReader = new JsonTextReader(reader))
                {
                
                    messageCommand = serializer.Deserialize<ProcessOrder>(jsonReader);
                    reader.Close();
                    jsonReader.Close();
                }

                messageCommand.ShouldBeEquivalentTo(command, "because the commands should match");

                messageBody?.Close();
                message?.Dispose();
            }
        }
        
        #region NestedClasses

            private class TestPublisher<T> : ServiceBusQueueCommandPublisher<T> where T : CommandBase
            {
                Func<BrokeredMessage, Instant?, Task> sendMessageAsyncDelegate;

                public TestPublisher(ILogger                                         logger, 
                                     ServiceBusQueueCommandPublisherConfiguration<T> configuration,
                                     Func<BrokeredMessage, Instant?, Task>           sendMessageAsyncDelegate) : base(logger, configuration)
                {

                    this.sendMessageAsyncDelegate = sendMessageAsyncDelegate ?? ( (message, time) => Task.CompletedTask);
                }

                protected override Task SendMessageAsync(BrokeredMessage message, Instant? publishTime = null) =>
                    this.sendMessageAsyncDelegate(message, publishTime);
        }

        #endregion
    }
}
