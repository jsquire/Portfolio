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
using OrderFulfillment.Notifier.Functions;
using OrderFulfillment.Notifier.Infrastructure;
using Moq;
using Moq.Protected;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using NodaTime;
using Serilog;
using Xunit;

namespace OrderFulfillment.Notifier.Tests.Functions
{
    /// <summary>
    ///   The suite of tests for the <see cref="NotifierFunctions" />
    ///   class.
    /// </summary>
    /// 
    public class NotifierFunctionsTests
    {
        /// <summary>
        ///   Verifies behavior of the constructor.
        /// </summary>
        /// 
        [Fact]
        public void ConstructorValidatesTheSerializer()
        {
            Action actionUnderTest = () => new NotifierFunctions(null, new CommandRetryThresholds(), Mock.Of<IClock>(), Mock.Of<INotifier>(), Mock.Of<ICommandPublisher<NotifyOfFatalFailure>>(), Mock.Of<IEventPublisher<EventBase>>(), Mock.Of<ILogger>(), Mock.Of<IDisposable>());
            actionUnderTest.ShouldThrow<ArgumentNullException>("because the serializer is required");
        }

        /// <summary>
        ///   Verifies behavior of the constructor.
        /// </summary>
        /// 
        [Fact]
        public void ConstructorValidatesTheThresholds()
        {
            Action actionUnderTest = () => new NotifierFunctions(new JsonSerializer(), null, Mock.Of<IClock>(), Mock.Of<INotifier>(), Mock.Of<ICommandPublisher<NotifyOfFatalFailure>>(), Mock.Of<IEventPublisher<EventBase>>(), Mock.Of<ILogger>(), Mock.Of<IDisposable>());
            actionUnderTest.ShouldThrow<ArgumentNullException>("because the serializer is required");
        }

        /// <summary>
        ///   Verifies behavior of the constructor.
        /// </summary>
        /// 
        [Fact]
        public void ConstructorValidatesTheClock()
        {
            Action actionUnderTest = () => new NotifierFunctions(new JsonSerializer(), new CommandRetryThresholds(), null, Mock.Of<INotifier>(), Mock.Of<ICommandPublisher<NotifyOfFatalFailure>>(), Mock.Of<IEventPublisher<EventBase>>(), Mock.Of<ILogger>(), Mock.Of<IDisposable>());
            actionUnderTest.ShouldThrow<ArgumentNullException>("because the clock is required");
        }

        /// <summary>
        ///   Verifies behavior of the constructor.
        /// </summary>
        /// 
        [Fact]
        public void ConstructorValidatesTheNotifier()
        {
            Action actionUnderTest = () => new NotifierFunctions(new JsonSerializer(), new CommandRetryThresholds(), Mock.Of<IClock>(), null, Mock.Of<ICommandPublisher<NotifyOfFatalFailure>>(), Mock.Of<IEventPublisher<EventBase>>(), Mock.Of<ILogger>(), Mock.Of<IDisposable>());
            actionUnderTest.ShouldThrow<ArgumentNullException>("because the order processor is required");
        }

        /// <summary>
        ///   Verifies behavior of the constructor.
        /// </summary>
        /// 
        [Fact]
        public void ConstructorValidatesTheNotifyFailureCommandPublisher()
        {
            Action actionUnderTest = () => new NotifierFunctions(new JsonSerializer(), new CommandRetryThresholds(), Mock.Of<IClock>(), Mock.Of<INotifier>(), null, Mock.Of<IEventPublisher<EventBase>>(), Mock.Of<ILogger>(), Mock.Of<IDisposable>());
            actionUnderTest.ShouldThrow<ArgumentNullException>("because the submit order command publisher is required");
        }

        /// <summary>
        ///   Verifies behavior of the constructor.
        /// </summary>
        /// 
        [Fact]
        public void ConstructorValidatesTheEventPublisher()
        {
            Action actionUnderTest = () => new NotifierFunctions(new JsonSerializer(), new CommandRetryThresholds(), Mock.Of<IClock>(), Mock.Of<INotifier>(), Mock.Of<ICommandPublisher<NotifyOfFatalFailure>>(), null, Mock.Of<ILogger>(), Mock.Of<IDisposable>());
            actionUnderTest.ShouldThrow<ArgumentNullException>("because the event publisher is required");
        }

        /// <summary>
        ///   Verifies behavior of the constructor.
        /// </summary>
        /// 
        [Fact]
        public void ConstructorValidatesTheLogger()
        {
            Action actionUnderTest = () => new NotifierFunctions(new JsonSerializer(), new CommandRetryThresholds(), Mock.Of<IClock>(), Mock.Of<INotifier>(), Mock.Of<ICommandPublisher<NotifyOfFatalFailure>>(), Mock.Of<IEventPublisher<EventBase>>(), null, Mock.Of<IDisposable>());
            actionUnderTest.ShouldThrow<ArgumentNullException>("because the logger is required");
        }

        /// <summary>
        ///   Verifies behavior of the <see cref="NotifierFunctions.HandleNotifyOfFatalFailureAsync" />
        ///   method;
        /// </summary>
        /// 
        [Fact]
        public void HandleNotifyOfFatalFailureAsyncValidatesTheBrokeredMessage()
        {
            var mockClock            = new Mock<IClock>();
            var mockNotifier         = new Mock<INotifier>();
            var mockFailurePublisher = new Mock<ICommandPublisher<NotifyOfFatalFailure>>();
            var mockEventPublisher   = new Mock<IEventPublisher<EventBase>>();
            var mockLogger           = new Mock<ILogger>();
            var mockLifetimeScope    = new Mock<IDisposable>();
            var serializerSettings   = new JsonSerializerSettings();
            var serializer           = new JsonSerializer { ContractResolver = new CamelCasePropertyNamesContractResolver() };
            var notifierFunctions    = new TestNotifierFunctions(serializer, new CommandRetryThresholds(-1, 1, 1), mockClock.Object, mockNotifier.Object, mockFailurePublisher.Object, mockEventPublisher.Object, mockLogger.Object, mockLifetimeScope.Object);

            serializer.Converters.Add(new StringEnumConverter());
            serializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            serializerSettings.Converters.Add(new StringEnumConverter());

            Action actionUnderTest = () => notifierFunctions.HandleNotifyOfFatalFailureAsync(null).GetAwaiter().GetResult();
            actionUnderTest.ShouldThrow<ArgumentNullException>("because the brokered order message is required");
        }

