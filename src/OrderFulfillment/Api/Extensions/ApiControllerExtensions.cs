using System;
using System.Collections.Generic;
using System.Web.Http;
using OrderFulfillment.Api.ActionResults;
using OrderFulfillment.Core.Models.Errors;

namespace OrderFulfillment.Api.Extensions
{
    /// <summary>
    ///   The set of extension methods for the <see cref="ApiController" /> class.
    /// </summary>
    /// 
    public static class ApiControllerExtensions
    {
        /// <summary>
        ///   Creates a result that corresponds to HTTP 202 (Accepted).
        /// </summary>
        /// 
        /// <typeparam name="T">The type of content being set.</typeparam>
        /// 
        /// <param name="instance">The instance that this method was invoked on.</param>
        /// <param name="content">The content to set as the response body.</param>
        /// <param name="retryAfter">The delta duration to use for setting the Retry-After header, to indicate to callers when they may query for status.</param>
        /// 
        /// <returns>The corresponding HTTP result.</returns>
        /// 
        public static AcceptedResult<T> Accepted<T>(this ApiController instance,
                                                         T             content,
                                                         TimeSpan      retryAfter)
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            return new AcceptedResult<T>(instance, content, retryAfter);
        }

        /// <summary>
        ///   Creates a result that corresponds to HTTP 202 (Accepted).
        /// </summary>
        /// 
        /// <typeparam name="T">The type of content being set.</typeparam>
        /// 
        /// <param name="instance">The instance that this method was invoked on.</param>
        /// <param name="content">The content to set as the response body.</param>
        /// 
        /// <returns>The corresponding HTTP result.</returns>
        /// 
        public static AcceptedResult<T> Accepted<T>(this ApiController instance,
                                                         T             content)
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            return new AcceptedResult<T>(instance, content);
        }

        /// <summary>
        ///   Creates a result that corresponds to HTTP 202 (Accepted).
        /// </summary>
        /// 
        /// <param name="instance">The instance that this method was invoked on.</param>
        /// <param name="retryAfter">The delta duration to use for setting the Retry-After header, to indicate to callers when they may query for status.</param>
        /// 
        /// <returns>The corresponding HTTP result.</returns>
        /// 
        public static AcceptedResult<object> Accepted(this ApiController instance,
                                                           TimeSpan      retryAfter)
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            return new AcceptedResult<object>(instance, null, retryAfter);
        }

        /// <summary>
        ///   Creates a result that corresponds to HTTP 202 (Accepted).
        /// </summary>
        /// 
        /// <param name="instance">The instance that this method was invoked on.</param>
        /// 
        /// <returns>The corresponding HTTP result.</returns>
        /// 
        public static AcceptedResult<object> Accepted(this ApiController instance)
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            return new AcceptedResult<object>(instance, null);
        }

        /// <summary>
        ///   Creates a result that corresponds to HTTP 400 and response
        ///   body contains context around failure to present proper messaging.
        /// </summary>
        /// 
        /// <param name="instance">The instance that this method was invoked on.</param>
        /// <param name="errorSet">The set of errors associated with the response.</param>
        /// <param name="contentLanguage">The ISO language-country code that represents the format of the human readable error descriptions.  If not provided, US English will be assumed.</param>
        /// <returns>The corresponding error scenario with details.</returns>
        /// 
        public static BadRequestWithErrorSetResult BadRequest(this ApiController instance, 
                                                                   ErrorSet      errorSet, 
                                                                   string        contentLanguage = null)
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            return new BadRequestWithErrorSetResult(instance, errorSet, contentLanguage);
        }

        /// <summary>
        ///   Creates a result that corresponds to HTTP 400 and response
        ///   body contains context around failure to present proper messaging.
        /// </summary>
        /// 
        /// <param name="instance">The instance that this method was invoked on.</param>
        /// <param name="errorInformation">The set of errors associated with the response in a list.</param>
        /// <returns>The corresponding error scenario with details.</returns>
        /// 
        public static BadRequestWithErrorSetResult BadRequest(this ApiController      instance,
                                                                   IEnumerable<Error> errorInformation,
                                                                   string             contentLanguage = null)
        {
            return ApiControllerExtensions.BadRequest(instance, new ErrorSet(errorInformation), contentLanguage);
        }

        /// <summary>
        ///   Creates a result that corresponds to HTTP 400 and response
        ///   body contains context around failure to present proper messaging.
        /// </summary>
        /// 
        /// <param name="instance">The instance that this method was invoked on.</param>
        /// <param name="errorInformation">The set of errors associated with the response in a list.</param>
        /// <returns>The corresponding error scenario with details.</returns>
        /// 
        public static BadRequestWithErrorSetResult BadRequest(this ApiController instance,
                                                                   Error         errorInformation,
                                                                   string        contentLanguage = null)
        {
            return ApiControllerExtensions.BadRequest(instance, new ErrorSet(errorInformation), contentLanguage);
        }

        /// <summary>
        /// Creates a result that corresponds to a HTTP 503 conflict error.
        /// </summary>
        /// <param name="instance">The instance that this method was invoked on.</param>
        /// <param name="code">The code that describes the error scenario.</param>
        /// <param name="description">String to help debug calls and should not be considered user-facing.</param>
        /// <param name="retryAfter">The delta duration to use for setting the Retry-After header, to indicate to callers when they may query for status.</param>
        /// <param name="contentLanguage">The ISO language-country code that represents the format of the human readable error descriptions.  
        ///                               If not provided, US English will be assumed.</param>
        /// <returns>The corresponding error scenario with details.</returns>
        public static ServiceUnavailableResult ServiceUnavailable(this ApiController instance,
                                                                       TimeSpan      retryAfter)
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            if (retryAfter == null)
            {
                throw new ArgumentNullException(nameof(retryAfter));
            }

            return new ServiceUnavailableResult(instance, retryAfter);
        }
    }
}