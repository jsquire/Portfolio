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

namespace OrderFulfillment.Core.Tests.Storage
{
    /// <summary>
    ///   The suite of tests for the <see cref="SkuMetadataBlobStorage" />
    ///   class.
    /// </summary>
    /// 
    public class SkuMetadataBlobStorageTests
    {
        /// <summary>
        ///   Verifies behavior of the constructor.
        /// </summary>
        /// 
        [Fact]
        public void ConstructorValidatesConfiguration()
        {
            Action actionUnderTest = () => new SkuMetadataBlobStorage(null);
            actionUnderTest.ShouldThrow<ArgumentNullException>("because the configuration is required");
        }

        /// <summary>
        ///  Verifies behavior of the <see cref="SkuMetadataBlobStorage.TryRetrieveSkuMetadataAsync" />
        ///  method.
        /// </summary>
        /// 
        [Fact]
        public async Task TryRetrieveSkuMetadataReturnsBlobContentWhenFound()
        {           
            var config = new SkuMetadataBlobStorageConfiguration
            {
                StorageConnectionString = "hey, connect to storage!",
                Container               = "the place where stuff lives"
            };

            var mockStorage = new Mock<SkuMetadataBlobStorage>(config) { CallBase = true };
            var blobName    = "Henry";
            var content     = "Some stuffs";
            
            mockStorage
                .Protected()
                .Setup<Task<(bool, string)>>("RetrieveBlobAsTextAsync", ItExpr.Is<string>(connection => connection == config.StorageConnectionString), ItExpr.Is<string>(container => container == config.Container), ItExpr.Is<string>(sku => sku == blobName))
                .Returns(Task.FromResult((true, content)))
                .Verifiable("The blob text should have been requested");

            var result = await mockStorage.Object.TryRetrieveSkuMetadataAsync(blobName);

            result.Found.Should().BeTrue("because the content was returned");
            result.Metadata.Should().Be(content, "because the content should have been retrieved");

            mockStorage.VerifyAll();
        }

        /// <summary>
        ///  Verifies behavior of the <see cref="SkuMetadataBlobStorage.TryRetrieveSkuMetadataAsync" />
        ///  method.
        /// </summary>
        /// 
        [Fact]
        public async Task TryRetrieveSkuMetadataReturnsNoBlobContentWhenNotFound()
        {           
            var config = new SkuMetadataBlobStorageConfiguration
            {
                StorageConnectionString = "hey, connect to storage!",
                Container               = "the place where stuff lives"
            };

            var mockStorage = new Mock<SkuMetadataBlobStorage>(config) { CallBase = true };
            var blobName    = "Henry";
            
            mockStorage
                .Protected()
                .Setup<Task<(bool, string)>>("RetrieveBlobAsTextAsync", ItExpr.Is<string>(connection => connection == config.StorageConnectionString), ItExpr.Is<string>(container => container == config.Container), ItExpr.Is<string>(sku => sku == blobName))
                .Returns(Task.FromResult<(bool, string)>((false, null)))
                .Verifiable("The blob text should have been requested");

            var result = await mockStorage.Object.TryRetrieveSkuMetadataAsync(blobName);

            result.Found.Should().BeFalse("because no content was fount");
            result.Metadata.Should().BeNull("because no content swas returned");

            mockStorage.VerifyAll();
        }
    }
}
