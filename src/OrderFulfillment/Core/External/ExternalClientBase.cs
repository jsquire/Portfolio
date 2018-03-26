using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using OrderFulfillment.Core.Exceptions;
using OrderFulfillment.Core.Infrastructure;

namespace OrderFulfillment.Core.External
{
    /// <summary>
    ///   Serves as a base class for clients responsible for communicating with
    ///   external systems.
    /// </summary>
    /// 
    public abstract class ExternalClientBase
    {
        /// <summary>
        ///   Creates an HttpClient to be used for service requests.
        /// </summary>
        /// 
        /// <param name="requestProtocol">The protocol (scheme) to use for the base host address.</param>
        /// <param name="hostServiceAddress">The base address of the resource that the client will be bound to</param>
        /// <param name="clientCertificateThumbprint">The thumprint of the client certificate to be used for requests.  If <c>null</c>, no certificate is used.</param>
        /// <param name="requestTimeoutSeconds">The timeout, in seconds, to set for the request.</param>
        /// <param name="connectionLeaseTimeoutSeconds">The timeout, in seconds, for the lease on the underlying connection.</param>
        /// 
        /// <returns>An <see cref="HttpClient"/> for use in external interactions.</returns>
        /// 
        protected virtual HttpClient CreateHttpClient(string requestProtocol,
                                                      string hostServiceAddress,
                                                      string clientCertificateThumbprint,
                                                      int    requestTimeoutSeconds,
                                                      int    connectionLeaseTimeoutSeconds)
        {
            var handler = new WebRequestHandler
            {
                UseProxy          = false,
                AllowAutoRedirect = true
            };

            // If there was a client certificate requested, attempt to find it and attach it to the handler.

            if (!String.IsNullOrWhiteSpace(clientCertificateThumbprint))
            {
                var certificate = this.SearchForCertificate(clientCertificateThumbprint);

                if (certificate == null)
                {
                    throw new MissingDependencyException($"Unable to locate the certificate with the thumbprint [{ clientCertificateThumbprint }] from the CurrentUser or LocalMachine store");
                }

                handler.ClientCertificates.Clear();
                handler.ClientCertificates.Add(certificate);
            }

            // Create the client specific to interacting with a JSON-based API.

            var port    = 0;
            var portLoc = (hostServiceAddress ?? String.Empty).IndexOf(':');	       
            var hasPort = (portLoc < 0) ? false : Int32.TryParse((hostServiceAddress.Substring(portLoc + 1)), out port);
            var host    = (portLoc < 0) ? hostServiceAddress : hostServiceAddress.Substring(0, portLoc);

            Uri baseAddress;
            
            if (hasPort)
            {
                baseAddress = new UriBuilder(requestProtocol, host, port).Uri;
            }
            else
            {
                baseAddress = new UriBuilder(requestProtocol, host).Uri;
            }

            var client = new HttpClient(handler)
            {
               BaseAddress = baseAddress,
               Timeout     = TimeSpan.FromSeconds(requestTimeoutSeconds)
            };

            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(MimeTypes.Json));

            client.DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue { NoStore = true };
            
            // Configure the service point to ensure that connection leases expire in a timely fashion.  This
            // is needed to ensure that connections are cycled periodically and can pick up any changes to DNS,
            // including the ability to fix a cached DNS failure.

            var servicePoint = ServicePointManager.FindServicePoint(client.BaseAddress);
            servicePoint.ConnectionLeaseTimeout = (connectionLeaseTimeoutSeconds * 1000);

            return client;
        }

        /// <summary>
        ///   Searches the local machine certificate stores for a certificate that matches the requested
        ///   <paramref name="thumbprint" />.
        /// </summary>
        /// 
        /// <param name="thumbprint">The thumbprint of the certificate to retrieve.</param>
        /// <param name="onlyRetrieveValidCertificate">When <c>true</c>, only certificates deemed valid are retrieved from the store; otherwise, the certificate is retrieved without framework-level validation.</param>
        /// 
        /// <returns>The requested certificate, if it was found in one of the certificate stores or <c>null</c> if it was not.</returns>
        /// 
        protected virtual X509Certificate2 SearchForCertificate(string thumbprint,
                                                                bool   onlyRetrieveValidCertificate = true)
        {
            return this.RetrieveCertificateFromStore(thumbprint, StoreLocation.CurrentUser, onlyRetrieveValidCertificate) 
                   ??
                   this.RetrieveCertificateFromStore(thumbprint, StoreLocation.LocalMachine, onlyRetrieveValidCertificate);
        }
    
        /// <summary>
        ///   Retrieves a certificate from a specific certificate store that matches the requested
        ///   <paramref name="thumbprint" />.
        /// </summary>
        /// 
        /// <param name="thumbprint">The thumbprint of the certificate to retrieve.</param>
        /// <param name="location">The certificate store location to read from.</param>
        /// <param name="onlyRetrieveValidCertificate">When <c>true</c>, only certificates deemed valid are retrieved from the store; otherwise, the certificate is retrieved without framework-level validation.</param>
        /// 
        /// <returns>The requested certificate, if it was found in the certificate store or <c>null</c> if it was not.</returns>
        /// 
        private X509Certificate2 RetrieveCertificateFromStore(string        thumbprint,
                                                              StoreLocation location,
                                                              bool          onlyRetrieveValidCertificate)
        {
            if (String.IsNullOrEmpty(thumbprint))
            {
                throw new ArgumentNullException(nameof(thumbprint));
            }
            
            var store = default(X509Store);

            try
            {
                store = new X509Store(StoreName.My, location);
                store.Open(OpenFlags.ReadOnly);

                var certificates = store.Certificates.Find(X509FindType.FindByThumbprint, thumbprint, onlyRetrieveValidCertificate);                
                return (certificates.Count >= 1) ? certificates[0] : null;
            }

            finally
            {
                store?.Close();
            }
        }
    }
}
