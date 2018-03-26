namespace OrderFulfillment.Core.Configuration
{
    /// <summary>
    ///   The set of configuration for the oder production client.
    /// </summary>
    /// 
    public class OrderProductionClientConfiguration : IConfiguration
    {
        /// <summary>
        ///   The protocol to use when making requests to the service.
        /// </summary>
        /// 
        /// <remarks>
        ///   For most services, this will be either "https" or "http".   When
        ///   possible, it is highly recommended to use "https"
        /// </remarks>
        /// 
        public string RequestProtocol { get;  set; }

        /// <summary>
        ///   The host address for the root of the service.
        /// </summary>
        /// 
        public string ServiceHostAddress { get;  set; }

        /// <summary>
        ///   The URL template for the endpoint to which an order is sent to be produced.
        /// </summary>
        /// 
        public string CreateOrderUrlTemplate { get;  set; }

        /// <summary>
        ///   The thumbprint of the client certificate to send with the request for 
        ///   authentication.
        /// </summary>
        /// 
        public string ClientCertificateThumbprint { get;  set; }


        /// <summary>
        ///   A JSON-serialized set of headers that should be included with each request.
        /// </summary>
        /// 
        /// <example>
        ///   <code>
        ///       {
        ///           "Authorization" : "SharedSecret secret=rosebud",
        ///           "MIM-Special"   : "Some Value"
        ///       }
        ///   </code>
        /// </example>
        public string StaticHeadersJson {get;  set; }

        /// <summary>
        ///   The timeout period, in seconds, for the underlying connection lease.
        /// </summary>
        /// 
        public int ConnectionLeaseTimeoutSeconds { get;  set; }

        /// <summary>
        ///   The timeout period, in seconds, for any requests made to the
        ///   eCommerce service.
        /// </summary>
        /// 
        public int RequestTimeoutSeconds { get;  set; }

        /// <summary>
        ///   The maximum number of retries for a given request.
        /// </summary>
        /// 
        public int RetryMaxCount { get;  set; }

        /// <summary>
        ///   The base of the exponential backoff, in seconds.
        /// </summary>
        /// 
        public int RetryExponentialSeconds { get;  set; }
    }
}
