namespace OrderFulfillment.Core.Infrastructure
{
    /// <summary>
    ///   Serves as a pseudo-enumeration for the names of queues tied to WebJob 
    ///   function triggers, since they are attribute-based and cannot easily be
    ///   configuration driven.
    /// </summary>
    /// 
    public static class TriggerQueueNames
    {
        /// <summary>The name of the command queue for the <see cref="ProcessOrder" /> command.</summary>
        public const string ProcessOrderCommandQueue = "process-order";

        /// <summary>The name of the command queue for the <see cref="SubmitOrderForProduction" /> command.</summary>
        public const string SubmitOrderForProductionCommandQueue = "submit-order";

        /// <summary>The name of the command queue for the <see cref="NotifyOfFatalFailure" /> command.</summary>
        public const string NotifyOfFatalFailureCommandQueue = "notify-fulfillment-failure";

        /// <summary>The name of the dead letter queue for the <see cref="ProcessOrder" /> command.</summary>
        public const string ProcessOrderDeadLetterQueue = "process-order/$DeadLetterQueue";

        /// <summary>The name of the dead letter queue for the <see cref="SubmitOrderForProduction" /> command.</summary>
        public const string SubmitOrderForProductionDeadLetterQueue = "submit-order/$DeadLetterQueue";

        /// <summary>The name of the dead letter queue for the <see cref="NotifyOfFatalFailure" /> command.</summary>
        public const string NotifyOfFatalFailureDeadLetterQueue = "notify-fulfillment-failure/$DeadLetterQueue";
    }
}
