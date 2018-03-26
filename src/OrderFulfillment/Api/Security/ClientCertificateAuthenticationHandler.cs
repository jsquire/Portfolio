using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using System.Threading;
using System.Web.Http.Filters;
using OrderFulfillment.Api.Configuration;
using OrderFulfillment.Api.Extensions;
using OrderFulfillment.Core.Extensions;
using NodaTime;
using Serilog;

namespace OrderFulfillment.Api.Security
{
    /// <summary>
    ///   Serves as a handler responsible for authenticating enttities using client certificates
    ///   as credentials.
    /// </summary>
    /// 
    /// <seealso cref="OrderFulfillment.Api.Security.IAuthenticationHandler" />
    /// 
    public class ClientCertificateAuthenticationHandler : IAuthenticationHandler
    {
        /// <summary>The configuration for the authentication scheme.</summary>
        private readonly ClientCertificateAuthenticationConfiguration configuration;
    
        /// <summary>The clock instance to use for date/time operations.</summary>
        private readonly IClock clock;
    
        /// <summary>The mapping of client certificate thumbprints to identiy claims.</summary>
        private readonly Lazy<ClientCertificateClaimsMap> claimsMap;
    
        /// <summary>The set of known client certificates, keyed by the certificate thumbprint.</summary>
        private readonly ConcurrentDictionary<string, X509Certificate2> certificates = new ConcurrentDictionary<string, X509Certificate2>();
    
        /// <summary>
         ///   Indicates whether or not the handler is enabled.
         /// </summary>
         /// 
         /// <value><c>true</c> if the handler is enabled; otherwise, <c>false</c>.</value>
         /// 
        public bool Enabled => this.configuration.Enabled;
    
        /// <summary>
         ///   Indicates whether or not the handler is capable of generating a challenge response.
         /// </summary>
         /// 
         /// <value><c>true</c> if the handler can generate a challenge; otherwise, <c>false</c>.</value>
         /// 
        public bool CanGenerateChallenge => false;
    
        /// <summary>
         ///   The type of authentication that can be handled.
         /// </summary>
         /// 
        public AuthenticationType HandlerType => AuthenticationType.ClientCertificate;
    
        /// <summary>
         ///   The relative strength of the authentication mechanism, used to make informed decisions
         ///   for selecting an authentication scheeme when multiple are available.
         /// </summary>
         /// 
        public AuthenticationStrength Strength => AuthenticationStrength.Stronger;
    
        /// <summary>
         ///   Initializes a new instance of the <see cref="SharedSecretAuthenticationHandler"/> class.
         /// </summary>
         /// 
         /// <param name="configuration">The configuration of the athentication scheme.</param>
         /// 
        public ClientCertificateAuthenticationHandler(ClientCertificateAuthenticationConfiguration configuration,
                                                      IClock                                       clock)
        {
            this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            this.clock         = clock         ?? throw new ArgumentNullException(nameof(clock));

            this.claimsMap = new Lazy<ClientCertificateClaimsMap>( () => CreateCertificateClaimsMap(this.configuration.SerializedCertificateClaimsMapping), LazyThreadSafetyMode.PublicationOnly);
        }
    
