using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Results;
using OrderFulfillment.Core.Models.Errors;

namespace OrderFulfillment.Api.ActionResults
{
    /// <summary>
    ///   Allows an API endpoint to return a failure result along with the set of errors
    ///   that were encountered.
    /// </summary>
    ///
    /// <seealso cref="System.Web.Http.Results.NegotiatedContentResult{ErrorSet}" />
    /// <seealso cref="System.Web.Http.IHttpActionResult" />
    ///
    public class FailureWithErrorSetResult : NegotiatedContentResult<ErrorSet>
    {    
        /// <summary>
        ///   The ISO language-country code that represents the format of the conflict message.
        /// </summary>
        ///
        public string ContentLanguage { get;  private set; }

        /// <summary>
        ///   Initializes a new instance of the <see cref="FailureWithErrorSetResult"/> class.
        /// </summary>
        ///
        /// <param name="statusCode">The HTTP status code associated with this failure.</param>
        /// <param name="controller">The controller that encountered this failure.</param>
        /// <param name="errorSet">The set of errors that provide context about the failure to callers.</param>
        /// <param name="contentLanguage">The ISO language-country code that represents the format of the human readable error descriptions.  If not provided, US English will be assumed.</param>
        ///
        public FailureWithErrorSetResult(HttpStatusCode statusCode,
                                         ApiController  controller,
                                         ErrorSet       errorSet,
                                         string         contentLanguage = null) : base(statusCode, errorSet, controller)
        {
            if (errorSet == null)
            {
            throw new ArgumentNullException(nameof(errorSet));
            }

            this.ContentLanguage = (String.IsNullOrWhiteSpace(contentLanguage) ? "en-US" : contentLanguage);
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

            // Override any negotiated language, since we're not internationalizing the message

            response.Content.Headers.ContentLanguage.Clear();
            response.Content.Headers.ContentLanguage.Add(this.ContentLanguage);

            return response;
        }
    }
}