using System;
using System.Threading.Tasks;
using OrderFulfillment.Core.Exceptions;
using OrderFulfillment.Core.Storage;
using OrderFulfillment.OrderProcessor.Configuration;
using OrderFulfillment.OrderProcessor.Models;
using RazorEngine.Configuration;
using RazorEngine.Templating;

namespace OrderFulfillment.OrderProcessor.Infrastructure
{
    /// <summary>
    ///   Provides functionality for consuming and processing metadata 
    ///   related to SKUs.
    /// </summary>
    /// 
    /// <seealso cref="ISkuMetadataProcessor" />
    /// 
    public class SkuMetadataProcessor : ISkuMetadataProcessor
    {
        /// <summary>The provider of SKU metadata information from storage.</summary>
        private readonly ISkuMetadataStorage metadataStorage;

        /// <summary>The configuration to use for influencing procesing behavior.</summary>
        private readonly SkuMetadataProcessorConfiguration configuration;

        /// <summary>The rendering engine to use for materializing templates.</summary>
        private readonly IRazorEngineService renderingEngine;

        /// <summary>
        ///   Initializes a new instance of the <see cref="SkuMetadataProcessor"/> class.
        /// </summary>
        /// 
        /// <param name="configuration">The configuration to use for influencing procesing behavior.</param>
        /// <param name="metadataStorage"he provider of SKU metadata information from storage.</param>
        /// 
        public SkuMetadataProcessor(SkuMetadataProcessorConfiguration configuration,
                                    ISkuMetadataStorage               metadataStorage)
        {
            this.configuration   = configuration   ?? throw new ArgumentNullException(nameof(configuration));
            this.metadataStorage = metadataStorage ?? throw new ArgumentNullException(nameof(metadataStorage));

            // Initialize the Razor rendering engine.

            var razorConfig = new TemplateServiceConfiguration
            {
               DisableTempFileLocking = true,
               CachingProvider        = new DefaultCachingProvider(_ => {})                
            };

            this.renderingEngine = RazorEngineService.Create(razorConfig);
        }

        /// <summary>
        ///   Renders an order item template fragment for the given metadata.
        /// </summary>
        /// 
        /// <param name="metadata">The metadata that describes the desired template rendering.</param>
        /// 
        /// <returns>The rendered order line item fragment.</returns>
        ///
        public async Task<string> RenderOrderTemplateAsync(OrderTemplateMetadata metadata) 
        {
            if (metadata == null)
            {
                throw new ArgumentNullException(nameof(metadata));
            }

            if (string.IsNullOrEmpty(metadata.Sku))
            {
                throw new ArgumentException("The SKU must be provided", nameof(metadata.Sku));
            }

            var storageResult = await this.metadataStorage.TryRetrieveSkuMetadataAsync(metadata.Sku);

            if (!storageResult.Found)
            {
                throw new MissingDependencyException("The sku metadata could not be located");
            }

            // NOTE: Razor Engine is unable to handle the same template name/key used with different templates
            //       so we use the hash to prevent it from throwing. It may leak a bit but it should be fine.
            //       Because this is all in process the string hash code should be good enough.
            var templateKey = $"{metadata.Sku}_{(storageResult.Metadata?.GetHashCode() ?? 0):X}";

            return this.renderingEngine.RunCompile(storageResult.Metadata, templateKey, typeof(OrderTemplateMetadata), metadata);
        }

        /// <summary>
        ///   Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// 
        public void Dispose() => 
            this.renderingEngine?.Dispose();
    }
}