        /// <summary>
        ///   Verifies behavior of the <see cref="NotifierFunctions.HandleNotifyOfFatalFailureAsync" />
        ///   method;
        /// </summary>
        /// 
        [Fact]
        public void HandleNotifyOfFatalFailureAsyncFailsWhenTheMessageContainsNoCommand()
        {
            var mockClock            = new Mock<IClock>();
            var mockNotifier         = new Mock<INotifier>();
            var mockFailurePublisher = new Mock<ICommandPublisher<NotifyOfFatalFailure>>();
            var mockEventPublisher   = new Mock<IEventPublisher<EventBase>>();
            var mockLogger           = new Mock<ILogger>();
            var mockLifetimeScope    = new Mock<IDisposable>();
            var serializerSettings   = new JsonSerializerSettings();
            var serializer           = new JsonSerializer { ContractResolver = new CamelCasePropertyNamesContractResolver() };
            var notifierFunctions    = new TestNotifierFunctions(serializer, new CommandRetryThresholds(-1, 1, 1), mockClock.Object, mockNotifier.Object, mockFailurePublisher.Object, mockEventPublisher.Object, mockLogger.Object, mockLifetimeScope.Object);

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

                Action actionUnderTest = () => notifierFunctions.HandleNotifyOfFatalFailureAsync(message).GetAwaiter().GetResult();
                actionUnderTest.ShouldThrow<MissingDependencyException>("because the brokered message must contain a command");

                jsonWriter.Close();
                writer.Close();
                memStream.Close();
            };
        }

        /// <summary>
        ///   Verifies behavior of the <see cref="NotifierFunctions.HandleNotifyOfFatalFailureAsync" />
        ///   method;
        /// </summary>
        /// 
        [Fact]
        public async Task HandleNotifyOfFatalFailureAsyncSendsNotification()
        {
            var mockClock            = new Mock<IClock>();
            var mockNotifier         = new Mock<INotifier>();
            var mockFailurePublisher = new Mock<ICommandPublisher<NotifyOfFatalFailure>>();
            var mockEventPublisher   = new Mock<IEventPublisher<EventBase>>();
            var mockLogger           = new Mock<ILogger>();
            var mockLifetimeScope    = new Mock<IDisposable>();
            var serializerSettings   = new JsonSerializerSettings();
            var serializer           = new JsonSerializer { ContractResolver = new CamelCasePropertyNamesContractResolver() };
            var notifierFunctions    = new TestNotifierFunctions(serializer, new CommandRetryThresholds(-1, 1, 1), mockClock.Object, mockNotifier.Object, mockFailurePublisher.Object, mockEventPublisher.Object, mockLogger.Object, mockLifetimeScope.Object);
            var partner              = "That guy";
            var orderId              = "ABC123";
            var correlationId        = "Hello";

            serializer.Converters.Add(new StringEnumConverter());
            serializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            serializerSettings.Converters.Add(new StringEnumConverter());

            mockLogger
                .Setup(logger => logger.ForContext(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<bool>()))
                .Returns(mockLogger.Object);

            mockNotifier
                .Setup(notifier => notifier.NotifyOfOrderFailureAsync(It.Is<string>(partnerCode => partnerCode == partner), 
                                                                      It.Is<string>(order => order == orderId),  
                                                                      It.Is<string>(correlation => correlation == correlationId)))
                .ReturnsAsync(new OperationResult { Outcome = Outcome.Success })
                .Verifiable("The notification should have been requested");

            var command = new NotifyOfFatalFailure
            {
               Id              = Guid.NewGuid(),
               CorrelationId   = correlationId,
               PartnerCode     = partner,
               OrderId         = orderId,
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

                    await notifierFunctions.HandleNotifyOfFatalFailureAsync(message);                    
                }

                jsonWriter.Close();
                writer.Close();
                memStream.Close();
            };

