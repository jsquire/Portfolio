using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using OrderFulfillment.Core.Configuration;
using OrderFulfillment.Core.Extensions;
using OrderFulfillment.Core.Infrastructure;
using OrderFulfillment.Core.Models.Operations;
using Newtonsoft.Json;

namespace OrderFulfillment.Core.External
{
    /// <summary>
    ///   Serves as a communications client for interactions with the eCommerce service.
    /// </summary>
    /// 
    /// <seealso cref="IEcommerceClient" />
    /// 
    public class EcommerceClient : ExternalClientBase, IEcommerceClient
    {
        /// <summary>The HTTP client to use for interacting with the external service.</summary>
        private readonly Lazy<HttpClient> httpClient;

        /// <summary>The set of static headers to send with requests.</summary>
        private readonly Lazy<Dictionary<string, string>> staticHeaders;

        /// <summary>The settings to use for JSON serialization.</summary>
        private readonly JsonSerializerSettings serializerSettings; 

        /// <summary>The configuration for the behaviors of the client.</summary>
        private readonly EcommerceClientConfiguration configuration;

        /// <summary>
        ///   Initializes a new instance of the <see cref="EcommerceClient"/> class.
        /// </summary>
        /// 
        /// <param name="configuration">The configuration to use for the client.</param>
        /// 
        public EcommerceClient(EcommerceClientConfiguration configuration,
                               JsonSerializerSettings       serializerSettings)
        {
            this.configuration      = configuration ?? throw new ArgumentNullException(nameof(configuration));
            this.serializerSettings = serializerSettings;

            this.httpClient = new Lazy<HttpClient>( () => this.CreateHttpClient(configuration.RequestProtocol, 
                                                                                configuration.ServiceHostAddress, 
                                                                                configuration.ClientCertificateThumbprint, 
                                                                                configuration.RequestTimeoutSeconds, 
                                                                                configuration.ConnectionLeaseTimeoutSeconds), 
                LazyThreadSafetyMode.PublicationOnly);

             this.staticHeaders = new Lazy<Dictionary<string, string>>( () => JsonConvert.DeserializeObject<Dictionary<string, string>>(this.configuration.StaticHeadersJson ?? String.Empty, this.serializerSettings) ?? new Dictionary<string, string>(),
                LazyThreadSafetyMode.PublicationOnly);
        }

        /// <summary>
        ///   Queries the details of an order from the eCommerce system.
        /// </summary>
        /// 
        /// <param name="orderId">The unique identifier of the order to query for.</param>
        /// <param name="correlationId">The correlation identifier to associate with the request.  If <c>null</c>, no correlation will be sent.</param>
        /// 
        /// <returns>The result of the operation.</returns>
        /// 
        public async Task<OperationResult> GetOrderDetailsAsync(string orderId,
                                                                string correlationId = null)
        {
            var timeout    = TimeSpan.FromSeconds(this.configuration.RequestTimeoutSeconds);
            var requestUrl = this.configuration.GetOrderUrlTemplate.Replace("{order}", orderId);            
            
            using (var request = new HttpRequestMessage(HttpMethod.Get, requestUrl))
            {
                if (correlationId != null)
                {
                    request.Headers.TryAddWithoutValidation(HttpHeaders.CorrelationId, correlationId);
                    request.Headers.TryAddWithoutValidation(HttpHeaders.DefaultApplicationInsightsOperationId, correlationId);
                }

                foreach (var pair in this.staticHeaders.Value)
                {
                    request.Headers.TryAddWithoutValidation(pair.Key, pair.Value);                 
                }
            
                using (var response = await this.SendRequestAsync(request, timeout))
                {
                    return new OperationResult
                    {
                        Outcome     = (response.IsSuccessStatusCode) ? Outcome.Success : Outcome.Failure,
                        Reason      = response.StatusCode.ToString(),
                        Recoverable = response.StatusCode.IsRetryEncouraged(),
                        Payload     = (response.Content == null) ? null : (await response.Content.ReadAsStringAsync())
                    };
                }
            }
        }

        /// <summary>
        ///   Asynchronously sends an HTTP request.
        /// </summary>
        /// 
        /// <param name="request">The request to send.</param>
        /// <param name="timeout">The desired timeout for the request.</param>
        /// 
        /// <returns>The response received from the request recpient.</returns>
        /// 
        protected virtual Task<HttpResponseMessage> SendRequestAsync(HttpRequestMessage request, TimeSpan timeout) =>
             this.httpClient.Value.SendAsync(request).WithTimeout(timeout);

        /// <summary>
        ///   Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// 
        public void Dispose()
        {
            if (this.httpClient.IsValueCreated)
            {
                this.httpClient.Value?.Dispose();
            }
        }
    }
}
