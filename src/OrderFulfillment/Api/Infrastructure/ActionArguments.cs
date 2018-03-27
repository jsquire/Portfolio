namespace OrderFulfillment.Api.Infrastructure
{
    /// <summary>
    ///   Serves as a pseudo-enumeration of the tokens used in API routes as replacement slugs
    /// </summary>
    /// 
    internal static class ActionArguments
    {
        /// <summary>The identifier for a specific partner.</summary>
        public const string Partner = "partner";

        /// <summary>The identifier for a specific order.</summary>
        public const string Order = "order";
    }
}