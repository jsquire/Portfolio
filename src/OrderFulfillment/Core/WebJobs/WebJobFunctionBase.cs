using System;
using System.Threading.Tasks;
using OrderFulfillment.Core.Commands;
using NodaTime;

namespace OrderFulfillment.Core.WebJobs
{
    /// <summary>
    ///   Serves as a base class for WebJob function containing classes, 
    ///   performing common resource management tasks.
    /// </summary>
    /// 
    public class WebJobFunctionBase : IDisposable
    {
        /// <summary>The dependency lifetime scope associated with the current class instance.</summary>
        private IDisposable lifetimeScope;

        /// <summary>
        ///   Initializes a new instance of the <see cref="WebJobFunctionBase"/> class.
        /// </summary>
        /// 
        /// <param name="lifetimeScope">The dependency lifetime scope to associate with the current class lifetime.</param>
        /// 
        public WebJobFunctionBase(IDisposable lifetimeScope)
        {
            this.lifetimeScope = lifetimeScope;
        }

        /// <summary>
        ///   Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// 
        public virtual void Dispose()
        {
            this.lifetimeScope?.Dispose();
        }

        /// <summary>
        ///   Schedules a command for retry, if it is eligible for doing so. 
        /// </summary>
        /// 
        /// <typeparam name="TCommand">The type of the command to be scheduled.</typeparam>
        /// 
        /// <param name="command">The command to be .</param>
        /// <param name="retryThresholds">The retry thresholds to apply when assigning the retry backoff.</param>
        /// <param name="rng">The random number generator to use for computing retry jitter.</param>
        /// <param name="clock">The clock to use for calculating the retry delay.</param>
        /// <param name="commandPublisher">The publisher to use for scheduling the command to be retried.</param>
        /// 
        /// <returns><c>true</c> if the command was scheduled for retry; <c>false</c> if it was not eligible for retry.</returns>
        /// 
        /// <remarks>
        ///     If scheduled to be retried, the incoming <paramref name="command"/> will have it's <see cref="CommandBase.PreviousAttemptsToHandleCount" />
        ///     incremented by this method.  Otherwise, it will be unaltered.
        /// </remarks>
        ///
        protected virtual async Task<bool> ScheduleCommandForRetryIfEligibleAsync<TCommand>(TCommand                    command,
                                                                                            CommandRetryThresholds      retryThresholds,                                                                                            
                                                                                            Random                      rng,
                                                                                            IClock                      clock,                                                                                            
                                                                                            ICommandPublisher<TCommand> commandPublisher) where TCommand : CommandBase
        {
            if (command == null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            if (retryThresholds == null)
            {
                throw new ArgumentNullException(nameof(retryThresholds));
            }

            if (rng == null)
            {
                throw new ArgumentNullException(nameof(rng));
            }

            if (clock == null)
            {
                throw new ArgumentNullException(nameof(clock));
            }

            if (commandPublisher == null)
            {
                throw new ArgumentNullException(nameof(commandPublisher));
            }

            // If the command is out of retries, then take no further action.

            var attempts = command.PreviousAttemptsToHandleCount;

            if (attempts >= retryThresholds.CommandRetryMaxCount)
            {
                return false;
            }

            ++attempts;
            command.PreviousAttemptsToHandleCount = attempts;

            // Publish the retry using an exponential backoff with random jitter.

            var retryInstant = clock.GetCurrentInstant().Plus(Duration.FromSeconds((Math.Pow(2, attempts) * retryThresholds.CommandRetryExponentialSeconds) + (rng.NextDouble() * retryThresholds.CommandRetryJitterSeconds)));
            await commandPublisher.PublishAsync(command, retryInstant);
            
            return true;          
        }
    }
}
