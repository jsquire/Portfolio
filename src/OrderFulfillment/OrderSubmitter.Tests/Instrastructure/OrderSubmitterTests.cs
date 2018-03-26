using System;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Moq.Protected;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using OrderFulfillment.Core.Exceptions;
using OrderFulfillment.Core.External;
using OrderFulfillment.Core.Models.External.OrderProduction;
using OrderFulfillment.Core.Models.Operations;
using OrderFulfillment.Core.Storage;
using OrderFulfillment.OrderProcessor.Configuration;
using Serilog;
using Xunit;
using UnderTest = OrderFulfillment.OrderSubmitter.Infrastructure;

namespace OrderFulfillment.OrderSubmitter.OrderSubmitter.Tests.Instrastructure
{
    /// <summary>
    ///   The suite of tests for the <see cref="OrderSubmitter.OrderSubmitter" />
    /// </summary>
    /// 
    public class OrderSubmitterTests
    {
        /// <summary>
        ///   Verifies behavior of the constructor.
        /// </summary>
        /// 
        [Fact]
        public void ConstructorValidatesTheConfiguration()
        {
            Action actionUnderTest = () => new UnderTest.OrderSubmitter(null, Mock.Of<IOrdeProductionClient>(), Mock.Of<IOrderStorage>(), Mock.Of<ILogger>(), new JsonSerializerSettings());
            actionUnderTest.ShouldThrow<ArgumentNullException>("because theconfiguration is required");
        }

        /// <summary>
        ///   Verifies behavior of the constructor.
        /// </summary>
        /// 
        [Fact]
        public void ConstructorValidatesTheorderProductionClient()
        {
            Action actionUnderTest = () => new UnderTest.OrderSubmitter(new OrderSubmitterConfiguration(), null, Mock.Of<IOrderStorage>(), Mock.Of<ILogger>(), new JsonSerializerSettings());
            actionUnderTest.ShouldThrow<ArgumentNullException>("because the order production client is required");
        }

        /// <summary>
        ///   Verifies behavior of the constructor.
        /// </summary>
        /// 
        [Fact]
        public void ConstructorValidatesTheOrderStorage()
        {
            Action actionUnderTest = () => new UnderTest.OrderSubmitter(new OrderSubmitterConfiguration(), Mock.Of<IOrdeProductionClient>(), null, Mock.Of<ILogger>(), new JsonSerializerSettings());
            actionUnderTest.ShouldThrow<ArgumentNullException>("because the storage is required");
        }

        /// <summary>
        ///   Verifies behavior of the constructor.
        /// </summary>
        /// 
        [Fact]
        public void ConstructorValidatesTheLogger()
        {
            Action actionUnderTest = () => new UnderTest.OrderSubmitter(new OrderSubmitterConfiguration(), Mock.Of<IOrdeProductionClient>(), Mock.Of<IOrderStorage>(), null, new JsonSerializerSettings());
            actionUnderTest.ShouldThrow<ArgumentNullException>("because the logger is required");
        }

        /// <summary>
        ///   Verifies behavior of the constructor.
        /// </summary>
        /// 
        [Fact]
        public void ConstructorValidatesTheSerializerSettings()
        {
            Action actionUnderTest = () => new UnderTest.OrderSubmitter(new OrderSubmitterConfiguration(), Mock.Of<IOrdeProductionClient>(), Mock.Of<IOrderStorage>(), Mock.Of<ILogger>(), null);
            actionUnderTest.ShouldThrow<ArgumentNullException>("because the serializer settings are required");
        }

        /// <summary>
        ///  Verifies functionality of the <see cref="UnderTest.OrderSubmitter.SubmitOrderForProductionAsync" />
        ///  method.
        /// </summary>
        /// 
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void SubmitOrderAsyncValidatesThePartner(string partner)
        {
            var processor =  new UnderTest.OrderSubmitter(new OrderSubmitterConfiguration(), Mock.Of<IOrdeProductionClient>(), Mock.Of<IOrderStorage>(), Mock.Of<ILogger>(), new JsonSerializerSettings());
            
            Action actionUnderTest = () => processor.SubmitOrderForProductionAsync(partner, "ABC", new DependencyEmulation(), "blue").GetAwaiter().GetResult();
            actionUnderTest.ShouldThrow<ArgumentException>("because the partner is required").And.ParamName.Should().Be(nameof(partner), "because the partner was invalid");
        }

        /// <summary>
        ///  Verifies functionality of the <see cref="UnderTest.OrderSubmitter.SubmitOrderForProductionAsync" />
        ///  method.
        /// </summary>
        /// 
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void SubmitOrderAsyncValidatesTheOrder(string orderId)
        {
            var processor =  new UnderTest.OrderSubmitter(new OrderSubmitterConfiguration(), Mock.Of<IOrdeProductionClient>(), Mock.Of<IOrderStorage>(), Mock.Of<ILogger>(), new JsonSerializerSettings());
            
            Action actionUnderTest = () => processor.SubmitOrderForProductionAsync("Bob", orderId, new DependencyEmulation(), "blue").GetAwaiter().GetResult();
            actionUnderTest.ShouldThrow<ArgumentException>("because the orderId is required").And.ParamName.Should().Be(nameof(orderId), "because the orderId was invalid");
        }

        /// <summary>
        ///  Verifies functionality of the <see cref="UnderTest.OrderSubmitter.SubmitOrderForProductionAsync" />
        ///  method.
        /// </summary>
        /// 
        [Fact]
        public async Task SubmitOrderAsyncReturnsAFailedResultIfPendingOrderRetrievalFailed()
        {
            var mockClient         = new Mock<IOrdeProductionClient>();
            var mockStorage        = new Mock<IOrderStorage>();
            var mockLogger         = new Mock<ILogger>();
            var serializerSettings = new JsonSerializerSettings();
            var processor          = new Mock<UnderTest.OrderSubmitter>(new OrderSubmitterConfiguration(), mockClient.Object, mockStorage.Object, mockLogger.Object, serializerSettings) { CallBase = true };
            var pendingResult      = new OperationResult<CreateOrderMessage> { Outcome = Outcome.Failure, Reason = "because", Recoverable = Recoverability.Retriable };
            var expectedResult     = new OperationResult { Outcome = Outcome.Failure, Reason = "because", Payload = String.Empty, Recoverable = Recoverability.Retriable };
            
            serializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            serializerSettings.Converters.Add(new StringEnumConverter());

            mockLogger
                .Setup(logger => logger.ForContext(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<bool>()))
                .Returns(mockLogger.Object);

            processor
                .Protected()
                .Setup<Task<OperationResult<CreateOrderMessage>>>("RetrievePendingOrderAsync", ItExpr.IsAny<ILogger>(), ItExpr.IsAny<IOrderStorage>(), ItExpr.IsAny<JsonSerializerSettings>(), ItExpr.IsAny<string>(), ItExpr.IsAny<string>(),  ItExpr.IsAny<string>(), ItExpr.IsAny<OperationResult>())
                .Returns(Task.FromResult(pendingResult));

            var result = await processor.Object.SubmitOrderForProductionAsync("ABC", "123");
            
            result.Should().NotBeNull("becuase a result should have been returned");
            result.ShouldBeEquivalentTo(expectedResult, "because the failed result should short-circuit the processing");

        }

