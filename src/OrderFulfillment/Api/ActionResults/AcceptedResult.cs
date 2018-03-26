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
    ///   Allows an API endpoint to return an Http 202 (Accepted) result along with a response payload.
    /// </summary>
    ///
    /// <seealso cref="System.Web.Http.Results.NegotiatedContentResult{T}" />
    /// <seealso cref="System.Web.Http.IHttpActionResult" />
    ///
    public class AcceptedResult<T> : NegotiatedContentResult<T>
    {    
        /// <summary>
        ///   The amount of time after which the caller may make their request again.
        /// </summary>
        ///
        public TimeSpan RetryAfter { get; }

        /// <summary>
        ///   The raw form of the response content provided
        /// </summary>
        ///
        public T ResponseContent { get; }

        /// <summary>
        ///   Initializes a new instance of the <see cref="AcceptedResult"/> class.
        /// </summary>
        ///
        /// <param name="controller">The controller that encountered this failure.</param>
        /// <param name="responseContent">The content to send with the response.</param>
        /// <param name="retryAfter">The amount of time after which the caller may make their request again.</param>
        /// 
        /// <remarks>
        ///   If the <paramref name="retryAfter" /> provided is TimeSpan.Zero, no Retry-After header will be emitted, indicating to callers that
        ///   they may query immediately.
        /// </remarks>
        ///
        public AcceptedResult(ApiController controller,
                              T             responseContent,
                              TimeSpan      retryAfter) : base(HttpStatusCode.Accepted, responseContent, controller)
        {
            this.RetryAfter      = retryAfter;
            this.ResponseContent = responseContent;
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref="AcceptedResult"/> class.
        /// </summary>
        ///
        /// <param name="controller">The controller that encountered this failure.</param>
        /// <param name="responseContent">The content to send with the response.</param>
        /// 
        /// <remarks>
        ///   The retry time specified when using this constructor is TimeSpan.Zero.  As a result, no Retry-After header will be emitted, indicating 
        ///   to callers that they may query immediately.
        /// </remarks>
        ///
        public AcceptedResult(ApiController controller,
                              T             responseContent) : this(controller, responseContent, TimeSpan.Zero)
        {            
        }
        
        /// <summary>
        ///   Executes the HTTP result, performing any needed contect negotiation.
        /// </summary>
        ///
        /// <param name="cancellationToken">The token that can be used to signal cancellation of the execute request.</param>
        ///
        /// <returns>The HTTP result, including the content post-negotiation.</returns>
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