using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Moq;
using FluentAssertions;
using OrderFulfillment.OrderSubmitter.Functions;
using OrderFulfillment.OrderSubmitter.Infrastructure;
using OrderFulfillment.Core.Commands;
using Serilog;
using OrderFulfillment.Core.Events;
using NodaTime;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json.Converters;
using Microsoft.ServiceBus.Messaging;
using System.IO;
using OrderFulfillment.Core.Infrastructure;
using OrderFulfillment.Core.Exceptions;
using OrderFulfillment.Core.Models.Operations;

namespace OrderFulfillment.OrderSubmitter.Tests.Functions
{
    /// <summary>
    ///   The suite of tests for the <see cref="OrderSubmitterFunctions" />
    ///   class.
    /// </summary>
    /// 
    public class OrderSubmitterFunctionsTests
    {
        /// <summary>
        ///   Verifies behavior of the constructor.
        /// </summary>
        /// 
        [Fact]
        public void ConstructorValidatesTheSerializer()
        {
            Action actionUnderTest = () => new OrderSubmitterFunctions(null, new CommandRetryThresholds(), Mock.Of<IClock>(), Mock.Of<IOrderSubmitter>(), Mock.Of<ICommandPublisher<SubmitOrderForProduction>>(), Mock.Of<ICommandPublisher<NotifyOfFatalFailure>>(), Mock.Of<IEventPublisher<EventBase>>(), Mock.Of<ILogger>(), Mock.Of<IDisposable>());
            actionUnderTest.ShouldThrow<ArgumentNullException>("because the serializer is required");
        }

        /// <summary>
        ///   Verifies behavior of the constructor.
        /// </summary>
        /// 
        [Fact]
        public void ConstructorValidatesTheThresholds()
        {
            Action actionUnderTest = () => new OrderSubmitterFunctions(new JsonSerializer(), null, Mock.Of<IClock>(), Mock.Of<IOrderSubmitter>(), Mock.Of<ICommandPublisher<SubmitOrderForProduction>>(), Mock.Of<ICommandPublisher<NotifyOfFatalFailure>>(), Mock.Of<IEventPublisher<EventBase>>(), Mock.Of<ILogger>(), Mock.Of<IDisposable>());
            actionUnderTest.ShouldThrow<ArgumentNullException>("because the thresholds are required");
        }

        /// <summary>
        ///   Verifies behavior of the constructor.
        /// </summary>
        /// 
        [Fact]
        public void ConstructorValidatesTheClock()
        {
            Action actionUnderTest = () => new OrderSubmitterFunctions(new JsonSerializer(), new CommandRetryThresholds(), null, Mock.Of<IOrderSubmitter>(), Mock.Of<ICommandPublisher<SubmitOrderForProduction>>(), Mock.Of<ICommandPublisher<NotifyOfFatalFailure>>(), Mock.Of<IEventPublisher<EventBase>>(), Mock.Of<ILogger>(), Mock.Of<IDisposable>());
            actionUnderTest.ShouldThrow<ArgumentNullException>("because the clock is required");
        }

        /// <summary>
        ///   Verifies behavior of the constructor.
        /// </summary>
        /// 
        [Fact]
        public void ConstructorValidatesTheOrderSubmitter()
        {
            Action actionUnderTest = () => new OrderSubmitterFunctions(new JsonSerializer(), new CommandRetryThresholds(), Mock.Of<IClock>(), null, Mock.Of<ICommandPublisher<SubmitOrderForProduction>>(), Mock.Of<ICommandPublisher<NotifyOfFatalFailure>>(), Mock.Of<IEventPublisher<EventBase>>(), Mock.Of<ILogger>(), Mock.Of<IDisposable>());
            actionUnderTest.ShouldThrow<ArgumentNullException>("because the order processor is required");
        }

        /// <summary>
        ///   Verifies behavior of the constructor.
        /// </summary>
        /// 
        [Fact]
        public void ConstructorValidatesTheSubmitOrderCommandPublisher()
        {
            Action actionUnderTest = () => new OrderSubmitterFunctions(new JsonSerializer(), new CommandRetryThresholds(), Mock.Of<IClock>(), Mock.Of<IOrderSubmitter>(), null, Mock.Of<ICommandPublisher<NotifyOfFatalFailure>>(), Mock.Of<IEventPublisher<EventBase>>(), Mock.Of<ILogger>(), Mock.Of<IDisposable>());
            actionUnderTest.ShouldThrow<ArgumentNullException>("because the submit order command publisher is required");
        }

