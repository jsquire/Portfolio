using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Web.Http;
using System.Web.Http.Routing;
using OrderFulfillment.Api.Extensions;
using OrderFulfillment.Core.Extensions;
using OrderFulfillment.Core.Models.Errors;
using Serilog;

namespace OrderFulfillment.Api.RouteConstraints
{
    /// <summary>
    ///   Performs the actions needed to validate a URL parameter intended to be an identifier in the 
    ///   order fulfillment context, returning the appropriate error if the parameter is malformed.
    /// </summary>
    /// 
    /// <remarks>
    ///   This constraint will return a failure response immediately upon completion; it will not attempt
    ///   to chain validation through other validators decorating the action.  This means that if the
    ///   caller has malformed multiple parameters, they will receive notification about a single failure for
    ///   the call.
    /// </remarks>
    /// 
    /// <seealso cref="System.Web.Http.Routing.IHttpRouteConstraint" />
    /// 
    public class IdentifierRouteConstraint : IHttpRouteConstraint
    {
        /// <summary>The expression that an identifier should match, to be considered valid.</summary>
        private static readonly Regex IdentityMatcher = new Regex(@"^[A-Za-z0-9\.\-_]+$", RegexOptions.ECMAScript | RegexOptions.Compiled);

        /// <summary>The error code to return in the event of a validation failure.</summary>
        private readonly string errorCode;

        /// <summary>The error description to return in the event of a validation failure.</summary>
        private readonly string errorDescription;

        /// <summary>The minimum allowable length, in characters, of the parameter value.</summary>
        private readonly int minimumLength;

        /// <summary>The maximum allowable length, in characters, of the parameter value.</summary>
        private readonly int maximumLength;

        /// <summary>
        ///   Initializes a new instance of the <see cref="IdentifierRouteConstraint"/> class.
        /// </summary>
        /// 
        /// <param name="errorCode">The error code to return if the validation fails.</param>
        /// <param name="errorDescription">The error description to return if the validation fails.</param>
        /// <param name="maximumLength">The maximum length, in characters, allowed for the parameter.</param>
        /// <param name="minimumLength">The minimum length, in characters, allowed for the parameter.</param>
        /// 
        public IdentifierRouteConstraint(string errorCode,
                                         string errorDescription,
                                         int    maximumLength = Int32.MaxValue,
                                         int    minimumLength = 1)
        {    
            this.errorCode        = errorCode        ?? throw new ArgumentNullException(nameof(errorCode));
            this.errorDescription = errorDescription ?? throw new ArgumentNullException(nameof(errorDescription));
            this.minimumLength    = minimumLength;
            this.maximumLength    = maximumLength;
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref="IdentifierRouteConstraint"/> class.
        /// </summary>
        /// 
        /// <param name="errorCode">The error code to return if the validation fails.</param>
        /// <param name="errorDescription">The error description to return if the validation fails.</param>
        /// <param name="maximumLength">The maximum length, in characters, allowed for the parameter.</param>
        /// <param name="minimumLength">The minimum length, in characters, allowed for the parameter.</param>
        /// 
        public IdentifierRouteConstraint(ErrorCode errorCode,
                                         string    errorDescription,
                                         int       maximumLength = Int32.MaxValue,
                                         int       minimumLength = 1) : this(errorCode.ToString(), errorDescription, maximumLength, minimumLength)
        {
        }

        /// <summary>
        ///   Determines whether or not the route can be matched based on the constraint.
        /// </summary>
        /// 
        /// <param name="request">The request being processed.</param>
        /// <param name="route">The route identified as potentially matching the request.</param>
        /// <param name="parameterName">The name of the parameter that the constraint is being evaluated for.</param>
        /// <param name="values">A list of parameter values available to the request.</param>
        /// <param name="routeDirection">An indicator of whether the route is being generated or resolved.</param>
        
        /// <returns><c>true</c> if the constraint is satisfied and the route should match; otherwise, <c>false</c>.</returns>
        /// 
        public bool Match(HttpRequestMessage          request, 
                          IHttpRoute                  route, 
                          string                      parameterName, 
                          IDictionary<string, object> values, 
                          HttpRouteDirection          routeDirection)
        {
            // If the parameter was present, then ensure that it has a valid set of characters and the length
            // is within the expected bounds.

            if (values.TryGetValue(parameterName, out object value))
            {
                var parameterValue = value?.ToString() ?? String.Empty;

                if ((parameterValue.Length < this.minimumLength) || 
                    (parameterValue.Length > this.maximumLength) || 
                    (!IdentifierRouteConstraint.IdentityMatcher.IsMatch(parameterValue)))
                {
                    throw IdentifierRouteConstraint.LogAndCreateFailureException(request, parameterName, this.errorCode, this.errorDescription);                  
                }
            }
            
            // If the constraint was not violated, then allow the route to match.

            return true;
        }

        /// <summary>
        ///   Logs a constraint failure and creates a response exception that represents it.
        /// </summary>
        /// 
        /// <param name="request">The current HTTP request message being processed.</param>
        /// <param name="parameterName">Name of the parameter that failed the constraint.</param>
        /// <param name="errorCode">The error code to be represented in the response.</param>
        /// <param name="errorDescription">The error description to be represented in the response.</param>
        /// 
        /// <returns>An <see cref="HttpResponseException" /> containing the provided details, which can be thrown to fail the current request with the appropriate response.</returns>
        /// 
        private static HttpResponseException LogAndCreateFailureException(HttpRequestMessage request,
                                                                          string             parameterName,
                                                                          string             errorCode, 
                                                                          string             errorDescription)
        {
            var errorSet = new ErrorSet(errorCode, errorDescription);

            try
            {
                var locator = request.GetDependencyScope();  
                var logger  = locator.GetService(typeof(ILogger)) as ILogger;

                if (logger != null)
                {
                    var body = request.SafeReadContentAsStringAsync().GetAwaiter().GetResult();
  
                    logger.WithCorrelationId(request.GetOrderFulfillmentCorrelationId())
                            .Information($"Response: {{Response}} { Environment.NewLine } Parameter validation failed for {{ParameterName}} of {{Route}}. { Environment.NewLine } The following errors were observed: { Environment.NewLine }{{ErrorSet}}", 
                                HttpStatusCode.BadRequest,
                                parameterName,
                                request.RequestUri, 
                                errorSet);
                }
            }
                
            catch
            {
                // Do nothing; logging is a non-critical operation that should not cause
                // cascading failures.
            }              
                    
            return new HttpResponseException(request.CreateResponse(HttpStatusCode.BadRequest, errorSet));         
        }
    }
}