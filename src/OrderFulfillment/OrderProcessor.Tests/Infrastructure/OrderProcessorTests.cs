using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using FluentAssertions;
using OrderFulfillment.Core.Exceptions;
using OrderFulfillment.Core.External;
using OrderFulfillment.Core.Models.External.Ecommerce;
using OrderFulfillment.Core.Models.External.OrderProduction;
using OrderFulfillment.Core.Models.Operations;
using OrderFulfillment.Core.Storage;
using OrderFulfillment.OrderProcessor.Configuration;
using OrderFulfillment.OrderProcessor.Infrastructure;
using OrderFulfillment.OrderProcessor.Models;
using Moq;
using Moq.Protected;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using NodaTime;
using NodaTime.Testing;
using Serilog;
using Xunit;


using Ecom      = OrderFulfillment.Core.Models.External.Ecommerce;
using Processor = OrderFulfillment.OrderProcessor.Infrastructure;

namespace OrderFulfillment.OrderProcessor.Tests.Infrastructure
{
    /// <summary>
    ///   The suite of tests for the <see cref="OrderProcessor" /> class.
    /// </summary>
    /// 
    public class OrderProcessorTests
    {
        /// <summary>
        ///   Verifies behavior of the constructor.
        /// </summary>
        /// 
        [Fact]
        public void ConstructorValidatesTheConfiguration()
        {
            Action actionUnderTest = () => new Processor.OrderProcessor(null, Mock.Of<IEcommerceClient>(), Mock.Of<IOrderStorage>(), Mock.Of<ISkuMetadataProcessor>(), Mock.Of<ILogger>(), Mock.Of<IClock>(), new JsonSerializerSettings());
            actionUnderTest.ShouldThrow<ArgumentNullException>("because the configuration is required");
        }

        /// <summary>
        ///   Verifies behavior of the constructor.
        /// </summary>
        /// 
        [Fact]
        public void ConstructorValidatesTheEcommerceClient()
        {
            Action actionUnderTest = () => new Processor.OrderProcessor(new OrderProcessorConfiguration(), null, Mock.Of<IOrderStorage>(), Mock.Of<ISkuMetadataProcessor>(), Mock.Of<ILogger>(), Mock.Of<IClock>(), new JsonSerializerSettings());
            actionUnderTest.ShouldThrow<ArgumentNullException>("because the eCommerce client is required");
        }

        /// <summary>
        ///   Verifies behavior of the constructor.
        /// </summary>
        /// 
        [Fact]
        public void ConstructorValidatesTheOrderStorage()
        {
            Action actionUnderTest = () => new Processor.OrderProcessor(new OrderProcessorConfiguration(), Mock.Of<IEcommerceClient>(), null, Mock.Of<ISkuMetadataProcessor>(), Mock.Of<ILogger>(), Mock.Of<IClock>(), new JsonSerializerSettings());
            actionUnderTest.ShouldThrow<ArgumentNullException>("because the storage is required");
        }

        /// <summary>
        ///   Verifies behavior of the constructor.
        /// </summary>
        /// 
        [Fact]
        public void ConstructorValidatesSkuMetadataProcessor()
        {
            Action actionUnderTest = () => new Processor.OrderProcessor(new OrderProcessorConfiguration(), Mock.Of<IEcommerceClient>(), Mock.Of<IOrderStorage>(), null, Mock.Of<ILogger>(), Mock.Of<IClock>(), new JsonSerializerSettings());
            actionUnderTest.ShouldThrow<ArgumentNullException>("because the SKU metadata processor is required");
        }

        /// <summary>
        ///   Verifies behavior of the constructor.
        /// </summary>
        /// 
        [Fact]
        public void ConstructorValidatesTheLogger()
        {
            Action actionUnderTest = () => new Processor.OrderProcessor(new OrderProcessorConfiguration(), Mock.Of<IEcommerceClient>(), Mock.Of<IOrderStorage>(), Mock.Of<ISkuMetadataProcessor>(), null, Mock.Of<IClock>(), new JsonSerializerSettings());
            actionUnderTest.ShouldThrow<ArgumentNullException>("because the logger is required");
        }

        /// <summary>
        ///   Verifies behavior of the constructor.
        /// </summary>
        /// 
        [Fact]
        public void ConstructorValidatesTheClock()
        {
            Action actionUnderTest = () => new Processor.OrderProcessor(new OrderProcessorConfiguration(), Mock.Of<IEcommerceClient>(), Mock.Of<IOrderStorage>(), Mock.Of<ISkuMetadataProcessor>(), Mock.Of<ILogger>(), null, new JsonSerializerSettings());
            actionUnderTest.ShouldThrow<ArgumentNullException>("because the clock is required");
        }

        /// <summary>
        ///   Verifies behavior of the constructor.
        /// </summary>
        /// 
        [Fact]
        public void ConstructorValidatesTheSerializerSettings()
        {
            Action actionUnderTest = () => new Processor.OrderProcessor(new OrderProcessorConfiguration(), Mock.Of<IEcommerceClient>(), Mock.Of<IOrderStorage>(), Mock.Of<ISkuMetadataProcessor>(), Mock.Of<ILogger>(), Mock.Of<IClock>(), null);
            actionUnderTest.ShouldThrow<ArgumentNullException>("because the serializer settings are required");
        }

        /// <summary>
        ///  Verifies functionality of the <see cref="Processor.OrderProcessor.ProcessOrderAsync" />
        ///  method.
        /// </summary>
        /// 
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void ProcessOrderAsyncValidatesThePartner(string partner)
        {
            var processor =  new Processor.OrderProcessor(new OrderProcessorConfiguration(), Mock.Of<IEcommerceClient>(), Mock.Of<IOrderStorage>(), Mock.Of<ISkuMetadataProcessor>(), Mock.Of<ILogger>(), Mock.Of<IClock>(), new JsonSerializerSettings());
            
            Action actionUnderTest = () => processor.ProcessOrderAsync(partner, "ABC", new Dictionary<string, string> {{ "one", "one" }}, new DependencyEmulation(), "blue").GetAwaiter().GetResult();
            actionUnderTest.ShouldThrow<ArgumentException>("because the partner is required").And.ParamName.Should().Be(nameof(partner), "because the partner was invalid");
        }

        /// <summary>
        ///  Verifies functionality of the <see cref="Processor.OrderProcessor.ProcessOrderAsync" />
        ///  method.
        /// </summary>
        /// 
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void ProcessOrderAsyncValidatesTheOrder(string orderId)
        {
            var processor =  new Processor.OrderProcessor(new OrderProcessorConfiguration(), Mock.Of<IEcommerceClient>(), Mock.Of<IOrderStorage>(), Mock.Of<ISkuMetadataProcessor>(), Mock.Of<ILogger>(), Mock.Of<IClock>(), new JsonSerializerSettings());
            
            Action actionUnderTest = () => processor.ProcessOrderAsync("Bob", orderId, new Dictionary<string, string> {{ "one", "one" }}, new DependencyEmulation(), "blue").GetAwaiter().GetResult();
            actionUnderTest.ShouldThrow<ArgumentException>("because the orderId is required").And.ParamName.Should().Be(nameof(orderId), "because the orderId was invalid");
        }

        /// <summary>
        ///  Verifies functionality of the <see cref="Processor.OrderProcessor.ProcessOrderAsync" />
        ///  method.
        /// </summary>
        /// 
        [Fact]
        public void ProcessOrderAsyncValidatesTheAssetsAreProvided()
        {
            var processor =  new Processor.OrderProcessor(new OrderProcessorConfiguration(), Mock.Of<IEcommerceClient>(), Mock.Of<IOrderStorage>(), Mock.Of<ISkuMetadataProcessor>(), Mock.Of<ILogger>(), Mock.Of<IClock>(), new JsonSerializerSettings());
            
            Action actionUnderTest = () => processor.ProcessOrderAsync("Bob", "ABC", null, new DependencyEmulation(), "blue").GetAwaiter().GetResult();
            actionUnderTest.ShouldThrow<ArgumentException>("because the order assets are required").And.ParamName.Should().Be("orderAssets", "because the order aseets were invalid");
        }