        /// <summary>
        ///   Verifies behavior of the constructor.
        /// </summary>
        /// 
        [Fact]
        public void ConstructorValidatesTheNotifyFailureCommandPublisher()
        {
            Action actionUnderTest = () => new OrderSubmitterFunctions(new JsonSerializer(), new CommandRetryThresholds(), Mock.Of<IClock>(), Mock.Of<IOrderSubmitter>(), Mock.Of<ICommandPublisher<SubmitOrderForProduction>>(), null, Mock.Of<IEventPublisher<EventBase>>(), Mock.Of<ILogger>(), Mock.Of<IDisposable>());
            actionUnderTest.ShouldThrow<ArgumentNullException>("because the notify failure command publisher is required");
        }

        /// <summary>
        ///   Verifies behavior of the constructor.
        /// </summary>
        /// 
        [Fact]
        public void ConstructorValidatesTheEventPublisher()
        {
            Action actionUnderTest = () => new OrderSubmitterFunctions(new JsonSerializer(), new CommandRetryThresholds(), Mock.Of<IClock>(), Mock.Of<IOrderSubmitter>(), Mock.Of<ICommandPublisher<SubmitOrderForProduction>>(), Mock.Of<ICommandPublisher<NotifyOfFatalFailure>>(), null, Mock.Of<ILogger>(), Mock.Of<IDisposable>());
            actionUnderTest.ShouldThrow<ArgumentNullException>("because the event publisher is required");
        }

        /// <summary>
        ///   Verifies behavior of the constructor.
        /// </summary>
        /// 
        [Fact]
        public void ConstructorValidatesTheLogger()
        {
            Action actionUnderTest = () => new OrderSubmitterFunctions(new JsonSerializer(), new CommandRetryThresholds(), Mock.Of<IClock>(), Mock.Of<IOrderSubmitter>(), Mock.Of<ICommandPublisher<SubmitOrderForProduction>>(), Mock.Of<ICommandPublisher<NotifyOfFatalFailure>>(), Mock.Of<IEventPublisher<EventBase>>(), null, Mock.Of<IDisposable>());
            actionUnderTest.ShouldThrow<ArgumentNullException>("because the logger is required");
        }

        /// <summary>
        ///   Verifies behavior of the <see cref="OrderSubmitterFunctions.HandleSubmitOrderForProductionAsync" />
        ///   method;
        /// </summary>
        /// 
        [Fact]
        public void HandleSubmitOrderForProductionAsyncValidatesTheBrokeredMessage()
        {
            var mockClock            = new Mock<IClock>();
            var mockSubmitter        = new Mock<IOrderSubmitter>();            
            var mockSubmitPublisher =  new Mock<ICommandPublisher<SubmitOrderForProduction>>();
            var mockFailurePublisher = new Mock<ICommandPublisher<NotifyOfFatalFailure>>();
            var mockEventPublisher   = new Mock<IEventPublisher<EventBase>>();
            var mockLogger           = new Mock<ILogger>();
            var mockLifetimeScope    = new Mock<IDisposable>();
            var serializerSettings   = new JsonSerializerSettings();
            var serializer           = new JsonSerializer { ContractResolver = new CamelCasePropertyNamesContractResolver() };
            var processorFunctions   = new TestOrderSubmitterFunctions(serializer, new CommandRetryThresholds(-1, 0, 0), mockClock.Object, mockSubmitter.Object, mockSubmitPublisher.Object, mockFailurePublisher.Object, mockEventPublisher.Object, mockLogger.Object, mockLifetimeScope.Object);

            serializer.Converters.Add(new StringEnumConverter());
            serializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            serializerSettings.Converters.Add(new StringEnumConverter());

            Action actionUnderTest = () => processorFunctions.HandleSubmitOrderForProductionAsync(null).GetAwaiter().GetResult();
            actionUnderTest.ShouldThrow<ArgumentNullException>("because the brokered order message is required");
        }

