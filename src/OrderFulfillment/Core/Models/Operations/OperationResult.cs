using System;
using OrderFulfillment.Core.Infrastructure;

namespace OrderFulfillment.Core.Models.Operations
{
    /// <summary>
    ///   The result of an operation.
    /// </summary>
    /// 
    public class OperationResult : OperationResult<string>
    {  
        /// <summary>A result that can be used to indicate that an unexepected exception has occurred./summary>
        public static new readonly OperationResult ExceptionResult = new OperationResult
        {
           Outcome     = Outcome.Failure,
           Reason      = FailureReason.ExceptionOccured,
           Recoverable = Recoverability.Retriable,
           Payload     = String.Empty           
        };
    }
}