        /// <summary>
        ///  Verifies functionality of the <see cref="Processor.OrderProcessor.ProcessOrderAsync" />
        ///  method.
        /// </summary>
        /// 
        [Fact]
        public void ProcessOrderAsyncValidatesTheAssetsArePresent()
        {
            var processor =  new Processor.OrderProcessor(new OrderProcessorConfiguration(), Mock.Of<IEcommerceClient>(), Mock.Of<IOrderStorage>(), Mock.Of<ISkuMetadataProcessor>(), Mock.Of<ILogger>(), Mock.Of<IClock>(), new JsonSerializerSettings());
            
            Action actionUnderTest = () => processor.ProcessOrderAsync("Bob", "ABC", new Dictionary<string, string>(), new DependencyEmulation(), "blue").GetAwaiter().GetResult();
            actionUnderTest.ShouldThrow<ArgumentException>("because the order assets are required").And.ParamName.Should().Be("orderAssets", "because the order aseets were invalid");
        }

        /// <summary>
        ///  Verifies functionality of the <see cref="Processor.OrderProcessor.ProcessOrderAsync" />
        ///  method.
        /// </summary>
        /// 
        [Fact]
        public async Task ProcessOrderAsyncReturnsTheDetailsResultIfFailed()
        {
            var mockClient         = new Mock<IEcommerceClient>();
            var mockStorage        = new Mock<IOrderStorage>();
            var mockSkuProcessor   = new Mock<ISkuMetadataProcessor>();
            var mockLogger         = new Mock<ILogger>();
            var fakeClock          = new FakeClock(Instant.FromUtc(2007, 06, 09, 11, 11, 15));
            var serializerSettings = new JsonSerializerSettings();
            var configuration      = new OrderProcessorConfiguration();
            var processor          = new Mock<Processor.OrderProcessor>(configuration, mockClient.Object, mockStorage.Object, mockSkuProcessor.Object, mockLogger.Object, fakeClock, serializerSettings) { CallBase = true };
            var expectedResult     = new OperationResult { Outcome = Outcome.Failure, Reason = "because", Payload = "payload!", Recoverable = Recoverability.Retriable };
            
            serializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            serializerSettings.Converters.Add(new StringEnumConverter());

            mockLogger
                .Setup(logger => logger.ForContext(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<bool>()))
                .Returns(mockLogger.Object);

            processor
                .Protected()
                .Setup<Task<OperationResult>>("RetrieveOrderDetailsAsync", ItExpr.IsAny<ILogger>(), ItExpr.IsAny<IEcommerceClient>(), ItExpr.IsAny<string>(), ItExpr.IsAny<string>(), ItExpr.IsAny<string>(), ItExpr.IsAny<OperationResult>())
                .Returns(Task.FromResult(expectedResult));

            var result = await processor.Object.ProcessOrderAsync("ABC", "123", new Dictionary<string, string> {{ "one", "one" }});
            
            result.Should().NotBeNull("becuase a result should have been returned");
            result.ShouldBeEquivalentTo(expectedResult, "because the failed result should short-circuit the processing");
        }

        /// <summary>
        ///  Verifies functionality of the <see cref="Processor.OrderProcessor.ProcessOrderAsync" />
        ///  method.
        /// </summary>
        /// 
        [Fact]
        public async Task ProcessOrderAsyncFollowsTheRetryPolicyResultIfRetrieveOrderDetailsFailed()
        {
            var mockClient         = new Mock<IEcommerceClient>();
            var mockStorage        = new Mock<IOrderStorage>();
            var mockSkuProcessor   = new Mock<ISkuMetadataProcessor>();
            var mockLogger         = new Mock<ILogger>();
            var fakeClock          = new FakeClock(Instant.FromUtc(2007, 06, 09, 11, 11, 15));
            var serializerSettings = new JsonSerializerSettings();
            var configuration      = new OrderProcessorConfiguration { OperationRetryMaxCount = 3, OperationRetryExponentialSeconds = 0, OperationRetryJitterSeconds = 0 };
            var processor          = new Mock<Processor.OrderProcessor>(configuration, mockClient.Object, mockStorage.Object, mockSkuProcessor.Object, mockLogger.Object, fakeClock, serializerSettings) { CallBase = true };
            var expectedResult     = new OperationResult { Outcome = Outcome.Failure, Reason = "because", Payload = "payload!", Recoverable = Recoverability.Retriable };
            var detailsInvoked     = 0;
            
            serializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            serializerSettings.Converters.Add(new StringEnumConverter());

            mockLogger
                .Setup(logger => logger.ForContext(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<bool>()))
                .Returns(mockLogger.Object);

            processor
                .Protected()
                .Setup<Task<OperationResult>>("RetrieveOrderDetailsAsync", ItExpr.IsAny<ILogger>(), ItExpr.IsAny<IEcommerceClient>(), ItExpr.IsAny<string>(), ItExpr.IsAny<string>(), ItExpr.IsAny<string>(), ItExpr.IsAny<OperationResult>())
                .Returns(Task.FromResult(expectedResult))
                .Callback( () => ++detailsInvoked);

            var result = await processor.Object.ProcessOrderAsync("ABC", "123", new Dictionary<string, string> {{ "one", "one" }});
            
            detailsInvoked.Should().Be(configuration.OperationRetryMaxCount + 1, "because the failures should have followed the retry policy.");
        }

        /// <summary>
        ///  Verifies functionality of the <see cref="Processor.OrderProcessor.ProcessOrderAsync" />
        ///  method.
        /// </summary>
        /// 
        [Fact]
        public async Task ProcessOrderAsyncReturnsTheCreateOrderMessageResultIfFailed()
        {
            var mockClient         = new Mock<IEcommerceClient>();
            var mockStorage        = new Mock<IOrderStorage>();
            var mockSkuProcessor   = new Mock<ISkuMetadataProcessor>();
            var mockLogger         = new Mock<ILogger>();
            var fakeClock          = new FakeClock(Instant.FromUtc(2007, 06, 09, 11, 11, 15));
            var serializerSettings = new JsonSerializerSettings();
            var configuration      = new OrderProcessorConfiguration();
            var processor          = new Mock<Processor.OrderProcessor>(configuration, mockClient.Object, mockStorage.Object, mockSkuProcessor.Object, mockLogger.Object, fakeClock, serializerSettings) { CallBase = true };
            var orderDetails       = new OrderDetails { OrderId = "ABC123", UserId = "Bob", LineItems = new List<Ecom.LineItem>(), Recipients = new List<Ecom.Recipient>() };
            var successResult      = new OperationResult { Outcome = Outcome.Success, Reason = String.Empty, Recoverable = Recoverability.Final };
            var expectedResult     = new OperationResult { Outcome = Outcome.Failure, Reason = "because", Payload = "payload!", Recoverable = Recoverability.Retriable };
            
            serializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            serializerSettings.Converters.Add(new StringEnumConverter());

            successResult.Payload = JsonConvert.SerializeObject(orderDetails, serializerSettings);
            
            mockLogger
                .Setup(logger => logger.ForContext(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<bool>()))
                .Returns(mockLogger.Object);

            processor
                .Protected()
                .Setup<Task<OperationResult>>("RetrieveOrderDetailsAsync", ItExpr.IsAny<ILogger>(), ItExpr.IsAny<IEcommerceClient>(), ItExpr.IsAny<string>(), ItExpr.IsAny<string>(), ItExpr.IsAny<string>(), ItExpr.IsAny<OperationResult>())
                .Returns(Task.FromResult(successResult));

            processor
                .Protected()
                .Setup<Task<OperationResult>>("BuildCreateOrderMessageFromDetailsAsync", ItExpr.IsAny<OrderProcessorConfiguration>(), ItExpr.IsAny<ILogger>(), ItExpr.IsAny<ISkuMetadataProcessor>(), ItExpr.IsAny<string>(), ItExpr.IsAny<string>(), ItExpr.IsAny<IReadOnlyDictionary<string, string>>(), ItExpr.IsAny<OrderDetails>(), ItExpr.IsAny<JsonSerializerSettings>(), ItExpr.IsAny<string>(), ItExpr.IsAny<OperationResult>())
                .Returns(Task.FromResult(expectedResult));

            var result = await processor.Object.ProcessOrderAsync("ABC", "123", new Dictionary<string, string> {{ "one", "one" }});
            
            result.Should().NotBeNull("becuase a result should have been returned");
            result.ShouldBeEquivalentTo(expectedResult, "because the failed result should short-circuit the processing");

        }