        /// <summary>
        ///  Verifies functionality of the <see cref="UnderTest.OrderSubmitter.SubmitOrderForProductionAsync" />
        ///  method.
        /// </summary>
        /// 
        [Fact]
        public async Task SubmitOrderAsyncReturnsAFailedResultIfSubmissionFailed()
        {
            var mockClient         = new Mock<IOrdeProductionClient>();
            var mockStorage        = new Mock<IOrderStorage>();
            var mockLogger         = new Mock<ILogger>();
            var serializerSettings = new JsonSerializerSettings();
            var order              = new CreateOrderMessage { Identity = new OrderIdentity { PartnerCode = "123", PartnerOrderId = "ABC" }};
            var processor          = new Mock<UnderTest.OrderSubmitter>(new OrderSubmitterConfiguration(), mockClient.Object, mockStorage.Object, mockLogger.Object, serializerSettings) { CallBase = true };
            var pendingResult      = new OperationResult<CreateOrderMessage> { Outcome = Outcome.Success, Reason = "because", Payload = order, Recoverable = Recoverability.Final };
            var expectedResult     = new OperationResult { Outcome = Outcome.Failure, Reason = "because", Payload = "payload!", Recoverable = Recoverability.Retriable };
            
            serializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            serializerSettings.Converters.Add(new StringEnumConverter());

            mockLogger
                .Setup(logger => logger.ForContext(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<bool>()))
                .Returns(mockLogger.Object);

            processor
                .Protected()
                .Setup<Task<OperationResult<CreateOrderMessage>>>("RetrievePendingOrderAsync", ItExpr.IsAny<ILogger>(), ItExpr.IsAny<IOrderStorage>(), ItExpr.IsAny<JsonSerializerSettings>(), ItExpr.IsAny<string>(), ItExpr.IsAny<string>(),  ItExpr.IsAny<string>(), ItExpr.IsAny<OperationResult>())
                .Returns(Task.FromResult(pendingResult));

            processor
                .Protected()
                .Setup<Task<OperationResult>>("SendOrderToProductionAsync", ItExpr.IsAny<ILogger>(), ItExpr.IsAny<IOrdeProductionClient>(), ItExpr.IsAny<CreateOrderMessage>(), ItExpr.IsAny<string>(), ItExpr.IsAny<OperationResult>())
                .Returns(Task.FromResult(expectedResult));

            var result = await processor.Object.SubmitOrderForProductionAsync(order.Identity.PartnerOrderId, order.Identity.PartnerOrderId);
            
            result.Should().NotBeNull("becuase a result should have been returned");
            result.ShouldBeEquivalentTo(expectedResult, "because the failed result should short-circuit the processing");

        }

        /// <summary>
        ///  Verifies functionality of the <see cref="UnderTest.OrderSubmitter.SubmitOrderForProductionAsync" />
        ///  method.
        /// </summary>
        /// 
        [Fact]
        public async Task SubmitOrderAsyncReturnsAFailedResultIfSavingTheCompletedOrderFailed()
        {
            var mockClient         = new Mock<IOrdeProductionClient>();
            var mockStorage        = new Mock<IOrderStorage>();
            var mockLogger         = new Mock<ILogger>();
            var serializerSettings = new JsonSerializerSettings();
            var order              = new CreateOrderMessage { Identity = new OrderIdentity { PartnerCode = "123", PartnerOrderId = "ABC" }};
            var processor          = new Mock<UnderTest.OrderSubmitter>(new OrderSubmitterConfiguration(), mockClient.Object, mockStorage.Object, mockLogger.Object, serializerSettings) { CallBase = true };
            var pendingResult      = new OperationResult<CreateOrderMessage> { Outcome = Outcome.Success, Reason = "because", Payload = order, Recoverable = Recoverability.Final };
            var successResult      = new OperationResult { Outcome = Outcome.Success, Reason = String.Empty, Payload = String.Empty, Recoverable = Recoverability.Final };
            var failedResult       = new OperationResult { Outcome = Outcome.Failure, Reason = "because", Payload = "payload!", Recoverable = Recoverability.Retriable };
            
            serializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            serializerSettings.Converters.Add(new StringEnumConverter());

            mockLogger
                .Setup(logger => logger.ForContext(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<bool>()))
                .Returns(mockLogger.Object);

            processor
                .Protected()
                .Setup<Task<OperationResult<CreateOrderMessage>>>("RetrievePendingOrderAsync", ItExpr.IsAny<ILogger>(), ItExpr.IsAny<IOrderStorage>(), ItExpr.IsAny<JsonSerializerSettings>(), ItExpr.IsAny<string>(), ItExpr.IsAny<string>(),  ItExpr.IsAny<string>(), ItExpr.IsAny<OperationResult>())
                .Returns(Task.FromResult(pendingResult));

            processor
                .Protected()
                .Setup<Task<OperationResult>>("SendOrderToProductionAsync", ItExpr.IsAny<ILogger>(), ItExpr.IsAny<IOrdeProductionClient>(), ItExpr.IsAny<CreateOrderMessage>(), ItExpr.IsAny<string>(), ItExpr.IsAny<OperationResult>())
                .Returns(Task.FromResult(successResult));

            processor
                .Protected()
                .Setup<Task<OperationResult>>("StoreOrderAsCompletedAsync", ItExpr.IsAny<ILogger>(), ItExpr.IsAny<IOrderStorage>(), ItExpr.IsAny<CreateOrderMessage>(), ItExpr.IsAny<string>(), ItExpr.IsAny<OperationResult>())
                .Returns(Task.FromResult(failedResult));

            var result = await processor.Object.SubmitOrderForProductionAsync(order.Identity.PartnerCode, order.Identity.PartnerOrderId);
            
            result.Should().NotBeNull("becuase a result should have been returned");
            result.ShouldBeEquivalentTo(failedResult, "because the failed result should cause processing to short-circuit");
        }

        /// <summary>
        ///  Verifies functionality of the <see cref="UnderTest.OrderSubmitter.SubmitOrderForProductionAsync" />
        ///  method.
        /// </summary>
        /// 
        [Fact]
        public async Task SubmitOrderAsyncReturnsASuccessResultButLogsTheWarningIfPendingOrderDeletionFailed()
        {
            var mockClient         = new Mock<IOrdeProductionClient>();
            var mockStorage        = new Mock<IOrderStorage>();
            var mockLogger         = new Mock<ILogger>();
            var serializerSettings = new JsonSerializerSettings();
            var order              = new CreateOrderMessage { Identity = new OrderIdentity { PartnerCode = "123", PartnerOrderId = "ABC" }};
            var processor          = new Mock<UnderTest.OrderSubmitter>(new OrderSubmitterConfiguration(), mockClient.Object, mockStorage.Object, mockLogger.Object, serializerSettings) { CallBase = true };
            var pendingResult      = new OperationResult<CreateOrderMessage> { Outcome = Outcome.Success, Reason = "because", Payload = order, Recoverable = Recoverability.Final };
            var successResult      = new OperationResult { Outcome = Outcome.Success, Reason = String.Empty, Payload = String.Empty, Recoverable = Recoverability.Final };
            var failedResult       = new OperationResult { Outcome = Outcome.Failure, Reason = "because", Payload = "payload!", Recoverable = Recoverability.Retriable };
            
            serializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            serializerSettings.Converters.Add(new StringEnumConverter());

            mockLogger
                .Setup(logger => logger.ForContext(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<bool>()))
                .Returns(mockLogger.Object);

            processor
                .Protected()
                .Setup<Task<OperationResult<CreateOrderMessage>>>("RetrievePendingOrderAsync", ItExpr.IsAny<ILogger>(), ItExpr.IsAny<IOrderStorage>(), ItExpr.IsAny<JsonSerializerSettings>(), ItExpr.IsAny<string>(), ItExpr.IsAny<string>(),  ItExpr.IsAny<string>(), ItExpr.IsAny<OperationResult>())
                .Returns(Task.FromResult(pendingResult));

            processor
                .Protected()
                .Setup<Task<OperationResult>>("SendOrderToProductionAsync", ItExpr.IsAny<ILogger>(), ItExpr.IsAny<IOrdeProductionClient>(), ItExpr.IsAny<CreateOrderMessage>(), ItExpr.IsAny<string>(), ItExpr.IsAny<OperationResult>())
                .Returns(Task.FromResult(successResult));

            processor
                .Protected()
                .Setup<Task<OperationResult>>("StoreOrderAsCompletedAsync", ItExpr.IsAny<ILogger>(), ItExpr.IsAny<IOrderStorage>(), ItExpr.IsAny<CreateOrderMessage>(), ItExpr.IsAny<string>(), ItExpr.IsAny<OperationResult>())
                .Returns(Task.FromResult(successResult));

            processor
                .Protected()
                .Setup<Task<OperationResult>>("DeletePendingOrderAsync", ItExpr.IsAny<ILogger>(), ItExpr.IsAny<IOrderStorage>(), ItExpr.IsAny<string>(), ItExpr.IsAny<string>(), ItExpr.IsAny<string>(), ItExpr.IsAny<OperationResult>())
                .Returns(Task.FromResult(failedResult));

            var result = await processor.Object.SubmitOrderForProductionAsync(order.Identity.PartnerCode, order.Identity.PartnerOrderId);
            
            result.Should().NotBeNull("becuase a result should have been returned");
            result.ShouldBeEquivalentTo(successResult, "because the failed delete should not cause processing to fail");

            mockLogger.Verify(logger => logger.Warning(It.IsAny<string>(), 
                                                       It.Is<string>(value => value == order.Identity.PartnerCode), 
                                                       It.Is<string>(value => value == order.Identity.PartnerOrderId)),
                Times.Once, "A warning should have been logged for the failed pending order deletion");
        }