        /// <summary>
        ///   Verifies behavior of the <see cref="OrderSubmitterFunctions.HandleSubmitOrderForProductionAsync" />
        ///   method;
        /// </summary>
        /// 
        [Fact]
        public void HandleSubmitOrderForProductionAsyncFailsWhenTheMessageContainsNoCommand()
        {
            var mockClock            = new Mock<IClock>();
            var mockSubmitter        = new Mock<IOrderSubmitter>();
            var mockSubmitPublisher =  new Mock<ICommandPublisher<SubmitOrderForProduction>>();
            var mockFailurePublisher = new Mock<ICommandPublisher<NotifyOfFatalFailure>>();
            var mockEventPublisher   = new Mock<IEventPublisher<EventBase>>();
            var mockLogger           = new Mock<ILogger>();
            var mockLifetimeScope    = new Mock<IDisposable>();
            var serializerSettings   = new JsonSerializerSettings();
            var serializer           = new JsonSerializer { ContractResolver = new CamelCasePropertyNamesContractResolver() };
            var processorFunctions   = new TestOrderSubmitterFunctions(serializer, new CommandRetryThresholds(-1, 0, 0), mockClock.Object, mockSubmitter.Object, mockSubmitPublisher.Object, mockFailurePublisher.Object, mockEventPublisher.Object, mockLogger.Object, mockLifetimeScope.Object);

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

                Action actionUnderTest = () => processorFunctions.HandleSubmitOrderForProductionAsync(message).GetAwaiter().GetResult();
                actionUnderTest.ShouldThrow<MissingDependencyException>("because the brokered message must contain a command");

                jsonWriter.Close();
                writer.Close();
                memStream.Close();
            };
        }

        /// <summary>
        ///   Verifies behavior of the <see cref="OrderSubmitterFunctions.HandleSubmitOrderForProductionAsync" />
        ///   method;
        /// </summary>
        /// 
        [Fact]
        public async Task HandleSubmitOrderForProductionAsyncSubmitsTheOrder()
        {
            var mockClock            = new Mock<IClock>();
            var mockSubmitter        = new Mock<IOrderSubmitter>();
            var mockSubmitPublisher =  new Mock<ICommandPublisher<SubmitOrderForProduction>>();
            var mockFailurePublisher = new Mock<ICommandPublisher<NotifyOfFatalFailure>>();
            var mockEventPublisher   = new Mock<IEventPublisher<EventBase>>();
            var mockLogger           = new Mock<ILogger>();
            var mockLifetimeScope    = new Mock<IDisposable>();
            var serializerSettings   = new JsonSerializerSettings();
            var serializer           = new JsonSerializer { ContractResolver = new CamelCasePropertyNamesContractResolver() };
            var processorFunctions   = new TestOrderSubmitterFunctions(serializer, new CommandRetryThresholds(-1, 0, 0), mockClock.Object, mockSubmitter.Object, mockSubmitPublisher.Object, mockFailurePublisher.Object, mockEventPublisher.Object, mockLogger.Object, mockLifetimeScope.Object);
            var partner              = "That guy";
            var orderId              = "ABC123";
            var correlationId        = "Hello";
            var emulation            = new DependencyEmulation { OrderDetails = new OperationResult { Payload = "Yay!" } };

            serializer.Converters.Add(new StringEnumConverter());
            serializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            serializerSettings.Converters.Add(new StringEnumConverter());

            mockLogger
                .Setup(logger => logger.ForContext(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<bool>()))
                .Returns(mockLogger.Object);

            mockSubmitter
                .Setup(processor => processor.SubmitOrderForProductionAsync(It.Is<string>(partnerCode => partnerCode == partner), 
                                                               It.Is<string>(order => order == orderId),  
                                                               It.Is<DependencyEmulation>(emu => emu.OrderDetails.Payload == emulation.OrderDetails.Payload), 
                                                               It.Is<string>(correlation => correlation == correlationId)))
                .ReturnsAsync(new OperationResult { Outcome = Outcome.Success })
                .Verifiable("The order should have been submitted");

            var command = new SubmitOrderForProduction
            {
               Id              = Guid.NewGuid(),
               CorrelationId   = correlationId,
               PartnerCode     = partner,
               OrderId         = orderId,
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

                    await processorFunctions.HandleSubmitOrderForProductionAsync(message);                    
                }

                jsonWriter.Close();
                writer.Close();
                memStream.Close();
            };