        /// <summary>
        ///  Verifies functionality of the <see cref="Processor.OrderProcessor.ProcessOrderAsync" />
        ///  method.
        /// </summary>
        /// 
        [Fact]
        public async Task ProcessOrderAsyncReturnsTheStorageResultIfFailed()
        {
            var mockClient         = new Mock<IEcommerceClient>();
            var mockStorage        = new Mock<IOrderStorage>();
            var mockSkuProcessor   = new Mock<ISkuMetadataProcessor>();
            var mockLogger         = new Mock<ILogger>();
            var fakeClock          = new FakeClock(Instant.FromUtc(2007, 06, 09, 11, 11, 15));
            var serializerSettings = new JsonSerializerSettings();
            var configuration      = new OrderProcessorConfiguration();
            var processor          = new Mock<Processor.OrderProcessor>(configuration, mockClient.Object, mockStorage.Object, mockSkuProcessor.Object, mockLogger.Object, fakeClock, serializerSettings) { CallBase = true };
            var orderDetails       = new OrderDetails { OrderId = "ABC123", UserId = "Bob", LineItems = new List<Ecom.LineItem>(), Recipients = new List<Ecom.Recipient>() };
            var detailsResult      = new OperationResult { Outcome = Outcome.Success, Reason = String.Empty, Recoverable = Recoverability.Final };
            var createOrderResult  = new OperationResult { Outcome = Outcome.Success, Reason = String.Empty, Recoverable = Recoverability.Final };
            var expectedResult     = new OperationResult { Outcome = Outcome.Failure, Reason = "because", Payload = "payload!", Recoverable = Recoverability.Retriable };
            
            serializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            serializerSettings.Converters.Add(new StringEnumConverter());

            detailsResult.Payload     = JsonConvert.SerializeObject(orderDetails, serializerSettings);
            createOrderResult.Payload = JsonConvert.SerializeObject(new CreateOrderMessage(), serializerSettings);
            
            mockLogger
                .Setup(logger => logger.ForContext(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<bool>()))
                .Returns(mockLogger.Object);

            processor
                .Protected()
                .Setup<Task<OperationResult>>("RetrieveOrderDetailsAsync", ItExpr.IsAny<ILogger>(), ItExpr.IsAny<IEcommerceClient>(), ItExpr.IsAny<string>(), ItExpr.IsAny<string>(), ItExpr.IsAny<string>(), ItExpr.IsAny<OperationResult>())
                .Returns(Task.FromResult(detailsResult));

            processor
                .Protected()
                .Setup<Task<OperationResult>>("BuildCreateOrderMessageFromDetailsAsync", ItExpr.IsAny<OrderProcessorConfiguration>(), ItExpr.IsAny<ILogger>(), ItExpr.IsAny<ISkuMetadataProcessor>(), ItExpr.IsAny<string>(), ItExpr.IsAny<string>(), ItExpr.IsAny<IReadOnlyDictionary<string, string>>(), ItExpr.IsAny<OrderDetails>(), ItExpr.IsAny<JsonSerializerSettings>(), ItExpr.IsAny<string>(), ItExpr.IsAny<OperationResult>())
                .Returns(Task.FromResult(createOrderResult));

            processor
                .Protected()
                .Setup<Task<OperationResult>>("StoreOrderForSubmissionAsync", ItExpr.IsAny<ILogger>(), ItExpr.IsAny<IOrderStorage>(), ItExpr.IsAny<string>(), ItExpr.IsAny<string>(), ItExpr.IsAny<CreateOrderMessage>(), ItExpr.IsAny<string>(), ItExpr.IsAny<OperationResult>())
                .Returns(Task.FromResult(expectedResult));

            var result = await processor.Object.ProcessOrderAsync("ABC", "123", new Dictionary<string, string> {{ "one", "one" }});
            
            result.Should().NotBeNull("becuase a result should have been returned");
            result.ShouldBeEquivalentTo(expectedResult, "because the failed result should short-circuit the processing");

        }

        /// <summary>
        ///  Verifies functionality of the <see cref="Processor.OrderProcessor.ProcessOrderAsync" />
        ///  method.
        /// </summary>
        /// 
        [Fact]
        public async Task ProcessOrderAsyncFollowsTheRetryPolicyIfTheStorageResultFailed()
        {
            var mockClient         = new Mock<IEcommerceClient>();
            var mockStorage        = new Mock<IOrderStorage>();
            var mockSkuProcessor   = new Mock<ISkuMetadataProcessor>();
            var mockLogger         = new Mock<ILogger>();
            var fakeClock          = new FakeClock(Instant.FromUtc(2007, 06, 09, 11, 11, 15));
            var serializerSettings = new JsonSerializerSettings();
            var configuration      = new OrderProcessorConfiguration { OperationRetryMaxCount = 6, OperationRetryExponentialSeconds = 0, OperationRetryJitterSeconds = 0 };
            var processor          = new Mock<Processor.OrderProcessor>(configuration, mockClient.Object, mockStorage.Object, mockSkuProcessor.Object, mockLogger.Object, fakeClock, serializerSettings) { CallBase = true };
            var orderDetails       = new OrderDetails { OrderId = "ABC123", UserId = "Bob", LineItems = new List<Ecom.LineItem>(), Recipients = new List<Ecom.Recipient>() };
            var detailsResult      = new OperationResult { Outcome = Outcome.Success, Reason = String.Empty, Recoverable = Recoverability.Final };
            var createOrderResult  = new OperationResult { Outcome = Outcome.Success, Reason = String.Empty, Recoverable = Recoverability.Final };
            var expectedResult     = new OperationResult { Outcome = Outcome.Failure, Reason = "because", Payload = "payload!", Recoverable = Recoverability.Retriable };
            var storageInvoked     = 0;
            
            serializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            serializerSettings.Converters.Add(new StringEnumConverter());

            detailsResult.Payload     = JsonConvert.SerializeObject(orderDetails, serializerSettings);
            createOrderResult.Payload = JsonConvert.SerializeObject(new CreateOrderMessage(), serializerSettings);
            
            mockLogger
                .Setup(logger => logger.ForContext(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<bool>()))
                .Returns(mockLogger.Object);

            processor
                .Protected()
                .Setup<Task<OperationResult>>("RetrieveOrderDetailsAsync", ItExpr.IsAny<ILogger>(), ItExpr.IsAny<IEcommerceClient>(), ItExpr.IsAny<string>(), ItExpr.IsAny<string>(), ItExpr.IsAny<string>(), ItExpr.IsAny<OperationResult>())
                .Returns(Task.FromResult(detailsResult));

            processor
                .Protected()
                .Setup<Task<OperationResult>>("BuildCreateOrderMessageFromDetailsAsync", ItExpr.IsAny<OrderProcessorConfiguration>(), ItExpr.IsAny<ILogger>(), ItExpr.IsAny<ISkuMetadataProcessor>(), ItExpr.IsAny<string>(), ItExpr.IsAny<string>(), ItExpr.IsAny<IReadOnlyDictionary<string, string>>(), ItExpr.IsAny<OrderDetails>(), ItExpr.IsAny<JsonSerializerSettings>(), ItExpr.IsAny<string>(), ItExpr.IsAny<OperationResult>())
                .Returns(Task.FromResult(createOrderResult));

            processor
                .Protected()
                .Setup<Task<OperationResult>>("StoreOrderForSubmissionAsync", ItExpr.IsAny<ILogger>(), ItExpr.IsAny<IOrderStorage>(), ItExpr.IsAny<string>(), ItExpr.IsAny<string>(), ItExpr.IsAny<CreateOrderMessage>(), ItExpr.IsAny<string>(), ItExpr.IsAny<OperationResult>())
                .Returns(Task.FromResult(expectedResult))
                .Callback( () => ++storageInvoked);

            var result = await processor.Object.ProcessOrderAsync("ABC", "123", new Dictionary<string, string> {{ "one", "one" }});
            
            storageInvoked.Should().Be(configuration.OperationRetryMaxCount + 1, "because the failures should have followed the retry policy.");

        }

