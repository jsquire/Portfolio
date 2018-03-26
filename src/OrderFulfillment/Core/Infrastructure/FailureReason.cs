namespace OrderFulfillment.Core.Infrastructure
{
    /// <summary>
    ///   Serves as a pseudo-enumeration for the non-HTTP status code reasons
    ///   for operation failures.
    /// </summary>
    /// 
    public static class FailureReason
    {
        /// <summary>Indicates that an unexpected exception occurred.</summary>
        public const string ExceptionOccured = "An exception occurred.";

        /// <summary>Indicates that an order was not present in the storage for orders pending submission.</summary>
        public const string OrderNotFoundInPendingStorage = "The order was not found in pending storage.";
    }
}