            mockSubmitter.VerifyAll();
        }

        /// <summary>
        ///   Verifies behavior of the <see cref="OrderSubmitterFunctions.HandleSubmitOrderForProductionAsync" />
        ///   method;
        /// </summary>
        /// 
        [Fact]
        public void HandleSubmitOrderForProductionAsyncThrowsWhenOrderSubmissionFailsAndIsNotRetried()
        {
            var mockClock            = new Mock<IClock>();
            var mockSubmitter        = new Mock<IOrderSubmitter>();
            var mockSubmitPublisher =  new Mock<ICommandPublisher<SubmitOrderForProduction>>();
            var mockFailurePublisher = new Mock<ICommandPublisher<NotifyOfFatalFailure>>();
            var mockEventPublisher   = new Mock<IEventPublisher<EventBase>>();
            var mockLogger           = new Mock<ILogger>();
            var mockLifetimeScope    = new Mock<IDisposable>();
            var serializerSettings   = new JsonSerializerSettings();
            var serializer           = new JsonSerializer { ContractResolver = new CamelCasePropertyNamesContractResolver() };
            var processorFunctions   = new TestOrderSubmitterFunctions(serializer, new CommandRetryThresholds(-1, 0, 0), mockClock.Object, mockSubmitter.Object, mockSubmitPublisher.Object, mockFailurePublisher.Object, mockEventPublisher.Object, mockLogger.Object, mockLifetimeScope.Object);
            var expectedException    = new OrderProcessingException();


            serializer.Converters.Add(new StringEnumConverter());
            serializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            serializerSettings.Converters.Add(new StringEnumConverter());

            mockLogger
                .Setup(logger => logger.ForContext(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<bool>()))
                .Returns(mockLogger.Object);

            mockSubmitter
                .Setup(processor => processor.SubmitOrderForProductionAsync(It.IsAny<string>(), 
                                                               It.IsAny<string>(), 
                                                               It.IsAny<DependencyEmulation>(), 
                                                               It.IsAny<string>()))
                .ReturnsAsync(OperationResult.ExceptionResult);

            var command = new SubmitOrderForProduction
            {
               Id              = Guid.NewGuid(),
               CorrelationId   = "ABC",
               PartnerCode     = "Bob",
               OrderId         = "123",
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

                    Action actionUnderTest = () => processorFunctions.HandleSubmitOrderForProductionAsync(message).GetAwaiter().GetResult();

                    actionUnderTest.ShouldThrow<FailedtoHandleCommandException>("because the order submitter failed to submit the order");
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
        ///   Verifies behavior of the <see cref="OrderSubmitterFunctions.HandleSubmitOrderForProductionAsync" />
        ///   method;
        /// </summary>
        /// 
        [Fact]
        public void HandleSubmitOrderForProductionAsyncThrowsWhenOrderSubmissionThrowsAndIsNotRetried()
        {
            var mockClock            = new Mock<IClock>();
            var mockSubmitter        = new Mock<IOrderSubmitter>();
            var mockSubmitPublisher =  new Mock<ICommandPublisher<SubmitOrderForProduction>>();
            var mockFailurePublisher = new Mock<ICommandPublisher<NotifyOfFatalFailure>>();
            var mockEventPublisher   = new Mock<IEventPublisher<EventBase>>();
            var mockLogger           = new Mock<ILogger>();
            var mockLifetimeScope    = new Mock<IDisposable>();
            var serializerSettings   = new JsonSerializerSettings();
            var serializer           = new JsonSerializer { ContractResolver = new CamelCasePropertyNamesContractResolver() };
            var processorFunctions   = new TestOrderSubmitterFunctions(serializer, new CommandRetryThresholds(-1, 0, 0), mockClock.Object, mockSubmitter.Object, mockSubmitPublisher.Object, mockFailurePublisher.Object, mockEventPublisher.Object, mockLogger.Object, mockLifetimeScope.Object);
            var expectedException    = new OrderProcessingException();


            serializer.Converters.Add(new StringEnumConverter());
            serializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            serializerSettings.Converters.Add(new StringEnumConverter());

            mockLogger
                .Setup(logger => logger.ForContext(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<bool>()))
                .Returns(mockLogger.Object);

            mockSubmitter
                .Setup(processor => processor.SubmitOrderForProductionAsync(It.IsAny<string>(), 
                                                               It.IsAny<string>(), 
                                                               It.IsAny<DependencyEmulation>(), 
                                                               It.IsAny<string>()))
                .ThrowsAsync(expectedException);

            var command = new SubmitOrderForProduction
            {
               Id              = Guid.NewGuid(),
               CorrelationId   = "ABC",
               PartnerCode     = "Bob",
               OrderId         = "123",
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

                    Action actionUnderTest = () => processorFunctions.HandleSubmitOrderForProductionAsync(message).GetAwaiter().GetResult();

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
        ///   Verifies behavior of the <see cref="OrderSubmitterFunctions.HandleSubmitOrderForProductionAsync" />
        ///   method;
        /// </summary>
        /// 
        [Fact]
        public void HandleSubmitOrderForProductionAsyncThrowsWhenOrderSubmissionThrowsAndTheRetryThrows()
        {
            var mockClock            = new Mock<IClock>();
            var mockSubmitter        = new Mock<IOrderSubmitter>();
            var mockSubmitPublisher =  new Mock<ICommandPublisher<SubmitOrderForProduction>>();
            var mockFailurePublisher = new Mock<ICommandPublisher<NotifyOfFatalFailure>>();
            var mockEventPublisher   = new Mock<IEventPublisher<EventBase>>();
            var mockLogger           = new Mock<ILogger>();
            var mockLifetimeScope    = new Mock<IDisposable>();
            var serializerSettings   = new JsonSerializerSettings();
            var serializer           = new JsonSerializer { ContractResolver = new CamelCasePropertyNamesContractResolver() };
            var processorFunctions   = new TestOrderSubmitterFunctions(serializer, new CommandRetryThresholds(1, 1, 1), mockClock.Object, mockSubmitter.Object, mockSubmitPublisher.Object, mockFailurePublisher.Object, mockEventPublisher.Object, mockLogger.Object, mockLifetimeScope.Object);
            var expectedException    = new OrderProcessingException();


            serializer.Converters.Add(new StringEnumConverter());
            serializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            serializerSettings.Converters.Add(new StringEnumConverter());

            mockLogger
                .Setup(logger => logger.ForContext(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<bool>()))
                .Returns(mockLogger.Object);

            mockSubmitPublisher
                .Setup(publisher => publisher.PublishAsync(It.IsAny<SubmitOrderForProduction>(), It.Is<Instant?>(value => value.HasValue)))
                .Throws(expectedException);

            mockSubmitter
                .Setup(processor => processor.SubmitOrderForProductionAsync(It.IsAny<string>(), 
                                                               It.IsAny<string>(), 
                                                               It.IsAny<DependencyEmulation>(), 
                                                               It.IsAny<string>()))
                .ThrowsAsync(new ApplicationException());

            var command = new SubmitOrderForProduction
            {
               Id              = Guid.NewGuid(),
               CorrelationId   = "ABC",
               PartnerCode     = "Bob",
               OrderId         = "123",
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

                    Action actionUnderTest = () => processorFunctions.HandleSubmitOrderForProductionAsync(message).GetAwaiter().GetResult();

                    actionUnderTest.ShouldThrow<OrderProcessingException>("because the command retry publishing experienced an exception")
                        .Subject.SingleOrDefault().Should().Be(expectedException, "because the exception should bubble");
                }

                jsonWriter.Close();
                writer.Close();
                memStream.Close();
            }
        }

        /// <summary>
        ///   Verifies behavior of the <see cref="OrderSubmitterFunctions.HandleSubmitOrderForProductionAsync" />
        ///   method;
        /// </summary>
        /// 
        [Fact]
        public async Task HandleSubmitOrderForProductionAsyncDoesNotCompleteTheMessageWhenOrderSubmissionFailsAndIsNotRetried()
        {
            var mockClock            = new Mock<IClock>();
            var mockSubmitter        = new Mock<IOrderSubmitter>();
            var mockSubmitPublisher =  new Mock<ICommandPublisher<SubmitOrderForProduction>>();
            var mockFailurePublisher = new Mock<ICommandPublisher<NotifyOfFatalFailure>>();
            var mockEventPublisher   = new Mock<IEventPublisher<EventBase>>();
            var mockLogger           = new Mock<ILogger>();
            var mockLifetimeScope    = new Mock<IDisposable>();
            var serializerSettings   = new JsonSerializerSettings();
            var serializer           = new JsonSerializer { ContractResolver = new CamelCasePropertyNamesContractResolver() };
            var processorFunctions   = new TestOrderSubmitterFunctions(serializer, new CommandRetryThresholds(-1, 0, 0), mockClock.Object, mockSubmitter.Object, mockSubmitPublisher.Object, mockFailurePublisher.Object, mockEventPublisher.Object, mockLogger.Object, mockLifetimeScope.Object);
            
            serializer.Converters.Add(new StringEnumConverter());
            serializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            serializerSettings.Converters.Add(new StringEnumConverter());

            mockLogger
                .Setup(logger => logger.ForContext(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<bool>()))
                .Returns(mockLogger.Object);

            mockSubmitter
                .Setup(processor => processor.SubmitOrderForProductionAsync(It.IsAny<string>(), 
                                                               It.IsAny<string>(),                                                               
                                                               It.IsAny<DependencyEmulation>(), 
                                                               It.IsAny<string>()))
                .ThrowsAsync(new OrderProcessingException());

            var command = new SubmitOrderForProduction
            {
               Id              = Guid.NewGuid(),
               CorrelationId   = "ABC",
               PartnerCode     = "Bob",
               OrderId         = "123",
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

                   try { await processorFunctions.HandleSubmitOrderForProductionAsync(message); }  catch {}
                }

                jsonWriter.Close();
                writer.Close();
                memStream.Close();
            };

            processorFunctions.CompletedMessages.Should().BeEmpty("because no messages should have been completed");
        }

        /// <summary>
        ///   Verifies behavior of the <see cref="OrderSubmitterFunctions.HandleSubmitOrderForProductionAsync" />
        ///   method;
        /// </summary>
        /// 
        [Fact]
        public async Task HandleSubmitOrderForProductionAsyncCompletesTheMessageWhenOrderSubmissionFailsAndIsRetried()
        {
            var mockClock            = new Mock<IClock>();
            var mockSubmitter        = new Mock<IOrderSubmitter>();
            var mockSubmitPublisher =  new Mock<ICommandPublisher<SubmitOrderForProduction>>();
            var mockFailurePublisher = new Mock<ICommandPublisher<NotifyOfFatalFailure>>();
            var mockEventPublisher   = new Mock<IEventPublisher<EventBase>>();
            var mockLogger           = new Mock<ILogger>();
            var mockLifetimeScope    = new Mock<IDisposable>();
            var serializerSettings   = new JsonSerializerSettings();
            var serializer           = new JsonSerializer { ContractResolver = new CamelCasePropertyNamesContractResolver() };
            var processorFunctions   = new TestOrderSubmitterFunctions(serializer, new CommandRetryThresholds(1, 1, 1), mockClock.Object, mockSubmitter.Object, mockSubmitPublisher.Object, mockFailurePublisher.Object, mockEventPublisher.Object, mockLogger.Object, mockLifetimeScope.Object);
            
            serializer.Converters.Add(new StringEnumConverter());
            serializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            serializerSettings.Converters.Add(new StringEnumConverter());

            mockLogger
                .Setup(logger => logger.ForContext(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<bool>()))
                .Returns(mockLogger.Object);

            mockSubmitter
                .Setup(processor => processor.SubmitOrderForProductionAsync(It.IsAny<string>(), 
                                                               It.IsAny<string>(),                                                               
                                                               It.IsAny<DependencyEmulation>(), 
                                                               It.IsAny<string>()))
                .ThrowsAsync(new OrderProcessingException());

            var command = new SubmitOrderForProduction
            {
               Id              = Guid.NewGuid(),
               CorrelationId   = "ABC",
               PartnerCode     = "Bob",
               OrderId         = "123",
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

                   try { await processorFunctions.HandleSubmitOrderForProductionAsync(message); }  catch {}
                }

                jsonWriter.Close();
                writer.Close();
                memStream.Close();
            };

            processorFunctions.CompletedMessages.Should().HaveCount(1, "because the message should have been completed");
        }

        /// <summary>
        ///   Verifies behavior of the <see cref="OrderSubmitterFunctions.HandleSubmitOrderForProductionAsync" />
        ///   method;
        /// </summary>
        /// 
        [Fact]
        public async Task HandleSubmitOrderForProductionAsyncPublishesTheProperEventWhenOrderSubmissionFails()
        {
            var mockClock            = new Mock<IClock>();
            var mockSubmitter        = new Mock<IOrderSubmitter>();
            var mockSubmitPublisher =  new Mock<ICommandPublisher<SubmitOrderForProduction>>();
            var mockFailurePublisher = new Mock<ICommandPublisher<NotifyOfFatalFailure>>();
            var mockEventPublisher   = new Mock<IEventPublisher<EventBase>>();
            var mockLogger           = new Mock<ILogger>();
            var mockLifetimeScope    = new Mock<IDisposable>();
            var serializerSettings   = new JsonSerializerSettings();
            var serializer           = new JsonSerializer { ContractResolver = new CamelCasePropertyNamesContractResolver() };
            var processorFunctions   = new TestOrderSubmitterFunctions(serializer, new CommandRetryThresholds(-1, 0, 0), mockClock.Object, mockSubmitter.Object, mockSubmitPublisher.Object, mockFailurePublisher.Object, mockEventPublisher.Object, mockLogger.Object, mockLifetimeScope.Object);
            
            serializer.Converters.Add(new StringEnumConverter());
            serializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            serializerSettings.Converters.Add(new StringEnumConverter());

            mockLogger
                .Setup(logger => logger.ForContext(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<bool>()))
                .Returns(mockLogger.Object);

            mockSubmitter
                .Setup(processor => processor.SubmitOrderForProductionAsync(It.IsAny<string>(), 
                                                               It.IsAny<string>(), 
                                                               It.IsAny<DependencyEmulation>(), 
                                                               It.IsAny<string>()))
                .ThrowsAsync(new OrderProcessingException());

            var command = new SubmitOrderForProduction
            {
               Id              = Guid.NewGuid(),
               CorrelationId   = "ABC",
               PartnerCode     = "Bob",
               OrderId         = "123",
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

                   try { await processorFunctions.HandleSubmitOrderForProductionAsync(message); }  catch {}
                }

                jsonWriter.Close();
                writer.Close();
                memStream.Close();
            };

            mockEventPublisher.Verify(pub => pub.TryPublishAsync(It.Is<OrderSubmissionFailed>(evt => 
                    ((evt.OrderId       == command.OrderId)        &&
                     (evt.CorrelationId == command.CorrelationId)  &&
                     (evt.PartnerCode   == command.PartnerCode)))), 
                Times.Once, "because the event should have been published");    
        }

        /// <summary>
        ///   Verifies behavior of the <see cref="OrderSubmitterFunctions.HandleSubmitOrderForProductionAsync" />
        ///   method;
        /// </summary>
        /// 
        [Fact]
        public async Task HandleSubmitOrderForProductionAsyncCompletesTheMessageWhenOrderSubmissionSucceeds()
        {
            var mockClock            = new Mock<IClock>();
            var mockSubmitter        = new Mock<IOrderSubmitter>();
            var mockSubmitPublisher =  new Mock<ICommandPublisher<SubmitOrderForProduction>>();
            var mockFailurePublisher = new Mock<ICommandPublisher<NotifyOfFatalFailure>>();
            var mockEventPublisher   = new Mock<IEventPublisher<EventBase>>();
            var mockLogger           = new Mock<ILogger>();
            var mockLifetimeScope    = new Mock<IDisposable>();
            var serializerSettings   = new JsonSerializerSettings();
            var serializer           = new JsonSerializer { ContractResolver = new CamelCasePropertyNamesContractResolver() };
            var processorFunctions   = new TestOrderSubmitterFunctions(serializer, new CommandRetryThresholds(-1, 0, 0), mockClock.Object, mockSubmitter.Object, mockSubmitPublisher.Object, mockFailurePublisher.Object, mockEventPublisher.Object, mockLogger.Object, mockLifetimeScope.Object);
            
            serializer.Converters.Add(new StringEnumConverter());
            serializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            serializerSettings.Converters.Add(new StringEnumConverter());

            mockLogger
                .Setup(logger => logger.ForContext(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<bool>()))
                .Returns(mockLogger.Object);

            mockSubmitter
                .Setup(processor => processor.SubmitOrderForProductionAsync(It.IsAny<string>(), 
                                                               It.IsAny<string>(),                                                               
                                                               It.IsAny<DependencyEmulation>(), 
                                                               It.IsAny<string>()))
                .ReturnsAsync(new OperationResult { Outcome = Outcome.Success });

            var command = new SubmitOrderForProduction
            {
               Id              = Guid.NewGuid(),
               CorrelationId   = "ABC",
               PartnerCode     = "Bob",
               OrderId         = "123",
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

                   try { await processorFunctions.HandleSubmitOrderForProductionAsync(message); }  catch {}
                }

                jsonWriter.Close();
                writer.Close();
                memStream.Close();
            };

            processorFunctions.CompletedMessages.Should().HaveCount(1, "because the message should have been completed");
        }

        /// <summary>
        ///   Verifies behavior of the <see cref="OrderSubmitterFunctions.HandleSubmitOrderForProductionAsync" />
        ///   method;
        /// </summary>
        /// 
        [Fact]
        public async Task HandleSubmitOrderForProductionAsyncPublishesTheProperEventWhenOrderSubmissionSucceeds()
        {
            var mockClock            = new Mock<IClock>();
            var mockSubmitter        = new Mock<IOrderSubmitter>();
            var mockSubmitPublisher =  new Mock<ICommandPublisher<SubmitOrderForProduction>>();
            var mockFailurePublisher = new Mock<ICommandPublisher<NotifyOfFatalFailure>>();
            var mockEventPublisher   = new Mock<IEventPublisher<EventBase>>();
            var mockLogger           = new Mock<ILogger>();
            var mockLifetimeScope    = new Mock<IDisposable>();
            var serializerSettings   = new JsonSerializerSettings();
            var serializer           = new JsonSerializer { ContractResolver = new CamelCasePropertyNamesContractResolver() };
            var processorFunctions   = new TestOrderSubmitterFunctions(serializer, new CommandRetryThresholds(-1, 0, 0), mockClock.Object, mockSubmitter.Object, mockSubmitPublisher.Object, mockFailurePublisher.Object, mockEventPublisher.Object, mockLogger.Object, mockLifetimeScope.Object);
            
            serializer.Converters.Add(new StringEnumConverter());
            serializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            serializerSettings.Converters.Add(new StringEnumConverter());

            mockLogger
                .Setup(logger => logger.ForContext(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<bool>()))
                .Returns(mockLogger.Object);

            mockSubmitter
                .Setup(processor => processor.SubmitOrderForProductionAsync(It.IsAny<string>(), 
                                                               It.IsAny<string>(),                                                               
                                                               It.IsAny<DependencyEmulation>(), 
                                                               It.IsAny<string>()))
                .ReturnsAsync(new OperationResult { Outcome = Outcome.Success });

            var command = new SubmitOrderForProduction
            {
               Id              = Guid.NewGuid(),
               CorrelationId   = "ABC",
               PartnerCode     = "Bob",
               OrderId         = "123",
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

                   try { await processorFunctions.HandleSubmitOrderForProductionAsync(message); }  catch {}
                }

                jsonWriter.Close();
                writer.Close();
                memStream.Close();
            };

            mockEventPublisher.Verify(pub => pub.TryPublishAsync(It.Is<OrderSubmitted>(evt => 
                    ((evt.OrderId      == command.OrderId)        &&
                     (evt.CorrelationId == command.CorrelationId)  &&
                     (evt.PartnerCode  == command.PartnerCode)))), 
                Times.Once, "because the event should have been published");    
        }   

        #region NestedClasses

            private class TestOrderSubmitterFunctions : OrderSubmitterFunctions
            {
                public HashSet<BrokeredMessage> CompletedMessages = new HashSet<BrokeredMessage>();

                public TestOrderSubmitterFunctions(JsonSerializer                              jsonSerializer,
                                                   CommandRetryThresholds                      retryThresholds,
                                                   IClock                                      clock,
                                                   IOrderSubmitter                             orderSubmitter,
                                                   ICommandPublisher<SubmitOrderForProduction> submitOrderForProductionPublisher,
                                                   ICommandPublisher<NotifyOfFatalFailure>     notifyOfFatalFailurePublisher,
                                                   IEventPublisher<EventBase>                  eventPublisher,
                                                   ILogger                                     logger,
                                                   IDisposable                                 lifetimeScope) : base(jsonSerializer, retryThresholds, clock, orderSubmitter, submitOrderForProductionPublisher, notifyOfFatalFailurePublisher, eventPublisher, logger, lifetimeScope)
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
