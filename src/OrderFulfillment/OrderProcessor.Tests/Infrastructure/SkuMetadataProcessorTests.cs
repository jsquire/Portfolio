using System;
using System.Threading.Tasks;
using FluentAssertions;
using OrderFulfillment.Core.Exceptions;
using OrderFulfillment.Core.Storage;
using OrderFulfillment.OrderProcessor.Configuration;
using OrderFulfillment.OrderProcessor.Infrastructure;
using OrderFulfillment.OrderProcessor.Models;
using Moq;
using Xunit;

namespace OrderFulfillment.OrderProcessor.Tests.Infrastructure
{
    /// <summary>
    ///   The suite of tests for the <see cref="SkuMetadataProcessor" />
    ///   class.
    /// </summary>
    public class SkuMetadataProcessorTests
    {
        /// <summary>
        ///  Verifies behavior of the constructor.
        /// </summary>
        /// 
        [Fact]
        public void ConstructorValidatesConfiguration()
        {
            Action actionUnderTest = () => new SkuMetadataProcessor(null, Mock.Of<ISkuMetadataStorage>());
            actionUnderTest.ShouldThrow<ArgumentNullException>("because the configuration should be required");
        }

        /// <summary>
        ///  Verifies behavior of the constructor.
        /// </summary>
        /// 
        [Fact]
        public void ConstructorValidatesStorage()
        {
            Action actionUnderTest = () => new SkuMetadataProcessor(new SkuMetadataProcessorConfiguration(), null);
            actionUnderTest.ShouldThrow<ArgumentNullException>("because the metadata storage should be required");
        }

        /// <summary>
        ///  Verifies functionality of the <see cref="SkuMetadataProcessor.RenderOrderTemplateAsync"/>
        /// </summary>
        /// 
        [Fact]
        public void RenderOrderTemplateAsyncValidatesTheMetadata()
        {
            using (var processor = new SkuMetadataProcessor(new SkuMetadataProcessorConfiguration(), Mock.Of<ISkuMetadataStorage>()))
            {
                Action actionUnderTest = () => processor.RenderOrderTemplateAsync(null).GetAwaiter().GetResult();
                actionUnderTest.ShouldThrow<ArgumentNullException>("because the metadata is required");
            }
        }

        /// <summary>
        ///  Verifies functionality of the <see cref="SkuMetadataProcessor.RenderOrderTemplateAsync"/>
        /// </summary>
        /// 
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void RenderOrderTemplateAsyncValidatesTheSku(string sku)
        {            
            var metadata = new OrderTemplateMetadata { Sku = sku };

            using (var processor = new SkuMetadataProcessor(new SkuMetadataProcessorConfiguration(), Mock.Of<ISkuMetadataStorage>()))
            {
                Action actionUnderTest = () => processor.RenderOrderTemplateAsync(null).GetAwaiter().GetResult();
                actionUnderTest.ShouldThrow<ArgumentException>("because the sku is required");
            }
        }

        /// <summary>
        ///  Verifies functionality of the <see cref="SkuMetadataProcessor.RenderOrderTemplateAsync"/>
        /// </summary>
        /// 
        [Fact]
        public void RenderOrderTemplateAsyncAsyncThrowsForMissingStorageMetadata()
        {
            var mockStorage = new Mock<ISkuMetadataStorage>();            
            var metadata    = new OrderTemplateMetadata { Sku = "ABC" };

            mockStorage.Setup(storage => storage.TryRetrieveSkuMetadataAsync(It.IsAny<string>()))
                       .ReturnsAsync((false, null))
                       .Verifiable("The SKU metadata should have been requested from storage");

            using (var processor   = new SkuMetadataProcessor(new SkuMetadataProcessorConfiguration(), mockStorage.Object))
            {
                Action actionUnderTest = () => processor.RenderOrderTemplateAsync(metadata).GetAwaiter().GetResult();
                actionUnderTest.ShouldThrow<MissingDependencyException>("because the metadata in storage must be found");
            }

            mockStorage.VerifyAll();
        }

        /// <summary>
        ///  Verifies functionality of the <see cref="SkuMetadataProcessor.RenderOrderTemplateAsync"/>
        /// </summary>
        /// 
        [Fact]
        public async Task RenderOrderTemplateAsyncAsyncRendersFromTheTemplate()
        {
            // Arrange
            var metadata = new OrderTemplateMetadata
            {
                Sku = "ABC",
                LineItemCount = 4,
                AdditionalSheets = 3,
                AssetUrl = "www.google.com"
            };
            
            var template = "Sku: @Model.Sku | LineItemCount: @Model.LineItemCount | TotalSheets: @Model.TotalSheets | AdditionalSheets: @Model.AdditionalSheets | AssetUrl: @Model.AssetUrl";
            var expected = template
                .Replace("@Model.Sku", metadata.Sku)
                .Replace("@Model.LineItemCount", metadata.LineItemCount.ToString())
                .Replace("@Model.TotalSheets", metadata.TotalSheets.ToString())
                .Replace("@Model.AdditionalSheets", metadata.AdditionalSheets.ToString())
                .Replace("@Model.AssetUrl", metadata.AssetUrl);

            var mockStorage = new Mock<ISkuMetadataStorage>();
            mockStorage.Setup(storage => storage.TryRetrieveSkuMetadataAsync(It.IsAny<string>()))
                       .ReturnsAsync((true, template));

            using (var processor   = new SkuMetadataProcessor(new SkuMetadataProcessorConfiguration(), mockStorage.Object))
            {
                // Act
                var result = await processor.RenderOrderTemplateAsync(metadata);

                // Assert
                result.Should().NotBeNull("because a result should have been returned");
                result.Should().Be(expected, "because the template should have been randered");
            }
        }
    }
}
