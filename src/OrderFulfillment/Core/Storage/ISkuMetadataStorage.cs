using System.Threading.Tasks;

namespace OrderFulfillment.Core.Storage
{
    /// <summary>
    ///   Defines the contract for storage providers of SKU metadata 
    ///   information.
    /// </summary>
    /// 
    public interface ISkuMetadataStorage
    {
        /// <summary>
        ///   Retrieves the metadata for the specified SKU.
        /// </summary>
        /// 
        /// <param name="sku">The SKU to retrieve metadata for.</param>
        /// 
        /// <returns>A tuple indicating whether or not the metadata was found in storage and, if so, the metadata.
        /// 
        Task<(bool Found, string Metadata)> TryRetrieveSkuMetadataAsync(string sku);
        
    }
}
