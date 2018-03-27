using System.Web.Http;
using OrderFulfillment.Core.Models.Errors;

namespace OrderFulfillment.Api.ActionResults
{
    /// <summary>
    ///   Allows an API endpoint to return an HTTP 400 (Bad Request) result along with the set of errors
    ///   that were encountered.
    /// </summary>
    ///
    /// <seealso cref="FailureWithErrorSetResult" />
    /// 
    public class BadRequestWithErrorSetResult : FailureWithErrorSetResult
    {
        /// <summary>
        ///   Initializes a new instance of the <see cref="BadRequestWithErrorSetResult"/> class.
        /// </summary>
        ///
        /// <param name="statusCode">The HTTP status code associated with this failure.</param>
        /// <param name="controller">The controller that encountered this failure.</param>
        /// <param name="errorSet">The set of errors that provide context about the failure to callers.</param>
        /// <param name="contentLanguage">The ISO language-country code that represents the format of the human readable error descriptions.  If not provided, US English will be assumed.</param>
        ///
        public BadRequestWithErrorSetResult(ApiController  controller,
                                            ErrorSet       errorSet,
                                            string         contentLanguage = null) : base(System.Net.HttpStatusCode.BadRequest, controller, errorSet, contentLanguage)
        {
        }
    }
}