        /// <summary>
        ///  Verifies functionality of the <see cref="UnderTest.OrderSubmitter.SubmitOrderForProductionAsync" />
        ///  method.
        /// </summary>
        /// 
        [Fact]
        public async Task SubmitOrderAsyncHonorsTheRetryPolicyIfPendingOrderRetrievalFailed()
        {
            var mockClient         = new Mock<IOrdeProductionClient>();
            var mockStorage        = new Mock<IOrderStorage>();
            var mockLogger         = new Mock<ILogger>();
            var serializerSettings = new JsonSerializerSettings();
            var config             = new OrderSubmitterConfiguration { OperationRetryMaxCount = 3, OperationRetryExponentialSeconds = 0, OperationRetryJitterSeconds = 0 };
            var processor          = new Mock<UnderTest.OrderSubmitter>(config, mockClient.Object, mockStorage.Object, mockLogger.Object, serializerSettings) { CallBase = true };
            var pendingResult      = new OperationResult<CreateOrderMessage> { Outcome = Outcome.Failure, Reason = "because", Recoverable = Recoverability.Retriable };
            var expectedResult     = new OperationResult { Outcome = Outcome.Failure, Reason = "because", Payload = String.Empty, Recoverable = Recoverability.Retriable };
            var retries            = 0;
            
            serializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            serializerSettings.Converters.Add(new StringEnumConverter());

            mockLogger
                .Setup(logger => logger.ForContext(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<bool>()))
                .Returns(mockLogger.Object);

            processor
                .Protected()
                .Setup<Task<OperationResult<CreateOrderMessage>>>("RetrievePendingOrderAsync", ItExpr.IsAny<ILogger>(), ItExpr.IsAny<IOrderStorage>(), ItExpr.IsAny<JsonSerializerSettings>(), ItExpr.IsAny<string>(), ItExpr.IsAny<string>(),  ItExpr.IsAny<string>(), ItExpr.IsAny<OperationResult>())
                .Returns(Task.FromResult(pendingResult))
                .Callback( () => ++retries);

            var result = await processor.Object.SubmitOrderForProductionAsync("ABC", "123");
            
            retries.Should().Be(config.OperationRetryMaxCount + 1, "because the failure should be retried");
        }

        /// <summary>
        ///  Verifies functionality of the <see cref="UnderTest.OrderSubmitter.SubmitOrderForProductionAsync" />
        ///  method.
        /// </summary>
        /// 
        [Fact]
        public async Task SubmitOrderAsyncHonorsTheRetryPolicyIfSubmissionFailed()
        {
            var mockClient         = new Mock<IOrdeProductionClient>();
            var mockStorage        = new Mock<IOrderStorage>();
            var mockLogger         = new Mock<ILogger>();
            var serializerSettings = new JsonSerializerSettings();
            var config             = new OrderSubmitterConfiguration { OperationRetryMaxCount = 3, OperationRetryExponentialSeconds = 0, OperationRetryJitterSeconds = 0 };
            var order              = new CreateOrderMessage { Identity = new OrderIdentity { PartnerCode = "123", PartnerOrderId = "ABC" }};
            var processor          = new Mock<UnderTest.OrderSubmitter>(config, mockClient.Object, mockStorage.Object, mockLogger.Object, serializerSettings) { CallBase = true };
            var pendingResult      = new OperationResult<CreateOrderMessage> { Outcome = Outcome.Success, Reason = "because", Payload = order, Recoverable = Recoverability.Final };
            var expectedResult     = new OperationResult { Outcome = Outcome.Failure, Reason = "because", Payload = "payload!", Recoverable = Recoverability.Retriable };
            var retries            = 0;
            
            serializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            serializerSettings.Converters.Add(new StringEnumConverter());

            mockLogger
                .Setup(logger => logger.ForContext(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<bool>()))
                .Returns(mockLogger.Object);

            processor
                .Protected()
                .Setup<Task<OperationResult<CreateOrderMessage>>>("RetrievePendingOrderAsync", ItExpr.IsAny<ILogger>(), ItExpr.IsAny<IOrderStorage>(), ItExpr.IsAny<JsonSerializerSettings>(), ItExpr.IsAny<string>(), ItExpr.IsAny<string>(),  ItExpr.IsAny<string>(), ItExpr.IsAny<OperationResult>())
                .Returns(Task.FromResult(pendingResult));

            processor
                .Protected()
                .Setup<Task<OperationResult>>("SendOrderToProductionAsync", ItExpr.IsAny<ILogger>(), ItExpr.IsAny<IOrdeProductionClient>(), ItExpr.IsAny<CreateOrderMessage>(), ItExpr.IsAny<string>(), ItExpr.IsAny<OperationResult>())
                .Returns(Task.FromResult(expectedResult))
                .Callback( () => ++retries);

            var result = await processor.Object.SubmitOrderForProductionAsync(order.Identity.PartnerOrderId, order.Identity.PartnerOrderId);
            
            retries.Should().Be(config.OperationRetryMaxCount + 1, "because the failure should be retried");
        }

