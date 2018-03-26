using System.Net;
using System.Web.Http.Controllers;
using OrderFulfillment.Core.Infrastructure;

namespace OrderFulfillment.Api.Security
{
    /// <summary>
    ///   An authorization policy that can be evaluated to determine if the specified HTTP context is valid
    ///   against it.
    /// </summary>
    /// 
    public interface IAuthorizationPolicy
    {
        /// <summary>
        ///   Indicates whether or not the policy is enabled.
        /// </summary>
        /// 
        /// <value><c>true</c> if the handler is enabled; otherwise, <c>false</c>.</value>
        /// 
        bool Enabled { get; }

        /// <summary>
        ///   Gets the priority of the authorization policy.
        /// </summary>
        /// 
        Priority Priority { get; }

        /// <summary>
        ///   The specific authorization policy represented.
        /// </summary>
        /// 
        AuthorizationPolicy Policy { get; }

        /// <summary>
        ///   Evaluates the authorization policy against the provided context.
        /// </summary>
        /// 
        /// <param name="context">The context for which to evaluate the authorization policy.</param>
        /// 
        /// <returns><c>null</c> if the authorization policy was satisfied; otherwise, the recommended <see cref="System.Net.HttpStatusCode" /> to respond with.</returns>
        /// 
        HttpStatusCode? Evaluate(HttpActionContext context);
    }
}