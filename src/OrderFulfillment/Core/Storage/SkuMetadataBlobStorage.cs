using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using OrderFulfillment.Core.Configuration;
using OrderFulfillment.Core.Exceptions;

namespace OrderFulfillment.Core.Storage
{
    /// <summary>
    ///   Provides storage operations for SKU metadata held in Azure Blob storage.
    /// </summary>
    /// 
    /// <seealso cref="ISkuMetadataStorage" />
    /// 
    public class SkuMetadataBlobStorage : ISkuMetadataStorage
    {
        /// <summary>The configuration to use for storage operations.</summary>
        private readonly SkuMetadataBlobStorageConfiguration configuration;

        /// <summary>
        ///   Initializes a new instance of the <see cref="SkuMetadataBlobStorage"/> class.
        /// </summary>
        /// 
        /// <param name="configuration">The configuration to use for storage operations.</param>
        /// 
        public SkuMetadataBlobStorage(SkuMetadataBlobStorageConfiguration configuration)
        {
            this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        /// <summary>
        ///   Retrieves the metadata for the specified SKU.
        /// </summary>
        /// 
        /// <param name="sku">The SKU to retrieve metadata for.</param>
        /// 
        /// <returns>A tuple indicating whether or not the metadata was found in storage and, if so, the metadata.
        /// 
        public async Task<(bool Found, string Metadata)> TryRetrieveSkuMetadataAsync(string sku) =>
            (await this.RetrieveBlobAsTextAsync(this.configuration.StorageConnectionString, this.configuration.Container, sku));

        /// <summary>
        ///   Retrieves a block blob as a text string, if it exists.
        /// </summary>
        /// 
        /// <param name="connectionString">The connection string to the storage account.</param>
        /// <param name="containerPath">The path to the blob container.</param>
        /// <param name="blobName">Name of the blob to retrieve.</param>
        /// 
        /// <returns>A tuple indicating whether the blob exists and what the textual contents of it were.</returns>
        ///
        protected virtual async Task<(bool, string)> RetrieveBlobAsTextAsync(string connectionString,
                                                                             string containerPath,
                                                                             string blobName)
        {
            if (!CloudStorageAccount.TryParse(connectionString, out var account))
            {
                throw new InvalidConnectionStringException("The connection string for Sku Metadata storage could not be parsed");
            }

            var blobClient = account.CreateCloudBlobClient();
            var container  = blobClient.GetContainerReference(containerPath);
            var blob       = container.GetBlockBlobReference(blobName);

            try
            {
                var result = await blob.DownloadTextAsync();
                return (true, result);
            }
            catch (StorageException storageEx) when (storageEx.RequestInformation?.HttpStatusCode == (int)HttpStatusCode.NotFound)
            {
                return (false, null);
            }
        }
    }
}
