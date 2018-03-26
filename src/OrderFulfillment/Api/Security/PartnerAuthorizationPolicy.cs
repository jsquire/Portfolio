using System;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Web.Http.Controllers;
using OrderFulfillment.Api.Configuration;
using OrderFulfillment.Api.Infrastructure;
using OrderFulfillment.Core.Infrastructure;

namespace OrderFulfillment.Api.Security
{
    /// <summary>
    ///   An authorization policy that enfoces that a principal's partner claim, if present, matches the partner of the requested
    ///   resource.  Should the principal have no partner claim, it is assumed that the caller is authorized to work with any
    ///   partner.
    /// </summary>
    /// 
    public class PartnerAuthorizationPolicy : IAuthorizationPolicy
    {
        /// <summary>The configuration for the policy</summary>
        private readonly PartnerAuthorizationPolicyConfiguration configuration;

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
        public AuthorizationPolicy Policy => AuthorizationPolicy.EnforcePartner;

        /// <summary>
        ///     Initializes a new instance of the <see cref="PartnerAuthorizationPolicy"/> class.
        /// </summary>
        /// 
        /// <param name="configuration">The configuration to use for the policy.</param>
        ///         
        public PartnerAuthorizationPolicy(PartnerAuthorizationPolicyConfiguration configuration)
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

            // If there was no authenticated principal or there was not a partner specified in the request,
            // then there is no action to take.

            var partner   = this.GetPartnerIdentifier(context);
            var principal = context.RequestContext.Principal as ClaimsPrincipal;  
            
            if ((principal == null) || (String.IsNullOrEmpty(partner)))
            {
                return null;
            };
            
            // If there was no partner claim present, it contained an empty value, or the claim matches the partner specified in the
            // request, then consider the request authorized.

            var partnerClaim = principal.FindFirst(CustomClaimTypes.Partner);

            if ((partnerClaim == null) || (String.IsNullOrEmpty(partnerClaim.Value)) || (String.Equals(partnerClaim.Value, partner, StringComparison.InvariantCultureIgnoreCase)))
            {
                return null;
            }
            
            // If the partner claim did not match the partner from the request, then the caller is not authorized to
            // interact with the requested partner.
                
            return (HttpStatusCode?)HttpStatusCode.Forbidden;
        }

        /// <summary>
        ///   Retrieves the partner from the mapped set of arguments for the requested resource route.
        /// </summary>
        /// 
        /// <param name="actionArguments">The set of mapped action arguments from the rquest.</param>
        /// 
        /// <returns>The value of the partner requested, if present in the argument set; otherwise, <c>null</c>.</returns>
        /// 
        private string GetPartnerIdentifier(HttpActionContext context)
        {
            if (context == null) 
            {
                return null;
            }

            var routeData = context.Request.GetRouteData();
            
            if ((routeData != null) && (routeData.Values != null) && (routeData.Values.ContainsKey(ActionArguments.Partner)))
            {
                return routeData.Values[ActionArguments.Partner]?.ToString();
            }

            return null;
        }
    }
}