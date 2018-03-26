namespace OrderFulfillment.Core.Configuration
{
    /// <summary>
    ///   The set of configuration for the Sku Metadata storage in Azure Blob.
    /// </summary>    
    /// 
    public class SkuMetadataBlobStorageConfiguration : IConfiguration
    {
        /// <summary>
        ///   The connection string to the Azure Blob storage account.
        /// </summary>
        /// 
        public string StorageConnectionString { get;  set; }

        /// <summary>
        ///   The container in storage which houses the metadata.
        /// </summary>
        /// 
        public string Container { get;  set; }
    }
}
