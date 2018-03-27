using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.ServiceBus.Messaging;
using OrderFulfillment.Core.Commands;
using OrderFulfillment.Core.Events;
using OrderFulfillment.Core.Exceptions;
using OrderFulfillment.Core.Infrastructure;
using OrderFulfillment.Core.Models.Operations;
using OrderFulfillment.OrderProcessor.Functions;
using OrderFulfillment.OrderProcessor.Infrastructure;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using NodaTime;
using Serilog;
using Xunit;

namespace OrderFulfillment.OrderProcessor.Tests.Functions
{
    /// <summary>
    ///   The suite of tests for the <see cref="OrderProcessorFunctions" />
    ///   class.
    /// </summary>
    /// 
    public class OrderProcessorFunctionsTests
    {
        /// <summary>
        ///   Verifies behavior of the constructor.
        /// </summary>
        /// 
        [Fact]
        public void ConstructorValidatesTheSerializer()
        {
            Action actionUnderTest = () => new OrderProcessorFunctions(null, new CommandRetryThresholds(), Mock.Of<IClock>(), Mock.Of<IOrderProcessor>(), Mock.Of<ICommandPublisher<ProcessOrder>>(), Mock.Of<ICommandPublisher<SubmitOrderForProduction>>(), Mock.Of<ICommandPublisher<NotifyOfFatalFailure>>(), Mock.Of<IEventPublisher<EventBase>>(), Mock.Of<ILogger>(), Mock.Of<IDisposable>());
            actionUnderTest.ShouldThrow<ArgumentNullException>("because the serializer is required");
        }

        /// <summary>
        ///   Verifies behavior of the constructor.
        /// </summary>
        /// 
        [Fact]
        public void ConstructorValidatesTheThresholds()
        {
            Action actionUnderTest = () => new OrderProcessorFunctions(new JsonSerializer(), null, Mock.Of<IClock>(), Mock.Of<IOrderProcessor>(), Mock.Of<ICommandPublisher<ProcessOrder>>(), Mock.Of<ICommandPublisher<SubmitOrderForProduction>>(), Mock.Of<ICommandPublisher<NotifyOfFatalFailure>>(), Mock.Of<IEventPublisher<EventBase>>(), Mock.Of<ILogger>(), Mock.Of<IDisposable>());
            actionUnderTest.ShouldThrow<ArgumentNullException>("because the thresholds are required");
        }

        /// <summary>
        ///   Verifies behavior of the constructor.
        /// </summary>
        /// 
        [Fact]
        public void ConstructorValidatesTheClock()
        {
            Action actionUnderTest = () => new OrderProcessorFunctions(new JsonSerializer(), new CommandRetryThresholds(), null, Mock.Of<IOrderProcessor>(), Mock.Of<ICommandPublisher<ProcessOrder>>(), Mock.Of<ICommandPublisher<SubmitOrderForProduction>>(), Mock.Of<ICommandPublisher<NotifyOfFatalFailure>>(), Mock.Of<IEventPublisher<EventBase>>(), Mock.Of<ILogger>(), Mock.Of<IDisposable>());
            actionUnderTest.ShouldThrow<ArgumentNullException>("because the clock is required");
        }

        /// <summary>
        ///   Verifies behavior of the constructor.
        /// </summary>
        /// 
        [Fact]
        public void ConstructorValidatesTheOrderProcessor()
        {
            Action actionUnderTest = () => new OrderProcessorFunctions(new JsonSerializer(), new CommandRetryThresholds(), Mock.Of<IClock>(), null, Mock.Of<ICommandPublisher<ProcessOrder>>(), Mock.Of<ICommandPublisher<SubmitOrderForProduction>>(), Mock.Of<ICommandPublisher<NotifyOfFatalFailure>>(), Mock.Of<IEventPublisher<EventBase>>(), Mock.Of<ILogger>(), Mock.Of<IDisposable>());
            actionUnderTest.ShouldThrow<ArgumentNullException>("because the order processor is required");
        }

        /// <summary>
        ///   Verifies behavior of the constructor.
        /// </summary>
        /// 
        [Fact]
        public void ConstructorValidatesTheProcessOrderCommandPublisher()
        {
            Action actionUnderTest = () => new OrderProcessorFunctions(new JsonSerializer(), new CommandRetryThresholds(), Mock.Of<IClock>(), Mock.Of<IOrderProcessor>(), null, Mock.Of<ICommandPublisher<SubmitOrderForProduction>>(), Mock.Of<ICommandPublisher<NotifyOfFatalFailure>>(), Mock.Of<IEventPublisher<EventBase>>(), Mock.Of<ILogger>(), Mock.Of<IDisposable>());
            actionUnderTest.ShouldThrow<ArgumentNullException>("because the submit order command publisher is required");
        }

        /// <summary>
        ///   Verifies behavior of the constructor.
        /// </summary>
        /// 
        [Fact]
        public void ConstructorValidatesTheSubmitOrderCommandPublisher()
        {
            Action actionUnderTest = () => new OrderProcessorFunctions(new JsonSerializer(), new CommandRetryThresholds(), Mock.Of<IClock>(), Mock.Of<IOrderProcessor>(), Mock.Of<ICommandPublisher<ProcessOrder>>(), null, Mock.Of<ICommandPublisher<NotifyOfFatalFailure>>(), Mock.Of<IEventPublisher<EventBase>>(), Mock.Of<ILogger>(), Mock.Of<IDisposable>());
            actionUnderTest.ShouldThrow<ArgumentNullException>("because the submit order command publisher is required");
        }

        /// <summary>
        ///   Verifies behavior of the constructor.
        /// </summary>
        /// 
        [Fact]
        public void ConstructorValidatesTheNotifyFailureCommandPublisher()
        {
            Action actionUnderTest = () => new OrderProcessorFunctions(new JsonSerializer(), new CommandRetryThresholds(), Mock.Of<IClock>(), Mock.Of<IOrderProcessor>(), Mock.Of<ICommandPublisher<ProcessOrder>>(), Mock.Of<ICommandPublisher<SubmitOrderForProduction>>(), null, Mock.Of<IEventPublisher<EventBase>>(), Mock.Of<ILogger>(), Mock.Of<IDisposable>());
            actionUnderTest.ShouldThrow<ArgumentNullException>("because the notify failure command publisher is required");
        }

