namespace OrderFulfillment.Api.Security
{
    /// <summary>
    ///   The set of authorization policies that can be applied to an endpoint to secure 
    ///   allowable actions.
    /// </summary>
    /// 
    public enum AuthorizationPolicy
    {
        /// <summary>No specific policy is in place; any authenticated principal is allowed.  If the <see cref="System.Web.Http.AllowAnonymousAttribute"/> is decorating the same endpoint, it will begiven precedence and honored.</summary>
        [DefaultPolicy]
        AuthenticatedPrincipal,

        /// <summary>SSL is required unless an exception is enabled for loopback (local) calls.</summary>
        [DefaultPolicy]
        RequireSsl,

        /// <summary>If the authorized principal has a partner claim, then endpoint usage will be restricted to that partner.</summary>
        [DefaultPolicy]
        EnforcePartner,

        /// <summary>Only authenticated principals with a claim allowing privilidged operations will be authorized.</summary>
        RequireSudo,
    }
}