        /// <summary>
        ///  Verifies functionality of the <see cref="Processor.OrderProcessor.ProcessOrderAsync" />
        ///  method.
        /// </summary>
        /// 
        [Fact]
        public async Task ProcessOrderAsyncRespectsAnEmulatedDetailsResult()
        {
            var mockClient         = new Mock<IEcommerceClient>();
            var mockStorage        = new Mock<IOrderStorage>();
            var mockSkuProcessor   = new Mock<ISkuMetadataProcessor>();
            var mockLogger         = new Mock<ILogger>();
            var fakeClock          = new FakeClock(Instant.FromUtc(2007, 06, 09, 11, 11, 15));
            var serializerSettings = new JsonSerializerSettings();
            var configuration      = new OrderProcessorConfiguration();
            var processor          = new Mock<Processor.OrderProcessor>(configuration, mockClient.Object, mockStorage.Object, mockSkuProcessor.Object, mockLogger.Object, fakeClock, serializerSettings) { CallBase = true };
            var orderDetails       = new OrderDetails { OrderId = "ABC123", UserId = "Bob", LineItems = new List<Ecom.LineItem>(), Recipients = new List<Ecom.Recipient>() };
            var createOrderResult  = new OperationResult { Outcome = Outcome.Success, Reason = String.Empty, Recoverable = Recoverability.Final };
            var storageResult      = new OperationResult { Outcome = Outcome.Success, Reason = String.Empty, Recoverable = Recoverability.Final };
            var expectedResult     = new OperationResult { Outcome = Outcome.Failure, Reason = "because", Payload = "payload!", Recoverable = Recoverability.Retriable };
            
            serializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            serializerSettings.Converters.Add(new StringEnumConverter());

            createOrderResult.Payload = JsonConvert.SerializeObject(new CreateOrderMessage(), serializerSettings);

            mockLogger
                .Setup(logger => logger.ForContext(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<bool>()))
                .Returns(mockLogger.Object);

            processor
                .Protected()
                .Setup<Task<OperationResult>>("BuildCreateOrderMessageFromDetailsAsync", ItExpr.IsAny<OrderProcessorConfiguration>(), ItExpr.IsAny<ILogger>(), ItExpr.IsAny<ISkuMetadataProcessor>(), ItExpr.IsAny<string>(), ItExpr.IsAny<string>(), ItExpr.IsAny<IReadOnlyDictionary<string, string>>(), ItExpr.IsAny<OrderDetails>(), ItExpr.IsAny<JsonSerializerSettings>(), ItExpr.IsAny<string>(), ItExpr.IsAny<OperationResult>())
                .Returns(Task.FromResult(createOrderResult));

            processor
                .Protected()
                .Setup<Task<OperationResult>>("StoreOrderForSubmissionAsync", ItExpr.IsAny<ILogger>(), ItExpr.IsAny<IOrderStorage>(), ItExpr.IsAny<string>(), ItExpr.IsAny<string>(), ItExpr.IsAny<CreateOrderMessage>(), ItExpr.IsAny<string>(), ItExpr.IsAny<OperationResult>())
                .Returns(Task.FromResult(storageResult));

            var result = await processor.Object.ProcessOrderAsync("ABC", "123", new Dictionary<string, string> {{ "one", "one" }}, new DependencyEmulation { OrderDetails = expectedResult });
            
            result.Should().NotBeNull("becuase a result should have been returned");
            result.ShouldBeEquivalentTo(expectedResult, "because the failed result should short-circuit the processing");

        }

        /// <summary>
        ///  Verifies functionality of the <see cref="Processor.OrderProcessor.ProcessOrderAsync" />
        ///  method.
        /// </summary>
        /// 
        [Fact]
        public async Task ProcessOrderAsyncRespectsAnEmulatedCreateOrderMessageResult()
        {
            var mockClient         = new Mock<IEcommerceClient>();
            var mockStorage        = new Mock<IOrderStorage>();
            var mockSkuProcessor   = new Mock<ISkuMetadataProcessor>();
            var mockLogger         = new Mock<ILogger>();
            var fakeClock          = new FakeClock(Instant.FromUtc(2007, 06, 09, 11, 11, 15));
            var serializerSettings = new JsonSerializerSettings();
            var configuration      = new OrderProcessorConfiguration();
            var processor          = new Mock<Processor.OrderProcessor>(configuration, mockClient.Object, mockStorage.Object, mockSkuProcessor.Object, mockLogger.Object, fakeClock, serializerSettings) { CallBase = true };
            var orderDetails       = new OrderDetails { OrderId = "ABC123", UserId = "Bob", LineItems = new List<Ecom.LineItem>(), Recipients = new List<Ecom.Recipient>() };
            var detailsResult      = new OperationResult { Outcome = Outcome.Success, Reason = String.Empty, Recoverable = Recoverability.Final };
            var storageResult      = new OperationResult { Outcome = Outcome.Success, Reason = String.Empty, Recoverable = Recoverability.Final };
            var expectedResult     = new OperationResult { Outcome = Outcome.Failure, Reason = "because", Payload = "payload!", Recoverable = Recoverability.Retriable };
            
            serializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            serializerSettings.Converters.Add(new StringEnumConverter());

            detailsResult.Payload = JsonConvert.SerializeObject(orderDetails, serializerSettings);

            mockLogger
                .Setup(logger => logger.ForContext(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<bool>()))
                .Returns(mockLogger.Object);

            processor
                .Protected()
                .Setup<Task<OperationResult>>("RetrieveOrderDetailsAsync", ItExpr.IsAny<ILogger>(), ItExpr.IsAny<IEcommerceClient>(), ItExpr.IsAny<string>(), ItExpr.IsAny<string>(), ItExpr.IsAny<string>(), ItExpr.IsAny<OperationResult>())
                .Returns(Task.FromResult(detailsResult));
                
            processor
                .Protected()
                .Setup<Task<OperationResult>>("StoreOrderForSubmissionAsync", ItExpr.IsAny<ILogger>(), ItExpr.IsAny<IOrderStorage>(), ItExpr.IsAny<string>(), ItExpr.IsAny<string>(), ItExpr.IsAny<CreateOrderMessage>(), ItExpr.IsAny<string>(), ItExpr.IsAny<OperationResult>())
                .Returns(Task.FromResult(storageResult));

            var result = await processor.Object.ProcessOrderAsync("ABC", "123", new Dictionary<string, string> {{ "one", "one" }}, new DependencyEmulation { CreateOrderMessage = expectedResult });
            
            result.Should().NotBeNull("becuase a result should have been returned");
            result.ShouldBeEquivalentTo(expectedResult, "because the failed result should short-circuit the processing");
        }

