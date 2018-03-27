using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.Filters;
using OrderFulfillment.Api.Extensions;
using OrderFulfillment.Core.Configuration;
using OrderFulfillment.Core.Extensions;
using OrderFulfillment.Core.Infrastructure;
using Serilog;

namespace OrderFulfillment.Api.Filters
{
    /// <summary>
    ///     Performs the tasks needed to handle API exceptions observed on a
    ///     given request.
    /// </summary>
    /// 
    public class GlobalExceptionFilter : ExceptionFilterAttribute
    {
        /// <summary>
        ///   The configuraiton to be used for error handling within the application.
        /// </summary>
        /// 
        private ErrorHandlingConfiguration Configuration { get; }

        /// <summary>
        ///   The logging provider to be used for recording information.
        /// </summary>
        /// 
        private ILogger Log { get; }

        /// <summary>
        ///   Initializes a new instance of the <see cref="GlobalExceptionFilter"/> class.
        /// </summary>
        /// 
        /// <param name="configuration">The configuration to use for error handling decisions.</param>
        /// <param name="log">The log instance to be used for capturing information around observed exceptions.</param>
        /// 
        public GlobalExceptionFilter(ErrorHandlingConfiguration configuration,
                                     ILogger                    log)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }
            
            if (log == null)
            {
                throw new ArgumentNullException(nameof(log));
            }
                        

            this.Configuration = configuration;
            this.Log           = log;           
        }

        /// <summary>
        ///   Performs the needed actions on an exception observed during an API request.
        /// </summary>
        /// 
        /// <param name="context">The execution context on which the exception was observed.</param>
        /// <param name="cancellationToken">The cancellation token used for short-circuiting processing.</param>
        /// 
        /// <returns>The task to be used for awaiting the asyncrhonous work</returns>
        /// 
        public override Task OnExceptionAsync(HttpActionExecutedContext context, CancellationToken cancellationToken)
        {
            var correlationId = context.Request?.GetOrderFulfillmentCorrelationId();
            var exception     = context.Exception;

            // Log the exception details.

            this.Log
                .WithCorrelationId(correlationId)
                .Error(exception,
                       "Exception thrown when handling the request for {RequestVerb} :: {RequestUri}.",
                       context.Request?.Method?.Method,
                       context.Request?.RequestUri?.ToString());

            // Rewrite the response to ensure that we have a consistent unhandled exception body and do not unintentionally
            // leak implementation details.

            context.Response?.Dispose();
            context.Response = null;

            HttpResponseMessage response;

            if (this.Configuration.ExceptionDetailsEnabled)
            {
                response = context.Request.CreateResponse(HttpStatusCode.InternalServerError, exception);
                response.Headers.Add(HttpHeaders.ExceptionDetails, GlobalExceptionFilter.FormatExceptionHeader(exception));
            }
            else
            {
                response = context.Request.CreateResponse(HttpStatusCode.InternalServerError);
            }
            
            response.ReasonPhrase = "An unexpected exception occurred during the request.";     
            response.Headers.Add(HttpHeaders.CorrelationId, correlationId);
            
            context.Response = response;
                                    
            return base.OnExceptionAsync(context, cancellationToken);
        }

        /// <summary>
        ///   Performs the tasks needed to format an exception for use in the exception header.
        /// </summary>
        /// 
        /// <param name="exception">The exception to format. </param>
        /// 
        /// <returns>A <see cref="System.String" /> that represents the <paramref name="exception" /> which safe for use in the exception header</returns>
        /// 
        private static string FormatExceptionHeader(Exception exception)
        {
            if (exception == null)
            {
                return String.Empty;
            } 

            return exception.ToString().Replace(Environment.NewLine, "\t");
        }
    }
}