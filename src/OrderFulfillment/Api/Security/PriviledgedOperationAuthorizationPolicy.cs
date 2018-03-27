using System;
using System.Net;
using System.Security.Claims;
using System.Web.Http.Controllers;
using OrderFulfillment.Api.Configuration;
using OrderFulfillment.Core.Infrastructure;

namespace OrderFulfillment.Api.Security
{
    /// <summary>
    ///   An authorization policy that enfoces that a principal's partner claim, if present, matches the partner of the requested
    ///   resource.  Should the principal have no partner claim, it is assumed that the caller is authorized to work with any
    ///   partner.
    /// </summary>
    /// 
    public class PriviledgedOperationAuthorizationPolicy : IAuthorizationPolicy
    {
        /// <summary>The configuration for the policy</summary>
        private readonly PriviledgedOperationAuthorizationPolicyConfiguration configuration;

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
        public Priority Priority => Priority.Normal;

        /// <summary>
        ///   The specific authorization policy represented.
        /// </summary>
        /// 
        public AuthorizationPolicy Policy => AuthorizationPolicy.RequireSudo;

        /// <summary>
        ///     Initializes a new instance of the <see cref="PriviledgedOperationAuthorizationPolicy"/> class.
        /// </summary>
        /// 
        /// <param name="configuration">The configuration to use for the policy.</param>
        ///         
        public PriviledgedOperationAuthorizationPolicy(PriviledgedOperationAuthorizationPolicyConfiguration configuration)
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

            // To satisfy the policy, there must be a claims principal present, and the principal must have 
            // the privileged operations claim.
            //
            // NOTE: The claim type is intentionally case-sensitive; while no standard enforces this, it is the common expectation.

            var holdsClaim = (context.RequestContext.Principal as ClaimsPrincipal)?.HasClaim(claim => claim.Type == CustomClaimTypes.MayAccessPriviledgedOperations);
            
            if ((holdsClaim.HasValue) && (holdsClaim.Value))
            {
                return null;
            };
            
            // If there was no principal or the principal did not have the expected claim, then the request is not authorized.
                
            return (HttpStatusCode?)HttpStatusCode.Forbidden;
        }
    }
}