        /// <summary>
         ///   Attempts to authenticate the entity specified in the request.
         /// </summary>
         /// 
         /// <param name="authenticationHeaderTokens">The tokens that were parsed from the HTTP header used for authentication.</param>
         /// <param name="authenticationContext">The current HTTP context to use for authentication.</param>
         /// 
         /// <returns>If the authentication was successful, a principal representing the authenticated entity; otherwise, <c>null</c>.</returns>
         /// 
        public IPrincipal Authenticate(IReadOnlyDictionary<string, string> authenticationHeaderTokens, 
                                       HttpAuthenticationContext           authenticationContext)
        {
            // Attempt to retrieve the client certificate from the context.  If there was no certificate present, or the thumbprint is not in
            // the known map set, then the caller cannot be authenticated.
    
            var callerCertificate = authenticationContext.Request.GetClientCertificate();
    
            if (callerCertificate == null) 
            {
                return null;
            }

            var request   = authenticationContext.Request;
            var locator   = request.GetDependencyScope();
            var logger    = (locator.GetService(typeof(ILogger)) as ILogger)?.WithCorrelationId(request?.GetOrderFulfillmentCorrelationId());            
            var claimsMap = this.claimsMap.Value;

            if (!claimsMap.ContainsThumbprint(callerCertificate.Thumbprint))
            {
                try
                {
                    logger?.Information("An unknown client certificate with the thumbprint: {Thumbprint} was received for {Route} with Headers: [{{Headers}}]", 
                        callerCertificate.Thumbprint, 
                        request?.RequestUri, 
                        request?.Headers);
                }

                catch
                {
                    // Do nothing; logging is a non-critical operation that should not cause
                    // cascading failures.
                }

                return null;
            }
    
            // Attempt to retrieve the certificate for comparison.  If the certificate was not found in the local machine certificate store, then 
            // the caller cannot be authenticated.
    
            var callerThumbprint = callerCertificate.Thumbprint;
            var knownCertificate = this.certificates.GetOrAdd(callerThumbprint, thumbprint => this.SearchForCertificate(thumbprint, this.configuration.EnforceLocalCertificateValidation));

            if (knownCertificate == null)
            {
                try
                {
                    logger?.Information("A client certificate with the thumbprint: {Thumbprint} was received, but it no corresponding local certificate was found for {Route} with Headers: [{{Headers}}]", 
                        callerCertificate.Thumbprint, 
                        request?.RequestUri, 
                        request?.Headers);
                }

                catch
                {
                    // Do nothing; logging is a non-critical operation that should not cause
                    // cascading failures.
                }

                return null;
            }

            // Attempt to validate the known certificate to ensure that it is valid for identification.

            var now = this.clock.GetCurrentInstant().ToDateTimeUtc();

            var valid = ((callerCertificate.Equals(knownCertificate)) &&
                         (callerCertificate.GetPublicKeyString() == knownCertificate.GetPublicKeyString()) &&
                         (knownCertificate.NotBefore.ToUniversalTime() <= now) &&
                         (knownCertificate.NotAfter.ToUniversalTime() >= now));

            if (!valid)
            {
                try
                {
                    logger?.Information("A client certificate with the thumbprint: {Thumbprint} was received, but the corresponding local certificate match was unsuccessful for {Route} with Headers: [{{Headers}}]", 
                        callerCertificate.Thumbprint, 
                        request?.RequestUri, 
                        request?.Headers);
                }

                catch
                {
                    // Do nothing; logging is a non-critical operation that should not cause
                    // cascading failures.
                }

                return null;
            }
            
            // The caller certificate has been accepted; map it to a claims identity.  Start with the well-known
            // claims for any client certificate.

            var identity = new ClaimsIdentity(AuthenticationType.ClientCertificate.ToString());
            identity.AddClaim(new Claim(ClaimTypes.Thumbprint, callerCertificate.Thumbprint));
            identity.AddClaim(new Claim(CustomClaimTypes.IdentityType, IdentityType.Service.ToString()));

            // Add the claims found in the claims mapping for this specific caller certificate.

            identity.AddClaims(claimsMap[callerThumbprint].Select(pair => new Claim(pair.Key, pair.Value)));

            return new ClaimsPrincipal(identity);
        }
    
        /// <summary>
         ///   Generates the challenge to be returned to callers when authorization was unsuccessful.
         /// </summary>
         /// 
         /// <param name="authenticationHeaderTokens">The tokens that were parsed from the HTTP header used for authentication.</param>
         /// <param name="challengeContext">The current HTTP context to use for challenge generation.</param>
         /// 
         /// <returns>The challenge to be sent to callers as part of the WWW-Authenticate response header.</returns>
         /// 
        public AuthenticationHeaderValue GenerateChallenge(IReadOnlyDictionary<string, string> authenticationHeaderTokens, 
                                                           HttpAuthenticationChallengeContext  challengeContext)
        {
            return null;
        }
        
        ///// <summary>
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

        /// <summary>
        ///   Creates the client certificate claims map containing the known certificates to authenticate,
        ///   and their associated  claims.
        /// </summary>
        /// 
        /// <param name="serializedCertificateClaimsMap">The serialized certificate claims map, if defined.</param>
        /// 
        /// <returns>The rehydrated serialized claims map, if populated; otherwise, an empty claims map to indicate that there are no known certificates.</returns>
        /// 
        private ClientCertificateClaimsMap CreateCertificateClaimsMap(string serializedCertificateClaimsMap)
        {
            if (String.IsNullOrEmpty(serializedCertificateClaimsMap))
            {
                return new ClientCertificateClaimsMap();
            }

            return ClientCertificateClaimsMap.Deserialize(serializedCertificateClaimsMap);
        }
    }
}