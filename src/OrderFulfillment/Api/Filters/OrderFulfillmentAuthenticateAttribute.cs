using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.Controllers;
using System.Web.Http.Dependencies;
using System.Web.Http.Filters;
using System.Web.Http.Results;
using OrderFulfillment.Api.Extensions;
using OrderFulfillment.Api.Infrastructure;
using OrderFulfillment.Api.Security;
using OrderFulfillment.Core.Exceptions;
using OrderFulfillment.Core.Extensions;
using OrderFulfillment.Core.Infrastructure;
using Serilog;

namespace OrderFulfillment.Api.Filters
{
    /// <summary>
    ///   Performs the tasks needed to authenticate a user for API endpoints.
    /// </summary>
    /// 
    /// <seealso cref="System.Web.Http.Filters.FilterAttribute" />
    /// <seealso cref="System.Web.Http.Filters.IAuthenticationFilter" />
    /// 
    public class OrderFulfillmentAuthenticateAttributeAttribute : FilterAttribute, IAuthenticationFilter
    {
        /// <summary>A reference to the internal-only type that is implicitly created as an IHttpActionResult result when an authorization filter sets a response.</summary>
        private static readonly Lazy<Type> AuthorizationFilterResultType = new Lazy<Type>( () => typeof(HttpActionContext).Assembly.GetTypes().Single(assemblyType => assemblyType.Name == "AuthorizationFilterResult"), LazyThreadSafetyMode.PublicationOnly);

        /// <summary>A shared empty readonly dictionary for string/string sets. </summary>
        private static readonly IReadOnlyDictionary<string, string> EmptyStringDictionary = new Dictionary<string, string>();
           
        /// <summary>
        ///   Indicates whether more than one instance of the attribute can be specified for a single program element.
        /// </summary>
        /// 
        /// <value><c>true</c> if more than one instance is allowed to be specified; otherwise, <c>false</c>.</value>
        /// 
        public override bool AllowMultiple => false;
        
        /// <summary>
        ///   Performs the actions needed to authenticate the request.
        /// </summary>
        /// 
        /// <param name="context">The authentication context to consider.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// 
        /// <returns>A Task that will perform the authentication challenge.</returns>
        /// 
        public async Task AuthenticateAsync(HttpAuthenticationContext context, 
                                            CancellationToken         cancellationToken)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var request        = context.Request;
            var locator        = request.GetDependencyScope();
            var logger         = (locator.GetService(typeof(ILogger)) as ILogger)?.WithCorrelationId(request?.GetOrderFulfillmentCorrelationId());
            var certThumbprint = request.GetClientCertificate()?.Thumbprint;
            var body           = await request.SafeReadContentAsStringAsync();
            var result         = OrderFulfillmentAuthenticateAttributeAttribute.ProcessRequest(request, cancellationToken);

           // If there was no available result after processing the request, authentication is not possible.

            if (result == null)
            {
                try
                {
                    logger.Information($"Authentication is not possible because No authentication handler was available for {{Route}} with Headers: [{{Headers}}] { Environment.NewLine }Client Certificate: [{{ClientCertificateThumbprint}}]", 
                        request?.RequestUri, 
                        request?.Headers, 
                        certThumbprint);
                }

                catch
                {
                    // Do nothing; logging is a non-critical operation that should not cause
                    // cascading failures.
                }

                return;
            }

            // If the handler was able to produce a principal, then set it on the authentication context.  This will indicate to the hosting
            // infrastructure that there is an authenticated entity.

            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            var principal = result.AuthenticationHandler.Authenticate(result.AuthenticationTokens, context);

            if (principal != null)
            {
                context.Principal = principal;
            }
            else
            {
                try
                {                
                    logger.Information($"Unable to authenticate for {{Route}} with Headers: [{{Headers}}]{ Environment.NewLine }Client Certificate: [{{ClientCertificateThumbprint}}]", 
                        request?.RequestUri, 
                        request?.Headers, 
                        certThumbprint);
                }

                catch
                {
                    // Do nothing; logging is a non-critical operation that should not cause
                    // cascading failures.
                }
            }
        }

