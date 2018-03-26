using OrderFulfillment.Core.Configuration;

namespace OrderFulfillment.Api.Configuration
{
    /// <summary>
    ///   The set of configuration needed for evaluating the authorization policy
    ///   that requires an SSL for non-local requests.
    /// </summary>
    /// 
    public class RequireSslAuthorizationPolicyConfiguration : IConfiguration
    {       
        /// <summary>
        ///   Indicates whether or not the handler is enabled.
        /// </summary>
        /// 
        /// <value><c>true</c> if the handler is enabled; otherwise, <c>false</c>.</value>
        /// 
        public bool Enabled { get;  set; }

        /// <summary>
        ///   Indicates whether or not an exception to the policy is allowed for
        ///   loopback (localhost) addresses.
        /// </summary>
        /// 
        /// <value>
        ///   <c>true</c> if the exception is granted; otherwise, <c>false</c>.
        /// </value>
        /// 
        public bool AllowLoopbackException { get;  set; }
    }
}