namespace OrderFulfillment.Core.Models.Operations
{
    /// <summary>
    ///   Represents the recoverability of a failed operation, indicating 
    ///   whether it can be safely retried or should be considered fatal.
    /// </summary>
    /// 
    public enum Recoverability
    {
        /// <summary>The retriability of the operation is not known.</summary>
        Unknown,

        /// <summary>The operation my be retried.</summary>
        Retriable,

        /// <summary>The result of the operation is final; no retries should be attempted.</summary>
        Final
    }
}