        /// <summary>
        ///   Verifies behavior of the constructor.
        /// </summary>
        /// 
        [Fact]
        public void ConstructorValidatesTheEventPublisher()
        {
            Action actionUnderTest = () => new OrderProcessorFunctions(new JsonSerializer(), new CommandRetryThresholds(), Mock.Of<IClock>(), Mock.Of<IOrderProcessor>(), Mock.Of<ICommandPublisher<ProcessOrder>>(), Mock.Of<ICommandPublisher<SubmitOrderForProduction>>(), Mock.Of<ICommandPublisher<NotifyOfFatalFailure>>(), null, Mock.Of<ILogger>(), Mock.Of<IDisposable>());
            actionUnderTest.ShouldThrow<ArgumentNullException>("because the event publisher is required");
        }

        /// <summary>
        ///   Verifies behavior of the constructor.
        /// </summary>
        /// 
        [Fact]
        public void ConstructorValidatesTheLogger()
        {
            Action actionUnderTest = () => new OrderProcessorFunctions(new JsonSerializer(), new CommandRetryThresholds(), Mock.Of<IClock>(), Mock.Of<IOrderProcessor>(), Mock.Of<ICommandPublisher<ProcessOrder>>(), Mock.Of<ICommandPublisher<SubmitOrderForProduction>>(), Mock.Of<ICommandPublisher<NotifyOfFatalFailure>>(), Mock.Of<IEventPublisher<EventBase>>(), null, Mock.Of<IDisposable>());
            actionUnderTest.ShouldThrow<ArgumentNullException>("because the logger is required");
        }

        /// <summary>
        ///   Verifies behavior of the <see cref="OrderProcessorFunctions.HandleProcessOrderAsync" />
        ///   method;
        /// </summary>
        /// 
        [Fact]
        public void HandleProcessOrderAsyncValidatesTheBrokeredMessage()
        {
            var mockClock            = new Mock<IClock>();
            var mockProcessor        = new Mock<IOrderProcessor>();
            var mockProcessPublisher = new Mock<ICommandPublisher<ProcessOrder>>();
            var mockSubmitPublisher =  new Mock<ICommandPublisher<SubmitOrderForProduction>>();
            var mockFailurePublisher = new Mock<ICommandPublisher<NotifyOfFatalFailure>>();
            var mockEventPublisher   = new Mock<IEventPublisher<EventBase>>();
            var mockLogger           = new Mock<ILogger>();
            var mockLifetimeScope    = new Mock<IDisposable>();
            var serializerSettings   = new JsonSerializerSettings();
            var serializer           = new JsonSerializer { ContractResolver = new CamelCasePropertyNamesContractResolver() };
            var processorFunctions   = new TestOrderProcessorFunctions(serializer, new CommandRetryThresholds(), mockClock.Object, mockProcessor.Object, mockProcessPublisher.Object, mockSubmitPublisher.Object, mockFailurePublisher.Object, mockEventPublisher.Object, mockLogger.Object, mockLifetimeScope.Object);

            serializer.Converters.Add(new StringEnumConverter());
            serializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            serializerSettings.Converters.Add(new StringEnumConverter());

            Action actionUnderTest = () => processorFunctions.HandleProcessOrderAsync(null).GetAwaiter().GetResult();
            actionUnderTest.ShouldThrow<ArgumentNullException>("because the brokered order message is required");
        }

        /// <summary>
        ///   Verifies behavior of the <see cref="OrderProcessorFunctions.HandleProcessOrderAsync" />
        ///   method;
        /// </summary>
        /// 
        [Fact]
        public void HandleProcessOrderAsyncFailsWhenTheMessageContainsNoCommand()
        {
            var mockClock            = new Mock<IClock>();
            var mockProcessor        = new Mock<IOrderProcessor>();
            var mockProcessPublisher = new Mock<ICommandPublisher<ProcessOrder>>();
            var mockSubmitPublisher =  new Mock<ICommandPublisher<SubmitOrderForProduction>>();
            var mockFailurePublisher = new Mock<ICommandPublisher<NotifyOfFatalFailure>>();
            var mockEventPublisher   = new Mock<IEventPublisher<EventBase>>();
            var mockLogger           = new Mock<ILogger>();
            var mockLifetimeScope    = new Mock<IDisposable>();
            var serializerSettings   = new JsonSerializerSettings();
            var serializer           = new JsonSerializer { ContractResolver = new CamelCasePropertyNamesContractResolver() };
            var processorFunctions   = new TestOrderProcessorFunctions(serializer, new CommandRetryThresholds(-1, 0, 0), mockClock.Object, mockProcessor.Object, mockProcessPublisher.Object, mockSubmitPublisher.Object, mockFailurePublisher.Object, mockEventPublisher.Object, mockLogger.Object, mockLifetimeScope.Object);

            serializer.Converters.Add(new StringEnumConverter());
            serializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            serializerSettings.Converters.Add(new StringEnumConverter());

            mockLogger
                .Setup(logger => logger.ForContext(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<bool>()))
                .Returns(mockLogger.Object);

            using (var memStream  = new MemoryStream())
            using (var writer     = new StreamWriter(memStream))
            using (var jsonWriter = new JsonTextWriter(writer))
            {                
                serializer.Serialize(jsonWriter, new { Name = "Test Guy" });
                jsonWriter.Flush();

                memStream.Seek(0, SeekOrigin.Begin);

                var message = new BrokeredMessage(memStream)
                {
                   ContentType   = MimeTypes.Json,
                   CorrelationId = Guid.NewGuid().ToString(),
                   MessageId     = Guid.NewGuid().ToString()
                };

                Action actionUnderTest = () => processorFunctions.HandleProcessOrderAsync(message).GetAwaiter().GetResult();
                actionUnderTest.ShouldThrow<MissingDependencyException>("because the brokered message must contain a command");

                jsonWriter.Close();
                writer.Close();
                memStream.Close();
            };
        }

