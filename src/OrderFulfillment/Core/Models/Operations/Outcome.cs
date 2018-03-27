namespace OrderFulfillment.Core.Models.Operations
{
    /// <summary>
    ///   Represents the outcome of an operation, indicating whether it was successful
    ///   or not.
    /// </summary>
    /// 
    public enum Outcome
    {
        /// <summary>The outcome of the operation was not known.</summary>
        Unknown,

        /// <summary>The operation was successful.</summary>
        Success,

        /// <summary>The operation was not successful.</summary>
        Failure
    }
}