        /// <summary>
        ///  Verifies functionality of the <see cref="UnderTest.OrderSubmitter.SubmitOrderForProductionAsync" />
        ///  method.
        /// </summary>
        /// 
        [Fact]
        public async Task SubmitOrderAsyncHonorsTheRetryPolicyIfSavingTheCompletedOrderFailed()
        {
            var mockClient         = new Mock<IOrdeProductionClient>();
            var mockStorage        = new Mock<IOrderStorage>();
            var mockLogger         = new Mock<ILogger>();
            var serializerSettings = new JsonSerializerSettings();
            var config             = new OrderSubmitterConfiguration { OperationRetryMaxCount = 4, OperationRetryExponentialSeconds = 0, OperationRetryJitterSeconds = 0 };
            var order              = new CreateOrderMessage { Identity = new OrderIdentity { PartnerCode = "123", PartnerOrderId = "ABC" }};
            var processor          = new Mock<UnderTest.OrderSubmitter>(config, mockClient.Object, mockStorage.Object, mockLogger.Object, serializerSettings) { CallBase = true };
            var pendingResult      = new OperationResult<CreateOrderMessage> { Outcome = Outcome.Success, Reason = "because", Payload = order, Recoverable = Recoverability.Final };
            var successResult      = new OperationResult { Outcome = Outcome.Success, Reason = String.Empty, Payload = String.Empty, Recoverable = Recoverability.Final };
            var failedResult       = new OperationResult { Outcome = Outcome.Failure, Reason = "because", Payload = "payload!", Recoverable = Recoverability.Retriable };
            var retries            = 0;
            
            serializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            serializerSettings.Converters.Add(new StringEnumConverter());

            mockLogger
                .Setup(logger => logger.ForContext(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<bool>()))
                .Returns(mockLogger.Object);

            processor
                .Protected()
                .Setup<Task<OperationResult<CreateOrderMessage>>>("RetrievePendingOrderAsync", ItExpr.IsAny<ILogger>(), ItExpr.IsAny<IOrderStorage>(), ItExpr.IsAny<JsonSerializerSettings>(), ItExpr.IsAny<string>(), ItExpr.IsAny<string>(),  ItExpr.IsAny<string>(), ItExpr.IsAny<OperationResult>())
                .Returns(Task.FromResult(pendingResult));

            processor
                .Protected()
                .Setup<Task<OperationResult>>("SendOrderToProductionAsync", ItExpr.IsAny<ILogger>(), ItExpr.IsAny<IOrdeProductionClient>(), ItExpr.IsAny<CreateOrderMessage>(), ItExpr.IsAny<string>(), ItExpr.IsAny<OperationResult>())
                .Returns(Task.FromResult(successResult));

            processor
                .Protected()
                .Setup<Task<OperationResult>>("StoreOrderAsCompletedAsync", ItExpr.IsAny<ILogger>(), ItExpr.IsAny<IOrderStorage>(), ItExpr.IsAny<CreateOrderMessage>(), ItExpr.IsAny<string>(), ItExpr.IsAny<OperationResult>())
                .Returns(Task.FromResult(failedResult))
                .Callback( () => ++retries);

            var result = await processor.Object.SubmitOrderForProductionAsync(order.Identity.PartnerCode, order.Identity.PartnerOrderId);
            
            retries.Should().Be(config.OperationRetryMaxCount + 1, "because the failure should be retried");
        }

        /// <summary>
        ///  Verifies functionality of the <see cref="UnderTest.OrderSubmitter.SubmitOrderForProductionAsync" />
        ///  method.
        /// </summary>
        /// 
        [Fact]
        public async Task SubmitOrderAsyncHonorsTheRetryPolicyIfPendingOrderDeletionFailed()
        {
            var mockClient         = new Mock<IOrdeProductionClient>();
            var mockStorage        = new Mock<IOrderStorage>();
            var mockLogger         = new Mock<ILogger>();
            var serializerSettings = new JsonSerializerSettings();
            var config             = new OrderSubmitterConfiguration { OperationRetryMaxCount = 2, OperationRetryExponentialSeconds = 0, OperationRetryJitterSeconds = 0 };
            var order              = new CreateOrderMessage { Identity = new OrderIdentity { PartnerCode = "123", PartnerOrderId = "ABC" }};
            var processor          = new Mock<UnderTest.OrderSubmitter>(config, mockClient.Object, mockStorage.Object, mockLogger.Object, serializerSettings) { CallBase = true };
            var pendingResult      = new OperationResult<CreateOrderMessage> { Outcome = Outcome.Success, Reason = "because", Payload = order, Recoverable = Recoverability.Final };
            var successResult      = new OperationResult { Outcome = Outcome.Success, Reason = String.Empty, Payload = String.Empty, Recoverable = Recoverability.Final };
            var failedResult       = new OperationResult { Outcome = Outcome.Failure, Reason = "because", Payload = "payload!", Recoverable = Recoverability.Retriable };
            var retries            = 0;
            
            serializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            serializerSettings.Converters.Add(new StringEnumConverter());

            mockLogger
                .Setup(logger => logger.ForContext(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<bool>()))
                .Returns(mockLogger.Object);

            processor
                .Protected()
                .Setup<Task<OperationResult<CreateOrderMessage>>>("RetrievePendingOrderAsync", ItExpr.IsAny<ILogger>(), ItExpr.IsAny<IOrderStorage>(), ItExpr.IsAny<JsonSerializerSettings>(), ItExpr.IsAny<string>(), ItExpr.IsAny<string>(),  ItExpr.IsAny<string>(), ItExpr.IsAny<OperationResult>())
                .Returns(Task.FromResult(pendingResult));

            processor
                .Protected()
                .Setup<Task<OperationResult>>("SendOrderToProductionAsync", ItExpr.IsAny<ILogger>(), ItExpr.IsAny<IOrdeProductionClient>(), ItExpr.IsAny<CreateOrderMessage>(), ItExpr.IsAny<string>(), ItExpr.IsAny<OperationResult>())
                .Returns(Task.FromResult(successResult));

            processor
                .Protected()
                .Setup<Task<OperationResult>>("StoreOrderAsCompletedAsync", ItExpr.IsAny<ILogger>(), ItExpr.IsAny<IOrderStorage>(), ItExpr.IsAny<CreateOrderMessage>(), ItExpr.IsAny<string>(), ItExpr.IsAny<OperationResult>())
                .Returns(Task.FromResult(successResult));

            processor
                .Protected()
                .Setup<Task<OperationResult>>("DeletePendingOrderAsync", ItExpr.IsAny<ILogger>(), ItExpr.IsAny<IOrderStorage>(), ItExpr.IsAny<string>(), ItExpr.IsAny<string>(), ItExpr.IsAny<string>(), ItExpr.IsAny<OperationResult>())
                .Returns(Task.FromResult(failedResult))
                .Callback( () => ++retries);

            var result = await processor.Object.SubmitOrderForProductionAsync(order.Identity.PartnerCode, order.Identity.PartnerOrderId);
            
            retries.Should().Be(config.OperationRetryMaxCount + 1, "because the failure should be retried");
        }

        
        /// <summary>
        ///  Verifies functionality of the <see cref="UnderTest.OrderSubmitter.SubmitOrderForProductionAsync" />
        ///  method.
        /// </summary>
        /// 
        [Fact]
        public async Task SubmitOrderAsyncRespectsAnEmulatedPendingOrderResult()
        {
            var mockClient         = new Mock<IOrdeProductionClient>();
            var mockStorage        = new Mock<IOrderStorage>();
            var mockLogger         = new Mock<ILogger>();
            var serializerSettings = new JsonSerializerSettings();
            var order              = new CreateOrderMessage { Identity = new OrderIdentity { PartnerCode = "123", PartnerOrderId = "ABC" }};
            var processor          = new Mock<UnderTest.OrderSubmitter>(new OrderSubmitterConfiguration(), mockClient.Object, mockStorage.Object, mockLogger.Object, serializerSettings) { CallBase = true };
            var pendingResult      = new OperationResult<CreateOrderMessage> { Outcome = Outcome.Failure, Reason = "because", Payload = order, Recoverable = Recoverability.Retriable };
            var successResult      = new OperationResult { Outcome = Outcome.Success, Reason = String.Empty, Payload = String.Empty, Recoverable = Recoverability.Final };
            var expectedResult     = new OperationResult { Outcome = Outcome.Failure, Reason = "because", Payload = String.Empty, Recoverable = Recoverability.Retriable };
                        
            serializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            serializerSettings.Converters.Add(new StringEnumConverter());

            mockLogger
                .Setup(logger => logger.ForContext(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<bool>()))
                .Returns(mockLogger.Object);
                
            processor
                .Protected()
                .Setup<Task<OperationResult>>("SendOrderToProductionAsync", ItExpr.IsAny<ILogger>(), ItExpr.IsAny<IOrdeProductionClient>(), ItExpr.IsAny<CreateOrderMessage>(), ItExpr.IsAny<string>(), ItExpr.IsAny<OperationResult>())
                .Returns(Task.FromResult(successResult));

            processor
                .Protected()
                .Setup<Task<OperationResult>>("DeletePendingOrderAsync", ItExpr.IsAny<ILogger>(), ItExpr.IsAny<IOrderStorage>(), ItExpr.IsAny<string>(), ItExpr.IsAny<string>(), ItExpr.IsAny<string>(), ItExpr.IsAny<OperationResult>())
                .Returns(Task.FromResult(successResult));


            var emulatedResult = new OperationResult
            {
                Outcome     = pendingResult.Outcome,
                Reason      = pendingResult.Reason,
                Recoverable = pendingResult.Recoverable,
                Payload     = JsonConvert.SerializeObject(pendingResult.Payload, serializerSettings)
            };

            var result = await processor.Object.SubmitOrderForProductionAsync(order.Identity.PartnerCode, order.Identity.PartnerOrderId, new DependencyEmulation { CreateOrderMessage = emulatedResult });
            
            result.Should().NotBeNull("becuase a result should have been returned");
            result.ShouldBeEquivalentTo(expectedResult, "because the emulated result should have been respected");
        }