        /// <summary>
        ///   Verifies behavior of the <see cref="OrderProcessorFunctions.HandleProcessOrderAsync" />
        ///   method;
        /// </summary>
        /// 
        [Fact]
        public async Task HandleProcessOrderAsyncProcessesTheOrder()
        {
            var mockClock            = new Mock<IClock>();
            var mockProcessor        = new Mock<IOrderProcessor>();
            var mockProcessPublisher = new Mock<ICommandPublisher<ProcessOrder>>();
            var mockSubmitPublisher =  new Mock<ICommandPublisher<SubmitOrderForProduction>>();
            var mockFailurePublisher = new Mock<ICommandPublisher<NotifyOfFatalFailure>>();
            var mockEventPublisher   = new Mock<IEventPublisher<EventBase>>();
            var mockLogger           = new Mock<ILogger>();
            var mockLifetimeScope    = new Mock<IDisposable>();
            var serializerSettings   = new JsonSerializerSettings();
            var serializer           = new JsonSerializer { ContractResolver = new CamelCasePropertyNamesContractResolver() };
            var processorFunctions   = new TestOrderProcessorFunctions(serializer, new CommandRetryThresholds(), mockClock.Object, mockProcessor.Object, mockProcessPublisher.Object, mockSubmitPublisher.Object, mockFailurePublisher.Object, mockEventPublisher.Object, mockLogger.Object, mockLifetimeScope.Object);
            var partner              = "That guy";
            var orderId              = "ABC123";
            var correlationId        = "Hello";
            var assets               = new Dictionary<string, string> {{ "one", "one" }};
            var emulation            = new DependencyEmulation { OrderDetails = new OperationResult { Payload = "Yay!" } };

            serializer.Converters.Add(new StringEnumConverter());
            serializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            serializerSettings.Converters.Add(new StringEnumConverter());

            mockLogger
                .Setup(logger => logger.ForContext(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<bool>()))
                .Returns(mockLogger.Object);

            mockProcessor
                .Setup(processor => processor.ProcessOrderAsync(It.Is<string>(partnerCode => partnerCode == partner), 
                                                                It.Is<string>(order => order == orderId), 
                                                                It.Is<IReadOnlyDictionary<string, string>>(set => ((set.Count == 1) && (set.Contains(assets.Single())))), 
                                                                It.Is<DependencyEmulation>(emu => emu.OrderDetails.Payload == emulation.OrderDetails.Payload), 
                                                                It.Is<string>(correlation => correlation == correlationId)))
                .ReturnsAsync(new OperationResult { Outcome = Outcome.Success })
                .Verifiable("The order should have been processed");

            var command = new ProcessOrder 
            {
               Id              = Guid.NewGuid(),
               CorrelationId   = correlationId,
               PartnerCode     = partner,
               OrderId         = orderId,
               Assets          = assets,
               Emulation       = emulation,
               OccurredTimeUtc = new DateTime(2017, 12, 09, 9, 0, 0, DateTimeKind.Utc)
            };

            using (var memStream  = new MemoryStream())
            using (var writer     = new StreamWriter(memStream))
            using (var jsonWriter = new JsonTextWriter(writer))
            {                
                serializer.Serialize(jsonWriter, command);
                jsonWriter.Flush();

                memStream.Seek(0, SeekOrigin.Begin);

                using (var message = new BrokeredMessage(memStream))
                {
                    message.ContentType   = MimeTypes.Json;
                    message.CorrelationId = Guid.NewGuid().ToString();
                    message.MessageId     = Guid.NewGuid().ToString();

                    await processorFunctions.HandleProcessOrderAsync(message);                    
                }

                jsonWriter.Close();
                writer.Close();
                memStream.Close();
            };

            mockProcessor.VerifyAll();
        }

        /// <summary>
        ///   Verifies behavior of the <see cref="OrderProcessorFunctions.HandleProcessOrderAsync" />
        ///   method;
        /// </summary>
        /// 
        [Fact]
        public void HandleProcessOrderAsyncThrowsWhenOrderProcessingFailsAndIsNotRetried()
        {
            var mockClock            = new Mock<IClock>();
            var mockProcessor        = new Mock<IOrderProcessor>();
            var mockProcessPublisher = new Mock<ICommandPublisher<ProcessOrder>>();
            var mockSubmitPublisher =  new Mock<ICommandPublisher<SubmitOrderForProduction>>();
            var mockFailurePublisher = new Mock<ICommandPublisher<NotifyOfFatalFailure>>();
            var mockEventPublisher   = new Mock<IEventPublisher<EventBase>>();
            var mockLogger           = new Mock<ILogger>();
            var mockLifetimeScope    = new Mock<IDisposable>();
            var serializerSettings   = new JsonSerializerSettings();
            var serializer           = new JsonSerializer { ContractResolver = new CamelCasePropertyNamesContractResolver() };
            var processorFunctions   = new TestOrderProcessorFunctions(serializer, new CommandRetryThresholds(-1, 0, 0), mockClock.Object, mockProcessor.Object, mockProcessPublisher.Object, mockSubmitPublisher.Object, mockFailurePublisher.Object, mockEventPublisher.Object, mockLogger.Object, mockLifetimeScope.Object);


            serializer.Converters.Add(new StringEnumConverter());
            serializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            serializerSettings.Converters.Add(new StringEnumConverter());

            mockLogger
                .Setup(logger => logger.ForContext(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<bool>()))
                .Returns(mockLogger.Object);

            mockProcessor
                .Setup(processor => processor.ProcessOrderAsync(It.IsAny<string>(), 
                                                                It.IsAny<string>(), 
                                                                It.IsAny<IReadOnlyDictionary<string, string>>(), 
                                                                It.IsAny<DependencyEmulation>(), 
                                                                It.IsAny<string>()))
                .ReturnsAsync(OperationResult.ExceptionResult);

            var command = new ProcessOrder 
            {
               Id              = Guid.NewGuid(),
               CorrelationId   = "ABC",
               PartnerCode     = "Bob",
               OrderId         = "123",
               Assets          = new Dictionary<string, string> {{ "one", "one" }},
               OccurredTimeUtc = new DateTime(2017, 12, 09, 9, 0, 0, DateTimeKind.Utc)
            };

            using (var memStream  = new MemoryStream())
            using (var writer     = new StreamWriter(memStream))
            using (var jsonWriter = new JsonTextWriter(writer))
            {                
                serializer.Serialize(jsonWriter, command);
                jsonWriter.Flush();

                memStream.Seek(0, SeekOrigin.Begin);

                using (var message = new BrokeredMessage(memStream))
                {
                    message.ContentType   = MimeTypes.Json;
                    message.CorrelationId = Guid.NewGuid().ToString();
                    message.MessageId     = Guid.NewGuid().ToString();

                    Action actionUnderTest = () => processorFunctions.HandleProcessOrderAsync(message).GetAwaiter().GetResult();

                    actionUnderTest.ShouldThrow<FailedtoHandleCommandException>("because the order processor failed to process");
                }

                jsonWriter.Close();
                writer.Close();
                memStream.Close();
            };

            mockFailurePublisher.Verify(publisher => publisher.TryPublishAsync(It.IsAny<NotifyOfFatalFailure>(), It.Is<Instant?>(value => value == null)), 
                Times.Once, 
                "The failure notification should have been published because the command wasn't eligible for retries");
        }

