using OrderFulfillment.Core.Infrastructure;
using Newtonsoft.Json;

namespace OrderFulfillment.Core.Models.Operations
{
    /// <summary>
    ///   The result of an operation.
    /// </summary>
    /// 
    /// <typeparam name="T">The type of payload stored in the result.</typeparam>
    /// 
    public class OperationResult<T>
    {
        /// <summary>A result that can be used to indicate that an unexepected exception has occurred./summary>
        public static readonly OperationResult<T> ExceptionResult = new OperationResult<T>
        {
           Outcome     = Outcome.Failure,
           Reason      = FailureReason.ExceptionOccured,
           Recoverable = Recoverability.Retriable,
           Payload     = default(T)
           
        };

        /// <summary>
        ///   The outcome of the operation, indicating whether it was successful
        ///   or not.
        /// </summary>
        /// 
        public Outcome Outcome { get;  set; }

        /// <summary>
        ///   A short textual description explaining the reason for the outcome.  This is 
        ///   not intended for display to users, rather for development and logging insight.
        /// </summary>
        /// 
        public string Reason { get;  set; }

        /// <summary>
        ///   An indication as to whether the outcome is final or it is permissible
        ///   to retry failures.
        /// </summary>
        /// 
        public Recoverability Recoverable { get;  set; }

        /// <summary>
        ///   The payload of the operation.  This will often take the form of a 
        ///   serialized version of any structure returned by an operation.
        /// </summary>
        /// 
        public T Payload { get;  set; }

        /// <summary>
        ///   Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// 
        /// <returns>A <see cref="System.String" /> that represents this instance.</returns>
        /// 
        public override string ToString() =>
            JsonConvert.SerializeObject(this, Formatting.Indented);
    }
}