        /// <summary>
        ///  Verifies functionality of the <see cref="UnderTest.OrderSubmitter.SubmitOrderForProductionAsync" />
        ///  method.
        /// </summary>
        /// 
        [Fact]
        public async Task SubmitOrderAsyncRespectsAnEmulatedOrderSubmission()
        {
            var mockClient         = new Mock<IOrdeProductionClient>();
            var mockStorage        = new Mock<IOrderStorage>();
            var mockLogger         = new Mock<ILogger>();
            var serializerSettings = new JsonSerializerSettings();
            var order              = new CreateOrderMessage { Identity = new OrderIdentity { PartnerCode = "123", PartnerOrderId = "ABC" }};
            var processor          = new Mock<UnderTest.OrderSubmitter>(new OrderSubmitterConfiguration(), mockClient.Object, mockStorage.Object, mockLogger.Object, serializerSettings) { CallBase = true };
            var pendingResult      = new OperationResult<CreateOrderMessage> { Outcome = Outcome.Success, Reason = "because", Payload = order, Recoverable = Recoverability.Final };
            var successResult      = new OperationResult { Outcome = Outcome.Success, Reason = String.Empty, Payload = String.Empty, Recoverable = Recoverability.Final };
            var failedResult       = new OperationResult { Outcome = Outcome.Failure, Reason = "because", Payload = "payload!", Recoverable = Recoverability.Retriable };
            
            serializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            serializerSettings.Converters.Add(new StringEnumConverter());

            mockLogger
                .Setup(logger => logger.ForContext(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<bool>()))
                .Returns(mockLogger.Object);

            processor
                .Protected()
                .Setup<Task<OperationResult<CreateOrderMessage>>>("RetrievePendingOrderAsync", ItExpr.IsAny<ILogger>(), ItExpr.IsAny<IOrderStorage>(), ItExpr.IsAny<JsonSerializerSettings>(), ItExpr.IsAny<string>(), ItExpr.IsAny<string>(),  ItExpr.IsAny<string>(), ItExpr.IsAny<OperationResult>())
                .Returns(Task.FromResult(pendingResult));

            processor
                .Protected()
                .Setup<Task<OperationResult>>("DeletePendingOrderAsync", ItExpr.IsAny<ILogger>(), ItExpr.IsAny<IOrderStorage>(), ItExpr.IsAny<string>(), ItExpr.IsAny<string>(), ItExpr.IsAny<string>(), ItExpr.IsAny<OperationResult>())
                .Returns(Task.FromResult(successResult));

            var result = await processor.Object.SubmitOrderForProductionAsync(order.Identity.PartnerCode, order.Identity.PartnerOrderId, new DependencyEmulation { OrderSubmission = failedResult });
            
            result.Should().NotBeNull("becuase a result should have been returned");
            result.ShouldBeEquivalentTo(failedResult, "because the emulated result should be respected");
        }

         /// <summary>
        ///  Verifies functionality of the <see cref="UnderTest.OrderSubmitter.SubmitOrderForProductionAsync" />
        ///  method.
        /// </summary>
        /// 
        [Fact]
        public async Task SubmitOrderAsyncReturnsTheExceptionResultIfPendingOrderRetrievalThrows()
        {
            var mockClient         = new Mock<IOrdeProductionClient>();
            var mockStorage        = new Mock<IOrderStorage>();
            var mockLogger         = new Mock<ILogger>();
            var serializerSettings = new JsonSerializerSettings();
            var processor          = new Mock<UnderTest.OrderSubmitter>(new OrderSubmitterConfiguration(), mockClient.Object, mockStorage.Object, mockLogger.Object, serializerSettings) { CallBase = true };
            var pendingResult      = new OperationResult<CreateOrderMessage> { Outcome = Outcome.Failure, Reason = "because", Recoverable = Recoverability.Retriable };
            
            serializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            serializerSettings.Converters.Add(new StringEnumConverter());

            mockLogger
                .Setup(logger => logger.ForContext(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<bool>()))
                .Returns(mockLogger.Object);

            processor
                .Protected()
                .Setup<Task<OperationResult<CreateOrderMessage>>>("RetrievePendingOrderAsync", ItExpr.IsAny<ILogger>(), ItExpr.IsAny<IOrderStorage>(), ItExpr.IsAny<JsonSerializerSettings>(), ItExpr.IsAny<string>(), ItExpr.IsAny<string>(),  ItExpr.IsAny<string>(), ItExpr.IsAny<OperationResult>())
                .ThrowsAsync(new MissingDependencyException());

            var result = await processor.Object.SubmitOrderForProductionAsync("ABC", "123");
            
            result.Should().NotBeNull("becuase a result should have been returned");
            result.ShouldBeEquivalentTo(OperationResult.ExceptionResult, "because the exception should short-circuit the processing");

        }

