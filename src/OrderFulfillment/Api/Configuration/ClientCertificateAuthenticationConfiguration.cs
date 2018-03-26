using OrderFulfillment.Core.Configuration;

namespace OrderFulfillment.Api.Configuration
{
    /// <summary>
    ///   The set of configuration needed for authentication of a calllers
    ///   using the client certificate security scheme.
    /// </summary>
    /// 
    public class ClientCertificateAuthenticationConfiguration : IConfiguration
    {        
        /// <summary>
        ///   Indicates whether or not the handler is enabled.
        /// </summary>
        /// 
        /// <value><c>true</c> if the handler is enabled; otherwise, <c>false</c>.</value>
        /// 
        public bool Enabled { get;  set; }

        /// <summary>
        ///   Indicates whether or not certificate validation is enforced for
        ///   certificates retrieved from the local certificate store.
        /// </summary>
        /// 
        /// <value><c>true</c> if the validation is enforced; otherwise, <c>false</c>.</value>
        /// 
        public bool EnforceLocalCertificateValidation { get;  set; }

        /// <summary>
        ///   The serialized mapping for client certificate thumbprints to a set of claims.
        /// </summary>
        /// 
        /// <value>
        ///   The format of this serialization is expected to be parsable by the <see cref="OrderFulfillment.Api.Security.ClientCertificateClaimsMap.Deserialize" /> 
        ///   method.
        /// </value>
        /// 
        public string SerializedCertificateClaimsMapping { get;  set; }
    }
}