            mockNotifier.VerifyAll();
        }

        /// <summary>
        ///   Verifies behavior of the <see cref="NotifierFunctions.HandleNotifyOfFatalFailureAsync" />
        ///   method;
        /// </summary>
        /// 
        [Fact]
        public void HandleNotifyOfFatalFailureAsyncThrowsWhenNotificationFailsAndIsNotRetried()
        {
            var mockClock            = new Mock<IClock>();
            var mockNotifier         = new Mock<INotifier>();
            var mockFailurePublisher = new Mock<ICommandPublisher<NotifyOfFatalFailure>>();
            var mockEventPublisher   = new Mock<IEventPublisher<EventBase>>();
            var mockLogger           = new Mock<ILogger>();
            var mockLifetimeScope    = new Mock<IDisposable>();
            var serializerSettings   = new JsonSerializerSettings();
            var serializer           = new JsonSerializer { ContractResolver = new CamelCasePropertyNamesContractResolver() };
            var notifierFunctions    = new TestNotifierFunctions(serializer, new CommandRetryThresholds(-1, 1, 1), mockClock.Object, mockNotifier.Object, mockFailurePublisher.Object, mockEventPublisher.Object, mockLogger.Object, mockLifetimeScope.Object);
            var expectedException    = new FormatException();


            serializer.Converters.Add(new StringEnumConverter());
            serializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            serializerSettings.Converters.Add(new StringEnumConverter());

            mockLogger
                .Setup(logger => logger.ForContext(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<bool>()))
                .Returns(mockLogger.Object);

            mockNotifier
                .Setup(notifier => notifier.NotifyOfOrderFailureAsync(It.IsAny<string>(), 
                                                                      It.IsAny<string>(), 
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

                    Action actionUnderTest = () => notifierFunctions.HandleNotifyOfFatalFailureAsync(message).GetAwaiter().GetResult();

                    actionUnderTest.ShouldThrow<FailedtoHandleCommandException>("because the notifier failed to send notification");
                }

                jsonWriter.Close();
                writer.Close();
                memStream.Close();
            };
        }

        /// <summary>
        ///   Verifies behavior of the <see cref="NotifierFunctions.HandleNotifyOfFatalFailureAsync" />
        ///   method;
        /// </summary>
        /// 
        [Fact]
        public void HandleNotifyOfFatalFailureAsyncThrowsWhenNotificationThrowsAndIsNotRetried()
        {
            var mockClock            = new Mock<IClock>();
            var mockNotifier         = new Mock<INotifier>();
            var mockFailurePublisher = new Mock<ICommandPublisher<NotifyOfFatalFailure>>();
            var mockEventPublisher   = new Mock<IEventPublisher<EventBase>>();
            var mockLogger           = new Mock<ILogger>();
            var mockLifetimeScope    = new Mock<IDisposable>();
            var serializerSettings   = new JsonSerializerSettings();
            var serializer           = new JsonSerializer { ContractResolver = new CamelCasePropertyNamesContractResolver() };
            var notifierFunctions    = new TestNotifierFunctions(serializer, new CommandRetryThresholds(-1, 1, 1), mockClock.Object, mockNotifier.Object, mockFailurePublisher.Object, mockEventPublisher.Object, mockLogger.Object, mockLifetimeScope.Object);
            var expectedException    = new FormatException();


            serializer.Converters.Add(new StringEnumConverter());
            serializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            serializerSettings.Converters.Add(new StringEnumConverter());

            mockLogger
                .Setup(logger => logger.ForContext(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<bool>()))
                .Returns(mockLogger.Object);

            mockNotifier
                .Setup(notifier => notifier.NotifyOfOrderFailureAsync(It.IsAny<string>(), 
                                                                      It.IsAny<string>(), 
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

                    Action actionUnderTest = () => notifierFunctions.HandleNotifyOfFatalFailureAsync(message).GetAwaiter().GetResult();

                    actionUnderTest.ShouldThrow<FormatException>("because the notifier experienced an exception")
                        .Subject.SingleOrDefault().Should().Be(expectedException, "because the exception should bubble");
                }

                jsonWriter.Close();
                writer.Close();
                memStream.Close();
            };
        }

        /// <summary>
        ///   Verifies behavior of the <see cref="NotifierFunctions.HandleNotifyOfFatalFailureAsync" />
        ///   method;
        /// </summary>
        /// 
        [Fact]
        public async Task HandleNotifyOfFatalFailureAsyncDoesNotCompleteTheMessageWhenOrderSubmissionFailsAndIsNotRetried()
        {
            var mockClock            = new Mock<IClock>();
            var mockNotifier         = new Mock<INotifier>();
            var mockFailurePublisher = new Mock<ICommandPublisher<NotifyOfFatalFailure>>();
            var mockEventPublisher   = new Mock<IEventPublisher<EventBase>>();
            var mockLogger           = new Mock<ILogger>();
            var mockLifetimeScope    = new Mock<IDisposable>();
            var serializerSettings   = new JsonSerializerSettings();
            var serializer           = new JsonSerializer { ContractResolver = new CamelCasePropertyNamesContractResolver() };
            var notifierFunctions    = new TestNotifierFunctions(serializer, new CommandRetryThresholds(-1, 1, 1), mockClock.Object, mockNotifier.Object, mockFailurePublisher.Object, mockEventPublisher.Object, mockLogger.Object, mockLifetimeScope.Object);
            
            serializer.Converters.Add(new StringEnumConverter());
            serializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            serializerSettings.Converters.Add(new StringEnumConverter());

            mockLogger
                .Setup(logger => logger.ForContext(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<bool>()))
                .Returns(mockLogger.Object);

            mockNotifier
                .Setup(notifier => notifier.NotifyOfOrderFailureAsync(It.IsAny<string>(), 
                                                                      It.IsAny<string>(),    
                                                                      It.IsAny<string>()))
                .ThrowsAsync(new IndexOutOfRangeException());

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

                   try { await notifierFunctions.HandleNotifyOfFatalFailureAsync(message); }  catch {}
                }

                jsonWriter.Close();
                writer.Close();
                memStream.Close();
            };

            notifierFunctions.CompletedMessages.Should().BeEmpty("because no messages should have been completed");
        }

        /// <summary>
        ///   Verifies behavior of the <see cref="NotifierFunctions.HandleNotifyOfFatalFailureAsync" />
        ///   method;
        /// </summary>
        /// 
        [Fact]
        public async Task HandleNotifyOfFatalFailureAsyncDoesNotCompleteTheMessageWhenOrderSubmissionFailsAndTheRetryFails()
        {
            var mockClock            = new Mock<IClock>();
            var mockNotifier         = new Mock<INotifier>();
            var mockFailurePublisher = new Mock<ICommandPublisher<NotifyOfFatalFailure>>();
            var mockEventPublisher   = new Mock<IEventPublisher<EventBase>>();
            var mockLogger           = new Mock<ILogger>();
            var mockLifetimeScope    = new Mock<IDisposable>();
            var serializerSettings   = new JsonSerializerSettings();
            var serializer           = new JsonSerializer { ContractResolver = new CamelCasePropertyNamesContractResolver() };
            var notifierFunctions    = new TestNotifierFunctions(serializer, new CommandRetryThresholds(-1, 1, 1), mockClock.Object, mockNotifier.Object, mockFailurePublisher.Object, mockEventPublisher.Object, mockLogger.Object, mockLifetimeScope.Object);
            
            serializer.Converters.Add(new StringEnumConverter());
            serializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            serializerSettings.Converters.Add(new StringEnumConverter());

            mockLogger
                .Setup(logger => logger.ForContext(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<bool>()))
                .Returns(mockLogger.Object);

            mockFailurePublisher
                .Setup(publisher => publisher.PublishAsync(It.IsAny<NotifyOfFatalFailure>(), It.Is<Instant?>(value => value.HasValue)))
                .ThrowsAsync(new AccessViolationException());                

            mockNotifier
                .Setup(notifier => notifier.NotifyOfOrderFailureAsync(It.IsAny<string>(), 
                                                                      It.IsAny<string>(),    
                                                                      It.IsAny<string>()))
                .ThrowsAsync(new IndexOutOfRangeException());

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

                   try { await notifierFunctions.HandleNotifyOfFatalFailureAsync(message); }  catch {}
                }

                jsonWriter.Close();
                writer.Close();
                memStream.Close();
            };

            notifierFunctions.CompletedMessages.Should().BeEmpty("because no messages should have been completed");
        }

        /// <summary>
        ///   Verifies behavior of the <see cref="NotifierFunctions.HandleNotifyOfFatalFailureAsync" />
        ///   method;
        /// </summary>
        /// 
        [Fact]
        public async Task HandleNotifyOfFatalFailureAsyncPublishesTheProperEventWhenOrderSubmissionFails()
        {
            var mockClock            = new Mock<IClock>();
            var mockNotifier         = new Mock<INotifier>();
            var mockFailurePublisher = new Mock<ICommandPublisher<NotifyOfFatalFailure>>();
            var mockEventPublisher   = new Mock<IEventPublisher<EventBase>>();
            var mockLogger           = new Mock<ILogger>();
            var mockLifetimeScope    = new Mock<IDisposable>();
            var serializerSettings   = new JsonSerializerSettings();
            var serializer           = new JsonSerializer { ContractResolver = new CamelCasePropertyNamesContractResolver() };
            var notifierFunctions    = new TestNotifierFunctions(serializer, new CommandRetryThresholds(-1, 1, 1), mockClock.Object, mockNotifier.Object, mockFailurePublisher.Object, mockEventPublisher.Object, mockLogger.Object, mockLifetimeScope.Object);
            
            serializer.Converters.Add(new StringEnumConverter());
            serializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            serializerSettings.Converters.Add(new StringEnumConverter());

            mockLogger
                .Setup(logger => logger.ForContext(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<bool>()))
                .Returns(mockLogger.Object);

            mockNotifier
                .Setup(notifier => notifier.NotifyOfOrderFailureAsync(It.IsAny<string>(), 
                                                                      It.IsAny<string>(), 
                                                                      It.IsAny<string>()))
                .ThrowsAsync(new OrderProcessingException());

            var command = new NotifyOfFatalFailure
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

                   try { await notifierFunctions.HandleNotifyOfFatalFailureAsync(message); }  catch {}
                }

                jsonWriter.Close();
                writer.Close();
                memStream.Close();
            };

            mockEventPublisher.Verify(pub => pub.TryPublishAsync(It.Is<NotificationFailed>(evt => 
                    ((evt.OrderId       == command.OrderId)        &&
                     (evt.CorrelationId == command.CorrelationId)  &&
                     (evt.PartnerCode   == command.PartnerCode)))), 
                Times.Once, "because the event should have been published");    
        }

        /// <summary>
        ///   Verifies behavior of the <see cref="NotifierFunctions.HandleNotifyOfFatalFailureAsync" />
        ///   method;
        /// </summary>
        /// 
        [Fact]
        public async Task HandleNotifyOfFatalFailureAsyncCompletesTheMessageWhenNotificationSucceeds()
        {
            var mockClock            = new Mock<IClock>();
            var mockNotifier         = new Mock<INotifier>();
            var mockFailurePublisher = new Mock<ICommandPublisher<NotifyOfFatalFailure>>();
            var mockEventPublisher   = new Mock<IEventPublisher<EventBase>>();
            var mockLogger           = new Mock<ILogger>();
            var mockLifetimeScope    = new Mock<IDisposable>();
            var serializerSettings   = new JsonSerializerSettings();
            var serializer           = new JsonSerializer { ContractResolver = new CamelCasePropertyNamesContractResolver() };
            var notifierFunctions    = new TestNotifierFunctions(serializer, new CommandRetryThresholds(-1, 1, 1), mockClock.Object, mockNotifier.Object, mockFailurePublisher.Object, mockEventPublisher.Object, mockLogger.Object, mockLifetimeScope.Object);
            
            serializer.Converters.Add(new StringEnumConverter());
            serializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            serializerSettings.Converters.Add(new StringEnumConverter());

            mockLogger
                .Setup(logger => logger.ForContext(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<bool>()))
                .Returns(mockLogger.Object);

            mockNotifier
                .Setup(notifier => notifier.NotifyOfOrderFailureAsync(It.IsAny<string>(), 
                                                                      It.IsAny<string>(),       
                                                                      It.IsAny<string>()))
                .ReturnsAsync(new OperationResult { Outcome = Outcome.Success });

            var command = new NotifyOfFatalFailure
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

                   try { await notifierFunctions.HandleNotifyOfFatalFailureAsync(message); }  catch {}
                }

                jsonWriter.Close();
                writer.Close();
                memStream.Close();
            };

            notifierFunctions    .CompletedMessages.Should().HaveCount(1, "because the message should have been completed");
        }

        /// <summary>
        ///   Verifies behavior of the <see cref="NotifierFunctions.HandleSubmitOrderForProductionAsync" />
        ///   method;
        /// </summary>
        /// 
        [Fact]
        public async Task HandleNotifyOfFatalFailureAsyncPublishesTheProperEventWhenNotificationSucceeds()
        {
            var mockClock            = new Mock<IClock>();
            var mockNotifier         = new Mock<INotifier>();
            var mockFailurePublisher = new Mock<ICommandPublisher<NotifyOfFatalFailure>>();
            var mockEventPublisher   = new Mock<IEventPublisher<EventBase>>();
            var mockLogger           = new Mock<ILogger>();
            var mockLifetimeScope    = new Mock<IDisposable>();
            var serializerSettings   = new JsonSerializerSettings();
            var serializer           = new JsonSerializer { ContractResolver = new CamelCasePropertyNamesContractResolver() };
            var notifierFunctions    = new TestNotifierFunctions(serializer, new CommandRetryThresholds(-1, 1, 1), mockClock.Object, mockNotifier.Object, mockFailurePublisher.Object, mockEventPublisher.Object, mockLogger.Object, mockLifetimeScope.Object);
            
            serializer.Converters.Add(new StringEnumConverter());
            serializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            serializerSettings.Converters.Add(new StringEnumConverter());

            mockLogger
                .Setup(logger => logger.ForContext(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<bool>()))
                .Returns(mockLogger.Object);

            mockNotifier
                .Setup(notifier => notifier.NotifyOfOrderFailureAsync(It.IsAny<string>(), 
                                                                      It.IsAny<string>(),      
                                                                      It.IsAny<string>()))
                .ReturnsAsync(new OperationResult { Outcome = Outcome.Success });

            var command = new NotifyOfFatalFailure
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

                   try { await notifierFunctions.HandleNotifyOfFatalFailureAsync(message); }  catch {}
                }

                jsonWriter.Close();
                writer.Close();
                memStream.Close();
            };

            mockEventPublisher.Verify(pub => pub.TryPublishAsync(It.Is<NotificationSent>(evt => 
                    ((evt.OrderId      == command.OrderId)        &&
                     (evt.CorrelationId == command.CorrelationId)  &&
                     (evt.PartnerCode  == command.PartnerCode)))), 
                Times.Once, "because the event should have been published");    
        }   

        /// <summary>
        ///   Verifies behavior of the <see cref="NotifierFunctions.ProcessDeadLetterMessage" />
        ///   method;
        /// </summary>
        /// 
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void ProcessDeadLetterMessageValidatesTheDeadLetterLocation(string location)
        {
            var mockClock            = new Mock<IClock>();
            var mockNotifier         = new Mock<INotifier>();
            var mockFailurePublisher = new Mock<ICommandPublisher<NotifyOfFatalFailure>>();
            var mockEventPublisher   = new Mock<IEventPublisher<EventBase>>();
            var mockLogger           = new Mock<ILogger>();
            var mockLifetimeScope    = new Mock<IDisposable>();
            var serializerSettings   = new JsonSerializerSettings();
            var serializer           = new JsonSerializer { ContractResolver = new CamelCasePropertyNamesContractResolver() };
            var notifierFunctions    = new TestNotifierFunctions(serializer, new CommandRetryThresholds(-1, 1, 1), mockClock.Object, mockNotifier.Object, mockFailurePublisher.Object, mockEventPublisher.Object, mockLogger.Object, mockLifetimeScope.Object);

            serializer.Converters.Add(new StringEnumConverter());
            serializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            serializerSettings.Converters.Add(new StringEnumConverter());

            Action actionUnderTest = () => notifierFunctions.ProcessDeadLetterMessage(location, new BrokeredMessage()).GetAwaiter().GetResult();
            actionUnderTest.ShouldThrow<ArgumentNullException>("because the dead letter location is required");
        }

        /// <summary>
        ///   Verifies behavior of the <see cref="NotifierFunctions.ProcessDeadLetterMessage" />
        ///   method;
        /// </summary>
        /// 
        [Fact]
        public void ProcessDeadLetterMessageValidatesTheBrokeredMessage()
        {
            var mockClock            = new Mock<IClock>();
            var mockNotifier         = new Mock<INotifier>();
            var mockFailurePublisher = new Mock<ICommandPublisher<NotifyOfFatalFailure>>();
            var mockEventPublisher   = new Mock<IEventPublisher<EventBase>>();
            var mockLogger           = new Mock<ILogger>();
            var mockLifetimeScope    = new Mock<IDisposable>();
            var serializerSettings   = new JsonSerializerSettings();
            var serializer           = new JsonSerializer { ContractResolver = new CamelCasePropertyNamesContractResolver() };
            var notifierFunctions    = new TestNotifierFunctions(serializer, new CommandRetryThresholds(-1, 1, 1), mockClock.Object, mockNotifier.Object, mockFailurePublisher.Object, mockEventPublisher.Object, mockLogger.Object, mockLifetimeScope.Object);

            serializer.Converters.Add(new StringEnumConverter());
            serializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            serializerSettings.Converters.Add(new StringEnumConverter());

            Action actionUnderTest = () => notifierFunctions.ProcessDeadLetterMessage("processOrder", null).GetAwaiter().GetResult();
            actionUnderTest.ShouldThrow<ArgumentNullException>("because the brokered order message is required");
        }

        /// <summary>
        ///   Verifies behavior of the <see cref="NotifierFunctions.ProcessDeadLetterMessage" />
        ///   method;
        /// </summary>
        /// 
        [Fact]
        public void ProcessDeadLetterMessageFailsWhenTheMessageContainsNoCommand()
        {
            var mockClock            = new Mock<IClock>();
            var mockNotifier         = new Mock<INotifier>();
            var mockFailurePublisher = new Mock<ICommandPublisher<NotifyOfFatalFailure>>();
            var mockEventPublisher   = new Mock<IEventPublisher<EventBase>>();
            var mockLogger           = new Mock<ILogger>();
            var mockLifetimeScope    = new Mock<IDisposable>();
            var serializerSettings   = new JsonSerializerSettings();
            var serializer           = new JsonSerializer { ContractResolver = new CamelCasePropertyNamesContractResolver() };
            var notifierFunctions    = new TestNotifierFunctions(serializer, new CommandRetryThresholds(-1, 1, 1), mockClock.Object, mockNotifier.Object, mockFailurePublisher.Object, mockEventPublisher.Object, mockLogger.Object, mockLifetimeScope.Object);

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

                Action actionUnderTest = () => notifierFunctions.ProcessDeadLetterMessage("somequeue", message).GetAwaiter().GetResult();
                actionUnderTest.ShouldThrow<MissingDependencyException>("because the brokered message must contain a command");

                jsonWriter.Close();
                writer.Close();
                memStream.Close();
            };
        }

        /// <summary>
        ///   Verifies behavior of the <see cref="NotifierFunctions.ProcessDeadLetterMessage" />
        ///   method;
        /// </summary>
        /// 
        [Fact]
        public async Task ProcessDeadLetterMessageSendsNotification()
        {
            var mockClock            = new Mock<IClock>();
            var mockNotifier         = new Mock<INotifier>();
            var mockFailurePublisher = new Mock<ICommandPublisher<NotifyOfFatalFailure>>();
            var mockEventPublisher   = new Mock<IEventPublisher<EventBase>>();
            var mockLogger           = new Mock<ILogger>();
            var mockLifetimeScope    = new Mock<IDisposable>();
            var serializerSettings   = new JsonSerializerSettings();
            var serializer           = new JsonSerializer { ContractResolver = new CamelCasePropertyNamesContractResolver() };
            var notifierFunctions    = new TestNotifierFunctions(serializer, new CommandRetryThresholds(-1, 1, 1), mockClock.Object, mockNotifier.Object, mockFailurePublisher.Object, mockEventPublisher.Object, mockLogger.Object, mockLifetimeScope.Object);
            var location             = "Some Queue";
            var partner              = "That guy";
            var orderId              = "ABC123";
            var correlationId        = "Hello";

            serializer.Converters.Add(new StringEnumConverter());
            serializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            serializerSettings.Converters.Add(new StringEnumConverter());

            mockLogger
                .Setup(logger => logger.ForContext(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<bool>()))
                .Returns(mockLogger.Object);

            mockNotifier
                .Setup(notifier => notifier.NotifyDeadLetterMessageAsync(It.Is<string>(loc => loc == location),
                                                                         It.Is<string>(partnerCode => partnerCode == partner), 
                                                                         It.Is<string>(order => order == orderId),  
                                                                         It.Is<string>(correlation => correlation == correlationId)))
                .ReturnsAsync(new OperationResult { Outcome = Outcome.Success })
                .Verifiable("The notification should have been requested");

            var command = new ProcessOrder
            {
               Id              = Guid.NewGuid(),
               CorrelationId   = correlationId,
               PartnerCode     = partner,
               OrderId         = orderId,
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

                    await notifierFunctions.ProcessDeadLetterMessage(location, message);
                }

                jsonWriter.Close();
                writer.Close();
                memStream.Close();
            };

            mockNotifier.VerifyAll();
        }

        /// <summary>
        ///   Verifies behavior of the <see cref="NotifierFunctions.ProcessDeadLetterMessage" />
        ///   method;
        /// </summary>
        /// 
        [Fact]
        public void ProcessDeadLetterMessageThrowsWhenNotificationFails()
        {
            var mockClock            = new Mock<IClock>();
            var mockNotifier         = new Mock<INotifier>();
            var mockFailurePublisher = new Mock<ICommandPublisher<NotifyOfFatalFailure>>();
            var mockEventPublisher   = new Mock<IEventPublisher<EventBase>>();
            var mockLogger           = new Mock<ILogger>();
            var mockLifetimeScope    = new Mock<IDisposable>();
            var serializerSettings   = new JsonSerializerSettings();
            var serializer           = new JsonSerializer { ContractResolver = new CamelCasePropertyNamesContractResolver() };
            var notifierFunctions    = new TestNotifierFunctions(serializer, new CommandRetryThresholds(-1, 1, 1), mockClock.Object, mockNotifier.Object, mockFailurePublisher.Object, mockEventPublisher.Object, mockLogger.Object, mockLifetimeScope.Object);
            var expectedException    = new FormatException();

            serializer.Converters.Add(new StringEnumConverter());
            serializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            serializerSettings.Converters.Add(new StringEnumConverter());

            mockLogger
                .Setup(logger => logger.ForContext(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<bool>()))
                .Returns(mockLogger.Object);

            mockNotifier
                .Setup(notifier => notifier.NotifyDeadLetterMessageAsync(It.IsAny<string>(), 
                                                                         It.IsAny<string>(), 
                                                                         It.IsAny<string>(), 
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

                    Action actionUnderTest = () => notifierFunctions.ProcessDeadLetterMessage("some-queue/$DeadLetter", message).GetAwaiter().GetResult();

                    actionUnderTest.ShouldThrow<FormatException>("because the notifier experienced an exception")
                        .Subject.SingleOrDefault().Should().Be(expectedException, "because the exception should bubble");
                }

                jsonWriter.Close();
                writer.Close();
                memStream.Close();
            };
        }

        /// <summary>
        ///   Verifies behavior of the <see cref="NotifierFunctions.ProcessDeadLetterMessage" />
        ///   method;
        /// </summary>
        /// 
        [Fact]
        public async Task ProcessDeadLetterMessageDoesNotCompleteTheMessageWhenOrderSubmissionFails()
        {
            var mockClock            = new Mock<IClock>();
            var mockNotifier         = new Mock<INotifier>();
            var mockFailurePublisher = new Mock<ICommandPublisher<NotifyOfFatalFailure>>();
            var mockEventPublisher   = new Mock<IEventPublisher<EventBase>>();
            var mockLogger           = new Mock<ILogger>();
            var mockLifetimeScope    = new Mock<IDisposable>();
            var serializerSettings   = new JsonSerializerSettings();
            var serializer           = new JsonSerializer { ContractResolver = new CamelCasePropertyNamesContractResolver() };
            var notifierFunctions    = new TestNotifierFunctions(serializer, new CommandRetryThresholds(-1, 1, 1), mockClock.Object, mockNotifier.Object, mockFailurePublisher.Object, mockEventPublisher.Object, mockLogger.Object, mockLifetimeScope.Object);
            
            serializer.Converters.Add(new StringEnumConverter());
            serializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            serializerSettings.Converters.Add(new StringEnumConverter());

            mockLogger
                .Setup(logger => logger.ForContext(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<bool>()))
                .Returns(mockLogger.Object);

            mockNotifier
                .Setup(notifier => notifier.NotifyDeadLetterMessageAsync(It.IsAny<string>(), 
                                                             It.IsAny<string>(), 
                                                                      It.IsAny<string>(),    
                                                                      It.IsAny<string>()))
                .ThrowsAsync(new IndexOutOfRangeException());

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

                   try { await notifierFunctions.ProcessDeadLetterMessage("some-queue", message); }  catch {}
                }

                jsonWriter.Close();
                writer.Close();
                memStream.Close();
            };

            notifierFunctions.CompletedMessages.Should().BeEmpty("because no messages should have been completed");
        }

        /// <summary>
        ///   Verifies behavior of the <see cref="NotifierFunctions.ProcessDeadLetterMessage" />
        ///   method;
        /// </summary>
        /// 
        [Fact]
        public async Task ProcessDeadLetterMessagePublishesTheProperEventWhenOrderSubmissionFails()
        {
            var mockClock            = new Mock<IClock>();
            var mockNotifier         = new Mock<INotifier>();
            var mockFailurePublisher = new Mock<ICommandPublisher<NotifyOfFatalFailure>>();
            var mockEventPublisher   = new Mock<IEventPublisher<EventBase>>();
            var mockLogger           = new Mock<ILogger>();
            var mockLifetimeScope    = new Mock<IDisposable>();
            var serializerSettings   = new JsonSerializerSettings();
            var serializer           = new JsonSerializer { ContractResolver = new CamelCasePropertyNamesContractResolver() };
            var notifierFunctions    = new TestNotifierFunctions(serializer, new CommandRetryThresholds(-1, 1, 1), mockClock.Object, mockNotifier.Object, mockFailurePublisher.Object, mockEventPublisher.Object, mockLogger.Object, mockLifetimeScope.Object);
            
            serializer.Converters.Add(new StringEnumConverter());
            serializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            serializerSettings.Converters.Add(new StringEnumConverter());

            mockLogger
                .Setup(logger => logger.ForContext(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<bool>()))
                .Returns(mockLogger.Object);

            mockNotifier
                .Setup(notifier => notifier.NotifyDeadLetterMessageAsync(It.IsAny<string>(),
                                                                         It.IsAny<string>(), 
                                                                         It.IsAny<string>(), 
                                                                         It.IsAny<string>()))
                .ThrowsAsync(new OrderProcessingException());

            var command = new ProcessOrder
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

                   try { await notifierFunctions.ProcessDeadLetterMessage("place", message); }  catch {}
                }

                jsonWriter.Close();
                writer.Close();
                memStream.Close();
            };

            mockEventPublisher.Verify(pub => pub.TryPublishAsync(It.Is<NotificationFailed>(evt => 
                    ((evt.OrderId       == command.OrderId)        &&
                     (evt.CorrelationId == command.CorrelationId)  &&
                     (evt.PartnerCode   == command.PartnerCode)))), 
                Times.Once, "because the event should have been published");    
        }

        /// <summary>
        ///   Verifies behavior of the <see cref="NotifierFunctions.ProcessDeadLetterMessage" />
        ///   method;
        /// </summary>
        /// 
        [Fact]
        public async Task ProcessDeadLetterMessageCompletesTheMessageWhenNotificationSucceeds()
        {
            var mockClock            = new Mock<IClock>();
            var mockNotifier         = new Mock<INotifier>();
            var mockFailurePublisher = new Mock<ICommandPublisher<NotifyOfFatalFailure>>();
            var mockEventPublisher   = new Mock<IEventPublisher<EventBase>>();
            var mockLogger           = new Mock<ILogger>();
            var mockLifetimeScope    = new Mock<IDisposable>();
            var serializerSettings   = new JsonSerializerSettings();
            var serializer           = new JsonSerializer { ContractResolver = new CamelCasePropertyNamesContractResolver() };
            var notifierFunctions    = new TestNotifierFunctions(serializer, new CommandRetryThresholds(-1, 1, 1), mockClock.Object, mockNotifier.Object, mockFailurePublisher.Object, mockEventPublisher.Object, mockLogger.Object, mockLifetimeScope.Object);
            
            serializer.Converters.Add(new StringEnumConverter());
            serializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            serializerSettings.Converters.Add(new StringEnumConverter());

            mockLogger
                .Setup(logger => logger.ForContext(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<bool>()))
                .Returns(mockLogger.Object);

            mockNotifier
                .Setup(notifier => notifier.NotifyDeadLetterMessageAsync(It.IsAny<string>(),
                                                                         It.IsAny<string>(), 
                                                                         It.IsAny<string>(),       
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

                   try { await notifierFunctions.ProcessDeadLetterMessage("queue/$DeadLetter", message); }  catch {}
                }

                jsonWriter.Close();
                writer.Close();
                memStream.Close();
            };

            notifierFunctions.CompletedMessages.Should().HaveCount(1, "because the message should have been completed");
        }

        /// <summary>
        ///   Verifies behavior of the <see cref="NotifierFunctions.ProcessDeadLetterMessage" />
        ///   method;
        /// </summary>
        /// 
        [Fact]
        public async Task ProcessDeadLetterMessagePublishesTheProperEventWhenNotificationSucceeds()
        {
            var mockClock            = new Mock<IClock>();
            var mockNotifier         = new Mock<INotifier>();
            var mockFailurePublisher = new Mock<ICommandPublisher<NotifyOfFatalFailure>>();
            var mockEventPublisher   = new Mock<IEventPublisher<EventBase>>();
            var mockLogger           = new Mock<ILogger>();
            var mockLifetimeScope    = new Mock<IDisposable>();
            var serializerSettings   = new JsonSerializerSettings();
            var serializer           = new JsonSerializer { ContractResolver = new CamelCasePropertyNamesContractResolver() };
            var notifierFunctions    = new TestNotifierFunctions(serializer, new CommandRetryThresholds(-1, 1, 1), mockClock.Object, mockNotifier.Object, mockFailurePublisher.Object, mockEventPublisher.Object, mockLogger.Object, mockLifetimeScope.Object);
            
            serializer.Converters.Add(new StringEnumConverter());
            serializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            serializerSettings.Converters.Add(new StringEnumConverter());

            mockLogger
                .Setup(logger => logger.ForContext(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<bool>()))
                .Returns(mockLogger.Object);

            mockNotifier
                .Setup(notifier => notifier.NotifyDeadLetterMessageAsync(It.IsAny<string>(),
                                                                         It.IsAny<string>(), 
                                                                         It.IsAny<string>(),      
                                                                         It.IsAny<string>()))
                .ReturnsAsync(new OperationResult { Outcome = Outcome.Success });

            var command = new ProcessOrder
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

                   try { await notifierFunctions.ProcessDeadLetterMessage("a-queue/$DeadLetter", message); }  catch {}
                }

                jsonWriter.Close();
                writer.Close();
                memStream.Close();
            };

            mockEventPublisher.Verify(pub => pub.TryPublishAsync(It.Is<NotificationSent>(evt => 
                    ((evt.OrderId      == command.OrderId)        &&
                     (evt.CorrelationId == command.CorrelationId)  &&
                     (evt.PartnerCode  == command.PartnerCode)))), 
                Times.Once, "because the event should have been published");    
        }   

        /// <summary>
        ///   Verifies behavior of the <see cref="NotifierFunctions.HandleProcessOrderDeadLetterAsync" />
        ///   method;
        /// </summary>
        /// 
        [Fact]
        public async Task HandleProcessOrderDeadLetterAsyncProcessesWithTheCorrectLocationAndMessage()
        {
            var mockClock             = new Mock<IClock>();
            var mockNotifier          = new Mock<INotifier>();
            var mockFailurePublisher  = new Mock<ICommandPublisher<NotifyOfFatalFailure>>();
            var mockEventPublisher    = new Mock<IEventPublisher<EventBase>>();
            var mockLogger            = new Mock<ILogger>();
            var mockLifetimeScope     = new Mock<IDisposable>();
            var serializerSettings    = new JsonSerializerSettings();
            var serializer            = new JsonSerializer { ContractResolver = new CamelCasePropertyNamesContractResolver() };
            var mockNotifierFunctions = new Mock<NotifierFunctions>(serializer, new CommandRetryThresholds(-1, 1, 1), mockClock.Object, mockNotifier.Object, mockFailurePublisher.Object, mockEventPublisher.Object, mockLogger.Object, mockLifetimeScope.Object);
            var expectedMessage       = new BrokeredMessage();
            var expectedLocation      = TriggerQueueNames.ProcessOrderDeadLetterQueue;
            var location              = default(string);
            var message               = default(BrokeredMessage);
            
            serializer.Converters.Add(new StringEnumConverter());
            serializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            serializerSettings.Converters.Add(new StringEnumConverter());

            mockLogger
                .Setup(logger => logger.ForContext(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<bool>()))
                .Returns(mockLogger.Object);

            mockNotifierFunctions
                .Protected()
                .Setup<Task>("ProcessDeadLetterMessage", ItExpr.IsAny<string>(), ItExpr.IsAny<BrokeredMessage>())
                .Returns(Task.CompletedTask)
                .Callback<string, BrokeredMessage>( (locParam, messageParam) => { location = locParam; message = messageParam; });

            await mockNotifierFunctions.Object.HandleProcessOrderDeadLetterAsync(expectedMessage);

            location.Should().Be(expectedLocation, "because the proper location should have been used");
            message.Should().Be(expectedMessage, "because the message should have been sent unaltered");
        }

        /// <summary>
        ///   Verifies behavior of the <see cref="NotifierFunctions.HandleSubmitOrderForProductionDeadLetterAsync" />
        ///   method;
        /// </summary>
        /// 
        [Fact]
        public async Task HandleSubmitOrderForProductionDeadLetterAsyncProcessesWithTheCorrectLocationAndMessage()
        {
            var mockClock             = new Mock<IClock>();
            var mockNotifier          = new Mock<INotifier>();
            var mockFailurePublisher  = new Mock<ICommandPublisher<NotifyOfFatalFailure>>();
            var mockEventPublisher    = new Mock<IEventPublisher<EventBase>>();
            var mockLogger            = new Mock<ILogger>();
            var mockLifetimeScope     = new Mock<IDisposable>();
            var serializerSettings    = new JsonSerializerSettings();
            var serializer            = new JsonSerializer { ContractResolver = new CamelCasePropertyNamesContractResolver() };
            var mockNotifierFunctions = new Mock<NotifierFunctions>(serializer, new CommandRetryThresholds(-1, 1, 1), mockClock.Object, mockNotifier.Object, mockFailurePublisher.Object, mockEventPublisher.Object, mockLogger.Object, mockLifetimeScope.Object);
            var expectedMessage       = new BrokeredMessage();
            var expectedLocation      = TriggerQueueNames.SubmitOrderForProductionDeadLetterQueue;
            var location              = default(string);
            var message               = default(BrokeredMessage);
            
            serializer.Converters.Add(new StringEnumConverter());
            serializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            serializerSettings.Converters.Add(new StringEnumConverter());

            mockLogger
                .Setup(logger => logger.ForContext(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<bool>()))
                .Returns(mockLogger.Object);

            mockNotifierFunctions
                .Protected()
                .Setup<Task>("ProcessDeadLetterMessage", ItExpr.IsAny<string>(), ItExpr.IsAny<BrokeredMessage>())
                .Returns(Task.CompletedTask)
                .Callback<string, BrokeredMessage>( (locParam, messageParam) => { location = locParam; message = messageParam; });

            await mockNotifierFunctions.Object.HandleSubmitOrderForProductionDeadLetterAsync(expectedMessage);

            location.Should().Be(expectedLocation, "because the proper location should have been used");
            message.Should().Be(expectedMessage, "because the message should have been sent unaltered");
        }

        /// <summary>
        ///   Verifies behavior of the <see cref="NotifierFunctions.HandleNotifyOfFatalFailureDeadLetterQueueDeadLetterAsync" />
        ///   method;
        /// </summary>
        /// 
        [Fact]
        public async Task HandleNotifyOfFatalFailureDeadLetterQueueDeadLetterAsyncProcessesWithTheCorrectLocationAndMessage()
        {
            var mockClock             = new Mock<IClock>();
            var mockNotifier          = new Mock<INotifier>();
            var mockFailurePublisher  = new Mock<ICommandPublisher<NotifyOfFatalFailure>>();
            var mockEventPublisher    = new Mock<IEventPublisher<EventBase>>();
            var mockLogger            = new Mock<ILogger>();
            var mockLifetimeScope     = new Mock<IDisposable>();
            var serializerSettings    = new JsonSerializerSettings();
            var serializer            = new JsonSerializer { ContractResolver = new CamelCasePropertyNamesContractResolver() };
            var mockNotifierFunctions = new Mock<NotifierFunctions>(serializer, new CommandRetryThresholds(-1, 1, 1), mockClock.Object, mockNotifier.Object, mockFailurePublisher.Object, mockEventPublisher.Object, mockLogger.Object, mockLifetimeScope.Object);
            var expectedMessage       = new BrokeredMessage();
            var expectedLocation      = TriggerQueueNames.NotifyOfFatalFailureDeadLetterQueue;
            var location              = default(string);
            var message               = default(BrokeredMessage);
            
            serializer.Converters.Add(new StringEnumConverter());
            serializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            serializerSettings.Converters.Add(new StringEnumConverter());

            mockLogger
                .Setup(logger => logger.ForContext(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<bool>()))
                .Returns(mockLogger.Object);

            mockNotifierFunctions
                .Protected()
                .Setup<Task>("ProcessDeadLetterMessage", ItExpr.IsAny<string>(), ItExpr.IsAny<BrokeredMessage>())
                .Returns(Task.CompletedTask)
                .Callback<string, BrokeredMessage>( (locParam, messageParam) => { location = locParam; message = messageParam; });

            await mockNotifierFunctions.Object.HandleNotifyOfFatalFailureDeadLetterQueueDeadLetterAsync(expectedMessage);

            location.Should().Be(expectedLocation, "because the proper location should have been used");
            message.Should().Be(expectedMessage, "because the message should have been sent unaltered");
        }

        #region NestedClasses

            private class TestNotifierFunctions : NotifierFunctions
            {
                public HashSet<BrokeredMessage> CompletedMessages = new HashSet<BrokeredMessage>();

                public TestNotifierFunctions(JsonSerializer                          jsonSerializer,
                                             CommandRetryThresholds                  retryThresholds,
                                             IClock                                  clock,
                                             INotifier                               notifier,
                                             ICommandPublisher<NotifyOfFatalFailure> notifyOfFatalFailurePublisher,
                                             IEventPublisher<EventBase>              eventPublisher,
                                             ILogger                                 logger,
                                             IDisposable                             lifetimeScope) : base(jsonSerializer, retryThresholds, clock, notifier, notifyOfFatalFailurePublisher, eventPublisher, logger, lifetimeScope)
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
