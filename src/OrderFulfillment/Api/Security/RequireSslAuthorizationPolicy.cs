using System;
using System.Net;
using System.Web.Http.Controllers;
using OrderFulfillment.Api.Configuration;
using OrderFulfillment.Core.Infrastructure;

namespace OrderFulfillment.Api.Security
{
    // <summary>
    ///   An authorization policy that enforces requests to the endpoint being made over SSL, unless
    ///   the request was initiated locally.
    /// </summary>
    /// 
    /// <seealso cref="OrderFulfillment.Api.Security.IAuthorizationPolicy" />
    /// 
    public class RequireSslAuthorizationPolicy : IAuthorizationPolicy
    {
        /// <summary>The configuration for the policy</summary>
        private readonly RequireSslAuthorizationPolicyConfiguration configuration;

        /// <summary>
        ///   Indicates whether or not the policy is enabled.
        /// </summary>
        /// 
        /// <value><c>true</c> if the handler is enabled; otherwise, <c>false</c>.</value>
        /// 
        public bool Enabled => this.configuration.Enabled;

        /// <summary>
        ///   Gets the priority of the authorization policy.
        /// </summary>
        /// 
        public Priority Priority => Priority.Highest;

        /// <summary>
        ///   The specific authorization policy represented.
        /// </summary>
        /// 
        public AuthorizationPolicy Policy => AuthorizationPolicy.RequireSsl;

        /// <summary>
        ///   Initializes a new instance of the <see cref="RequireSslAuthorizationPolicy"/> class.
        /// </summary>
        /// 
        /// <param name="configuration">The configuration to use for the policy.</param>
        ///         
        public RequireSslAuthorizationPolicy(RequireSslAuthorizationPolicyConfiguration configuration)
        {
            this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        /// <summary>
        ///   Evaluates the authorization policy against the provided context.
        /// </summary>
        /// 
        /// <param name="context">The context for which to evaluate the authorization policy.</param>
        /// 
        /// <returns><c>null</c> if the authorization policy was satisfied; otherwise, the recommended <see cref="System.Net.HttpStatusCode" /> to respond with.</returns>
        /// 
        public HttpStatusCode? Evaluate(HttpActionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (!this.Enabled)
            {
              return null;
            }

            var uri = context.Request.RequestUri;

            if ((uri.Scheme == Uri.UriSchemeHttps) ||
               ((this.configuration.AllowLoopbackException) && (uri.IsLoopback)))
            {
                return null;
            }

            return HttpStatusCode.Forbidden;
        }
    }
}