using System;
using System.Linq;
using System.Net;
using System.Web.Http;
using System.Web.Http.Controllers;
using OrderFulfillment.Api.Configuration;
using OrderFulfillment.Core.Infrastructure;

namespace OrderFulfillment.Api.Security
{
    /// <summary>
    ///   An authorization policy that enforces requests to the endpoint have an associated principal that
    ///   has been authenticated.
    /// </summary>
    /// 
    /// <seealso cref="OrderFulfillment.Api.Security.IAuthorizationPolicy" />
    /// 
    public class AuthenticatedPrincipalAuthorizationPolicy : IAuthorizationPolicy
    {
        /// <summary>The configuration for the policy</summary>
        private readonly AuthenticatedPrincipalAuthorizationPolicyConfiguration configuration;

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
        public Priority Priority => Priority.High;

        /// <summary>
        ///   The specific authorization policy represented.
        /// </summary>
        /// 
        public AuthorizationPolicy Policy => AuthorizationPolicy.AuthenticatedPrincipal;

        /// <summary>
        ///     Initializes a new instance of the <see cref="AuthenticatedPrincipalAuthorizationPolicy"/> class.
        /// </summary>
        /// 
        /// <param name="configuration">The configuration to use for the policy.</param>
        ///         
        public AuthenticatedPrincipalAuthorizationPolicy(AuthenticatedPrincipalAuthorizationPolicyConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            this.configuration = configuration;
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

            if ((!this.Enabled) || (AuthenticatedPrincipalAuthorizationPolicy.AllowAnonymous(context)))
            {
              return null;
            }

            var principal = context.RequestContext.Principal;            
            return (principal?.Identity?.IsAuthenticated ?? false) ? null : (HttpStatusCode?)HttpStatusCode.Unauthorized;
        }

        /// <summary>
        ///   Determines if anonymous requests are allowed due to the use of the 
        ///   <see cref="System.Web.Http.AllowAnonymousAttribute"/> in the context scope.
        /// </summary>
        /// 
        /// <param name="context">The HTTP context to consider.</param>
        /// 
        /// <returns><c>true</c> if authorization should be overridden to allow anonymous requests; otherwise, <c>false</c>.</returns>
        /// 
        private static bool AllowAnonymous(HttpActionContext context)
        {
            // If the action itself was not decorated, then allow the determination to be made by whether or not the 
            // controller was decorated.

            if (!context.ActionDescriptor.GetCustomAttributes<AllowAnonymousAttribute>().Any())
            {
                return context.ControllerContext.ControllerDescriptor.GetCustomAttributes<AllowAnonymousAttribute>().Any();
            }

            return true;
        }
    }
}