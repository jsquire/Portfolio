using System;
using System.Linq;
using System.Collections.Generic;
using System.Security.Claims;
using System.Security.Principal;
using System.Web.Http.Filters;
using OrderFulfillment.Api.Configuration;
using OrderFulfillment.Core.Infrastructure;
using System.Net.Http.Headers;

namespace OrderFulfillment.Api.Security
{
    /// <summary>
    ///   Serves as a handler responsible for authenticating enttities using the AppSecret
    ///   scheme.
    /// </summary>
    /// 
    /// <seealso cref="OrderFulfillment.Api.Security.IAuthenticationHandler" />
    /// 
    public class SharedSecretAuthenticationHandler : IAuthenticationHandler
    {   
        /// <summary>The configuration for the authentication scheme.</summary>
        private readonly SharedSecretAuthenticationConfiguration configuration;

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
        public bool CanGenerateChallenge => true;

        /// <summary>
        ///   The type of authentication that can be handled.
        /// </summary>
        /// 
        public AuthenticationType HandlerType => AuthenticationType.SharedSecret;

        /// <summary>
        ///   The relative strength of the authentication mechanism, used to make informed decisions
        ///   for selecting an authentication scheeme when multiple are available.
        /// </summary>
        /// 
        public AuthenticationStrength Strength => AuthenticationStrength.Weak;

        /// <summary>
        ///   Initializes a new instance of the <see cref="SharedSecretAuthenticationHandler"/> class.
        /// </summary>
        /// 
        /// <param name="configuration">The configuration of the athentication scheme.</param>
        /// 
        public SharedSecretAuthenticationHandler(SharedSecretAuthenticationConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            if ((configuration.Enabled) && (String.IsNullOrEmpty(configuration.PrimaryKey)))
            {
                throw new ArgumentException($"The primary key must be configured in { nameof(configuration.PrimaryKey) }", nameof(configuration));
            }

            if ((configuration.Enabled) && (String.IsNullOrWhiteSpace(configuration.PrimarySecret)))
            {
                throw new ArgumentException($"The primary secret must be configured in { nameof(configuration.PrimarySecret) }", nameof(configuration));
            }

            this.configuration = configuration;
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
            var headers = authenticationContext?.Request.Headers;

            // If the necessary headers were not present or the necessary then authentication cannot be performed.

            if ((!headers.TryGetValues(Core.Infrastructure.HttpHeaders.ApplicationKey, out var headerKeys)) || 
                (!headers.TryGetValues(Core.Infrastructure.HttpHeaders.ApplicationSecret, out var headerSecrets)))
            {
                return null;
            }

            var keyValue    = headerKeys.FirstOrDefault() ?? String.Empty;
            var secretValue = headerSecrets.FirstOrDefault() ?? String.Empty;
            
            // If the key and secret matches the primary set, then consider the entity authentication. This comparison is intentionally case-sensitive.              

            var authenticated = ((String.Equals(this.configuration.PrimaryKey, keyValue, StringComparison.InvariantCulture)) && 
                                 (String.Equals(this.configuration.PrimarySecret, secretValue, StringComparison.InvariantCulture)));

            
            // If authentication failed against the primery set, fallback and verify against the secondary set, if they were populated.
            
            if ((!authenticated) && (!String.IsNullOrWhiteSpace(this.configuration.SecondaryKey)) && (!String.IsNullOrWhiteSpace(this.configuration.SecondarySecret)))
            {
                authenticated = ((String.Equals(this.configuration.SecondaryKey, keyValue, StringComparison.InvariantCulture)) && 
                                 (String.Equals(this.configuration.SecondarySecret, secretValue, StringComparison.InvariantCulture)));
            }                                

            if (!authenticated)
            { 
                return null;
            }

            return new ClaimsPrincipal(new ClaimsIdentity(this.HandlerType.ToString()));
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
            return new AuthenticationHeaderValue(this.HandlerType.ToString());
        }
    }
}