using System.Net;

namespace Squire.Toolbox
{
    /// <summary>
    ///   The set of extension methods for the <see cref="HttpStatusCode" />
    ///   enum.
    /// </summary>
    ///
    public static class HttpStatusCodeExtensions
    {
        /// <summary>Represents HTTP 429 (Rate Limit Exceeded), which isn't part of the oficial enumeration.</summary>
        internal const HttpStatusCode RateLimitExceeded = (HttpStatusCode)429;

        /// <summary>
        ///   Determines if the request associated with a given status code is
        ///   encouraged to be retried.
        /// </summary>
        ///
        /// <param name="instance">The instance that this method was invoked on.</param>
        ///
        /// <returns>The recommendation for whether or not the request should be retried.</returns>
        ///
        public static bool ShouldRetry(this HttpStatusCode instance)
        {
            switch (instance)
            {
                case HttpStatusCode.Unauthorized:
                case HttpStatusCode.RequestTimeout:
                case HttpStatusCode.InternalServerError:
                case HttpStatusCode.ServiceUnavailable:
                case HttpStatusCode.GatewayTimeout:
                case HttpStatusCodeExtensions.RateLimitExceeded:
                    return true;

                default:
                    return false;
            }
        }
    }
}