        /// <summary>
        ///  Verifies functionality of the <see cref="Processor.OrderProcessor.ProcessOrderAsync" />
        ///  method.
        /// </summary>
        /// 
        [Fact]
        public async Task ProcessOrderAsyncReturnsTheExceptionResultIfThrownDuringDetailsRetrieval()
        {
            var mockClient         = new Mock<IEcommerceClient>();
            var mockStorage        = new Mock<IOrderStorage>();
            var mockSkuProcessor   = new Mock<ISkuMetadataProcessor>();
            var mockLogger         = new Mock<ILogger>();
            var fakeClock          = new FakeClock(Instant.FromUtc(2007, 06, 09, 11, 11, 15));
            var serializerSettings = new JsonSerializerSettings();
            var configuration      = new OrderProcessorConfiguration();
            var processor          = new Mock<Processor.OrderProcessor>(configuration, mockClient.Object, mockStorage.Object, mockSkuProcessor.Object, mockLogger.Object, fakeClock, serializerSettings) { CallBase = true };
            
            serializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            serializerSettings.Converters.Add(new StringEnumConverter());

            mockLogger
                .Setup(logger => logger.ForContext(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<bool>()))
                .Returns(mockLogger.Object);

            processor
                .Protected()
                .Setup<Task<OperationResult>>("RetrieveOrderDetailsAsync", ItExpr.IsAny<ILogger>(), ItExpr.IsAny<IEcommerceClient>(), ItExpr.IsAny<string>(), ItExpr.IsAny<string>(), ItExpr.IsAny<string>(), ItExpr.IsAny<OperationResult>())
                .ThrowsAsync(new MissingDependencyException());

            var result = await processor.Object.ProcessOrderAsync("ABC", "123", new Dictionary<string, string> {{ "one", "one" }});
            
            result.Should().NotBeNull("becuase a result should have been returned");
            result.ShouldBeEquivalentTo(OperationResult.ExceptionResult, "because the exception should short-circuit the processing");

        }

        /// <summary>
        ///  Verifies functionality of the <see cref="Processor.OrderProcessor.ProcessOrderAsync" />
        ///  method.
        /// </summary>
        /// 
        [Fact]
        public async Task ProcessOrderAsyncReturnsTheExceptionResultIfThrownDuringOrderMessageCreation()
        {
            var mockClient         = new Mock<IEcommerceClient>();
            var mockStorage        = new Mock<IOrderStorage>();
            var mockSkuProcessor   = new Mock<ISkuMetadataProcessor>();
            var mockLogger         = new Mock<ILogger>();
            var fakeClock          = new FakeClock(Instant.FromUtc(2007, 06, 09, 11, 11, 15));
            var serializerSettings = new JsonSerializerSettings();
            var configuration      = new OrderProcessorConfiguration();
            var processor          = new Mock<Processor.OrderProcessor>(configuration, mockClient.Object, mockStorage.Object, mockSkuProcessor.Object, mockLogger.Object, fakeClock, serializerSettings) { CallBase = true };
            var orderDetails       = new OrderDetails { OrderId = "ABC123", UserId = "Bob", LineItems = new List<Ecom.LineItem>(), Recipients = new List<Ecom.Recipient>() };
            var successResult      = new OperationResult { Outcome = Outcome.Success, Reason = String.Empty, Recoverable = Recoverability.Final };
            
            serializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            serializerSettings.Converters.Add(new StringEnumConverter());

            successResult.Payload = JsonConvert.SerializeObject(orderDetails, serializerSettings);
            
            mockLogger
                .Setup(logger => logger.ForContext(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<bool>()))
                .Returns(mockLogger.Object);

            processor
                .Protected()
                .Setup<Task<OperationResult>>("RetrieveOrderDetailsAsync", ItExpr.IsAny<ILogger>(), ItExpr.IsAny<IEcommerceClient>(), ItExpr.IsAny<string>(), ItExpr.IsAny<string>(), ItExpr.IsAny<string>(), ItExpr.IsAny<OperationResult>())
                .Returns(Task.FromResult(successResult));

            processor
                .Protected()
                .Setup<Task<OperationResult>>("BuildCreateOrderMessageFromDetailsAsync", ItExpr.IsAny<OrderProcessorConfiguration>(), ItExpr.IsAny<ILogger>(), ItExpr.IsAny<ISkuMetadataProcessor>(), ItExpr.IsAny<string>(), ItExpr.IsAny<string>(), ItExpr.IsAny<IReadOnlyDictionary<string, string>>(), ItExpr.IsAny<OrderDetails>(), ItExpr.IsAny<JsonSerializerSettings>(), ItExpr.IsAny<string>(), ItExpr.IsAny<OperationResult>())
                .ThrowsAsync(new MissingDependencyException());

            var result = await processor.Object.ProcessOrderAsync("ABC", "123", new Dictionary<string, string> {{ "one", "one" }});
            
            result.Should().NotBeNull("becuase a result should have been returned");
            result.ShouldBeEquivalentTo(OperationResult.ExceptionResult, "because the exception should short-circuit the processing");

        }

        /// <summary>
        ///  Verifies functionality of the <see cref="Processor.OrderProcessor.ProcessOrderAsync" />
        ///  method.
        /// </summary>
        /// 
        [Fact]
        public async Task ProcessOrderAsyncReturnsTheExceptionResultIfThrownDuringStorageUse()
        {
            var mockClient         = new Mock<IEcommerceClient>();
            var mockStorage        = new Mock<IOrderStorage>();
            var mockSkuProcessor   = new Mock<ISkuMetadataProcessor>();
            var mockLogger         = new Mock<ILogger>();
            var fakeClock          = new FakeClock(Instant.FromUtc(2007, 06, 09, 11, 11, 15));
            var serializerSettings = new JsonSerializerSettings();
            var configuration      = new OrderProcessorConfiguration();
            var processor          = new Mock<Processor.OrderProcessor>(configuration, mockClient.Object, mockStorage.Object, mockSkuProcessor.Object, mockLogger.Object, fakeClock, serializerSettings) { CallBase = true };
            var orderDetails       = new OrderDetails { OrderId = "ABC123", UserId = "Bob", LineItems = new List<Ecom.LineItem>(), Recipients = new List<Ecom.Recipient>() };
            var detailsResult      = new OperationResult { Outcome = Outcome.Success, Reason = String.Empty, Recoverable = Recoverability.Final };
            var createOrderResult  = new OperationResult { Outcome = Outcome.Success, Reason = String.Empty, Recoverable = Recoverability.Final };
            
            serializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            serializerSettings.Converters.Add(new StringEnumConverter());

            detailsResult.Payload     = JsonConvert.SerializeObject(orderDetails, serializerSettings);
            createOrderResult.Payload = JsonConvert.SerializeObject(new CreateOrderMessage(), serializerSettings);
            
            mockLogger
                .Setup(logger => logger.ForContext(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<bool>()))
                .Returns(mockLogger.Object);

            processor
                .Protected()
                .Setup<Task<OperationResult>>("RetrieveOrderDetailsAsync", ItExpr.IsAny<ILogger>(), ItExpr.IsAny<IEcommerceClient>(), ItExpr.IsAny<string>(), ItExpr.IsAny<string>(), ItExpr.IsAny<string>(), ItExpr.IsAny<OperationResult>())
                .Returns(Task.FromResult(detailsResult));

            processor
                .Protected()
                .Setup<Task<OperationResult>>("BuildCreateOrderMessageFromDetailsAsync", ItExpr.IsAny<OrderProcessorConfiguration>(), ItExpr.IsAny<ILogger>(), ItExpr.IsAny<ISkuMetadataProcessor>(), ItExpr.IsAny<string>(), ItExpr.IsAny<string>(), ItExpr.IsAny<IReadOnlyDictionary<string, string>>(), ItExpr.IsAny<OrderDetails>(), ItExpr.IsAny<JsonSerializerSettings>(), ItExpr.IsAny<string>(), ItExpr.IsAny<OperationResult>())
                .Returns(Task.FromResult(createOrderResult));

            processor
                .Protected()
                .Setup<Task<OperationResult>>("StoreOrderForSubmissionAsync", ItExpr.IsAny<ILogger>(), ItExpr.IsAny<IOrderStorage>(), ItExpr.IsAny<string>(), ItExpr.IsAny<string>(), ItExpr.IsAny<CreateOrderMessage>(), ItExpr.IsAny<string>(), ItExpr.IsAny<OperationResult>())
                .ThrowsAsync(new MissingDependencyException());

            var result = await processor.Object.ProcessOrderAsync("ABC", "123", new Dictionary<string, string> {{ "one", "one" }});
            
            result.Should().NotBeNull("becuase a result should have been returned");
            result.ShouldBeEquivalentTo(OperationResult.ExceptionResult, "because the exception should short-circuit the processing");

        }

