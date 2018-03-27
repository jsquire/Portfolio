namespace OrderFulfillment.Notifier.Infrastructure
{
    /// <summary>
    ///   Serves as a pseudo-enumeration for the replacement tokens found in an email 
    ///   body emplate.
    /// </summary>
    public static class EmailBodyTokens
    {
        /// <summary>The location of a dead letter message.  For example, the queue name</summary>
        public const string DeadLetterLocation = "{location}";

        /// <summary>The partner code associated with an order.</summary>
        public const string Partner = "{partner}";

        /// <summary>The identifier associated with an order.</summary>
        public const string Order = "{orderId}";

        /// <summary>The correlation identifier associated with an order operation.</summary>
        public const string Correlation = "{correlationId}";
    }
}