        /// <summary>
        ///   Verifies behavior of the <see cref="OrderProcessorFunctions.HandleProcessOrderAsync" />
        ///   method;
        /// </summary>
        /// 
        [Fact]
        public void HandleProcessOrderAsyncThrowsWhenOrderProcessingThrowsAndIsNotRetried()
        {
            var mockClock            = new Mock<IClock>();
            var mockProcessor        = new Mock<IOrderProcessor>();
            var mockProcessPublisher = new Mock<ICommandPublisher<ProcessOrder>>();
            var mockSubmitPublisher =  new Mock<ICommandPublisher<SubmitOrderForProduction>>();
            var mockFailurePublisher = new Mock<ICommandPublisher<NotifyOfFatalFailure>>();
            var mockEventPublisher   = new Mock<IEventPublisher<EventBase>>();
            var mockLogger           = new Mock<ILogger>();
            var mockLifetimeScope    = new Mock<IDisposable>();
            var serializerSettings   = new JsonSerializerSettings();
            var serializer           = new JsonSerializer { ContractResolver = new CamelCasePropertyNamesContractResolver() };
            var processorFunctions   = new TestOrderProcessorFunctions(serializer, new CommandRetryThresholds(-1, 0, 0), mockClock.Object, mockProcessor.Object, mockProcessPublisher.Object, mockSubmitPublisher.Object, mockFailurePublisher.Object, mockEventPublisher.Object, mockLogger.Object, mockLifetimeScope.Object);
            var expectedException    = new OrderProcessingException();


            serializer.Converters.Add(new StringEnumConverter());
            serializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            serializerSettings.Converters.Add(new StringEnumConverter());

            mockLogger
                .Setup(logger => logger.ForContext(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<bool>()))
                .Returns(mockLogger.Object);

            mockProcessor
                .Setup(processor => processor.ProcessOrderAsync(It.IsAny<string>(), 
                                                                It.IsAny<string>(), 
                                                                It.IsAny<IReadOnlyDictionary<string, string>>(), 
                                                                It.IsAny<DependencyEmulation>(), 
                                                                It.IsAny<string>()))
                .ThrowsAsync(expectedException);

            var command = new ProcessOrder 
            {
               Id              = Guid.NewGuid(),
               CorrelationId   = "ABC",
               PartnerCode     = "Bob",
               OrderId         = "123",
               Assets          = new Dictionary<string, string> {{ "one", "one" }},
               OccurredTimeUtc = new DateTime(2017, 12, 09, 9, 0, 0, DateTimeKind.Utc)
            };

            using (var memStream  = new MemoryStream())
            using (var writer     = new StreamWriter(memStream))
            using (var jsonWriter = new JsonTextWriter(writer))
            {                
                serializer.Serialize(jsonWriter, command);
                jsonWriter.Flush();

                memStream.Seek(0, SeekOrigin.Begin);

                using (var message = new BrokeredMessage(memStream))
                {
                    message.ContentType   = MimeTypes.Json;
                    message.CorrelationId = Guid.NewGuid().ToString();
                    message.MessageId     = Guid.NewGuid().ToString();

                    Action actionUnderTest = () => processorFunctions.HandleProcessOrderAsync(message).GetAwaiter().GetResult();

                    actionUnderTest.ShouldThrow<OrderProcessingException>("because the order processor experienced an exception")
                        .Subject.SingleOrDefault().Should().Be(expectedException, "because the exception should bubble");
                }

                jsonWriter.Close();
                writer.Close();
                memStream.Close();
            };

            mockFailurePublisher.Verify(publisher => publisher.TryPublishAsync(It.IsAny<NotifyOfFatalFailure>(), It.Is<Instant?>(value => value == null)), 
                Times.Once, 
                "The failure notification should have been published because the command wasn't eligible for retries");
        }

