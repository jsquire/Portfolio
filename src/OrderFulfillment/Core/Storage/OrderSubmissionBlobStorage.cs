using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using OrderFulfillment.Core.Configuration;
using OrderFulfillment.Core.Exceptions;
using OrderFulfillment.Core.Models.External.OrderProduction;
using Newtonsoft.Json;

namespace OrderFulfillment.Core.Storage
{
    /// <summary>
    ///   rovides storage operations for orders held in Azure Blob storage.
    /// </summary>
    /// 
    /// <seealso cref="IOrderStorage" />
    /// 
    public class OrderSubmissionBlobStorage : IOrderStorage
    {
        /// <summary>The configuration to use for storage operations.</summary>
        private readonly OrderSubmissionBlobStorageConfiguration configuration;

        /// <summary>The settings to use for JSON serialization.</summary>
        private readonly JsonSerializerSettings serializerSettings;

        /// <summary>
        ///   Initializes a new instance of the <see cref="OrderSubmissionBlobStorage"/> class.
        /// </summary>
        /// 
        /// <param name="configuration">The configuration to use for storage operations.</param>
        /// <param name="serializerSettings">The settings to use for JSON serialization.</param>
        /// 
        public OrderSubmissionBlobStorage(OrderSubmissionBlobStorageConfiguration configuration,
                                          JsonSerializerSettings                  serializerSettings)
        {
            this.configuration      = configuration  ?? throw new ArgumentNullException(nameof(configuration));
            this.serializerSettings = serializerSettings;
        }

        /// <summary>
        ///   Retrieves the pending order for the partner/orderId combination.
        /// </summary>
        /// 
        /// <param name="partner">The code of the partner assocaited with the order.</param>
        /// <param name="orderId">The unique identifier of the desired order.</param>
        /// 
        /// <returns>A tuple indicating whether or not the order was found in storage and, if so, the order.
        /// 
        public async Task<(bool Found, CreateOrderMessage Order)> TryRetrievePendingOrderAsync(string partner, string orderId)
        {
            var blobText = await this.RetrieveBlobAsTextAsync(this.configuration.StorageConnectionString, 
                                                              this.configuration.PendingContainer, 
                                                              this.FormatPendingOrderBlobName(partner, orderId));

            if (!blobText.Found)
            {
                return (false, null);
            }

            return (true, JsonConvert.DeserializeObject<CreateOrderMessage>(blobText.Content, this.serializerSettings));
        }

        /// <summary>
        ///   Deletes the pending order for the partner/orderId combination.
        /// </summary>
        /// 
        /// <param name="partner">The code of the partner assocaited with the order.</param>
        /// <param name="orderId">The unique identifier of the desired order.</param>
        ///
        public async Task DeletePendingOrderAsync(string partner, string orderId)
        {
            await this.DeleteBlobAsync(this.configuration.StorageConnectionString, 
                                       this.configuration.PendingContainer, 
                                       this.FormatPendingOrderBlobName(partner, orderId));
        }

        /// <summary>
        ///   Saves an order that is pending submission.
        /// </summary>
        /// 
        /// <param name="partner">The code of the partner assocaited with the order.</param>
        /// <param name="orderId">The unique identifier of the desired order.</param>
        /// <param name="order">The order data to save.</param>      
        /// 
        /// <returns>The key for the order in storage.</returns>
        ///
        public async Task<string> SavePendingOrderAsync(string partner, string orderId, CreateOrderMessage order)
        {
            if (partner == null)
            {
                throw new ArgumentNullException(nameof(partner));
            }
            if (String.IsNullOrEmpty(partner))
            {
                throw new ArgumentException("The order partner code is required", nameof(partner));
            }
            if (orderId == null)
            {
                throw new ArgumentNullException(nameof(orderId));
            }
            if (String.IsNullOrEmpty(orderId))
            {
                throw new ArgumentException("The order identifier is required", nameof(orderId));
            }
            if (order == null)
            {
                throw new ArgumentNullException(nameof(order));
            }

            var blobName = this.FormatPendingOrderBlobName(partner, orderId);

            await this.UploadTextAsBlobAsync(this.configuration.StorageConnectionString,
                                             this.configuration.PendingContainer,
                                             blobName,                                             
                                             JsonConvert.SerializeObject(order, this.serializerSettings));

            return blobName;
        }