        /// <summary>
        ///  Verifies functionality of the <see cref="Processor.OrderProcessor.RetrieveOrderDetailsAsync" />
        ///  method.
        /// </summary>
        /// 
        [Fact]
        public async Task RetrieveOrderDetailsAsyncRequestsOrderDetails()
        {
            var mockClient         = new Mock<IEcommerceClient>();
            var mockStorage        = new Mock<IOrderStorage>();
            var mockSkuProcessor   = new Mock<ISkuMetadataProcessor>();
            var mockLogger         = new Mock<ILogger>();
            var fakeClock          = new FakeClock(Instant.FromUtc(2007, 06, 09, 11, 11, 15));
            var serializerSettings = new JsonSerializerSettings();
            var configuration      = new OrderProcessorConfiguration();
            var processor          = new Mock<Processor.OrderProcessor>(configuration, mockClient.Object, mockStorage.Object, mockSkuProcessor.Object, mockLogger.Object, fakeClock, serializerSettings) { CallBase = true };
            var orderId            = "123";
            var partner            = "ABC";
            var correlationId      = "correlate!";
            var orderDetails       = new OrderDetails { OrderId = orderId, UserId = "Bob", LineItems = new List<Ecom.LineItem>(), Recipients = new List<Ecom.Recipient>() };
            var orderDetailsResult = new OperationResult { Outcome = Outcome.Success, Reason = String.Empty, Recoverable = Recoverability.Final };
            var createOrderResult  = new OperationResult { Outcome = Outcome.Success, Reason = String.Empty, Recoverable = Recoverability.Final };
            var storageResult      = new OperationResult { Outcome = Outcome.Success, Reason = String.Empty, Recoverable = Recoverability.Final };
            var expectedResult     = new OperationResult { Outcome = Outcome.Success, Reason = String.Empty, Recoverable = Recoverability.Final };
            
            serializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            serializerSettings.Converters.Add(new StringEnumConverter());

            orderDetailsResult.Payload = JsonConvert.SerializeObject(orderDetails, serializerSettings);
            createOrderResult.Payload  = JsonConvert.SerializeObject(new CreateOrderMessage(), serializerSettings);

            mockLogger
                .Setup(logger => logger.ForContext(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<bool>()))
                .Returns(mockLogger.Object);

            mockClient
                .Setup(client => client.GetOrderDetailsAsync(It.Is<string>(order => order == orderId), It.Is<string>(correlation => correlation == correlationId)))
                .ReturnsAsync(orderDetailsResult)
                .Verifiable("The order details should have been requested");             

            processor
                .Protected()
                .Setup<Task<OperationResult>>("BuildCreateOrderMessageFromDetailsAsync", ItExpr.IsAny<OrderProcessorConfiguration>(), ItExpr.IsAny<ILogger>(), ItExpr.IsAny<ISkuMetadataProcessor>(), ItExpr.IsAny<string>(), ItExpr.IsAny<string>(), ItExpr.IsAny<IReadOnlyDictionary<string, string>>(), ItExpr.IsAny<OrderDetails>(), ItExpr.IsAny<JsonSerializerSettings>(), ItExpr.IsAny<string>(), ItExpr.IsAny<OperationResult>())
                .Returns(Task.FromResult(createOrderResult));

            processor
                .Protected()
                .Setup<Task<OperationResult>>("StoreOrderForSubmissionAsync", ItExpr.IsAny<ILogger>(), ItExpr.IsAny<IOrderStorage>(), ItExpr.IsAny<string>(), ItExpr.IsAny<string>(), ItExpr.IsAny<CreateOrderMessage>(), ItExpr.IsAny<string>(), ItExpr.IsAny<OperationResult>())
                .Returns(Task.FromResult(storageResult));

            var result = await processor.Object.ProcessOrderAsync(partner, orderId, new Dictionary<string, string> {{ "one", "one" }}, null, correlationId);
            result.ShouldBeEquivalentTo(expectedResult, "because the processing should have succeeded.");

            mockClient.VerifyAll();
        }

        /// <summary>
        ///  Verifies functionality of the <see cref="Processor.OrderProcessor.RetrieveOrderDetailsAsync" />
        ///  method.
        /// </summary>
        /// 
        [Fact]
        public async Task StoreOrderForSubmissionAsyncSavesTheOrderAsPending()
        {
            var mockClient         = new Mock<IEcommerceClient>();
            var mockStorage        = new Mock<IOrderStorage>();
            var mockSkuProcessor   = new Mock<ISkuMetadataProcessor>();
            var mockLogger         = new Mock<ILogger>();
            var fakeClock          = new FakeClock(Instant.FromUtc(2007, 06, 09, 11, 11, 15));
            var serializerSettings = new JsonSerializerSettings();
            var configuration      = new OrderProcessorConfiguration();
            var processor          = new Mock<Processor.OrderProcessor>(configuration, mockClient.Object, mockStorage.Object, mockSkuProcessor.Object, mockLogger.Object, fakeClock, serializerSettings) { CallBase = true };
            var orderId            = "123";
            var partner            = "ABC";
            var correlationId      = "correlate!";
            var orderDetails       = new OrderDetails { OrderId = orderId, UserId = "Bob", LineItems = new List<Ecom.LineItem>(), Recipients = new List<Ecom.Recipient>() };
            var createOrderMessage = new CreateOrderMessage();
            var storageKey         = "a key";
            var orderDetailsResult = new OperationResult { Outcome = Outcome.Success, Reason = String.Empty, Recoverable = Recoverability.Final };
            var createOrderResult  = new OperationResult { Outcome = Outcome.Success, Reason = String.Empty, Recoverable = Recoverability.Final };
            var storageResult      = new OperationResult { Outcome = Outcome.Success, Reason = String.Empty, Recoverable = Recoverability.Final };
            var expectedResult     = new OperationResult { Outcome = Outcome.Success, Reason = String.Empty, Recoverable = Recoverability.Final };
            
            serializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            serializerSettings.Converters.Add(new StringEnumConverter());

            orderDetailsResult.Payload = JsonConvert.SerializeObject(orderDetails, serializerSettings);
            createOrderResult.Payload  = JsonConvert.SerializeObject(createOrderMessage, serializerSettings);
            expectedResult.Payload     = storageKey;

            mockLogger
                .Setup(logger => logger.ForContext(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<bool>()))
                .Returns(mockLogger.Object);

            processor
                .Protected()
                .Setup<Task<OperationResult>>("RetrieveOrderDetailsAsync", ItExpr.IsAny<ILogger>(), ItExpr.IsAny<IEcommerceClient>(), ItExpr.IsAny<string>(), ItExpr.IsAny<string>(), ItExpr.IsAny<string>(), ItExpr.IsAny<OperationResult>())
                .Returns(Task.FromResult(orderDetailsResult));         

            processor
                .Protected()
                .Setup<Task<OperationResult>>("BuildCreateOrderMessageFromDetailsAsync", ItExpr.IsAny<OrderProcessorConfiguration>(), ItExpr.IsAny<ILogger>(), ItExpr.IsAny<ISkuMetadataProcessor>(), ItExpr.IsAny<string>(), ItExpr.IsAny<string>(), ItExpr.IsAny<IReadOnlyDictionary<string, string>>(), ItExpr.IsAny<OrderDetails>(), ItExpr.IsAny<JsonSerializerSettings>(), ItExpr.IsAny<string>(), ItExpr.IsAny<OperationResult>())
                .Returns(Task.FromResult(createOrderResult));

            mockStorage
                .Setup(storage => storage.SavePendingOrderAsync(partner, orderId, It.IsAny<CreateOrderMessage>()))
                .ReturnsAsync(storageKey)
                .Verifiable("The order should have been saved");

            var result = await processor.Object.ProcessOrderAsync(partner, orderId, new Dictionary<string, string> {{ "one", "one" }}, null, correlationId);
            result.ShouldBeEquivalentTo(expectedResult, "because the processing should have succeeded.");

            mockStorage.Verify();
        }

