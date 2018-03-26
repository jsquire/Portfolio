namespace OrderFulfillment.Core.Logging
{
    /// <summary>
    ///   Serves as a pseudo-enumeration for the well-known names of properties used in
    ///   a logging context.
    /// </summary>
    /// 
    public static class LogPropertyNames
    {
        /// <summary>The correlation identifier associated with the current request or operation.</summary>
        public static readonly string CorrelationId = "CorrelationId";
    }
}