        /// <summary>
        ///   Performs the actions needed to add an authentication challenge to the inner IHttpActionResult, if needed.
        /// </summary>
        /// 
        /// <param name="context">The authentication challenge context to consider.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// 
        /// <returns>A Task that will perform the authentication challenge.</returns>
        /// 
        /// <remarks>
        ///   This method will be executed by the framework after the action has executed, regardless of whether or not a challenge 
        ///   should be generated.  Before generating the challenge, determine if it
        /// </remarks>
        /// 
        public async Task ChallengeAsync(HttpAuthenticationChallengeContext context, CancellationToken cancellationToken)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var request = context.Request; 
            
            // If there was an authenticated principal associated with the request or there was a result generated that should not 
            // overridden, then no challenge is needed.

            if ((cancellationToken.IsCancellationRequested) || (request.GetRequestContext()?.Principal != null) || (!OrderFulfillmentAuthenticateAttributeAttribute.ShouldOverrideResponse(context)))
            {
                return;
            }  
            
            var result = OrderFulfillmentAuthenticateAttributeAttribute.ProcessRequest(request, cancellationToken, true);    
            
            // If there was no available handler located after processing the request, then attempt to retrieve the
            // default challenge handler.
            
            if (result == null)
            {
               var handler = OrderFulfillmentAuthenticateAttributeAttribute.SelectDefaultChallengeHandler(request.GetDependencyScope());

               if (handler != null)
               {
                   result = new ProcessResult(OrderFulfillmentAuthenticateAttributeAttribute.EmptyStringDictionary, handler);
               }
            }        

            // If there was no available challenge handler, authentication is not possible.  Make
            // no alterations to the current response.

            if ((cancellationToken.IsCancellationRequested) || (result == null))
            {
                return;
            }

            // If the handler was able to produce a challenge, then clear any existing result/response and 
            // set the new Unauthorized with the correct challenge.  

            var challenge = result.AuthenticationHandler.GenerateChallenge(result.AuthenticationTokens, context); 

