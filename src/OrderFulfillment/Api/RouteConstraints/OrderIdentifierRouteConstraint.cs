using OrderFulfillment.Core.Models.Errors;

namespace OrderFulfillment.Api.RouteConstraints
{
    /// <summary>
    ///   Serves as a constraint on URL slugs intended as Order identifiers.  
    /// </summary>
    /// 
    /// <remarks>
    ///   This constraint will enforce that the identifier is well-formed, returning an HTTP 400 (Bad Request) if 
    ///   not, rather than failing to bind the route.
    /// </remarks>
    /// 
    /// <seealso cref="OrderFulfillment.Api.Common.RouteConstraints.IdentifierRouteConstraint" />
    /// 
    [RouteConstraint("orderIdentifier")]
    public class OrderIdentifierRouteConstraint : IdentifierRouteConstraint
    {
        /// <summary>The maximum length of the order identifier, as defined in the API spec.</summary>
        private const int OrderCodeMaxLength = 50;

        /// <summary>
        ///   Initializes a new instance of the <see cref="OrderIdentifierRouteConstraint"/> class.
        /// </summary>
        /// 
        public OrderIdentifierRouteConstraint() : base(ErrorCode.OrderIdentifierMalformed,  "The order identifier had unpermitted characters or length violations", OrderIdentifierRouteConstraint.OrderCodeMaxLength)
        {
        }
    }
}