        /// <summary>
        ///   Verifies behavior of the <see cref="OrderProcessorFunctions.HandleProcessOrderAsync" />
        ///   method;
        /// </summary>
        /// 
        [Fact]
        public void HandleProcessOrderAsyncThrowsWhenOrderProcessingThrowsAndTheRetryThrows()
        {
            var mockClock            = new Mock<IClock>();
            var mockProcessor        = new Mock<IOrderProcessor>();
            var mockProcessPublisher = new Mock<ICommandPublisher<ProcessOrder>>();
            var mockSubmitPublisher =  new Mock<ICommandPublisher<SubmitOrderForProduction>>();
            var mockFailurePublisher = new Mock<ICommandPublisher<NotifyOfFatalFailure>>();
            var mockEventPublisher   = new Mock<IEventPublisher<EventBase>>();
            var mockLogger           = new Mock<ILogger>();
            var mockLifetimeScope    = new Mock<IDisposable>();
            var serializerSettings   = new JsonSerializerSettings();
            var serializer           = new JsonSerializer { ContractResolver = new CamelCasePropertyNamesContractResolver() };
            var processorFunctions   = new TestOrderProcessorFunctions(serializer, new CommandRetryThresholds(1, 1, 1), mockClock.Object, mockProcessor.Object, mockProcessPublisher.Object, mockSubmitPublisher.Object, mockFailurePublisher.Object, mockEventPublisher.Object, mockLogger.Object, mockLifetimeScope.Object);
            var expectedException    = new ApplicationException();


            serializer.Converters.Add(new StringEnumConverter());
            serializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            serializerSettings.Converters.Add(new StringEnumConverter());

            mockLogger
                .Setup(logger => logger.ForContext(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<bool>()))
                .Returns(mockLogger.Object);

            mockProcessPublisher
                .Setup(publisher => publisher.PublishAsync(It.IsAny<ProcessOrder>(), It.Is<Instant?>(value => value.HasValue)))
                .Throws(expectedException);

            mockProcessor
                .Setup(processor => processor.ProcessOrderAsync(It.IsAny<string>(), 
                                                                It.IsAny<string>(), 
                                                                It.IsAny<IReadOnlyDictionary<string, string>>(), 
                                                                It.IsAny<DependencyEmulation>(), 
                                                                It.IsAny<string>()))
                .ThrowsAsync(new OrderProcessingException());

            var command = new ProcessOrder 
            {
               Id              = Guid.NewGuid(),
               CorrelationId   = "ABC",
               PartnerCode     = "Bob",
               OrderId         = "123",
               Assets          = new Dictionary<string, string> {{ "one", "one" }},
               OccurredTimeUtc = new DateTime(2017, 12, 09, 9, 0, 0, DateTimeKind.Utc)
            };

            using (var memStream  = new MemoryStream())
            using (var writer     = new StreamWriter(memStream))
            using (var jsonWriter = new JsonTextWriter(writer))
            {                
                serializer.Serialize(jsonWriter, command);
                jsonWriter.Flush();

                memStream.Seek(0, SeekOrigin.Begin);

                using (var message = new BrokeredMessage(memStream))
                {
                    message.ContentType   = MimeTypes.Json;
                    message.CorrelationId = Guid.NewGuid().ToString();
                    message.MessageId     = Guid.NewGuid().ToString();

                    Action actionUnderTest = () => processorFunctions.HandleProcessOrderAsync(message).GetAwaiter().GetResult();

                    actionUnderTest.ShouldThrow<ApplicationException>("because command retry publishing experienced an exception")
                        .Subject.SingleOrDefault().Should().Be(expectedException, "because the exception should bubble");
                }

                jsonWriter.Close();
                writer.Close();
                memStream.Close();
            };
        }

        /// <summary>
        ///   Verifies behavior of the <see cref="OrderProcessorFunctions.HandleProcessOrderAsync" />
        ///   method;
        /// </summary>
        /// 
        [Fact]
        public async Task HandleProcessOrderAsyncDoesNotCompleteTheMessageWhenOrderProcessingFailsAndIsNotRetried()
        {
            var mockClock            = new Mock<IClock>();
            var mockProcessor        = new Mock<IOrderProcessor>();
            var mockProcessPublisher = new Mock<ICommandPublisher<ProcessOrder>>();
            var mockSubmitPublisher =  new Mock<ICommandPublisher<SubmitOrderForProduction>>();
            var mockFailurePublisher = new Mock<ICommandPublisher<NotifyOfFatalFailure>>();
            var mockEventPublisher   = new Mock<IEventPublisher<EventBase>>();
            var mockLogger           = new Mock<ILogger>();
            var mockLifetimeScope    = new Mock<IDisposable>();
            var serializerSettings   = new JsonSerializerSettings();
            var serializer           = new JsonSerializer { ContractResolver = new CamelCasePropertyNamesContractResolver() };
            var retryThresholds      = new CommandRetryThresholds(-1, 1, 1);
            var processorFunctions   = new TestOrderProcessorFunctions(serializer, retryThresholds, mockClock.Object, mockProcessor.Object, mockProcessPublisher.Object, mockSubmitPublisher.Object, mockFailurePublisher.Object, mockEventPublisher.Object, mockLogger.Object, mockLifetimeScope.Object);
            
            serializer.Converters.Add(new StringEnumConverter());
            serializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            serializerSettings.Converters.Add(new StringEnumConverter());

            mockLogger
                .Setup(logger => logger.ForContext(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<bool>()))
                .Returns(mockLogger.Object);

            mockProcessor
                .Setup(processor => processor.ProcessOrderAsync(It.IsAny<string>(), 
                                                                It.IsAny<string>(), 
                                                                It.IsAny<IReadOnlyDictionary<string, string>>(), 
                                                                It.IsAny<DependencyEmulation>(), 
                                                                It.IsAny<string>()))
                .ThrowsAsync(new OrderProcessingException());

            var command = new ProcessOrder 
            {
               Id              = Guid.NewGuid(),
               CorrelationId   = "ABC",
               PartnerCode     = "Bob",
               OrderId         = "123",
               Assets          = new Dictionary<string, string> {{ "one", "one" }},
               OccurredTimeUtc = new DateTime(2017, 12, 09, 9, 0, 0, DateTimeKind.Utc)
            };

            using (var memStream  = new MemoryStream())
            using (var writer     = new StreamWriter(memStream))
            using (var jsonWriter = new JsonTextWriter(writer))
            {                
                serializer.Serialize(jsonWriter, command);
                jsonWriter.Flush();

                memStream.Seek(0, SeekOrigin.Begin);

                using (var message = new BrokeredMessage(memStream))
                {
                    message.ContentType   = MimeTypes.Json;
                    message.CorrelationId = Guid.NewGuid().ToString();
                    message.MessageId     = Guid.NewGuid().ToString();

                   try { await processorFunctions.HandleProcessOrderAsync(message); }  catch {}
                }

                jsonWriter.Close();
                writer.Close();
                memStream.Close();
            };

            processorFunctions.CompletedMessages.Should().BeEmpty("because no messages should have been completed");
        }