        /// <summary>
        ///  Verifies functionality of the <see cref="Processor.OrderProcessor.RetrieveOrderDetailsAsync" />
        ///  method.
        /// </summary>
        /// 
        [Fact]
        public async Task ProcessOrderAsyncUsesTheStorageKeyAsTheResultPayload()
        {
            var mockClient         = new Mock<IEcommerceClient>();
            var mockStorage        = new Mock<IOrderStorage>();
            var mockSkuProcessor   = new Mock<ISkuMetadataProcessor>();
            var mockLogger         = new Mock<ILogger>();
            var fakeClock          = new FakeClock(Instant.FromUtc(2007, 06, 09, 11, 11, 15));
            var serializerSettings = new JsonSerializerSettings();
            var configuration      = new OrderProcessorConfiguration();
            var processor          = new Mock<Processor.OrderProcessor>(configuration, mockClient.Object, mockStorage.Object, mockSkuProcessor.Object, mockLogger.Object, fakeClock, serializerSettings) { CallBase = true };
            var orderId            = "123";
            var partner            = "ABC";
            var correlationId      = "correlate!";
            var orderDetails       = new OrderDetails { OrderId = orderId, UserId = "Bob", LineItems = new List<Ecom.LineItem>(), Recipients = new List<Ecom.Recipient>() };
            var createOrderMessage = new CreateOrderMessage();
            var storageKey         = "a key";            
            var orderDetailsResult = new OperationResult { Outcome = Outcome.Success, Reason = String.Empty, Recoverable = Recoverability.Final };
            var createOrderResult  = new OperationResult { Outcome = Outcome.Success, Reason = String.Empty, Recoverable = Recoverability.Final };
            var storageResult      = new OperationResult { Outcome = Outcome.Success, Reason = String.Empty, Recoverable = Recoverability.Final };
            var expectedResult     = new OperationResult { Outcome = Outcome.Success, Reason = String.Empty, Recoverable = Recoverability.Final };
            
            serializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            serializerSettings.Converters.Add(new StringEnumConverter());

            orderDetailsResult.Payload = JsonConvert.SerializeObject(orderDetails, serializerSettings);
            createOrderResult.Payload  = JsonConvert.SerializeObject(createOrderMessage, serializerSettings);
            expectedResult.Payload     = storageKey;

            mockLogger
                .Setup(logger => logger.ForContext(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<bool>()))
                .Returns(mockLogger.Object);

            processor
                .Protected()
                .Setup<Task<OperationResult>>("RetrieveOrderDetailsAsync", ItExpr.IsAny<ILogger>(), ItExpr.IsAny<IEcommerceClient>(), ItExpr.IsAny<string>(), ItExpr.IsAny<string>(), ItExpr.IsAny<string>(), ItExpr.IsAny<OperationResult>())
                .Returns(Task.FromResult(orderDetailsResult));         

            processor
                .Protected()
                .Setup<Task<OperationResult>>("BuildCreateOrderMessageFromDetailsAsync", ItExpr.IsAny<OrderProcessorConfiguration>(), ItExpr.IsAny<ILogger>(), ItExpr.IsAny<ISkuMetadataProcessor>(), ItExpr.IsAny<string>(), ItExpr.IsAny<string>(), ItExpr.IsAny<IReadOnlyDictionary<string, string>>(), ItExpr.IsAny<OrderDetails>(), ItExpr.IsAny<JsonSerializerSettings>(), ItExpr.IsAny<string>(), ItExpr.IsAny<OperationResult>())
                .Returns(Task.FromResult(createOrderResult));

            processor
                .Protected()
                .Setup<Task<OperationResult>>("StoreOrderForSubmissionAsync", ItExpr.IsAny<ILogger>(), ItExpr.IsAny<IOrderStorage>(), partner, orderId, ItExpr.IsAny<CreateOrderMessage>(), ItExpr.IsAny<string>(), ItExpr.IsAny<OperationResult>())
                .Returns(Task.FromResult(expectedResult));

            var result = await processor.Object.ProcessOrderAsync(partner, orderId, new Dictionary<string, string> {{ "one", "one" }}, null, correlationId);
            result.ShouldBeEquivalentTo(expectedResult, "because the processing should have succeeded.");
        }

        /// <summary>
        ///   Verifies functionality of the <see cref="Processor.OrderProcessor.BuildCreateOrderMessageFromDetailsAsync" />
        ///   method.
        /// </summary>
        ///
        [Fact]
        public void BuildCreateOrderMessageFromDetailsAsyncSetsTheLineItems()
        {
            var mockClient          = new Mock<IEcommerceClient>();
            var mockStorage         = new Mock<IOrderStorage>();
            var mockSkuProcessor    = new Mock<ISkuMetadataProcessor>();
            var mockLogger          = new Mock<ILogger>();
            var fakeClock           = new FakeClock(Instant.FromUtc(2007, 06, 09, 11, 11, 15));
            var serializerSettings  = new JsonSerializerSettings();
            var configuration       = new OrderProcessorConfiguration { ServiceLevelAgreementCode = "Blue" };
            var processor           = new Processor.OrderProcessor(configuration, mockClient.Object, mockStorage.Object, mockSkuProcessor.Object, mockLogger.Object, fakeClock, serializerSettings);
            var details             = this.GenerateOrderDetails();            
            var orderAssets         = details.LineItems.ToDictionary(item => item.LineItemId, item => item.LineItemId);
            var itemCounts          = new Dictionary<string, int>();
            var partner             = "SQUIRE";
            var transactionId       = "123987";

            serializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            serializerSettings.Converters.Add(new StringEnumConverter());

            foreach (var lineItem in details.LineItems)
            {
                itemCounts.Add(lineItem.LineItemId, details.Recipients.SelectMany(recipient => recipient.OrderedItems).Where(orderedItem => orderedItem.LineItemId == lineItem.LineItemId).Sum(orderedItem => (int)orderedItem.Quantity));
            }

            mockLogger
                .Setup(logger => logger.ForContext(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<bool>()))
                .Returns(mockLogger.Object);

            mockSkuProcessor
                .Setup(skuProc => skuProc.RenderOrderTemplateAsync(It.IsAny<OrderTemplateMetadata>()))
                .ReturnsAsync<OrderTemplateMetadata, ISkuMetadataProcessor, string>(metadata => JsonConvert.SerializeObject(metadata));

            var task = this.InvokeBuildCreateOrderMessageFromDetailsAsync(processor, configuration, mockLogger.Object, mockSkuProcessor.Object, partner, transactionId, orderAssets, details, serializerSettings);

            task.Should().NotBeNull("becaues a task should have been returned");
            
            var result = task.GetAwaiter().GetResult();
            result.Should().NotBeNull("because a result should have been returned");
            result.Payload.Should().NotBeNullOrWhiteSpace("because the result should have had a payload");

            var message = JsonConvert.DeserializeObject<CreateOrderMessage>(task.Result.Payload);
            message.Should().NotBeNull("because the payload should be a create order message");
            message.LineItems.Should().HaveCount(details.LineItems.Count, "because the line items should have been translated");

            foreach (var detailsItem in details.LineItems)
            {
                var messageItem = message.LineItems.SingleOrDefault(item => item.LineItemId == detailsItem.LineItemId);
                messageItem.Should().NotBeNull("because item {0} should have been found", detailsItem.LineItemId);
                messageItem.Item.Should().NotBeNullOrWhiteSpace("because the detail for item {0} should have been populated", detailsItem.LineItemId);
                messageItem.ServiceLevelAgreement.Should().Be(configuration.ServiceLevelAgreementCode, "because the service level should have been used for item {0}", detailsItem.LineItemId);

                var metadata = JsonConvert.DeserializeObject<OrderTemplateMetadata>(messageItem.Item);
                metadata.Should().NotBeNull("because the item should contain the detail metadata for item {0}", detailsItem.LineItemId);
                metadata.Sku.Should().Be(detailsItem.ProductCode, "because the product code should be used as the SKU for item {0}", detailsItem.LineItemId);
                metadata.AdditionalSheets.Should().Be(detailsItem.AdditionalSheetCount, "because the additional pages should carry over for item {0}", detailsItem.LineItemId);
                metadata.AssetUrl.Should().Be(orderAssets[detailsItem.LineItemId], "because the correct asset should have been used for item {0}", details.LineItems);
                metadata.LineItemCount.Should().Be(itemCounts[detailsItem.LineItemId], "because the correct ordered quantity across all recipients should be represented for item {0}", details.LineItems);
                metadata.NumberOfRecipients.Should().Be(details.Recipients.Count(recipient => recipient.OrderedItems.Any(orderedItem => orderedItem.LineItemId == detailsItem.LineItemId)), "becase the count of recipoients should include all that ordred item {0}", detailsItem.LineItemId);
            }
        }
        
