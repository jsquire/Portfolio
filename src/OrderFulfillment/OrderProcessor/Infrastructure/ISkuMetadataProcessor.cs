using System;
using System.Threading.Tasks;
using OrderFulfillment.OrderProcessor.Models;

namespace OrderFulfillment.OrderProcessor.Infrastructure
{
    /// <summary>
    ///   Defines the contract to be filled by processors of SKU metadata
    /// </summary>
    /// 
    public interface ISkuMetadataProcessor : IDisposable
    {
        /// <summary>
        ///   Renders an order template fragment for the given metadata.
        /// </summary>
        /// 
        /// <param name="metadata">The metadata that describes the desired order template rendering.</param>
        /// 
        /// <returns>The rendered order line item fragment.</returns>
        /// 
        Task<string> RenderOrderTemplateAsync(OrderTemplateMetadata metadata); 
    }
}
