using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using OrderFulfillment.Api.Extensions;
using OrderFulfillment.Core.Extensions;
using OrderFulfillment.Core.Models.Errors;
using OrderFulfillment.Core.Validators;
using Serilog;

namespace OrderFulfillment.Api.Filters
{
    /// <summary>
    ///   Performs the actions needed to validate the message passes as the model to an API
    ///   endpoint, returning the appropriate error if the structure of the message is invalid.
    /// </summary>
    /// 
    /// <remarks>
    ///   This filter will return a failure response immediately upon completion; it will not attempt
    ///   to chain validation through other validators decorating the action.  
    ///   
    ///   This filter executes message validation only; business rule validation is not performed.
    /// </remarks>
    /// 
    /// <seealso cref="System.Web.Http.Filters.ActionFilterAttribute" />
    /// 
    public class ValidateMessageAttribute : ActionFilterAttribute
    {
        /// <summary>The type definition for the unbounded variety of the message validator interface.</summary>
        private static readonly Type BaseValidatorInterfaceType = typeof(IMessageValidator<>);

        /// <summary>
        ///   Called before the action method that the attribute decorates is executed.
        /// </summary>
        /// 
        /// <param name="actionContext">The context in which the action will be invoked.</param>
        /// <param name="cancellationToken">A token which can be used to signal that the operation should be cancelled.</param>
        /// 
        /// <returns>The Task that represents the unit of work.</returns>
        /// 
        public override async Task OnActionExecutingAsync(HttpActionContext actionContext, 
                                                          CancellationToken cancellationToken)
        {
            var locator  = actionContext.Request.GetDependencyScope();            
            var failures = Enumerable.Empty<Error>();

            // If there is an available message validator for the parameter passed to the controller, use it
            // to validate the parameter.

            foreach (var pair in actionContext.ActionArguments)
            {
                var value = pair.Value;

                if (value == null)
                {
                    continue;
                }

                var validatorType = ValidateMessageAttribute.BaseValidatorInterfaceType.MakeGenericType(value.GetType());
                var validator = locator.GetService(validatorType) as IValidator;

                if (validator != null)
                {
                    failures = failures.Concat(validator.Validate(value)
                                                        .Select(error => new Error(error.Code, ValidateMessageAttribute.FormatMemberPath(pair.Key, error.MemberPath), error.Description)));
                }
            }

            // If there were failures, then consider the request invalid and return the error set.

            if (failures.Any())
            {            
                var errorSet = new ErrorSet(failures);
                actionContext.Response = actionContext.Request.CreateResponse(HttpStatusCode.BadRequest, errorSet); 
                
                try
                {
                    var logger = locator.GetService(typeof(ILogger)) as ILogger;

                    if (logger != null)
                    {
                        var request = actionContext.Request;
                        var body    = await request.SafeReadContentAsStringAsync();
  
                        logger.WithCorrelationId(actionContext.Request?.GetOrderFulfillmentCorrelationId())
                              .Information($"Response: {{Response}} { Environment.NewLine } Message validation failed for {{Route}} with Headers: [{{Headers}}] { Environment.NewLine } Body: {{RequestBody}}. { Environment.NewLine } The following errors were observed: { Environment.NewLine }{{ErrorSet}}", 
                                  HttpStatusCode.BadRequest,
                                  request?.RequestUri, 
                                  request?.Headers, 
                                  body, 
                                  errorSet);
                    }
                }
                
                catch
                {
                    // Do nothing; logging is a non-critical operation that should not cause
                    // cascading failures.
                }                         
            }
        }

        /// <summary>
        ///   Called before the action method that the attribute decorates is executed.
        /// </summary>
        /// 
        /// <param name="actionContext">The context in which the action will be invoked.</param>        
        /// 
        public override void OnActionExecuting(HttpActionContext actionContext)
        {
            this.OnActionExecutingAsync(actionContext, CancellationToken.None).GetAwaiter().GetResult();
        }

        /// <summary>
        ///   Formats the member path of an error, based on the found path and the controller parameter 
        ///   that owns themember.
        /// </summary>
        /// 
        /// <param name="ownerObject">The controller parameter determined to be the owner of the error.</param>
        /// <param name="currentPath">The current path used to express the error location.</param>
        /// 
        /// <returns>The string to use as the member path.</returns>
        /// 
        private static string FormatMemberPath(string ownerObject,
                                               string currentPath)
        {
            if (String.IsNullOrEmpty(currentPath))
            {
                return ownerObject;
            }

            return $"{ ownerObject }::{ currentPath }";
        }
    }
}