        /// <summary>
        ///   Verifies behavior of the <see cref="OrderProcessorFunctions.HandleProcessOrderAsync" />
        ///   method;
        /// </summary>
        /// 
        [Fact]
        public async Task HandleProcessOrderAsyncCompletesTheMessageWhenOrderProcessingFailsAndIsRetried()
        {
            var mockClock            = new Mock<IClock>();
            var mockProcessor        = new Mock<IOrderProcessor>();
            var mockProcessPublisher = new Mock<ICommandPublisher<ProcessOrder>>();
            var mockSubmitPublisher =  new Mock<ICommandPublisher<SubmitOrderForProduction>>();
            var mockFailurePublisher = new Mock<ICommandPublisher<NotifyOfFatalFailure>>();
            var mockEventPublisher   = new Mock<IEventPublisher<EventBase>>();
            var mockLogger           = new Mock<ILogger>();
            var mockLifetimeScope    = new Mock<IDisposable>();
            var serializerSettings   = new JsonSerializerSettings();
            var serializer           = new JsonSerializer { ContractResolver = new CamelCasePropertyNamesContractResolver() };
            var retryThresholds      = new CommandRetryThresholds(1, 1, 1);
            var processorFunctions   = new TestOrderProcessorFunctions(serializer, retryThresholds, mockClock.Object, mockProcessor.Object, mockProcessPublisher.Object, mockSubmitPublisher.Object, mockFailurePublisher.Object, mockEventPublisher.Object, mockLogger.Object, mockLifetimeScope.Object);
            
            serializer.Converters.Add(new StringEnumConverter());
            serializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            serializerSettings.Converters.Add(new StringEnumConverter());

            mockLogger
                .Setup(logger => logger.ForContext(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<bool>()))
                .Returns(mockLogger.Object);

            mockProcessor
                .Setup(processor => processor.ProcessOrderAsync(It.IsAny<string>(), 
                                                                It.IsAny<string>(), 
                                                                It.IsAny<IReadOnlyDictionary<string, string>>(), 
                                                                It.IsAny<DependencyEmulation>(), 
                                                                It.IsAny<string>()))
                .ThrowsAsync(new OrderProcessingException());

            var command = new ProcessOrder 
            {
               Id              = Guid.NewGuid(),
               CorrelationId   = "ABC",
               PartnerCode     = "Bob",
               OrderId         = "123",
               Assets          = new Dictionary<string, string> {{ "one", "one" }},
               OccurredTimeUtc = new DateTime(2017, 12, 09, 9, 0, 0, DateTimeKind.Utc)
            };

            using (var memStream  = new MemoryStream())
            using (var writer     = new StreamWriter(memStream))
            using (var jsonWriter = new JsonTextWriter(writer))
            {                
                serializer.Serialize(jsonWriter, command);
                jsonWriter.Flush();

                memStream.Seek(0, SeekOrigin.Begin);


                using (var message = new BrokeredMessage(memStream))
                {
                    message.ContentType   = MimeTypes.Json;
                    message.CorrelationId = Guid.NewGuid().ToString();
                    message.MessageId     = Guid.NewGuid().ToString();

                   try { await processorFunctions.HandleProcessOrderAsync(message); }  catch {}
                }

                jsonWriter.Close();
                writer.Close();
                memStream.Close();
            };

            processorFunctions.CompletedMessages.Should().HaveCount(1, "because the message should have been completed");
        }

        /// <summary>
        ///   Verifies behavior of the <see cref="OrderProcessorFunctions.HandleProcessOrderAsync" />
        ///   method;
        /// </summary>
        /// 
        [Fact]
        public async Task HandleProcessOrderAsyncPublishesTheProperEventWhenOrderProcessingFails()
        {
            var mockClock            = new Mock<IClock>();
            var mockProcessor        = new Mock<IOrderProcessor>();
            var mockProcessPublisher = new Mock<ICommandPublisher<ProcessOrder>>();
            var mockSubmitPublisher =  new Mock<ICommandPublisher<SubmitOrderForProduction>>();
            var mockFailurePublisher = new Mock<ICommandPublisher<NotifyOfFatalFailure>>();
            var mockEventPublisher   = new Mock<IEventPublisher<EventBase>>();
            var mockLogger           = new Mock<ILogger>();
            var mockLifetimeScope    = new Mock<IDisposable>();
            var serializerSettings   = new JsonSerializerSettings();
            var serializer           = new JsonSerializer { ContractResolver = new CamelCasePropertyNamesContractResolver() };
            var processorFunctions   = new TestOrderProcessorFunctions(serializer, new CommandRetryThresholds(), mockClock.Object, mockProcessor.Object, mockProcessPublisher.Object, mockSubmitPublisher.Object, mockFailurePublisher.Object, mockEventPublisher.Object, mockLogger.Object, mockLifetimeScope.Object);
            
            serializer.Converters.Add(new StringEnumConverter());
            serializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            serializerSettings.Converters.Add(new StringEnumConverter());

            mockLogger
                .Setup(logger => logger.ForContext(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<bool>()))
                .Returns(mockLogger.Object);

            mockProcessor
                .Setup(processor => processor.ProcessOrderAsync(It.IsAny<string>(), 
                                                                It.IsAny<string>(), 
                                                                It.IsAny<IReadOnlyDictionary<string, string>>(), 
                                                                It.IsAny<DependencyEmulation>(), 
                                                                It.IsAny<string>()))
                .ThrowsAsync(new OrderProcessingException());

            var command = new ProcessOrder 
            {
               Id              = Guid.NewGuid(),
               CorrelationId   = "ABC",
               PartnerCode     = "Bob",
               OrderId         = "123",
               Assets          = new Dictionary<string, string> {{ "one", "one" }},
               OccurredTimeUtc = new DateTime(2017, 12, 09, 9, 0, 0, DateTimeKind.Utc)
            };

            using (var memStream  = new MemoryStream())
            using (var writer     = new StreamWriter(memStream))
            using (var jsonWriter = new JsonTextWriter(writer))
            {                
                serializer.Serialize(jsonWriter, command);
                jsonWriter.Flush();

                memStream.Seek(0, SeekOrigin.Begin);

                using (var message = new BrokeredMessage(memStream))
                {
                    message.ContentType   = MimeTypes.Json;
                    message.CorrelationId = Guid.NewGuid().ToString();
                    message.MessageId     = Guid.NewGuid().ToString();

                   try { await processorFunctions.HandleProcessOrderAsync(message); }  catch {}
                }

                jsonWriter.Close();
                writer.Close();
                memStream.Close();
            };

            mockEventPublisher.Verify(pub => pub.TryPublishAsync(It.Is<OrderProcessingFailed>(evt => 
                    ((evt.OrderId       == command.OrderId)        &&
                     (evt.CorrelationId == command.CorrelationId)  &&
                     (evt.PartnerCode   == command.PartnerCode)))), 
                Times.Once, "because the event should have been published");    
        }

