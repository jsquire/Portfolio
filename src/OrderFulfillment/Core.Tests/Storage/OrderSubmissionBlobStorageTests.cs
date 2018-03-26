using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Moq;
using FluentAssertions;
using OrderFulfillment.Core.Storage;
using OrderFulfillment.Core.Configuration;
using Moq.Protected;
using OrderFulfillment.Core.Models.External.OrderProduction;
using Newtonsoft.Json;

namespace OrderFulfillment.Core.Tests.Storage
{
    /// <summary>
    ///   The suite of tests for the <see cref="OrderSubmissionBlobStorage" />
    ///   class.
    /// </summary>
    /// 
    public class OrderSubmissionBlobStorageTests
    {
        /// <summary>
        ///   Verifies behavior of the constructor.
        /// </summary>
        /// 
        [Fact]
        public void ConstructorValidatesConfiguration()
        {
            Action actionUnderTest = () => new OrderSubmissionBlobStorage(null, null);
            actionUnderTest.ShouldThrow<ArgumentNullException>("because the configuration is required");
        }

        /// <summary>
        ///  Verifies behavior of the <see cref="OrderSubmissionBlobStorage.TryRetrievePendingOrderAsync" />
        ///  method.
        /// </summary>
        /// 
        [Fact]
        public async Task TryRetrievePendingOrderAsyncReturnsBlobContentWhenFound()
        {           
            var config = new OrderSubmissionBlobStorageConfiguration
            {
                StorageConnectionString = "hey, connect to storage!",
                PendingContainer        = "the place where stuff lives"
            };

            var mockStorage = new Mock<OrderSubmissionBlobStorage>(config, null) { CallBase = true };
            var partner     = "Henry";
            var orderId     = "ABD123";
            var content     = new CreateOrderMessage();
            
            mockStorage
                .Protected()
                .Setup<Task<(bool, string)>>("RetrieveBlobAsTextAsync", ItExpr.Is<string>(connection => connection == config.StorageConnectionString), 
                                                                        ItExpr.Is<string>(container => container == config.PendingContainer), 
                                                                        ItExpr.Is<string>(blob => ((blob.Contains(partner)) && (blob.Contains(orderId)))))
                .Returns(Task.FromResult((true, JsonConvert.SerializeObject(content))))
                .Verifiable("The blob text should have been requested");

            var result = await mockStorage.Object.TryRetrievePendingOrderAsync(partner, orderId);

            result.Found.Should().BeTrue("because the content was returned");
            result.Order.ShouldBeEquivalentTo(content, "because the content should have been retrieved");

            mockStorage.VerifyAll();
        }

        /// <summary>
        ///  Verifies behavior of the <see cref="OrderSubmissionBlobStorage.DeleteBlobAsync" />
        ///  method.
        /// </summary>
        /// 
        [Fact]
        public async Task DeletePendingOrderAsyncAttemptsToDeleteTheBlob()
        {           
            var config = new OrderSubmissionBlobStorageConfiguration
            {
                StorageConnectionString = "hey, connect to storage!",
                PendingContainer        = "the place where stuff lives"
            };

            var mockStorage = new Mock<OrderSubmissionBlobStorage>(config, null) { CallBase = true };
            var partner     = "Henry";
            var orderId     = "ABD123";
            
            mockStorage
                .Protected()
                .Setup<Task>("DeleteBlobAsync", ItExpr.Is<string>(connection => connection == config.StorageConnectionString), 
                                                ItExpr.Is<string>(container => container == config.PendingContainer), 
                                                ItExpr.Is<string>(blob => ((blob.Contains(partner)) && (blob.Contains(orderId)))))
                .Returns(Task.CompletedTask)
                .Verifiable("The blob deletion should have been requested");

            await mockStorage.Object.DeletePendingOrderAsync(partner, orderId);
            mockStorage.VerifyAll();
        }