        /// <summary>
        ///   Invokes the <see cref="Processor.OrderProcessor.BuildCreateOrderMessageFromDetailsAsync" />
        ///   method on the given Order Processor.
        /// </summary>
        /// 
        /// <param name="processor">The instance of the order processor to invoke the methon on.</param>
        /// <param name="configuration">The configuration to use for building the message.</param>
        /// <param name="logger">The logger to use for emitting telemetry.</param>
        /// <param name="skuProcessor">The instance of the sku processor to use for obtaining the line item detail.</param>
        /// <param name="partner">The partner code for the order being processed.</param>
        /// <param name="transactionId">The transaction identifier to use for the submission request.</param>
        /// <param name="orderAssets">The set of assets associated with the order.</param>
        /// <param name="details">The details of the order, from the eCommerce system.</param>
        /// <param name="serializerSettings">The settings to use for JSON serialization.</param>
        /// <param name="correlationId">The correlation identifier for tying together request components.</param>
        /// <param name="emulatedResult">The emulated result, if one should be used.</param>
        /// 
        /// <returns>The result of the operation.</returns>
        /// 
        private Task<OperationResult> InvokeBuildCreateOrderMessageFromDetailsAsync(Processor.OrderProcessor    processor,
                                                                                    OrderProcessorConfiguration configuration,
                                                                                    ILogger                     logger,
                                                                                    ISkuMetadataProcessor       skuProcessor,
                                                                                    string                      partner,
                                                                                    string                      transactionId,
                                                                                    Dictionary<string, string>  orderAssets,
                                                                                    OrderDetails                details,
                                                                                    JsonSerializerSettings      serializerSettings,
                                                                                    string                      correlationId      = null,
                                                                                    OperationResult             emulatedResult     = null)
        {
            var createMessageMethod = processor.GetType().GetMethod("BuildCreateOrderMessageFromDetailsAsync", BindingFlags.Instance | BindingFlags.NonPublic);
            var result              = createMessageMethod.Invoke(processor, new object[] { configuration, logger, skuProcessor, partner, transactionId, orderAssets, details, serializerSettings, correlationId, emulatedResult });
            
            if (result is Task<OperationResult> task)
            {
                return task;
            }
             
            throw new InvalidOperationException();
        }


        /// <summary>
        ///   Generates a mostly-populated OrderDetails instance for testing.
        /// </summary>
        /// 
        /// <returns>The generated order details.</returns>
        /// 
        private OrderDetails GenerateOrderDetails()
        {
            return new OrderDetails
            {
                OrderId = "ABC123",
                Recipients = new List<Ecom.Recipient>
                {
                   new Ecom.Recipient
                   {
                      Id           = "1",
                      LanguageCode = "en-us",
                      Shipping     = new Ecom.RecipientShippingInformation
                      {
                          Address = new Ecom.Address
                          {
                              FirstName       = "Alex",
                              LastName        = "Summers",
                              CareOf          = "Lorna Dane",
                              Line1           = "1407 Graymalken Lane",
                              City            = "Salem Center",
                              StateOrProvince = "NY",
                              PostalCode      = "10560",
                              CountryCode     = "USA",
                              Email           = "havok@schoolforthegifted.com",
                              Phone           = "212-479-7990",
                              Region          = Ecom.Region.Americas
                          },

                          DeliveryExpectation = Ecom.DeliveryExpectation.OnOrBeforeDate,
                          RatingAccountCode   = "A1"
                       },

                       OrderedItems = new List<Ecom.OrderItemDetails>
                       {
                          new Ecom.OrderItemDetails { LineItemId = "1", Quantity = 2 },
                          new Ecom.OrderItemDetails { LineItemId = "3", Quantity = 4 }
                       }
                   },
                   new Ecom.Recipient
                   {
                      Id           = "2",
                      LanguageCode = "en-us",
                      Shipping     = new Ecom.RecipientShippingInformation
                      {
                          Address = new Ecom.Address
                          {
                              FirstName       = "Scott",
                              LastName        = "Summers",
                              Line1           = "1407 Graymalken Lane",
                              City            = "Salem Center",
                              StateOrProvince = "NY",
                              PostalCode      = "10560",
                              CountryCode     = "USA",
                              Email           = "cyclops@schoolforthegifted.com",
                              Phone           = "212-479-7990",
                              Region          = Ecom.Region.Americas
                          },

                          DeliveryExpectation = Ecom.DeliveryExpectation.OnOrBeforeDate,
                          RatingAccountCode   = "A2"
                       },

                       OrderedItems = new List<Ecom.OrderItemDetails>
                       {
                          new Ecom.OrderItemDetails { LineItemId = "1", Quantity = 8 }
                       }
                    }
                },

                LineItems = new List<Ecom.LineItem>
                {
                   new Ecom.LineItem
                   {
                     LineItemId            = "1",
                     AdditionalSheetCount  = 2,
                     TotalSheetCount       = 10,
                     CountInSet            = 27,
                     ProductCode           = "OMGNO",
                     Description           = "Some thing",
                     DeclaredValue         = new Ecom.PriceInformation { Amount = 5, CurrencyCode = "USD" },
                     UnitPrice             = new Ecom.PriceInformation { Amount = 10, CurrencyCode = "GBP" },
                     ResourceId            = "Hello",
                     ServiceLevelAgreement = "Some agreement"
                   },

                   new Ecom.LineItem
                   {
                     LineItemId           = "3",
                     AdditionalSheetCount = 4,
                     TotalSheetCount      = 9,
                     CountInSet           = 12,
                     ProductCode          = "YAS!",
                     Description          = "Other thing"
                   }
                }
            };
        }
    }
}
