using OrderFulfillment.Core.Configuration;

namespace OrderFulfillment.Api.Configuration
{
    /// <summary>
    ///   The set of configuration needed for evaluating the authorization policy
    ///   that ensures that the caller may perform priviledged operations.
    /// </summary>
    /// 
    public class PriviledgedOperationAuthorizationPolicyConfiguration : IConfiguration
    {
        /// <summary>
        ///   Indicates whether or not the handler is enabled.
        /// </summary>
        /// 
        /// <value><c>true</c> if the handler is enabled; otherwise, <c>false</c>.</value>
        /// 
        public bool Enabled { get;  set; }   
    }
}