        /// <summary>
        ///  Verifies behavior of the <see cref="OrderSubmissionBlobStorage.SavePendingOrderAsync" />
        ///  method.
        /// </summary>
        /// 
        [Fact]
        public void SavePendingOrderAsyncVerifiesTheOrderIsPresent()
        {           
            var config = new OrderSubmissionBlobStorageConfiguration
            {
                StorageConnectionString = "hey, connect to storage!",
                PendingContainer        = "the place where stuff lives"
            };

            var storage = new OrderSubmissionBlobStorage(config, null);

            Action actionUnderTest = () => storage.SavePendingOrderAsync("partner", "order-id", null).GetAwaiter().GetResult();
            actionUnderTest.ShouldThrow<ArgumentNullException>("because the order is required");
        }

        /// <summary>
        ///  Verifies behavior of the <see cref="OrderSubmissionBlobStorage.SavePendingOrderAsync" />
        ///  method.
        /// </summary>
        /// 
        [Fact]
        public void SavePendingOrderAsyncValidatesThePartnerCode()
        {
            var config = new OrderSubmissionBlobStorageConfiguration
            {
                StorageConnectionString = "hey, connect to storage!",
                PendingContainer        = "the place where stuff lives"
            };

            var partner = (string)null;
            var orderId = "88888888";
            var order   = new CreateOrderMessage();
            var storage = new OrderSubmissionBlobStorage(config, null);

            Action actionUnderTest = () => storage.SavePendingOrderAsync(partner, orderId, order).GetAwaiter().GetResult();
            actionUnderTest.ShouldThrow<ArgumentException>("because the partner is required").And.ParamName.Should().Be(nameof(partner));
        }

        /// <summary>
        ///  Verifies behavior of the <see cref="OrderSubmissionBlobStorage.SavePendingOrderAsync" />
        ///  method.
        /// </summary>
        /// 
        [Fact]
        public void SavePendingOrderAsyncValidatesTheOrderId()
        {           
            var config = new OrderSubmissionBlobStorageConfiguration
            {
                StorageConnectionString = "hey, connect to storage!",
                PendingContainer        = "the place where stuff lives"
            };

            var partner = "ABC123";
            var orderId = (string)null;
            var order   = new CreateOrderMessage();
            var storage = new OrderSubmissionBlobStorage(config, null);

            Action actionUnderTest = () => storage.SavePendingOrderAsync(partner, orderId, order).GetAwaiter().GetResult();
            actionUnderTest.ShouldThrow<ArgumentException>("because the order id is required").And.ParamName.Should().Be(nameof(orderId));
        }

        /// <summary>
        ///  Verifies behavior of the <see cref="OrderSubmissionBlobStorage.SavePendingOrderAsync" />
        ///  method.
        /// </summary>
        /// 
        [Fact]
        public async Task SavePendingOrderAsyncAttemptsToUploadTheBlob()
        {           
            var config = new OrderSubmissionBlobStorageConfiguration
            {
                StorageConnectionString = "hey, connect to storage!",
                PendingContainer        = "the place where stuff lives"
            };

            var mockStorage = new Mock<OrderSubmissionBlobStorage>(config,  null) { CallBase = true };
            var partner     = "Henry";
            var orderId     = "ABD123";
            var order       = new CreateOrderMessage { Identity = new OrderIdentity { PartnerCode = "Whatever", PartnerOrderId = "Some Unrelated Id" }};
            var serialized  = JsonConvert.SerializeObject(order);
            
            mockStorage
                .Protected()
                .Setup<Task>("UploadTextAsBlobAsync", ItExpr.Is<string>(connection => connection == config.StorageConnectionString), 
                                                      ItExpr.Is<string>(container => container == config.PendingContainer), 
                                                      ItExpr.Is<string>(blob => ((blob.Contains(partner)) && (blob.Contains(orderId)))), 
                                                      ItExpr.Is<string>(content => content == serialized))
                .Returns(Task.CompletedTask)
                .Verifiable("The blob upload should have been requested");

            await mockStorage.Object.SavePendingOrderAsync(partner, orderId, order);
            mockStorage.VerifyAll();
        }

