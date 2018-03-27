using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Results;

namespace OrderFulfillment.Api.ActionResults
{
    /// <summary>
    ///   Allows an API endpoint to return an HTTP 503 (Service Unavailable) result along with the set of errors
    ///   that were encountered.
    /// </summary>
    /// 
    /// <seealso cref="System.Web.Http.Results.StatusCodeResult" />
    /// <seealso cref="System.Web.Http.IHttpActionResult" />
    /// 
    public class ServiceUnavailableResult : StatusCodeResult
    {
        /// <summary>
        ///   The amount of time after which the caller may make their request again.
        /// </summary>
        /// 
        public TimeSpan RetryAfter { get; }

        /// <summary>
        ///   Initializes a new instance of the <see cref="ServiceUnavailableResult"/> class. 
        /// </summary>
        /// 
        /// <param name="controller">The controller that encountered this failure</param>
        /// <param name="retryAfter">The amount of time after which the caller may make their request again.</param>
        /// 
        public ServiceUnavailableResult(ApiController controller,
                                        TimeSpan      retryAfter) : base(HttpStatusCode.ServiceUnavailable, controller)
        {
            this.RetryAfter = retryAfter;
        }

        /// <summary>
        ///   Executes the HTTP result, performing any needed content negotiation.
        /// </summary>
        /// 
        /// <param name="cancellationToken">The token that can be used to signal cancellation of the execute request.</param>
        ///
        /// <returns>The HTTP result, including the content post-negotiation.</returns>s
        /// 
        public override async Task<HttpResponseMessage> ExecuteAsync(CancellationToken cancellationToken)
        {
            var response = await base.ExecuteAsync(cancellationToken);
            
            if (this.RetryAfter != TimeSpan.Zero)
            {
                response.Headers.RetryAfter = new RetryConditionHeaderValue(this.RetryAfter);
            }
        
            return response;
        }
    }
}