        /// <summary>
        ///   Saves an order that as completed submission.
        /// </summary>
        /// 
        /// <param name="order">The order to save.</param>       
        /// 
        /// <returns>The key for the order in storage.</returns>
        /// 
        public async Task<string> SaveCompletedOrderAsync(CreateOrderMessage order)
        {
            if (order == null)
            {
                throw new ArgumentNullException(nameof(order));
            }

            var identity = order.Identity;
            
            if (String.IsNullOrEmpty(identity?.PartnerCode))
            {
                throw new ArgumentException("The order partner code is required", nameof(order.Identity.PartnerCode));
            }
            
            if (String.IsNullOrEmpty(identity?.PartnerOrderId))
            {
                throw new ArgumentException("The order identifier is required", nameof(order.Identity.PartnerOrderId));
            }
            
            var blobName = this.FormatCompletedOrderBlobName(identity.PartnerCode, identity.PartnerOrderId);

            await this.UploadTextAsBlobAsync(this.configuration.StorageConnectionString,
                                             this.configuration.CompletedContainer,
                                             blobName,
                                             JsonConvert.SerializeObject(order, this.serializerSettings));
            return blobName;
        }

        /// <summary>
        ///   Formats the name of a pending order blob from its composite pieces.
        /// </summary>
        /// 
        /// <param name="partner">The partner associated with the order.</param>
        /// <param name="orderId">The unique identifier of the order.</param>
        /// 
        /// <returns>The name of the blob that corresponds to the specified order</returns>
        /// 
        private string FormatPendingOrderBlobName(string partner, 
                                                  string orderId) => $"{ partner }\\{ orderId }";

        /// <summary>
        ///   Formats the name of a completed order blob from its composite pieces.
        /// </summary>
        /// 
        /// <param name="partner">The partner associated with the order.</param>
        /// <param name="orderId">The unique identifier of the order.</param>
        /// 
        /// <returns>The name of the blob that corresponds to the specified order</returns>
        /// 
        private string FormatCompletedOrderBlobName(string partner, 
                                                    string orderId) => $"{ partner }\\{ orderId }";


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
        protected virtual async Task<(bool Found, string Content)> RetrieveBlobAsTextAsync(string connectionString,
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

        /// <summary>
        ///   Deletes a block blob, if it exists.
        /// </summary>
        /// 
        /// <param name="connectionString">The connection string to the storage account.</param>
        /// <param name="containerPath">The path to the blob container.</param>
        /// <param name="blobName">Name of the blob to delete.</param>
        /// 
        protected virtual async Task DeleteBlobAsync(string connectionString,
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
                                    
            await blob.DeleteIfExistsAsync();
        }

        /// <summary>
        ///   Uploads text as a block blob.
        /// </summary>
        /// 
        /// <param name="connectionString">The connection string to the storage account.</param>
        /// <param name="containerPath">The path to the blob container.</param>
        /// <param name="blobName">Name of the blob to write.</param>
        ///
        protected virtual async Task UploadTextAsBlobAsync(string connectionString,
                                                           string containerPath,
                                                           string blobName,
                                                           string content)
        {
            if (!CloudStorageAccount.TryParse(connectionString, out var account))
            {
                throw new InvalidConnectionStringException("The connection string for Sku Metadata storage could not be parsed");
            }

            var blobClient = account.CreateCloudBlobClient();
            var container  = blobClient.GetContainerReference(containerPath);

            container.CreateIfNotExists();

            await container
                     .GetBlockBlobReference(blobName)
                     .UploadTextAsync(content);
        }
    }
}
