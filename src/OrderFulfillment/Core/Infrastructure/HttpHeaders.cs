namespace OrderFulfillment.Core.Infrastructure
{
    /// <summary>
    ///   Serves as a pseudo-enum for the well-known set of HTTP headers understood by the
    ///   application and provides functionality to work with them.
    /// </summary>
    /// 
    /// <remarks>
    ///   Custom headers specific to order fulfillment should use the prefix "ORD-" to group them and disambiguate their 
    ///   origin.  Note that the X- prefix is not used, as it is no longer recommended by the IETF.
    ///   
    ///   Please see RFC 6648 (http://tools.ietf.org/html/rfc6648) for additional context.
    /// </remarks>
    /// 
    public static class HttpHeaders
    {
        /// <summary>The header that indicates the caller is passing data for proof of authentication/authorization.</summary>
        public const string Authorization = "Authorization";

        /// <summary>The header used for pasing an application key for authentication/authorization schemes using a shared secret.</summary>
        public const string ApplicationKey = "ORD-AppKey";

        /// <summary>The header used for pasing an application secret for authentication/authorization schemes using a shared secret.</summary>
        public const string ApplicationSecret = "ORD-AppSecret";

        /// <summary>The header used for transport of the request's correlation identifer; used in both request and response messages.</summary>
        public const string CorrelationId = "ORD-Correlation";

        /// <summary>The default header used for setting the operation identifier for Application Insights.</summary>
        public const string DefaultApplicationInsightsOperationId = "X-Operation-Id";

        /// <summary>The header used for expressing details of an exception that was observed during the request.</summary>
        public const string ExceptionDetails = "ORD-Exception";
    }
}
