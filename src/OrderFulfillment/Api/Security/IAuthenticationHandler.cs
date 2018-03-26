using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Security.Principal;
using System.Web.Http.Filters;

namespace OrderFulfillment.Api.Security
{
    /// <summary>
    ///   Provides functionality to handle authentication of a given type
    /// </summary>
    ///     
    public interface IAuthenticationHandler
    {
        /// <summary>
        ///   Indicates whether or not the handler is enabled.
        /// </summary>
        /// 
        /// <value><c>true</c> if the handler is enabled; otherwise, <c>false</c>.</value>
        /// 
        bool Enabled { get; }

        /// <summary>
        ///   Indicates whether or not the handler is capable of generating a challenge response.
        /// </summary>
        /// 
        /// <value><c>true</c> if the handler can generate a challenge; otherwise, <c>false</c>.</value>
        /// 
        bool CanGenerateChallenge { get; }

        /// <summary>
        ///   The relative strength of the authentication mechanism.
        /// </summary>
        /// 
        AuthenticationStrength Strength { get; }

        /// <summary>
        ///   The type of authentication that can be handled.
        /// </summary>
        /// 
        AuthenticationType HandlerType { get; }

        /// <summary>
        ///   Attempts to authenticate the entity specified in the request.
        /// </summary>
        /// 
        /// <param name="authenticationHeaderTokens">The tokens that were parsed from the HTTP header used for authentication.</param>
        /// <param name="authenticationContext">The current HTTP context to use for authentication.</param>
        /// 
        /// <returns>If the authentication was successful, a principal representing the authenticated entity; otherwise, <c>null</c>.</returns>
        /// 
        IPrincipal Authenticate(IReadOnlyDictionary<string, string> authenticationHeaderTokens,
                                HttpAuthenticationContext           authenticationContext);

        /// <summary>
        ///   Generates the challenge to be returned to callers when authorization was unsuccessful.
        /// </summary>
        /// 
        /// <param name="authenticationHeaderTokens">The tokens that were parsed from the HTTP header used for authentication.</param>
        /// <param name="challengeContext">The current HTTP context to use for challenge generation.</param>
        /// 
        /// <returns>The challenge to be sent to callers as part of the WWW-Authenticate response header.</returns>
        /// 
        AuthenticationHeaderValue GenerateChallenge(IReadOnlyDictionary<string, string> authenticationHeaderTokens,
                                                    HttpAuthenticationChallengeContext  challengeContext);
    }
}