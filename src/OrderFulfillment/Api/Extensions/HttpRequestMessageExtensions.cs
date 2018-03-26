using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using OrderFulfillment.Core.Infrastructure;

namespace OrderFulfillment.Api.Extensions
{
    /// <summary>
    ///   The set of extension methods for the <see cref="System.Net.Http.HttpRequestMessage" />
    ///   class.
    /// </summary>
    public static class HttpRequestMessageExtensions
    {
        /// <summary>
        ///   Gets the Order Fulfillment correlation identifier for a request.
        /// </summary>
        /// 
        /// <param name="instance">The instance that this method was invoked on.</param>
        /// 
        /// <returns>The correlation identifier associated with the request, if any.</returns>
        /// 
        public static string GetOrderFulfillmentCorrelationId(this HttpRequestMessage instance)
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            if ((instance.Headers.TryGetValues(HttpHeaders.CorrelationId, out var rawCorrelationIdValues)) && (rawCorrelationIdValues != null))
            {
                var correlationId = rawCorrelationIdValues.FirstOrDefault();

                if (correlationId != null)
                {
                    return correlationId;
                }
            }

            return instance.GetCorrelationId().ToString();
        }

        /// <summary>
        ///   Reads the content of the request message as a string.
        /// </summary>
        /// 
        /// <param name="instance">The instance that this method was invoked on.</param>
        /// 
        /// <returns>The content of the request message as a string.  If no content is available, an empty string is returned.</returns>
        /// 
        public static async Task<string> ReadContentAsStringAsync(this HttpRequestMessage instance)
        {
            if ((instance == null) || (instance.Content == null))
            {
                return String.Empty;
            }

            var contentBytes = await instance.Content.ReadAsByteArrayAsync();

            if ((contentBytes == null) || (contentBytes.Length == 0))
            {
                return String.Empty;
            }

            return Encoding.UTF8.GetString(contentBytes);
        }

        /// <summary>
        ///  Reads the content of the message as a string without allowing any exceptions
        ///  to bubble.
        /// </summary>
        /// 
        /// <param name="instance">The instance that this method was invoked on.</param>
        /// <param name="errorValue">The value to return if an exception is encountered while attempting to read.</param>
        /// 
        /// <returns>The content of the request message, if the read was successful; otherwise, the <paramref name="errorValue" /> provided.</returns>
        /// 
        public static async Task<string> SafeReadContentAsStringAsync(this HttpRequestMessage instance, 
                                                                           string             errorValue = "{ ERROR READING CONTENT }")
        {
            try
            {
                return await instance.ReadContentAsStringAsync();
            }

            catch
            {
                return errorValue;
            }
        }
    }
}