            if (challenge != null)
            {            
                context.ActionContext.Response?.Dispose();
                context.ActionContext.Response = null;

                context.Result = new UnauthorizedResult(new[] { challenge }, request);

                try
                {
                    var locator = request.GetDependencyScope();
                    var logger  = (locator.GetService(typeof(ILogger)) as ILogger);                    

                    if (logger != null)
                    { 
                        var body = await request.SafeReadContentAsStringAsync();

                        logger.WithCorrelationId(request?.GetOrderFulfillmentCorrelationId())
                              .Information($"Response: {{Response}} { Environment.NewLine } Authentication is needed; a challenge was issued for {{Route}} with Headers: [{{Headers}}]",
                                  HttpStatusCode.Unauthorized,
                                  request?.RequestUri, 
                                  request?.Headers);
                    }
                }
                
                catch
                {
                    // Do nothing; logging is a non-critical operation that should not cause
                    // cascading failures.
                }
            }
        }

        /// <summary>
        ///   Performs the actiosn needed to process a request to compile the needed artifacts
        ///   for performing authentication operations.
        /// </summary>
        /// 
        /// <param name="request">The request to be processed.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <param name="challengeGenerationRequired"><c>true</c> if the handler is required to be able to generate a challenge reponse; othewise, <c>false</c>.</param>
        /// <param name="authorizationnHeaderName">The name of the HTTP header expected to hold the authentication data; this will be used as the token key to the requested authentication scheme in the result.</param>        
        /// 
        /// <returns>The result of the processing.</returns>
        /// 
        private static ProcessResult ProcessRequest(HttpRequestMessage request, 
                                                    CancellationToken  cancellationToken,
                                                    bool               challengeGenerationRequired = false,
                                                    string             authorizationnHeaderName    = HttpHeaders.Authorization)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var locator = request.GetDependencyScope();

            // If there was a client certificate present, and processing does not require challenge generation, then attempt to locate a handler for client certificate
            // authentication and give it precedence.

            if ((!challengeGenerationRequired) && (request.GetClientCertificate() != null))
            {
                var certHandler = OrderFulfillmentAuthenticateAttributeAttribute.SelectHandler(request, cancellationToken, locator, AuthenticationType.ClientCertificate.ToString(), challengeGenerationRequired);

                if (certHandler != null)
                {
                    return new ProcessResult(OrderFulfillmentAuthenticateAttributeAttribute.EmptyStringDictionary, certHandler);
                }
            }

            // Attempt to locate the value of the HTTP authorization header.  There may only be a single instance of the standard header, by spec 
            // (see: http://tools.ietf.org/html/rfc7235#section-4.1) and the same semantics will be applied to any custom authorization headers that
            // may be used.

            IEnumerable<string> headerValues;                      
            
            // If a value was not available for the expected HTTP Authorization header, then a result cannot be determined.

            if ((cancellationToken.IsCancellationRequested) || (!request.Headers.TryGetValues(authorizationnHeaderName, out headerValues)))
            {
               return null;
            }

            var headerValue = headerValues.FirstOrDefault();

            if (String.IsNullOrEmpty(headerValue))
            {
                return null;
            }

            // Parse the authorization header into its composite tokens; if there was no entry for the header name, than the
            // desired authentiation scheme was not present in the header and a result cannot be determined.
                        
            var parser = locator.GetService(typeof(IHttpHeaderParser)) as IHttpHeaderParser;
            
            if (parser == null)
            {
               throw new MissingDependencyException("An IHttpParser could not be located");
            } 
                        
            var headerTokens = parser.ParseAuthorization(headerValue, authorizationnHeaderName);

            if (!headerTokens.ContainsKey(authorizationnHeaderName))
            {
                return null;
            }

            // If there was no available handler for the desired authentication scheme, a result cannot be determined.

            var handler = OrderFulfillmentAuthenticateAttributeAttribute.SelectHandler(request, cancellationToken, locator, headerTokens[authorizationnHeaderName], challengeGenerationRequired);

            if ((cancellationToken.IsCancellationRequested) || (handler == null))
            {
                return null;
            }

            return new ProcessResult(headerTokens, handler);
        }

        /// <summary>
        ///   Selects a handler for performing authentication for the specified request.
        /// </summary>
        /// 
        /// <param name="request">The request that authentication will be handled for.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <param name="headerTokens">The set of authentication-related tokens that were parsed from the HTTP authorization header.</param>
        /// <param name="locator">The resolver to use for locating dependencies</param>
        /// <param name="authenticationSchemeName">The name of the desired authentication scheme.</param>
        /// <param name="challengeGenerationRequired"><c>true</c> if the handler is required to be able to generate a challenge reponse; othewise, <c>false</c>.</param>
        /// 
        /// <returns>The <see cref="OrderFulfillment.Api.Security.IAuthenticationHandler" /> appropriate for the <paramref name="request" />, if one is available; otherwise, <c>null</c>.</returns>
        /// 
        private static IAuthenticationHandler SelectHandler(HttpRequestMessage request, 
                                                            CancellationToken  cancellationToken,
                                                            IDependencyScope   locator,
                                                            string             authenticationSchemeName,
                                                            bool               challengeGenerationRequired = false)
        {
            // If there was no request, dependency resolver, or header name, then a handler cannot be selected.

            if ((request == null) || (locator == null) || (String.IsNullOrEmpty(authenticationSchemeName)))
            {
                return null;
            }
                        
            var scheme = AuthenticationType.Unknown;
            
            // If there were no tokens parsed from the header or the authentication scheme as missing / invalid, then no handler 
            // can be selected.

            if ((cancellationToken.IsCancellationRequested) ||
                (!Enum.IsDefined(typeof(AuthenticationType), authenticationSchemeName)) || 
                (!Enum.TryParse(authenticationSchemeName, out scheme)))
            {
                return null;
            }

            // Atempt to locate a handler for the requested authentication method.  

            var handlerCandidates    = (IEnumerable<IAuthenticationHandler>)locator.GetServices(typeof(IAuthenticationHandler));                        
            var challengeNotRequired = (!challengeGenerationRequired);

            return handlerCandidates?.Where(candidate => ((candidate.Enabled) && 
                                                          (candidate.HandlerType == scheme) && 
                                                          (candidate.CanGenerateChallenge || (challengeNotRequired))))
                .OrderByDescending(candidate => candidate.Strength)
                .FirstOrDefault();
        }

        /// <summary>
        ///   Selects a handler to serve as the default generator of HTTP challenges.
        /// </summary>
        /// 
        /// <param name="locator">The resolver to use for locating dependencies</param>
        /// 
        /// <returns>The <see cref="OrderFulfillment.Api.Security.IAuthenticationHandler" /> to use as the deafult challenge generator, if one is available; otherwise, <c>null</c>.</returns>
        /// 
        private static IAuthenticationHandler SelectDefaultChallengeHandler(IDependencyScope locator)
        {
            // If there was no dependency resolver then a handler cannot be selected.

            if (locator == null)
            {
                return null;
            }
            
            // Select the strongest authentication handler to issue the default challenge.

            var handlerCandidates = (IEnumerable<IAuthenticationHandler>)locator.GetServices(typeof(IAuthenticationHandler));            
            return handlerCandidates?.Where(candidate => ((candidate.Enabled) && (candidate.CanGenerateChallenge))).OrderByDescending(candidate => candidate.Strength).FirstOrDefault();
        }

        /// <summary>
        ///   Determines whether a response set for the context should be overridden as the result of
        ///   challenge generation.
        /// </summary>
        /// 
        /// <param name="context">The challenge context to consider.</param>
        /// 
        /// <returns><c>true</c>, if the response should be overridden; otherwise, <c>false</c>.</returns>
        /// 
        private static bool ShouldOverrideResponse(HttpAuthenticationChallengeContext context)
        {
            // If no response was set, then consider it a valid case to "override."
            if ((context == null) || ((context.Result == null) && (context.ActionContext.Response == null)))
            {
                return true;
            }

            // If the request resulted in a response that corresponds to an HTTP 401 (Unauthorized), then it should be overridden so that the proper
            // challenge can be presented to the caller.  Any other response indicates that the request awas able to be fully satisfied, so no challenge 
            // needs to be presented.  

            if (context.Result != null)
            {
                // If there was an IHttpActionResult set for the context, then consider that the authoritative response.  Because there is no direct
                // result that corresponds to Forbidden, when an authorization filter sets a respoonse, it does so in the form of an internal-only type 
                // called "AuthorizationFilterResult."  If either that or the standard Unauthorized result was set, then the resposne should be overidden.

                var resultType = context.Result.GetType();

                if ((resultType == typeof(UnauthorizedResult)) || (resultType == OrderFulfillmentAuthenticateAttributeAttribute.AuthorizationFilterResultType.Value))
                {
                    return true;
                }                

                // Another common approach to setting the response to a specific status code is to use a StatusCodeResult, which can be inspected for
                // the raw HTTP response code used.  If the result was set with a 401, then it should be overridden.

                var statusResult = context.Result as StatusCodeResult;

                if ((statusResult != null) && (statusResult.StatusCode == HttpStatusCode.Unauthorized))
                {
                    return true;
                }
            }

            // If an HttpResponseMessage was set directly on the action context, then it's status code can be directly inspected.  If there
            // was a response set with a 401, then it should be overridden.  

            var status = context.ActionContext?.Response?.StatusCode;            
            
            if ((status.HasValue) && (status.Value == HttpStatusCode.Unauthorized))
            {
                return true;
            }

            // If no case was found that proves the result should be overridden, then default to preserving it.

            return false;
        }

        #region Nested Classes

            /// <summary>
            ///   The result of processing a request, containing the artifacts needed to
            ///   authenticate or generate a challenge for the request.
            /// </summary>
            /// 
            private class ProcessResult
            {
                /// <summary>The set of tokens relevant to the authentication process.</summary>
                public readonly IReadOnlyDictionary<string, string> AuthenticationTokens;

                /// <summary>The handler to use for authentication operations.</summary>
                public readonly IAuthenticationHandler AuthenticationHandler;                

                /// <summary>
                ///   Initializes a new instance of the <see cref="ProcessResult"/> class.
                /// </summary>
                /// 
                /// <param name="tokens">The set of tokens relevent to the authentication process.</param>
                /// <param name="handler">The handler to use for authentication operations.</param>
                /// 
                public ProcessResult(IReadOnlyDictionary<string, string> tokens,
                                     IAuthenticationHandler              handler)
                {
                    this.AuthenticationTokens  = tokens;
                    this.AuthenticationHandler = handler;
                }
            }

        #endregion                                                      
    }
}