        /// <summary>
        ///  Verifies functionality of the <see cref="UnderTest.OrderSubmitter.SubmitOrderForProductionAsync" />
        ///  method.
        /// </summary>
        /// 
        [Fact]
        public async Task SubmitOrderAsyncReturnsTheExcpetionResultIfSubmissionThrows()
        {
            var mockClient         = new Mock<IOrdeProductionClient>();
            var mockStorage        = new Mock<IOrderStorage>();
            var mockLogger         = new Mock<ILogger>();
            var serializerSettings = new JsonSerializerSettings();
            var order              = new CreateOrderMessage { Identity = new OrderIdentity { PartnerCode = "123", PartnerOrderId = "ABC" }};
            var processor          = new Mock<UnderTest.OrderSubmitter>(new OrderSubmitterConfiguration(), mockClient.Object, mockStorage.Object, mockLogger.Object, serializerSettings) { CallBase = true };
            var pendingResult      = new OperationResult<CreateOrderMessage> { Outcome = Outcome.Success, Reason = "because", Payload = order, Recoverable = Recoverability.Final };
            
            serializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            serializerSettings.Converters.Add(new StringEnumConverter());

            mockLogger
                .Setup(logger => logger.ForContext(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<bool>()))
                .Returns(mockLogger.Object);

            processor
                .Protected()
                .Setup<Task<OperationResult<CreateOrderMessage>>>("RetrievePendingOrderAsync", ItExpr.IsAny<ILogger>(), ItExpr.IsAny<IOrderStorage>(), ItExpr.IsAny<JsonSerializerSettings>(), ItExpr.IsAny<string>(), ItExpr.IsAny<string>(),  ItExpr.IsAny<string>(), ItExpr.IsAny<OperationResult>())
                .Returns(Task.FromResult(pendingResult));

            processor
                .Protected()
                .Setup<Task<OperationResult>>("SendOrderToProductionAsync", ItExpr.IsAny<ILogger>(), ItExpr.IsAny<IOrdeProductionClient>(), ItExpr.IsAny<CreateOrderMessage>(), ItExpr.IsAny<string>(), ItExpr.IsAny<OperationResult>())
                .ThrowsAsync(new MissingFieldException());

            var result = await processor.Object.SubmitOrderForProductionAsync(order.Identity.PartnerOrderId, order.Identity.PartnerOrderId);
            
            result.Should().NotBeNull("becuase a result should have been returned");
            result.ShouldBeEquivalentTo(OperationResult.ExceptionResult, "because the exception should short-circuit the processing");

        }

        /// <summary>
        ///  Verifies functionality of the <see cref="UnderTest.OrderSubmitter.SubmitOrderForProductionAsync" />
        ///  method.
        /// </summary>
        /// 
        [Fact]
        public async Task SubmitOrderAsyncReturnsTheExceptionResultResultIfSavingTheCompletedOrderThrows()
        {
            var mockClient         = new Mock<IOrdeProductionClient>();
            var mockStorage        = new Mock<IOrderStorage>();
            var mockLogger         = new Mock<ILogger>();
            var serializerSettings = new JsonSerializerSettings();
            var order              = new CreateOrderMessage { Identity = new OrderIdentity { PartnerCode = "123", PartnerOrderId = "ABC" }};
            var processor          = new Mock<UnderTest.OrderSubmitter>(new OrderSubmitterConfiguration(), mockClient.Object, mockStorage.Object, mockLogger.Object, serializerSettings) { CallBase = true };
            var pendingResult      = new OperationResult<CreateOrderMessage> { Outcome = Outcome.Success, Reason = "because", Payload = order, Recoverable = Recoverability.Final };
            var successResult      = new OperationResult { Outcome = Outcome.Success, Reason = String.Empty, Payload = String.Empty, Recoverable = Recoverability.Final };
            
            serializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            serializerSettings.Converters.Add(new StringEnumConverter());

            mockLogger
                .Setup(logger => logger.ForContext(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<bool>()))
                .Returns(mockLogger.Object);

            processor
                .Protected()
                .Setup<Task<OperationResult<CreateOrderMessage>>>("RetrievePendingOrderAsync", ItExpr.IsAny<ILogger>(), ItExpr.IsAny<IOrderStorage>(), ItExpr.IsAny<JsonSerializerSettings>(), ItExpr.IsAny<string>(), ItExpr.IsAny<string>(),  ItExpr.IsAny<string>(), ItExpr.IsAny<OperationResult>())
                .Returns(Task.FromResult(pendingResult));

            processor
                .Protected()
                .Setup<Task<OperationResult>>("SendOrderToProductionAsync", ItExpr.IsAny<ILogger>(), ItExpr.IsAny<IOrdeProductionClient>(), ItExpr.IsAny<CreateOrderMessage>(), ItExpr.IsAny<string>(), ItExpr.IsAny<OperationResult>())
                .Returns(Task.FromResult(successResult));

            processor
                .Protected()
                .Setup<Task<OperationResult>>("StoreOrderAsCompletedAsync", ItExpr.IsAny<ILogger>(), ItExpr.IsAny<IOrderStorage>(), ItExpr.IsAny<CreateOrderMessage>(), ItExpr.IsAny<string>(), ItExpr.IsAny<OperationResult>())
                .ThrowsAsync(new FormatException());

            var result = await processor.Object.SubmitOrderForProductionAsync(order.Identity.PartnerCode, order.Identity.PartnerOrderId);
            
            result.Should().NotBeNull("becuase a result should have been returned");
            result.ShouldBeEquivalentTo(OperationResult.ExceptionResult, "because the failed result should cause processing to short-circuit");
        }

        /// <summary>
        ///  Verifies functionality of the <see cref="UnderTest.OrderSubmitter.SubmitOrderForProductionAsync" />
        ///  method.
        /// </summary>
        /// 
        [Fact]
        public async Task SubmitOrderAsyncReturnsASuccessResultButLogsTheWarningIfPendingOrderDeletionThrows()
        {
            var mockClient         = new Mock<IOrdeProductionClient>();
            var mockStorage        = new Mock<IOrderStorage>();
            var mockLogger         = new Mock<ILogger>();
            var serializerSettings = new JsonSerializerSettings();
            var order              = new CreateOrderMessage { Identity = new OrderIdentity { PartnerCode = "123", PartnerOrderId = "ABC" }};
            var processor          = new Mock<UnderTest.OrderSubmitter>(new OrderSubmitterConfiguration(), mockClient.Object, mockStorage.Object, mockLogger.Object, serializerSettings) { CallBase = true };
            var pendingResult      = new OperationResult<CreateOrderMessage> { Outcome = Outcome.Success, Reason = "because", Payload = order, Recoverable = Recoverability.Final };
            var successResult      = new OperationResult { Outcome = Outcome.Success, Reason = String.Empty, Payload = String.Empty, Recoverable = Recoverability.Final };
            var failedResult       = new OperationResult { Outcome = Outcome.Failure, Reason = "because", Payload = "payload!", Recoverable = Recoverability.Retriable };
            
            serializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            serializerSettings.Converters.Add(new StringEnumConverter());

            mockLogger
                .Setup(logger => logger.ForContext(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<bool>()))
                .Returns(mockLogger.Object);

            processor
                .Protected()
                .Setup<Task<OperationResult<CreateOrderMessage>>>("RetrievePendingOrderAsync", ItExpr.IsAny<ILogger>(), ItExpr.IsAny<IOrderStorage>(), ItExpr.IsAny<JsonSerializerSettings>(), ItExpr.IsAny<string>(), ItExpr.IsAny<string>(),  ItExpr.IsAny<string>(), ItExpr.IsAny<OperationResult>())
                .Returns(Task.FromResult(pendingResult));

            processor
                .Protected()
                .Setup<Task<OperationResult>>("SendOrderToProductionAsync", ItExpr.IsAny<ILogger>(), ItExpr.IsAny<IOrdeProductionClient>(), ItExpr.IsAny<CreateOrderMessage>(), ItExpr.IsAny<string>(), ItExpr.IsAny<OperationResult>())
                .Returns(Task.FromResult(successResult));

            processor
                .Protected()
                .Setup<Task<OperationResult>>("StoreOrderAsCompletedAsync", ItExpr.IsAny<ILogger>(), ItExpr.IsAny<IOrderStorage>(), ItExpr.IsAny<CreateOrderMessage>(), ItExpr.IsAny<string>(), ItExpr.IsAny<OperationResult>())
                .Returns(Task.FromResult(successResult));

            processor
                .Protected()
                .Setup<Task<OperationResult>>("DeletePendingOrderAsync", ItExpr.IsAny<ILogger>(), ItExpr.IsAny<IOrderStorage>(), ItExpr.IsAny<string>(), ItExpr.IsAny<string>(), ItExpr.IsAny<string>(), ItExpr.IsAny<OperationResult>())
                .ThrowsAsync(new DivideByZeroException());

            var result = await processor.Object.SubmitOrderForProductionAsync(order.Identity.PartnerCode, order.Identity.PartnerOrderId);
            
            result.Should().NotBeNull("becuase a result should have been returned");
            result.ShouldBeEquivalentTo(successResult, "because the failed delete should not cause processing to fail");

            mockLogger.Verify(logger => logger.Warning(It.IsAny<string>(), 
                                                       It.Is<string>(value => value == order.Identity.PartnerCode), 
                                                       It.Is<string>(value => value == order.Identity.PartnerOrderId)),
                Times.Once, "A warning should have been logged for the failed pending order deletion");
        }

