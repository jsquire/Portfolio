namespace OrderFulfillment.Api.Security
{
    /// <summary>
    ///   The set of authentication requirements that may be required when 
    ///   accessing a URI. 
    /// </summary>
    ///  
    public enum AuthenticationType
    {
        /// <summary>The authentication type is unknown and, therefore, invalid.</summary>
        Unknown,

        /// <summary>No credentials are authentication is required; anyone may access the resource.</summary>
        Anonymous,

        /// <summary>Callers are expected to provide a "secret" string that is shared among all callers.</summary>
        SharedSecret,

        /// <summary>Callers are expected to provide a trusted client certificate to access the resource.</summary>
        ClientCertificate,

        /// <summary>Callers are expected to provide a token to access the resource.</summary>
        Token
    }
}