        /// <summary>
        ///   Verifies behavior of the <see cref="OrderProcessorFunctions.HandleProcessOrderAsync" />
        ///   method;
        /// </summary>
        /// 
        [Fact]
        public async Task HandleProcessOrderAsyncCompletesTheMessageWhenOrderProcessingSucceeds()
        {
            var mockClock            = new Mock<IClock>();
            var mockProcessor        = new Mock<IOrderProcessor>();
            var mockProcessPublisher = new Mock<ICommandPublisher<ProcessOrder>>();
            var mockSubmitPublisher =  new Mock<ICommandPublisher<SubmitOrderForProduction>>();
            var mockFailurePublisher = new Mock<ICommandPublisher<NotifyOfFatalFailure>>();
            var mockEventPublisher   = new Mock<IEventPublisher<EventBase>>();
            var mockLogger           = new Mock<ILogger>();
            var mockLifetimeScope    = new Mock<IDisposable>();
            var serializerSettings   = new JsonSerializerSettings();
            var serializer           = new JsonSerializer { ContractResolver = new CamelCasePropertyNamesContractResolver() };
            var processorFunctions   = new TestOrderProcessorFunctions(serializer, new CommandRetryThresholds(), mockClock.Object, mockProcessor.Object, mockProcessPublisher.Object, mockSubmitPublisher.Object, mockFailurePublisher.Object, mockEventPublisher.Object, mockLogger.Object, mockLifetimeScope.Object);
            
            serializer.Converters.Add(new StringEnumConverter());
            serializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            serializerSettings.Converters.Add(new StringEnumConverter());

            mockLogger
                .Setup(logger => logger.ForContext(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<bool>()))
                .Returns(mockLogger.Object);

            mockProcessor
                .Setup(processor => processor.ProcessOrderAsync(It.IsAny<string>(), 
                                                                It.IsAny<string>(), 
                                                                It.IsAny<IReadOnlyDictionary<string, string>>(), 
                                                                It.IsAny<DependencyEmulation>(), 
                                                                It.IsAny<string>()))
                .ReturnsAsync(new OperationResult { Outcome = Outcome.Success });

            var command = new ProcessOrder 
            {
               Id              = Guid.NewGuid(),
               CorrelationId   = "ABC",
               PartnerCode     = "Bob",
               OrderId         = "123",
               Assets          = new Dictionary<string, string> {{ "one", "one" }},
               OccurredTimeUtc = new DateTime(2017, 12, 09, 9, 0, 0, DateTimeKind.Utc)
            };

            using (var memStream  = new MemoryStream())
            using (var writer     = new StreamWriter(memStream))
            using (var jsonWriter = new JsonTextWriter(writer))
            {                
                serializer.Serialize(jsonWriter, command);
                jsonWriter.Flush();

                memStream.Seek(0, SeekOrigin.Begin);

                using (var message = new BrokeredMessage(memStream))
                {
                    message.ContentType   = MimeTypes.Json;
                    message.CorrelationId = Guid.NewGuid().ToString();
                    message.MessageId     = Guid.NewGuid().ToString();

                   try { await processorFunctions.HandleProcessOrderAsync(message); }  catch {}
                }

                jsonWriter.Close();
                writer.Close();
                memStream.Close();
            };

            processorFunctions.CompletedMessages.Should().HaveCount(1, "because the message should have been completed");
        }

        /// <summary>
        ///   Verifies behavior of the <see cref="OrderProcessorFunctions.HandleProcessOrderAsync" />
        ///   method;
        /// </summary>
        /// 
        [Fact]
        public async Task HandleProcessOrderAsyncPublishesTheProperEventWhenOrderProcessingSucceeds()
        {
            var mockClock            = new Mock<IClock>();
            var mockProcessor        = new Mock<IOrderProcessor>();
            var mockProcessPublisher = new Mock<ICommandPublisher<ProcessOrder>>();
            var mockSubmitPublisher =  new Mock<ICommandPublisher<SubmitOrderForProduction>>();
            var mockFailurePublisher = new Mock<ICommandPublisher<NotifyOfFatalFailure>>();
            var mockEventPublisher   = new Mock<IEventPublisher<EventBase>>();
            var mockLogger           = new Mock<ILogger>();
            var mockLifetimeScope    = new Mock<IDisposable>();
            var serializerSettings   = new JsonSerializerSettings();
            var serializer           = new JsonSerializer { ContractResolver = new CamelCasePropertyNamesContractResolver() };
            var processorFunctions   = new TestOrderProcessorFunctions(serializer, new CommandRetryThresholds(), mockClock.Object, mockProcessor.Object, mockProcessPublisher.Object, mockSubmitPublisher.Object, mockFailurePublisher.Object, mockEventPublisher.Object, mockLogger.Object, mockLifetimeScope.Object);
            
            serializer.Converters.Add(new StringEnumConverter());
            serializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            serializerSettings.Converters.Add(new StringEnumConverter());

            mockLogger
                .Setup(logger => logger.ForContext(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<bool>()))
                .Returns(mockLogger.Object);

            mockProcessor
                .Setup(processor => processor.ProcessOrderAsync(It.IsAny<string>(), 
                                                                It.IsAny<string>(), 
                                                                It.IsAny<IReadOnlyDictionary<string, string>>(), 
                                                                It.IsAny<DependencyEmulation>(), 
                                                                It.IsAny<string>()))
                .ReturnsAsync(new OperationResult { Outcome = Outcome.Success });

            var command = new ProcessOrder 
            {
               Id              = Guid.NewGuid(),
               CorrelationId   = "ABC",
               PartnerCode     = "Bob",
               OrderId         = "123",
               Assets          = new Dictionary<string, string> {{ "one", "one" }},
               OccurredTimeUtc = new DateTime(2017, 12, 09, 9, 0, 0, DateTimeKind.Utc)
            };

            using (var memStream  = new MemoryStream())
            using (var writer     = new StreamWriter(memStream))
            using (var jsonWriter = new JsonTextWriter(writer))
            {                
                serializer.Serialize(jsonWriter, command);
                jsonWriter.Flush();

                memStream.Seek(0, SeekOrigin.Begin);

                using (var message = new BrokeredMessage(memStream))
                {
                    message.ContentType   = MimeTypes.Json;
                    message.CorrelationId = Guid.NewGuid().ToString();
                    message.MessageId     = Guid.NewGuid().ToString();

                   try { await processorFunctions.HandleProcessOrderAsync(message); }  catch {}
                }

                jsonWriter.Close();
                writer.Close();
                memStream.Close();
            };

            mockEventPublisher.Verify(pub => pub.TryPublishAsync(It.Is<OrderProcessed>(evt => 
                    ((evt is OrderProcessed)                                         &&
                     (((OrderProcessed)evt).OrderId       == command.OrderId)        &&
                     (((OrderProcessed)evt).CorrelationId == command.CorrelationId)  &&
                     (((OrderProcessed)evt).PartnerCode   == command.PartnerCode)))), 
                Times.Once, "because the event should have been published");    
        }

