namespace OrderFulfillment.Core.Commands
{
    /// <summary>
    ///   Represents the thresholds to use when retrying a command.
    /// </summary>
    /// 
    public class CommandRetryThresholds
    {
        /// <summary>
        ///   The maximum number of retry counts for handling a given command.  When these 
        ///   retries are exhuasted, the handler will give up and consider it a fatal
        ///   failure.
        /// </summary>
        /// 
        public int CommandRetryMaxCount { get;  set; }

        /// <summary>
        ///   The baseline number of seconds to apply when calculating the
        ///   exponential backoff for performing a retry.
        /// </summary>
        /// 
        public double CommandRetryExponentialSeconds { get;  set; }

        /// <summary>
        ///   The baseline number of seconds to combine with a random multiplier
        ///   when calculating the jitter for a retry.
        /// </summary>
        /// 
        public double CommandRetryJitterSeconds { get;  set; }

        /// <summary>
        ///   Initializes a new instance of the <see cref="CommandRetryThresholds"/> class.
        /// </summary>
        /// 
        public CommandRetryThresholds()
        {
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref="CommandRetryThresholds"/> class.
        /// </summary>
        /// 
        /// <param name="commandRetryMaxCount">The command retry maximum count.</param>
        /// <param name="commandRetryExponentialSeconds">The command retry exponential seconds.</param>
        /// <param name="commandRetryJitterSeconds">The command retry jitter seconds.</param>
        /// 
        public CommandRetryThresholds(int    commandRetryMaxCount,
                                      double commandRetryExponentialSeconds,
                                      double commandRetryJitterSeconds)
        {
            this.CommandRetryMaxCount           = commandRetryMaxCount;
            this.CommandRetryExponentialSeconds = commandRetryExponentialSeconds;
            this.CommandRetryJitterSeconds      = commandRetryJitterSeconds;
        }
    }
}
