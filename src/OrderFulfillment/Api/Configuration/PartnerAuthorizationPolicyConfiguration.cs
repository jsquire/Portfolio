using OrderFulfillment.Core.Configuration;

namespace OrderFulfillment.Api.Configuration
{
    /// <summary>
    ///   The set of configuration needed for evaluating the authorization policy
    ///   that ensures that the caller of an endpoint has permission to interact with the partner
    ///   specified by the request.
    /// </summary>
    /// 
    public class PartnerAuthorizationPolicyConfiguration : IConfiguration
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