        /// <summary>
        ///  Verifies behavior of the <see cref="OrderSubmissionBlobStorage.SavePendingOrderAsync" />
        ///  method.
        /// </summary>
        /// 
        [Fact]
        public async Task SavePendingOrderAsyncReturnsARepresentativeBlobPath()
        {           
            var config = new OrderSubmissionBlobStorageConfiguration
            {
                StorageConnectionString = "hey, connect to storage!",
                PendingContainer        = "the place where stuff lives"
            };

            var mockStorage = new Mock<OrderSubmissionBlobStorage>(config, null) { CallBase = true };
            var partner     = "Henry";
            var orderId     = "ABD123";
            var order       = new CreateOrderMessage { Identity = new OrderIdentity { PartnerCode = "Jane", PartnerOrderId = "88888888" }};
            var serialized  = JsonConvert.SerializeObject(order);
            
            mockStorage
                .Protected()
                .Setup<Task>("UploadTextAsBlobAsync", ItExpr.Is<string>(connection => connection == config.StorageConnectionString), 
                                                      ItExpr.Is<string>(container => container == config.PendingContainer), 
                                                      ItExpr.Is<string>(blob => ((blob.Contains(partner)) && (blob.Contains(orderId)))), 
                                                      ItExpr.Is<string>(content => content == serialized))
                .Returns(Task.CompletedTask);

            var result = await mockStorage.Object.SavePendingOrderAsync(partner, orderId, order);
            result.Should().Contain(partner, "because the partner should be part of the blob key");
            result.Should().Contain(orderId, "because the order id should be part of the blob key");
        }

        /// <summary>
        ///  Verifies behavior of the <see cref="OrderSubmissionBlobStorage.SaveCompletedOrderAsync" />
        ///  method.
        /// </summary>
        /// 
        [Fact]
        public void SaveCompletedOrderAsyncVerifiesTheOrderIsPresent()
        {           
            var config = new OrderSubmissionBlobStorageConfiguration
            {
                StorageConnectionString = "hey, connect to storage!",
                CompletedContainer        = "the place where stuff lives"
            };

            var storage = new OrderSubmissionBlobStorage(config, null);

            Action actionUnderTest = () => storage.SaveCompletedOrderAsync(null).GetAwaiter().GetResult();
            actionUnderTest.ShouldThrow<ArgumentNullException>("because the order is required");
        }

        /// <summary>
        ///  Verifies behavior of the <see cref="OrderSubmissionBlobStorage.SaveCompletedOrderAsync" />
        ///  method.
        /// </summary>
        /// 
        [Fact]
        public void SaveCompletedOrderAsyncFailsWhenTheOrderIdentityIsNull()
        {           
            var config = new OrderSubmissionBlobStorageConfiguration
            {
                StorageConnectionString = "hey, connect to storage!",
                CompletedContainer        = "the place where stuff lives"
            };

            var order   = new CreateOrderMessage();
            var storage = new OrderSubmissionBlobStorage(config, null);

            Action actionUnderTest = () => storage.SaveCompletedOrderAsync(order).GetAwaiter().GetResult();
            actionUnderTest.ShouldThrow<ArgumentException>("because the order is required");
        }
        
        /// <summary>
        ///  Verifies behavior of the <see cref="OrderSubmissionBlobStorage.SaveCompletedOrderAsync" />
        ///  method.
        /// </summary>
        /// 
        [Fact]
        public void SaveCompletedOrderAsyncValidatesThePartnerCode()
        {           
            var config = new OrderSubmissionBlobStorageConfiguration
            {
                StorageConnectionString = "hey, connect to storage!",
                CompletedContainer        = "the place where stuff lives"
            };

            var order   = new CreateOrderMessage { Identity = new OrderIdentity { PartnerOrderId = "ABC123" }};
            var storage = new OrderSubmissionBlobStorage(config, null);

            Action actionUnderTest = () => storage.SaveCompletedOrderAsync(order).GetAwaiter().GetResult();
            actionUnderTest.ShouldThrow<ArgumentException>("because the partner is required").And.ParamName.Should().Be(nameof(order.Identity.PartnerCode));
        }

