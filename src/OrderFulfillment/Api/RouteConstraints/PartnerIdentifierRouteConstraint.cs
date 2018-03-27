using OrderFulfillment.Core.Models.Errors;

namespace OrderFulfillment.Api.RouteConstraints
{
    /// <summary>
    ///   Serves as a constraint on URL slugs intended as partner identifiers.  
    /// </summary>
    /// 
    /// <remarks>
    ///   This constraint will enforce that the identifier is well-formed, returning an HTTP 400 (Bad Request) if 
    ///   not, rather than failing to bind the route.
    /// </remarks>
    /// 
    /// <seealso cref="OrderFulfillment.Api.Common.RouteConstraints.IdentifierRouteConstraint" />
    /// 
    [RouteConstraint("partnerIdentifier")]
    public class PartnerIdentifierRouteConstraint : IdentifierRouteConstraint
    {
        /// <summary>The maximum length of the partner code, as defined in the API spec.</summary>
        private const int PartnerCodeMaxLength = 15;

        /// <summary>
        ///   Initializes a new instance of the <see cref="PartnerIdentifierRouteConstraint"/> class.
        /// </summary>
        /// 
        public PartnerIdentifierRouteConstraint() : base(ErrorCode.PartnerIdentifierMalformed,  "The partner identifier had unpermitted characters or length violations", PartnerIdentifierRouteConstraint.PartnerCodeMaxLength)
        {
        }
    }
}