        /// <summary>
        ///   Verifies behavior of the <see cref="OrderProcessorFunctions.HandleProcessOrderAsync" />
        ///   method;
        /// </summary>
        /// 
        [Fact]
        public async Task HandleProcessOrderAsyncPublishesTheProperCommandWhenOrderProcessingSucceeds()
        {
            var mockClock            = new Mock<IClock>();
            var mockProcessor        = new Mock<IOrderProcessor>();
            var mockProcessPublisher = new Mock<ICommandPublisher<ProcessOrder>>();
            var mockSubmitPublisher =  new Mock<ICommandPublisher<SubmitOrderForProduction>>();
            var mockFailurePublisher = new Mock<ICommandPublisher<NotifyOfFatalFailure>>();
            var mockEventPublisher   = new Mock<IEventPublisher<EventBase>>();
            var mockLogger           = new Mock<ILogger>();
            var mockLifetimeScope    = new Mock<IDisposable>();
            var serializerSettings   = new JsonSerializerSettings();
            var serializer           = new JsonSerializer { ContractResolver = new CamelCasePropertyNamesContractResolver() };
            var processorFunctions   = new TestOrderProcessorFunctions(serializer, new CommandRetryThresholds(), mockClock.Object, mockProcessor.Object, mockProcessPublisher.Object, mockSubmitPublisher.Object, mockFailurePublisher.Object, mockEventPublisher.Object, mockLogger.Object, mockLifetimeScope.Object);
            
            serializer.Converters.Add(new StringEnumConverter());
            serializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            serializerSettings.Converters.Add(new StringEnumConverter());

            mockLogger
                .Setup(logger => logger.ForContext(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<bool>()))
                .Returns(mockLogger.Object);

            mockProcessor
                .Setup(processor => processor.ProcessOrderAsync(It.IsAny<string>(), 
                                                                It.IsAny<string>(), 
                                                                It.IsAny<IReadOnlyDictionary<string, string>>(), 
                                                                It.IsAny<DependencyEmulation>(), 
                                                                It.IsAny<string>()))
                .ReturnsAsync(new OperationResult { Outcome = Outcome.Success });

            var command = new ProcessOrder 
            {
               Id              = Guid.NewGuid(),
               CorrelationId   = "ABC",
               PartnerCode     = "Bob",
               OrderId         = "123",
               Assets          = new Dictionary<string, string> {{ "one", "one" }},
               OccurredTimeUtc = new DateTime(2017, 12, 09, 9, 0, 0, DateTimeKind.Utc)
            };

            using (var memStream  = new MemoryStream())
            using (var writer     = new StreamWriter(memStream))
            using (var jsonWriter = new JsonTextWriter(writer))
            {                
                serializer.Serialize(jsonWriter, command);
                jsonWriter.Flush();

                memStream.Seek(0, SeekOrigin.Begin);

                using (var message = new BrokeredMessage(memStream))
                {
                    message.ContentType   = MimeTypes.Json;
                    message.CorrelationId = Guid.NewGuid().ToString();
                    message.MessageId     = Guid.NewGuid().ToString();

                   try { await processorFunctions.HandleProcessOrderAsync(message); }  catch {}
                }

                jsonWriter.Close();
                writer.Close();
                memStream.Close();
            };

            mockSubmitPublisher.Verify(pub => pub.PublishAsync(
                It.Is<SubmitOrderForProduction>(cmd => 
                    ((cmd.OrderId       == command.OrderId)        &&
                     (cmd.CorrelationId == command.CorrelationId)  &&
                     (cmd.PartnerCode   == command.PartnerCode))),
                It.Is<Instant?>(time => time == null)), 
                Times.Once, 
                "because the command should have been published");    
        }

        #region NestedClasses

            private class TestOrderProcessorFunctions : OrderProcessorFunctions
            {
                public HashSet<BrokeredMessage> CompletedMessages = new HashSet<BrokeredMessage>();

                public TestOrderProcessorFunctions(JsonSerializer                              jsonSerializer,
                                                   CommandRetryThresholds                      retryThresholds,
                                                   IClock                                      clock,
                                                   IOrderProcessor                             orderProcessor,
                                                   ICommandPublisher<ProcessOrder>             processOrderPublisher,
                                                   ICommandPublisher<SubmitOrderForProduction> submitOrderForProductionPublisher,
                                                   ICommandPublisher<NotifyOfFatalFailure>     notifyOfFatalFailurePublisher,
                                                   IEventPublisher<EventBase>                  eventPublisher,
                                                   ILogger                                     logger,
                                                   IDisposable                                 lifetimeScope) : base(jsonSerializer, retryThresholds, clock, orderProcessor, processOrderPublisher, submitOrderForProductionPublisher, notifyOfFatalFailurePublisher, eventPublisher, logger, lifetimeScope)
                {
                }

                protected override Task CompleteMessageAsync(BrokeredMessage message)
                {
                    this.CompletedMessages.Add(message);
                    return Task.CompletedTask;
                }
        }

        #endregion
    }
}