        /// <summary>
        ///  Verifies behavior of the <see cref="OrderSubmissionBlobStorage.SaveCompletedOrderAsync" />
        ///  method.
        /// </summary>
        /// 
        [Fact]
        public void SaveCompletedOrderAsyncValidatesTheOrderId()
        {           
            var config = new OrderSubmissionBlobStorageConfiguration
            {
                StorageConnectionString = "hey, connect to storage!",
                CompletedContainer        = "the place where stuff lives"
            };

            var order   = new CreateOrderMessage { Identity = new OrderIdentity { PartnerCode = "ABC123" }};
            var storage = new OrderSubmissionBlobStorage(config, null);

            Action actionUnderTest = () => storage.SaveCompletedOrderAsync(order).GetAwaiter().GetResult();
            actionUnderTest.ShouldThrow<ArgumentException>("because the order id is required").And.ParamName.Should().Be(nameof(order.Identity.PartnerOrderId));
        }

        /// <summary>
        ///  Verifies behavior of the <see cref="OrderSubmissionBlobStorage.SaveCompletedOrderAsync" />
        ///  method.
        /// </summary>
        /// 
        [Fact]
        public async Task SaveCompletedOrderAsyncAttemptsToUploadTheBlob()
        {           
            var config = new OrderSubmissionBlobStorageConfiguration
            {
                StorageConnectionString = "hey, connect to storage!",
                CompletedContainer      = "the place where stuff lives"
            };

            var mockStorage = new Mock<OrderSubmissionBlobStorage>(config, null) { CallBase = true };
            var partner     = "Henry";
            var orderId     = "ABD123";
            var order       = new CreateOrderMessage { Identity = new OrderIdentity { PartnerCode = partner, PartnerOrderId = orderId }};
            var serialized  = JsonConvert.SerializeObject(order);
            
            mockStorage
                .Protected()
                .Setup<Task>("UploadTextAsBlobAsync", ItExpr.Is<string>(connection => connection == config.StorageConnectionString), 
                                                      ItExpr.Is<string>(container => container == config.CompletedContainer), 
                                                      ItExpr.Is<string>(blob => ((blob.Contains(partner)) && (blob.Contains(orderId)))), 
                                                      ItExpr.Is<string>(content => content == serialized))
                .Returns(Task.CompletedTask)
                .Verifiable("The blob upload should have been requested");

            await mockStorage.Object.SaveCompletedOrderAsync(order);
            mockStorage.VerifyAll();
        }

        /// <summary>
        ///  Verifies behavior of the <see cref="OrderSubmissionBlobStorage.SaveCompletedOrderAsync" />
        ///  method.
        /// </summary>
        /// 
        [Fact]
        public async Task SaveCompletedOrderAsyncReturnsARepresentativeBlobPath()
        {           
            var config = new OrderSubmissionBlobStorageConfiguration
            {
                StorageConnectionString = "hey, connect to storage!",
                CompletedContainer        = "the place where stuff lives"
            };

            var mockStorage = new Mock<OrderSubmissionBlobStorage>(config, null) { CallBase = true };
            var partner     = "Henry";
            var orderId     = "ABD123";
            var order       = new CreateOrderMessage { Identity = new OrderIdentity { PartnerCode = partner, PartnerOrderId = orderId }};
            var serialized  = JsonConvert.SerializeObject(order);
            
            mockStorage
                .Protected()
                .Setup<Task>("UploadTextAsBlobAsync", ItExpr.Is<string>(connection => connection == config.StorageConnectionString), 
                                                      ItExpr.Is<string>(container => container == config.CompletedContainer), 
                                                      ItExpr.Is<string>(blob => ((blob.Contains(partner)) && (blob.Contains(orderId)))), 
                                                      ItExpr.Is<string>(content => content == serialized))
                .Returns(Task.CompletedTask);

            var result = await mockStorage.Object.SaveCompletedOrderAsync(order);
            result.Should().Contain(partner, "because the partner should be part of the blob key");
            result.Should().Contain(orderId, "because the order id should be part of the blob key");
        }
    }
}