        /// <summary>
        ///  Verifies functionality of the <see cref="UnderTest.OrderSubmitter.SubmitOrderForProductionAsync" />
        ///  method.
        /// </summary>
        /// 
        [Fact]
        public async Task RetrievePendingOrderAsyncTheOrderFromPendingStorage()
        {
            var mockClient         = new Mock<IOrdeProductionClient>();
            var mockStorage        = new Mock<IOrderStorage>();
            var mockLogger         = new Mock<ILogger>();
            var serializerSettings = new JsonSerializerSettings();
            var order              = new CreateOrderMessage { Identity = new OrderIdentity { PartnerCode = "123", PartnerOrderId = "ABC" }};
            var processor          = new Mock<UnderTest.OrderSubmitter>(new OrderSubmitterConfiguration(), mockClient.Object, mockStorage.Object, mockLogger.Object, serializerSettings) { CallBase = true };
            var pendingResult      = new OperationResult<CreateOrderMessage> { Outcome = Outcome.Success, Reason = "because", Payload = order, Recoverable = Recoverability.Final };
            var successResult      = new OperationResult { Outcome = Outcome.Success, Reason = String.Empty, Payload = String.Empty, Recoverable = Recoverability.Final };
            var failedResult       = new OperationResult { Outcome = Outcome.Failure, Reason = "because", Payload = "payload!", Recoverable = Recoverability.Retriable };
            
            serializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            serializerSettings.Converters.Add(new StringEnumConverter());

            mockLogger
                .Setup(logger => logger.ForContext(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<bool>()))
                .Returns(mockLogger.Object);

            mockStorage
                .Setup(storage => storage.TryRetrievePendingOrderAsync(It.Is<string>(value => value == order.Identity.PartnerCode), It.Is<string>(value => value == order.Identity.PartnerOrderId)))
                .ReturnsAsync((true, order))
                .Verifiable("The order should have been saved as completed");
                
            processor
                .Protected()
                .Setup<Task<OperationResult>>("SendOrderToProductionAsync", ItExpr.IsAny<ILogger>(), ItExpr.IsAny<IOrdeProductionClient>(), ItExpr.IsAny<CreateOrderMessage>(), ItExpr.IsAny<string>(), ItExpr.IsAny<OperationResult>())
                .Returns(Task.FromResult(successResult));

            processor
                .Protected()
                .Setup<Task<OperationResult>>("StoreOrderAsCompletedAsync", ItExpr.IsAny<ILogger>(), ItExpr.IsAny<IOrderStorage>(), ItExpr.IsAny<CreateOrderMessage>(), ItExpr.IsAny<string>(), ItExpr.IsAny<OperationResult>())
                .Returns(Task.FromResult(successResult));

            processor
                .Protected()
                .Setup<Task<OperationResult>>("DeletePendingOrderAsync", ItExpr.IsAny<ILogger>(), ItExpr.IsAny<IOrderStorage>(), ItExpr.IsAny<string>(), ItExpr.IsAny<string>(), ItExpr.IsAny<string>(), ItExpr.IsAny<OperationResult>())
                .Returns(Task.FromResult(successResult));

            var result = await processor.Object.SubmitOrderForProductionAsync(order.Identity.PartnerCode, order.Identity.PartnerOrderId);
            
            result.Should().NotBeNull("becuase a result should have been returned");
            result.ShouldBeEquivalentTo(successResult, "because the order submission should have been successful");

            mockStorage.VerifyAll();
        }

        /// <summary>
        ///  Verifies functionality of the <see cref="UnderTest.OrderSubmitter.SubmitOrderForProductionAsync" />
        ///  method.
        /// </summary>
        /// 
        [Fact]
        public async Task SubmitOrderForProductionAsyncAttemptsOrderSubmission()
        {
            var mockClient         = new Mock<IOrdeProductionClient>();
            var mockStorage        = new Mock<IOrderStorage>();
            var mockLogger         = new Mock<ILogger>();
            var serializerSettings = new JsonSerializerSettings();
            var order              = new CreateOrderMessage { Identity = new OrderIdentity { PartnerCode = "123", PartnerOrderId = "ABC" }};
            var processor          = new Mock<UnderTest.OrderSubmitter>(new OrderSubmitterConfiguration(), mockClient.Object, mockStorage.Object, mockLogger.Object, serializerSettings) { CallBase = true };
            var pendingResult      = new OperationResult<CreateOrderMessage> { Outcome = Outcome.Success, Reason = "because", Payload = order, Recoverable = Recoverability.Final };
            var successResult      = new OperationResult { Outcome = Outcome.Success, Reason = String.Empty, Payload = String.Empty, Recoverable = Recoverability.Final };
            var failedResult       = new OperationResult { Outcome = Outcome.Failure, Reason = "because", Payload = "payload!", Recoverable = Recoverability.Retriable };
            var correlationId      = "oh hai";
            
            serializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            serializerSettings.Converters.Add(new StringEnumConverter());

            mockLogger
                .Setup(logger => logger.ForContext(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<bool>()))
                .Returns(mockLogger.Object);

            mockClient
                .Setup(client => client.SubmitOrderForProductionAsync(It.Is<CreateOrderMessage>(value => ((value.Identity.PartnerCode == order.Identity.PartnerCode) && (order.Identity.PartnerOrderId == order.Identity.PartnerOrderId))), It.Is<string>(value => value == correlationId)))
                .ReturnsAsync(successResult)
                .Verifiable("The order production client should have been asked to submit the order");

            processor
                .Protected()
                .Setup<Task<OperationResult<CreateOrderMessage>>>("RetrievePendingOrderAsync", ItExpr.IsAny<ILogger>(), ItExpr.IsAny<IOrderStorage>(), ItExpr.IsAny<JsonSerializerSettings>(), ItExpr.IsAny<string>(), ItExpr.IsAny<string>(),  ItExpr.IsAny<string>(), ItExpr.IsAny<OperationResult>())
                .Returns(Task.FromResult(pendingResult));
                
            processor
                .Protected()
                .Setup<Task<OperationResult>>("StoreOrderAsCompletedAsync", ItExpr.IsAny<ILogger>(), ItExpr.IsAny<IOrderStorage>(), ItExpr.IsAny<CreateOrderMessage>(), ItExpr.IsAny<string>(), ItExpr.IsAny<OperationResult>())
                .Returns(Task.FromResult(successResult));

            processor
                .Protected()
                .Setup<Task<OperationResult>>("DeletePendingOrderAsync", ItExpr.IsAny<ILogger>(), ItExpr.IsAny<IOrderStorage>(), ItExpr.IsAny<string>(), ItExpr.IsAny<string>(), ItExpr.IsAny<string>(), ItExpr.IsAny<OperationResult>())
                .Returns(Task.FromResult(successResult));

            var result = await processor.Object.SubmitOrderForProductionAsync(order.Identity.PartnerCode, order.Identity.PartnerOrderId, null, correlationId);
            
            result.Should().NotBeNull("becuase a result should have been returned");
            result.ShouldBeEquivalentTo(successResult, "because submission should have been successful");

            mockClient.VerifyAll();
        }

