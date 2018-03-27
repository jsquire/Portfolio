namespace OrderFulfillment.Api.Models.Requests
{
    /// <summary>
    ///   An sset associated with a line item of the order
    ///   being fulfilled.
    /// </summary>
    /// 
    public class ItemAsset
    {
        /// <summary>
        ///   The name of the asset.
        /// </summary>
        /// 
        public string Name { get;  set; }
                
        /// <summary>
        ///   The location for the details about the asset.
        /// </summary>
        /// 
        /// <value>
        ///   This should be a fully qualified URL to its location, including any security tokens needed to perform a GET request against it.
        /// </value>
        /// 
        public string Location { get;  set; }
    }
}