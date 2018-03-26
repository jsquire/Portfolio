using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using OrderFulfillment.Api.Extensions;
using OrderFulfillment.Api.Security;
using OrderFulfillment.Core.Extensions;
using Serilog;

namespace OrderFulfillment.Api.Filters
{
    /// <summary>
    ///   Performs the tasks needed to authorize the calller of an endpoint against a set of authorization
    ///   policies.
    /// </summary>
    /// 
    /// <seealso cref="System.Web.Http.Filters.AuthorizationFilterAttribute" />
    /// 
    public class OrderFulfillmentAuthorizeAttribute : AuthorizationFilterAttribute
    {
        /// <summary>The active authorization policies to be satisfied to prove authorization./summary>
        private readonly HashSet<AuthorizationPolicy> activePolicies;

        /// <summary>
        ///   Indicates whether multiple authorization filters are allowed.
        /// </summary>
        /// 
        /// <value><c>true</c> if more than one instance is allowed to be specified; otherwise, <c>false</c>.</value>
        /// 
        public override bool AllowMultiple => true;

        /// <summary>
        ///   Initializes a new instance of the <see cref="OrderFulfillmentAuthorizeAttribute"/> class using
        ///   all enabled authorization polices.
        /// </summary>
        /// 
        public OrderFulfillmentAuthorizeAttribute() : this(null)
        {
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref="OrderFulfillmentAuthorizeAttribute"/> class.
        /// </summary>
        /// 
        /// <param name="policies">The set of policies of which all must be satisfied to consider a caller authorized for the decorated endpoint.</param>
        /// 
        public OrderFulfillmentAuthorizeAttribute(params AuthorizationPolicy[] policies)
        {
            this.activePolicies = new HashSet<AuthorizationPolicy>(policies ?? Enumerable.Empty<AuthorizationPolicy>());
        }

        /// <summary>
        ///   Invoked when authorization is requested.
        /// </summary>
        /// 
        /// <param name="context">The HTTP context to consider when determining authorization.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// 
        /// <returns>A Task that encapsulates the outstanding work needed to complete authorization.</returns>
        /// 
        public override async Task OnAuthorizationAsync(HttpActionContext context, 
                                                        CancellationToken cancellationToken)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }
            
            var completedTask      = Task.FromResult(default(object));
            var hasFailureResponse = (context.Response == null) ? false : (!context.Response.IsSuccessStatusCode);            
            
            // If the request already has a failure response or cancellation was requested, take no further action.

            if ((hasFailureResponse) || (cancellationToken.IsCancellationRequested))
            {
                return;
            }

            // Locate the requested authorization policies, igoring any that are disabled.  If there was a set of active policies specified,
            // then enforce only those policies.

            var request = context.Request;
            var locator = request.GetDependencyScope();   

            var authPolicies = locator.GetServices(typeof(IAuthorizationPolicy))
                                      .Cast<IAuthorizationPolicy>()
                                      .Where(policy => ((policy.Enabled) && ((this.activePolicies.Count == 0) || this.activePolicies.Contains(policy.Policy))))
                                      .OrderByDescending(policy => policy.Priority);

            // Evaluate the policies using an addative approach; for a request to be valid, all specified policies must be satisfied.  If a policy
            // is proven to not be satisfied, then authorization fails.  

            HttpStatusCode? evaluationResult;

            foreach (var policy in authPolicies)
            {
              if (cancellationToken.IsCancellationRequested)
              {
                  return;
              }

              // If a policy evaluation returns a recommended status code, then the policy was not satisfied and the
              // request should not be allowed to execute.  Shortcircuit other evaluations and create a response.

              evaluationResult = policy.Evaluate(context);

              if (evaluationResult.HasValue)
              {
                  context.Response = request.CreateResponse(evaluationResult.Value);

                  try
                  {
                      var logger = locator.GetService(typeof(ILogger)) as ILogger;                      
                  
                      if (logger != null)
                      {
                          var body = await request.SafeReadContentAsStringAsync();

                          logger.WithCorrelationId(request?.GetOrderFulfillmentCorrelationId())
                                .Information($"Response: {{Response}} { Environment.NewLine } The authorization policy {{PolicyName}} was not satisfied for {{Route}} with Headers: [{{Headers}}]", 
                                    evaluationResult.Value,
                                    policy.GetType().Name,                                    
                                    request?.RequestUri, 
                                    request?.Headers);
                      }
                  }
                  
                  catch
                  {
                      // Do nothing; logging is a non-critical operation that should not cause
                      // cascading failures.
                  }             

                  break;
              }              
            }            
        }

        /// <summary>
        ///   Invoked when authorization is requested.
        /// </summary>
        /// 
        /// <param name="context">The HTTP context to consider when determining authorization.</param>
        /// 
        public override void OnAuthorization(HttpActionContext actionContext)
        {
            this.OnAuthorizationAsync(actionContext, CancellationToken.None).GetAwaiter().GetResult();
        }
    }
}