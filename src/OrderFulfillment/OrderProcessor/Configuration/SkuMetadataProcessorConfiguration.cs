using OrderFulfillment.Core.Configuration;

namespace OrderFulfillment.OrderProcessor.Configuration
{
    /// <summary>
    ///   The set of configuration needed for processing SKU metadata
    /// </summary>
    /// 
    public class SkuMetadataProcessorConfiguration : IConfiguration
    {
        /// <summary>
        ///   The duration, in minutes, that the SKU metadata may be cached for.
        /// </summary>
        /// 
        public int MetadataCacheDurationMinutes { get;  set; }
    }
}
