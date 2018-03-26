namespace OrderFulfillment.Core.Configuration
{
    /// <summary>
    ///   The set of configuration for pending order storage in Azure Blob.
    /// </summary>    
    /// 
    public class OrderSubmissionBlobStorageConfiguration : IConfiguration
    {
        /// <summary>
        ///   The connection string to the Azure Blob storage account.
        /// </summary>
        /// 
        public string StorageConnectionString { get;  set; }

        /// <summary>
        ///   The container in storage which houses the orders pending submission.
        /// </summary>
        /// 
        public string PendingContainer { get;  set; }

        /// <summary>
        ///   The container in storage which houses the orders that were sucessfully submitted.
        /// </summary>
        /// 
        public string CompletedContainer { get;  set; }
    }
}