        /// <summary>
        ///  Verifies functionality of the <see cref="UnderTest.OrderSubmitter.SubmitOrderForProductionAsync" />
        ///  method.
        /// </summary>
        /// 
        [Fact]
        public async Task StoreOrderAsCompletedAsyncSavesTheOrderAsCompleted()
        {
            var mockClient         = new Mock<IOrdeProductionClient>();
            var mockStorage        = new Mock<IOrderStorage>();
            var mockLogger         = new Mock<ILogger>();
            var serializerSettings = new JsonSerializerSettings();
            var order              = new CreateOrderMessage { Identity = new OrderIdentity { PartnerCode = "123", PartnerOrderId = "ABC" }};
            var processor          = new Mock<UnderTest.OrderSubmitter>(new OrderSubmitterConfiguration(), mockClient.Object, mockStorage.Object, mockLogger.Object, serializerSettings) { CallBase = true };
            var pendingResult      = new OperationResult<CreateOrderMessage> { Outcome = Outcome.Success, Reason = "because", Payload = order, Recoverable = Recoverability.Final };
            var successResult      = new OperationResult { Outcome = Outcome.Success, Reason = String.Empty, Payload = String.Empty, Recoverable = Recoverability.Final };
            var failedResult       = new OperationResult { Outcome = Outcome.Failure, Reason = "because", Payload = "payload!", Recoverable = Recoverability.Retriable };
            
            serializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            serializerSettings.Converters.Add(new StringEnumConverter());

            mockLogger
                .Setup(logger => logger.ForContext(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<bool>()))
                .Returns(mockLogger.Object);

            mockStorage
                .Setup(storage => storage.SaveCompletedOrderAsync(It.Is<CreateOrderMessage>(value => ((value.Identity.PartnerCode == order.Identity.PartnerCode) && (order.Identity.PartnerOrderId == order.Identity.PartnerOrderId)))))
                .ReturnsAsync("yay")
                .Verifiable("The order should have been saved as completed");

            processor
                .Protected()
                .Setup<Task<OperationResult<CreateOrderMessage>>>("RetrievePendingOrderAsync", ItExpr.IsAny<ILogger>(), ItExpr.IsAny<IOrderStorage>(), ItExpr.IsAny<JsonSerializerSettings>(), ItExpr.IsAny<string>(), ItExpr.IsAny<string>(),  ItExpr.IsAny<string>(), ItExpr.IsAny<OperationResult>())
                .Returns(Task.FromResult(pendingResult));

            processor
                .Protected()
                .Setup<Task<OperationResult>>("SendOrderToProductionAsync", ItExpr.IsAny<ILogger>(), ItExpr.IsAny<IOrdeProductionClient>(), ItExpr.IsAny<CreateOrderMessage>(), ItExpr.IsAny<string>(), ItExpr.IsAny<OperationResult>())
                .Returns(Task.FromResult(successResult));
                
            processor
                .Protected()
                .Setup<Task<OperationResult>>("DeletePendingOrderAsync", ItExpr.IsAny<ILogger>(), ItExpr.IsAny<IOrderStorage>(), ItExpr.IsAny<string>(), ItExpr.IsAny<string>(), ItExpr.IsAny<string>(), ItExpr.IsAny<OperationResult>())
                .Returns(Task.FromResult(successResult));

            var result = await processor.Object.SubmitOrderForProductionAsync(order.Identity.PartnerCode, order.Identity.PartnerOrderId);
            
            result.Should().NotBeNull("becuase a result should have been returned");
            result.ShouldBeEquivalentTo(successResult, "because the order submission should have been successful");

            mockStorage.VerifyAll();
        }

        /// <summary>
        ///  Verifies functionality of the <see cref="UnderTest.OrderSubmitter.SubmitOrderForProductionAsync" />
        ///  method.
        /// </summary>
        /// 
        [Fact]
        public async Task DeletePendingOrderAsyncDeletesTheOrderFromPendingStorage()
        {
            var mockClient         = new Mock<IOrdeProductionClient>();
            var mockStorage        = new Mock<IOrderStorage>();
            var mockLogger         = new Mock<ILogger>();
            var serializerSettings = new JsonSerializerSettings();
            var order              = new CreateOrderMessage { Identity = new OrderIdentity { PartnerCode = "123", PartnerOrderId = "ABC" }};
            var processor          = new Mock<UnderTest.OrderSubmitter>(new OrderSubmitterConfiguration(), mockClient.Object, mockStorage.Object, mockLogger.Object, serializerSettings) { CallBase = true };
            var pendingResult      = new OperationResult<CreateOrderMessage> { Outcome = Outcome.Success, Reason = "because", Payload = order, Recoverable = Recoverability.Final };
            var successResult      = new OperationResult { Outcome = Outcome.Success, Reason = String.Empty, Payload = String.Empty, Recoverable = Recoverability.Final };
            var failedResult       = new OperationResult { Outcome = Outcome.Failure, Reason = "because", Payload = "payload!", Recoverable = Recoverability.Retriable };
            
            serializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            serializerSettings.Converters.Add(new StringEnumConverter());

            mockLogger
                .Setup(logger => logger.ForContext(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<bool>()))
                .Returns(mockLogger.Object);

            mockStorage
                .Setup(storage => storage.DeletePendingOrderAsync(It.Is<string>(value => value == order.Identity.PartnerCode), It.Is<string>(value => value == order.Identity.PartnerOrderId)))
                .Returns(Task.CompletedTask)
                .Verifiable("The order should have been been deleted from pending storage");

            processor
                .Protected()
                .Setup<Task<OperationResult<CreateOrderMessage>>>("RetrievePendingOrderAsync", ItExpr.IsAny<ILogger>(), ItExpr.IsAny<IOrderStorage>(), ItExpr.IsAny<JsonSerializerSettings>(), ItExpr.IsAny<string>(), ItExpr.IsAny<string>(),  ItExpr.IsAny<string>(), ItExpr.IsAny<OperationResult>())
                .Returns(Task.FromResult(pendingResult));

            processor
                .Protected()
                .Setup<Task<OperationResult>>("SendOrderToProductionAsync", ItExpr.IsAny<ILogger>(), ItExpr.IsAny<IOrdeProductionClient>(), ItExpr.IsAny<CreateOrderMessage>(), ItExpr.IsAny<string>(), ItExpr.IsAny<OperationResult>())
                .Returns(Task.FromResult(successResult));

            processor
                .Protected()
                .Setup<Task<OperationResult>>("StoreOrderAsCompletedAsync", ItExpr.IsAny<ILogger>(), ItExpr.IsAny<IOrderStorage>(), ItExpr.IsAny<CreateOrderMessage>(), ItExpr.IsAny<string>(), ItExpr.IsAny<OperationResult>())
                .Returns(Task.FromResult(successResult));

            var result = await processor.Object.SubmitOrderForProductionAsync(order.Identity.PartnerCode, order.Identity.PartnerOrderId);
            
            result.Should().NotBeNull("becuase a result should have been returned");
            result.ShouldBeEquivalentTo(successResult, "because the order submission should have been successful");

            mockStorage.VerifyAll();
        }
    }
}
